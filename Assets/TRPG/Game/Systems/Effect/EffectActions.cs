
using Mono.Cecil;
using System.Linq;
using TRPG.Core.Action;
using TRPG.Core.Conditions;
using TRPG.Core.Database;
using TRPG.Core.Event;
using TRPG.Core.Stats;
using TRPG.Game.Const;
using TRPG.Game.Data.GameActions;
using TRPG.Game.TacticalBattle;
using TRPG.Game.Unit;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;
#nullable enable
namespace TRPG.Game.Systems.TRPGEfect
{
    public abstract class EffectActionData : SourcedTargetedActionData
    {
        protected EffectActionData(
            string name,
            int targetUnitId,
            int? sourceUnitId,
            ICondition? condition = null)
            : base(name, targetUnitId, sourceUnitId, condition) { }
    }

    public sealed class ApplyEffectActionData : EffectActionData
    {
        public int EffectId { get; }
        public TragetScope TragetScope { get; }

        public ApplyEffectActionData(
            int targetUnitId,
            int? sourceUnitId,
            int effectId)
            : base("ApplyEffect", targetUnitId, sourceUnitId)
        {
            EffectId = effectId;
        }
    }

    public sealed class RemoveEffectActionData : EffectActionData
    {
        public int EffectId { get; }
        public bool MatchSourceOnly { get; }

        public RemoveEffectActionData(
            int targetUnitId,
            int effectId,
            int? sourceUnitId,
            bool matchSourceOnly,
            ICondition? condition = null)
            : base("RemoveEffect", targetUnitId, sourceUnitId, condition)
        {
            EffectId = effectId;
            MatchSourceOnly = matchSourceOnly;
        }
    }

    public sealed class IncreaseEffectStackActionData : EffectActionData
    {
        public int EffectId { get; }
        public int Amount { get; }

        public IncreaseEffectStackActionData(
            int targetUnitId,
            int? sourceUnitId,
            int effectId,
            int amount,
            ICondition? condition = null)
            : base("IncreaseEffectStack", targetUnitId, sourceUnitId, condition)
        {
            EffectId = effectId;
            Amount = amount;
        }
    }

    public sealed class DecreaseEffectStackActionData : EffectActionData
    {
        public int EffectId { get; }
        public int Amount { get; }
        

        public DecreaseEffectStackActionData(
            int targetUnitId,
            int? sourceUnitId,
            int effectId,
            int amount,
            ICondition? condition = null)
            : base("DecreaseEffectStack", targetUnitId, sourceUnitId, condition)
        {
            EffectId = effectId;
            Amount = amount;
        }
    }


    public sealed class ReplaceEffectActionData : EffectActionData
    {
        public int FromEffectId { get; }
        public int ToEffectId { get; }

        public ReplaceEffectActionData(
            int targetUnitId,
            int fromEffectId,
            int toEffectId,
            int? sourceUnitId = null)
            : base("ReplaceEffect", targetUnitId, sourceUnitId)
        {
            FromEffectId = fromEffectId;
            ToEffectId = toEffectId;
        }
    }

    public abstract class HealthActionData : TargetedActionData
    {
        public int Amount { get; }

        protected HealthActionData(
            string name,
            int targetUnitId,
            int amount,
            ICondition? condition = null)
            : base(name, targetUnitId, condition)
        {
            Amount = amount;
        }
    }

    public sealed class ApplyDamageAction : HealthActionData
    {
        public bool IsCritical { get; }

        public ApplyDamageAction(
            int targetUnitId,
            int amount,
            bool isCritical = false)
            : base("ApplyDamage", targetUnitId, amount)
        {
            IsCritical = isCritical;
        }
    }

    public sealed class ApplyHealingAction : HealthActionData
    {
        public ApplyHealingAction(int targetUnitId, int amount)
            : base("ApplyHealing", targetUnitId, amount) { }
    }

    public sealed class AddStatModifierFromEffectAction : SourcedTargetedActionData
    {
        public StatKey StatKey { get; }
        public ModifierTypeKey ModifierType { get; }
        public float Value { get; }
        public int Order { get; }

