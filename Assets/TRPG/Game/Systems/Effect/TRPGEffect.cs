using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Core.Action;
using TRPG.Core.Conditions;
using TRPG.Core.Database;
using TRPG.Core.Event;
using TRPG.Core.PhaseSystem;
using TRPG.Core.Stats;
using TRPG.Game.Const;
using TRPG.Game.Data.GameActions;
using TRPG.Game.TacticalBattle;
using TRPG.Game.Unit;
using static TRPG.Game.Const.GameConst;


namespace TRPG.Game.Systems.TRPGEfect
{
    public enum BattelScope
    {
        Battle,
        Phase,
        Turn,
        Comabt
    }

    public enum TragetScope
    {
        Self,
        Target
    }

    public enum DurationType
    {
        None,
        TurnBased,
        TurnStackedBased,
        CombatOnly
    }

    public enum TriggerExecutionMode
    {
        Always,
        OncePerEvent,
        OncePerTurn
    }


    public sealed class EffectData : IDataEntry
    {
        public int ID { get; }

        public string Name;

        public bool IsStackable;
        public int MaxStacks;

        public DurationType DurationType;
        public int BaseDuration;

        public bool CanRefresh;

        public EffectTriggerDefinition[] Triggers;
    }

    public sealed class EffectDataAuthor : IDataAuthor<EffectData>
    {
        public EffectData Build()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class EffectTriggerDefinition
    {
        public BattelScope Scope;
        public int TurnRequirment;
        public EventKey EventKey; // typeof(DamageAppliedEvent)
        public TragetScope TragetScope;
        public List<ICondition> Conditions;
        public ActionGroup Actions;

        public TriggerExecutionMode ExecutionMode;

        public int Order;
    }
}
namespace TRPG.Game.Systems.TRPGEfect
{
    public class EffectStatModifierTracker
    {
        public TRPGUnit Owner;
        public ModifierHandle ModifierHandler;

        public EffectStatModifierTracker(TRPGUnit owner, ModifierHandle modifier)
        {
            Owner = owner;
            ModifierHandler = modifier;
        }
    }

    public sealed class EffectInstance
    {
        public Guid InstanceId { get; }
        public EffectData Definition { get; }
        public TRPGUnit Source { get; }
        public TRPGUnit Owner { get; }

        public int RemainingDuration { get; private set; }
        public int StackCount { get; private set; }

        private readonly List<IEventListener> _listeners = new();
        public IReadOnlyList<IEventListener> Listeners => _listeners;

        public void AddListener(IEventListener listener) => _listeners.Add(listener);

        private readonly EffectTriggerRuntime[] _triggers;
        public IReadOnlyList<EffectTriggerRuntime> Triggers => _triggers;



        private readonly List<EffectStatModifierTracker> _appliedModifiers = new();
        public IReadOnlyList<EffectStatModifierTracker> AppliedModifiers => _appliedModifiers;


        private bool _markedForRemoval;
        public void MarkForRemoval() => _markedForRemoval = true;

        public EffectInstance(
            EffectData definition,
            TRPGUnit source,
            TRPGUnit owner)
        {
            Definition = definition;
            Source = source;
            Owner = owner;

            RemainingDuration = definition.BaseDuration;
            StackCount = 1;

            _triggers = definition.Triggers.Select(t => new EffectTriggerRuntime(t)).ToArray();
        }

        public void Activate(EventBus bus, ActionPipeline pipeline)
        {
            foreach (var trigger in Triggers)
            {
                var listener = new EffectEventListener(trigger,pipeline,this,Owner);
                _listeners.Add(listener);
                bus.Register(listener);
            }
        }

        public void RemoveAllModifiers()
        {
            foreach (var tracker in _appliedModifiers)
                tracker.Owner.Stats.RemoveModifier(tracker.ModifierHandler);
            _appliedModifiers.Clear();
        }

        public void IncreaseStack(int amount, int maxStacks)
        {
            StackCount = Math.Min(StackCount + amount, maxStacks);
        }

        public void DecreaseStack(int amount)
        {
            StackCount -= amount;
        }

        public void AddModiferHadler(EffectStatModifierTracker modHandlerTracker) => _appliedModifiers.Add(modHandlerTracker);
        public void DecrementDuration() => RemainingDuration--;
        public void SetRemainingDuration(int amout) => RemainingDuration = amout;
    }

    public sealed class EffectTriggerRuntime
    {
        private readonly EffectTriggerDefinition _definition;
        private int _lastExecutionTurn = -1;

        public EffectTriggerRuntime(EffectTriggerDefinition definition)
        {
            _definition = definition;
        }

        public bool CanExecute(int currentTurn)
        {
            if (_definition.ExecutionMode == TriggerExecutionMode.Always)
                return true;

            if (_definition.ExecutionMode == TriggerExecutionMode.OncePerTurn)
                return _lastExecutionTurn != currentTurn;

            return true;
        }

        public void MarkExecuted(int turn)
        {
            _lastExecutionTurn = turn;
        }



