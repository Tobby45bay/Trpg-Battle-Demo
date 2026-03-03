using System;
using System.Collections.Generic;
using TRPG.Core.Conditions;
using TRPG.Core.Logging;
#nullable enable
/*
    TRPG.Core.Action

    Engine-level action execution framework.

    This module provides:
    - Pure action data definitions
    - Executor routing via ActionDispatcher
    - Deterministic execution via ActionPipeline
    - Flow control via ExecutionPolicy

    Design Principles:
    - Core is game-agnostic
    - No dependency on Unity or gameplay systems
    - No domain logic
    - Context is provided externally

    Extension Rules:
    - Add new actions by subclassing ActionData
    - Implement corresponding ActionExecutor<T>
    - Register executors during system setup
    - Do NOT add gameplay logic directly to this module
*/
namespace TRPG.Core.Action
// === CORE CONSTRAINTS ===
// - No Unity references
// - No domain logic
// - No static state
// - No gameplay assumptions
{
    /// <summary>
    /// Marker interface representing execution context.
    /// 
    /// Implemented by higher-level systems (battle, AI, simulation).
    /// 
    /// Core does not assume any specific structure.
    /// Context casting is responsibility of executors.
    /// </summary>
    public interface IActionContext { }

    /// <summary>
    /// Base type for action definitions.
    /// 
    /// Represents pure configuration data.
    /// Contains no behavior.
    /// 
    /// Execution logic is defined in IActionExecutor implementations.
    /// </summary>
    /// 

    public abstract class ActionData
    {
        /// <summary>
        /// Optional debug/display label.
        /// Not used internally by the core.
        /// </summary>
        public string ActionName { get; }
        public ICondition? Condition { get; }

        protected ActionData(string actionName, ICondition? condition = null)
        {
            ActionName = actionName;
            Condition = condition;
        }
    }

    /// <summary>
    /// Result of executing a single action.
    /// 
    /// Used by ActionPipeline to determine flow control.
    /// 
    /// Designed to be extendable in the future
    /// (e.g., error codes, rollback tokens, metadata).
    /// </summary>
    public readonly struct ActionResult
    {
        /// <summary>
        /// Indicates whether execution succeeded.
        /// </summary>
        public readonly bool Success;

        public ActionResult(bool success)
        {
            Success = success;
        }

        public static ActionResult Completed => new(true);
        public static ActionResult Failed => new(false);
    }

    public interface IActionExecutor
    {
        Type ActionType { get; }

        ActionResult Execute(ActionData data,IActionContext context);
    }

    public abstract class ActionExecutor<T>: IActionExecutor where T : ActionData
    {
        public Type ActionType => typeof(T);

        public ActionResult Execute(ActionData data,IActionContext context)
        {
            return ExecuteTyped((T)data, context);
        }

        protected abstract ActionResult ExecuteTyped(T data,IActionContext context);
    }

    /// <summary>
    /// Central registry that maps ActionData types
    /// to their corresponding IActionExecutor.
    /// 
    /// Executors must be registered before dispatch.
    /// 
    /// Does not perform validation beyond executor lookup.
    /// </summary>
    public sealed class ActionDispatcher
    {
        private readonly Dictionary<Type, IActionExecutor> _executors = new();
        private readonly Dictionary<Type, List<IActionValidator>> _validators = new();
        private readonly ICoreLogger? _logger;

        public ActionDispatcher(ICoreLogger? logger = null)
        {
            _logger = logger;
        }

        public void RegisterExecutor(IActionExecutor executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            if (_executors.ContainsKey(executor.ActionType))
                throw new InvalidOperationException(
                    $"Executor already registered for {executor.ActionType}"
                );

            _executors.Add(executor.ActionType, executor);

            _logger?.Log(
                LogLevel.Trace,
                "ActionDispatcher",
                $"Registered executor {executor.GetType().Name} for {executor.ActionType.Name}"
            );
        }

        public void RegisterValidator(IActionValidator validator)
        {
            if (!_validators.TryGetValue(validator.ActionType, out var list))
            {
                list = new List<IActionValidator>();
                _validators[validator.ActionType] = list;
            }

            list.Add(validator);
        }


        public bool HasExecutor(Type type) => _executors.ContainsKey(type);

        /// <summary>
        /// Executes a single ActionData instance using the
        /// registered executor.
        /// 
        /// Returns ActionResult.Failed if:
        /// - data is null
        /// - no executor is registered for the action type
        /// </summary>
        public ActionResult Dispatch(ActionData data, IActionContext context)
        {
            if (data == null)
            {
                _logger?.Log(
                    LogLevel.Warning,
                    "ActionDispatcher",
                    "Attempted to dispatch null ActionData"
                );
                return ActionResult.Failed;
            }

            var actionType = data.GetType();
            var executor = FindExecutor(actionType);

            if (executor == null)
            {
                _logger?.Log(
                    LogLevel.Warning,
                    "ActionDispatcher",
                    $"No executor registered for {actionType.Name}"
                );
                return ActionResult.Failed;
            }

            _logger?.Log(
                LogLevel.Trace,
                "ActionDispatcher",
                $"Dispatching {actionType.Name} ({data.ActionName}) using {executor.GetType().Name}"
            );

            var validators = FindValidators(actionType);

            if (validators != null)
            {
                foreach (var validator in validators)
                {
                    if (!validator.Validate(data, context))
                    {
                        _logger?.Log(
                            LogLevel.Info,
                            "ActionDispatcher",
                            $"Validation failed for {actionType.Name} in {validator.GetType().Name}"
                        );

                        return ActionResult.Failed;
                    }
                }
            }

            try
            {
                var result = executor.Execute(data, context);

                _logger?.Log(
                    LogLevel.Trace,
                    "ActionDispatcher",
                    $"Result for {actionType.Name}: {(result.Success ? "Success" : "Failed")}"
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger?.Log(
                    LogLevel.Error,
                    "ActionDispatcher",
                    $"Executor {executor.GetType().Name} threw exception: {ex.Message}"
                );

                throw; // preserve deterministic crash
            }
        }


        private IActionExecutor? FindExecutor(Type actionType)
        {
            //Exact match first (fast path)
            if (_executors.TryGetValue(actionType, out var exact))
                return exact;

            // Walk up inheritance chain
            var current = actionType.BaseType;

            while (current != null && typeof(ActionData).IsAssignableFrom(current))
            {
                if (_executors.TryGetValue(current, out var executor))
                    return executor;

                current = current.BaseType;
            }

            return null;
        }

        private List<IActionValidator>? FindValidators(Type actionType)
        {
            // Exact match first
            if (_validators.TryGetValue(actionType, out var exact))
                return exact;

            var current = actionType.BaseType;

            while (current != null && typeof(ActionData).IsAssignableFrom(current))
            {
                if (_validators.TryGetValue(current, out var list))
                    return list;

                current = current.BaseType;
            }

            return null;
        }


    }

    /// <summary>
    /// Immutable container for a sequence of ActionData.
    /// 
    /// Order of actions is preserved and significant.
    /// 
    /// Used as the unit of execution in ActionPipeline.
    /// </summary>
    public sealed class ActionGroup
    {
        private readonly List<ActionData> _actions = new();

        public IReadOnlyList<ActionData> Actions => _actions;

        public void Add(ActionData action)
        {
            if (action != null)
                _actions.Add(action);
        }
    }

    /// <summary>
    /// Determines how ActionPipeline behaves
    /// when an action returns a failed result.
    /// </summary>
    public enum ExecutionPolicy
    {
        ContinueOnFailure,
        StopOnFailure
    }

    /// <summary>
    /// Responsible for executing ActionGroup instances
    /// according to the selected ExecutionPolicy.
    /// 
    /// Controls flow but does not contain action logic.
    /// 
    /// This is the only place where multi-action execution
    /// behavior is defined.
    /// </summary>
    public sealed class ActionPipeline
    {
        private readonly ActionDispatcher _dispatcher;
        private readonly ICoreLogger? _logger;

        public ActionPipeline(ActionDispatcher dispatcher, ICoreLogger? logger = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger;
        }

        /// <summary>
        /// Executes all actions in the group.
        /// 
        /// If policy is StopOnFailure, execution halts when
        /// an action returns a failed result.
        /// </summary>
        public ActionResult Execute(ActionGroup group,IActionContext context,ExecutionPolicy policy)
        {
            if (group == null)
            {
                _logger?.Log(
                    LogLevel.Warning,
                    "ActionPipeline",
                    "Attempted to execute null ActionGroup"
                );
                return ActionResult.Failed;
            }

            _logger?.Log(
                LogLevel.Trace,
                "ActionPipeline",
                $"Executing ActionGroup with {group.Actions.Count} actions"
            );

            var actions = group.Actions;
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];

                if (action.Condition != null)
                {
                    if (context is not IConditionContext conditionContext ||
                        !action.Condition.Evaluate(conditionContext))
                    {
                        continue;
                    }
                }
                
                var result = _dispatcher.Dispatch(action, context);

                if (!result.Success &&
                    policy == ExecutionPolicy.StopOnFailure)
                {
                    _logger?.Log(
                        LogLevel.Info,
                        "ActionPipeline",
                        $"Stopped execution due to failure in {action.GetType().Name}"
                    );

                    return result;
                }
            }

            return ActionResult.Completed;
        }
    }
    public interface IActionValidator
    {
        Type ActionType { get; }

        bool Validate(ActionData data, IActionContext context);
    }

    public abstract class ActionValidator<T> : IActionValidator where T : ActionData
    {
        public Type ActionType => typeof(T);

        public bool Validate(ActionData data, IActionContext context)
        {
            return ValidateTyped((T)data, context);
        }

        protected abstract bool ValidateTyped(T data, IActionContext context);
    }

}