        /// <summary>
        /// The effect instance applying this modifier.
        /// Used as modifier source identity.
        /// </summary>

        public AddStatModifierFromEffectAction(
            int targetUnitId,
            int? sourceUnitId,
            StatKey statKey,
            ModifierTypeKey modifierType,
            float value,
            int order,
            ICondition? condition = null)
            : base("AddStatModifierFromEffect", targetUnitId, sourceUnitId, condition)
        {
            StatKey = statKey;
            ModifierType = modifierType;
            Value = value;
            Order = order;
        }
    }

}

namespace TRPG.Game.Systems.TRPGEfect
{
    public sealed class AddStatModifierFromEffectExecutor
    : ActionExecutor<AddStatModifierFromEffectAction>
    {
        protected override ActionResult ExecuteTyped(
            AddStatModifierFromEffectAction data,
            IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;
            var effect = battleContext.CurrentEffect;
            if (effect == null) return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var modifier = new StatModifier
            {
                Type = data.ModifierType,
                Value = data.Value,
                Order = data.Order,
                Source = effect
            };

            var handler = unit.Stats.AddModifier(data.StatKey, modifier);
            var handlerTracker = new EffectStatModifierTracker(unit, handler);

            effect.AddModiferHadler(handlerTracker);

            return ActionResult.Completed;
        }
    }

    public sealed class ApplyEffectActionExecutor : ActionExecutor<ApplyEffectActionData>
    {
        private readonly GameDatabase<EffectData> _effectDatabase;

        public ApplyEffectActionExecutor(GameDatabase<EffectData> repo)
        {
            _effectDatabase = repo;
        }

        protected override ActionResult ExecuteTyped(ApplyEffectActionData data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;
            var source = battleContext.SourceUnit;
            var target = battleContext.TargetUnit;

            var effectData = _effectDatabase.Get(data.EffectId);

            var effectComponent = target.Effects;

            var instance = new EffectInstance(effectData, source, target);

            effectComponent.AddEffect(instance);
            return ActionResult.Completed;
        }
    }

    public sealed class RemoveEffectActionExecutor : ActionExecutor<RemoveEffectActionData>
    {
        protected override ActionResult ExecuteTyped(RemoveEffectActionData data,IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;
            var source = battleContext.SourceUnit;
            var target = battleContext.TargetUnit;

            var effects = target.Effects;

            var matches = effects.Effects
                .Where(e => e.Definition.ID == data.EffectId)
                .ToList();

            foreach (var effect in matches)
            {
                if (data.MatchSourceOnly && effect.Source != source)
                    continue;

                effects.RemoveEffect(effect);
            }

            return ActionResult.Completed;
        }
    }

    public sealed class IncreaseEffectStackActionExecutor
    : ActionExecutor<IncreaseEffectStackActionData>
    {
        protected override ActionResult ExecuteTyped(
            IncreaseEffectStackActionData data,
            IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;
            var source = battleContext.SourceUnit;
            var target = battleContext.TargetUnit;

            target.Effects.IncreaseStack(
                data.EffectId,
                source,
                data.Amount);

            return ActionResult.Completed;
        }
    }

    public sealed class DecreaseEffectStackActionExecutor
    : ActionExecutor<DecreaseEffectStackActionData>
    {
        protected override ActionResult ExecuteTyped(
            DecreaseEffectStackActionData data,
            IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;
            var source = battleContext.SourceUnit;
            var target = battleContext.TargetUnit;

            target.Effects.DecreaseStack(
                data.EffectId,
                source,
                data.Amount);

            return ActionResult.Completed;
        }
    }

