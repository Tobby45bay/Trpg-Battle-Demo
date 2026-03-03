using System;
using System.Collections.Generic;
using TRPG.Core.Action;

namespace TRPG.Core.Conditions
{
    public interface IConditionContext
    {
        object Event { get; }
    }

    public sealed class ConditionRequirements
    {
        public Type? RequiredEventType { get; }
        public bool IsImpossible { get; }

        public ConditionRequirements(
            Type? requiredEventType = null,
            bool isImpossible = false)
        {
            RequiredEventType = requiredEventType;
            IsImpossible = isImpossible;
        }

        public static ConditionRequirements None => new();

        public static ConditionRequirements Impossible =>
            new(null, true);

        public static ConditionRequirements Merge(
            ConditionRequirements a,
            ConditionRequirements b)
        {
            if (a.IsImpossible || b.IsImpossible)
                return Impossible;

            if (a.RequiredEventType == null)
                return b;

            if (b.RequiredEventType == null)
                return a;

            if (a.RequiredEventType == b.RequiredEventType)
                return a;

            return Impossible;
        }
    }

    public interface ICondition
    {
        bool Evaluate(IConditionContext context);

        ConditionRequirements Requirements { get; }
    }

    public sealed class AndCondition : ICondition
    {
        public IReadOnlyList<ICondition> Conditions { get; }

        public ConditionRequirements Requirements { get; }

        public AndCondition(params ICondition[] conditions)
        {
            Conditions = conditions;

            var req = ConditionRequirements.None;

            foreach (var c in conditions)
                req = ConditionRequirements.Merge(req, c.Requirements);

            Requirements = req;
        }

        public bool Evaluate(IConditionContext context)
        {
            foreach (var c in Conditions)
                if (!c.Evaluate(context))
                    return false;

            return true;
        }
    }

    public sealed class OrCondition : ICondition
    {
        private readonly ICondition[] _conditions;

        public ConditionRequirements Requirements { get; }

        public OrCondition(params ICondition[] conditions)
        {
            _conditions = conditions;

            Type? required = conditions[0].Requirements.RequiredEventType;

            foreach (var c in conditions)
            {
                if (c.Requirements.RequiredEventType != required)
                {
                    required = null;
                    break;
                }
            }

            Requirements = new ConditionRequirements(required);
        }

        public bool Evaluate(IConditionContext context)
        {
            foreach (var condition in _conditions)
                if (condition.Evaluate(context))
                    return true;

            return false;
        }
    }

    public sealed class NotCondition : ICondition
    {
        private readonly ICondition _condition;

        public ConditionRequirements Requirements => _condition.Requirements;

        public NotCondition(ICondition condition)
        {
            _condition = condition;
        }

        public bool Evaluate(IConditionContext context)
            => !_condition.Evaluate(context);
    }
}