       public EventKey EventKey => _definition.EventKey;
       public List<ICondition> Conditions => _definition.Conditions;
       public ActionGroup Actions => _definition.Actions;

        public TriggerScope Scope => _definition.Scope;
    }

    public sealed class UnitEffectComponent
    {
        private readonly List<EffectInstance> _effects = new();
        public IReadOnlyList<EffectInstance> Effects => _effects;

        private readonly TRPGUnit _owner;
        private readonly EventBus _eventBus;
        private readonly ActionDispatcher _actionDispatcher;
        private readonly ActionPipeline _pipeline;

        public UnitEffectComponent(TRPGUnit owner, EventBus eventBus,ActionDispatcher actionDispatcher, ActionPipeline pipeline)
        {
            _owner = owner;
            _eventBus = eventBus;
            _actionDispatcher = actionDispatcher;
            _pipeline = pipeline;
        }

        public void AddEffect(EffectInstance instance)
        {
            if (instance == null) return;

            var existing = _effects.FirstOrDefault(e => e.Definition.ID == instance.Definition.ID);

            if (existing != null)
            {
                _eventBus.Enqueue(
                    new EffectReappliedEvent(
                        instance.Source,
                        instance.Owner,
                        existing,
                        instance.Definition));
                return;
            }

            _effects.Add(instance);

            instance.Activate(_eventBus,_pipeline);

            _eventBus.Enqueue(new EffectAppliedEvent(instance.Source,instance.Owner,instance));
        }

        public void RemoveEffect(EffectInstance instance)
        {
            if (!_effects.Remove(instance)) return;

            instance.RemoveAllModifiers();
            instance.MarkForRemoval();

            // Fire OnExpire triggers
            _eventBus.Enqueue(new EffectExpiredEvent(instance.Source, instance.Owner, instance));

            foreach (var listner in instance.Listeners)
            {
                _eventBus.Unregister(listner);
            }
        }

        public void IncreaseStack(int effectId, TRPGUnit source, int amount)
        {
            var effect = FindEffect(effectId, source);
            if (effect == null) return;

            var old = effect.StackCount;
            effect.IncreaseStack(amount, effect.Definition.MaxStacks);
            var newStack = effect.StackCount;

            _eventBus.Enqueue(new EffectStackChangedEvent(
                effect.Source, 
                effect.Owner,
                effect,
                old,
                newStack
                ));

        }

        public void DecreaseStack(int effectId, TRPGUnit source, int amount)
        {
            var effect = FindEffect(effectId, source);
            if (effect == null) return;

            var old = effect.StackCount;
            effect.DecreaseStack(amount);
            var newStack = effect.StackCount;

            _eventBus.Enqueue(new EffectStackChangedEvent(
                effect.Source,
                effect.Owner,
                effect,
                old,
                newStack
                ));
        }

        public EffectInstance? FindEffect(int effectId, TRPGUnit source)
        {
            return _effects.FirstOrDefault(e =>
                e.Definition.ID == effectId &&
                e.Source == source);
        }

        public IEnumerable<EffectInstance> GetEffects(int effectId)
        {
            return _effects.Where(e => e.Definition.ID == effectId);
        }

        public IEnumerable<EffectInstance> GetEffectsFromSource(TRPGUnit source)
        {
            return _effects.Where(e => e.Source == source);
        }
    }

    internal sealed class EffectEventListener : EventListener<GameEvent>
    {
        private readonly EffectTriggerRuntime _trigger;
        private readonly EffectInstance _instance;
        private readonly TRPGUnit _owner;
        private readonly ActionPipeline _pipeline;

        public EffectEventListener(EffectTriggerRuntime trigger,ActionPipeline actionPipeline, EffectInstance instance, TRPGUnit owner)
        {
            _trigger = trigger;
            _pipeline = actionPipeline;
            _instance = instance;
            _owner = owner;
        }

        public override int Priority => EffectConts.ListenerPriority; // adjust as needed


        protected override void HandleTyped(GameEvent ev)
        {
            if (ev is EffectEvent ec && ec.Effect != _instance) return;

            int currentTurn = ev.BattleManager.GetCurrentTurn();

            if (!ev.EventKey.Equals(_trigger.EventKey))
                return;


            if (!_trigger.CanExecute(currentTurn))
                return;
            IConditionContext conditionContext = new BattleConditionContext(_owner, ev.TargetUnit);
            if (ev.EventKey.Equals(EffectEventKeys.EffectStackChanged))
            {
                var esce = (EffectStackChangedEvent)ev;

                conditionContext = new EffectConditionContext(_instance, esce,currentTurn);
            }
            // Evaluate conditions
            if (_trigger.Conditions != null)
            {
                foreach (var condition in _trigger.Conditions)
                {
                    if (!condition.Evaluate(conditionContext))
                        return;
                }
            }

            if (_trigger.Actions != null)
            {
                var context = new BattleActionContext(_owner,ev.TargetUnit,_instance);

                _pipeline.Execute(
                    _trigger.Actions,
                    context,
                    ExecutionPolicy.ContinueOnFailure);
            }

            _trigger.MarkExecuted(currentTurn);
        }
    }

    
}