    public sealed class ReplaceEffectActionExecutor
    : ActionExecutor<ReplaceEffectActionData>
    {
        private readonly ActionDispatcher _actionDispatcher;

        public ReplaceEffectActionExecutor(ActionDispatcher actionDispatcher)
        {
            _actionDispatcher = actionDispatcher;
        }

        protected override ActionResult ExecuteTyped(
            ReplaceEffectActionData data,
            IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var source = battleContext.SourceUnit;
            var target = battleContext.TargetUnit;


            var existing = battleContext.CurrentEffect;

            if (existing == null)
                return ActionResult.Failed;

            if (existing != null)
                target.Effects.RemoveEffect(existing);

            var applyEffectActionData = new ApplyEffectActionData(target.Id,source.Id,data.ToEffectId);
            var newBattleActionContext = new BattleActionContext(source, target);

            return _actionDispatcher.Dispatch(applyEffectActionData, newBattleActionContext);
        }
    }
}

namespace TRPG.Game.Systems.TRPGEfect
{
    public static class EffectEventKeys
    {
        public static EventKey EffectApplied = new("EffectApplied");
        public static EventKey EffectExpired = new("EffectExpired");
        public static EventKey EffectEffectReapplied = new("EffectReapplied");
        public static EventKey EffectStackChanged = new("EffectStackChanged");
    }

    public abstract class EffectEvent : GameEvent
    {
        public EffectInstance Effect { get; }
        protected EffectEvent(TRPGUnit source, TRPGUnit target, EffectInstance effect) : base(source, target)
        {
            Effect = effect;
        }
    }

    public sealed class EffectAppliedEvent : EffectEvent
    {

        public override EventKey EventKey => EffectEventKeys.EffectApplied;

        public EffectAppliedEvent(TRPGUnit source, TRPGUnit target, EffectInstance effect ): base(source, target,effect)
        {
        }
    }

    public sealed class EffectExpiredEvent : EffectEvent
    {
        public override EventKey EventKey => EffectEventKeys.EffectExpired;
        public EffectExpiredEvent(TRPGUnit source, TRPGUnit target, EffectInstance effect) : base(source, target, effect)
        {
        }
    }

    public sealed class EffectReappliedEvent : EffectEvent
    {
        public EffectData EffectData { get; }
        public override EventKey EventKey => EffectEventKeys.EffectEffectReapplied;
        public EffectReappliedEvent(TRPGUnit source, TRPGUnit target, EffectInstance existingEffect,EffectData effectData) : base(source, target,existingEffect)
        {
            EffectData = effectData;
        } 
    }

    public sealed class EffectStackChangedEvent : EffectEvent
    {
        public int OldStack { get; }
        public int NewStack { get; }

        public override EventKey EventKey => EffectEventKeys.EffectStackChanged;

        public EffectStackChangedEvent(
            TRPGUnit source,
            TRPGUnit target,
            EffectInstance effect,
            int oldStack,
            int newStack)
            : base(source, target, effect)
        {
            OldStack = oldStack;
            NewStack = newStack;
        }
    }
}

namespace TRPG.Game.Systems.TRPGEfect
{
    public enum ComparisonType
    {
        GreaterOrEqual,
        Greater,
        LessOrEqual,
        Less,
        Equal
    }

    public abstract class EventCondition<TEvent> : ICondition
    {
        public ConditionRequirements Requirements =>
            new(typeof(TEvent));

        public bool Evaluate(IConditionContext context)
        {
            if (context.Event is not TEvent ev)
                return false;

            return EvaluateTyped(ev, context);
        }

        protected abstract bool EvaluateTyped(
            TEvent ev,
            IConditionContext context);
    }

    public sealed class StackAmountCondition
        : EventCondition<EffectStackChangedEvent>
    {
        public int Amount { get; }
        public ComparisonType Comparison { get; }

        public StackAmountCondition(int amount, ComparisonType comparison)
        {
            Amount = amount;
            Comparison = comparison;
        }

        protected override bool EvaluateTyped(
            EffectStackChangedEvent ev,
            IConditionContext context)
        {
            int stack = ev.NewStack;

            return Comparison switch
            {
                ComparisonType.GreaterOrEqual => stack >= Amount,
                ComparisonType.Greater => stack > Amount,
                ComparisonType.LessOrEqual => stack <= Amount,
                ComparisonType.Less => stack < Amount,
                ComparisonType.Equal => stack == Amount,
                _ => false
            };
        }
    }

}