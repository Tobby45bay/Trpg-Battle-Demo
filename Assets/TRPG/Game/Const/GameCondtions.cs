
using TRPG.Core.Conditions;
using TRPG.Game.TacticalBattle;
using TRPG.Game.Unit;

namespace TRPG.Game.Const
{
    public enum ConditionType
    {
        DistanceEquals,
        DistanceGreaterThan,
        IsRangedAttack,
        IsReaction,
        IsFrontAttack,
        IsBackAttack,
        OwnerIsAttacker,
        OwnerIsDefender,
        HasTag
    }
    public  abstract class GameConditionContext : IConditionContext
    {
        public BattleManager BattleManager { get; }
        public TRPGUnit Source { get; }
        public TRPGUnit? Target { get; }

        public GameConditionContext(BattleManager battleManager, TRPGUnit source, TRPGUnit target)
        {
            BattleManager = battleManager;
            Source = source;
            Target = target;
        }
    }

    public sealed class BattleConditionContext: GameConditionContext
    {
        public BattleConditionContext(BattleManager battleManager,TRPGUnit source,TRPGUnit? target ):base(battleManager,source,target) { }
    }

    public sealed class HpBelowCondition : ICondition
    {
        private readonly float _percent;

        public HpBelowCondition(float percent)
        {
            _percent = percent;
        }

        public bool Evaluate(IConditionContext context)
        {
            var ctx = (BattleConditionContext)context;

            var current = ctx.Source.Health.CurrentHP;
            var max = ctx.Source.Health.MaxHP;

            return (current / (float)max) <= _percent;
        }
    }

    public sealed class HpAboveCondition : ICondition
    {
        private readonly float _percent;

        public HpAboveCondition(float percent)
        {
            _percent = percent;
        }

        public bool Evaluate(IConditionContext context)
        {
            var ctx = (BattleConditionContext)context;

            var current = ctx.Source.Health.CurrentHP;
            var max = ctx.Source.Health.MaxHP;

            return (current / (float)max)>= _percent;
        }
    }
}
