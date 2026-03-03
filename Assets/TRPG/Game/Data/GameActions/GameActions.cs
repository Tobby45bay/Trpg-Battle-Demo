using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Core.Action;
using TRPG.Core.Conditions;
using TRPG.Core.Event;
using TRPG.Game.Systems.TRPGEfect;
using TRPG.Game.TacticalBattle;
using TRPG.Game.Unit;
#nullable enable
namespace TRPG.Game.Data.GameActions
{
#nullable enable

    public sealed class BattleActionContext : IActionContext
    {
        public readonly TRPGUnit SourceUnit;
        public readonly TRPGUnit TargetUnit;
        public EffectInstance? CurrentEffect { get; }

        public BattleActionContext(TRPGUnit sourceUnit, TRPGUnit targetUnit, EffectInstance? currentEffect = null)
        {
            SourceUnit = sourceUnit;
            TargetUnit = targetUnit;
            CurrentEffect = currentEffect;
        }


    }

    public abstract class GameActionData : ActionData
    {
        protected GameActionData(string name, ICondition? condition = null)
            : base(name, condition) { }
    }

    public abstract class TargetedActionData : GameActionData
    {
        public int TargetUnitId { get; }

        protected TargetedActionData(
            string name,
            int targetUnitId,
            ICondition? condition = null)
            : base(name, condition)
        {
            TargetUnitId = targetUnitId;
        }
    }

    public abstract class SourcedTargetedActionData : TargetedActionData
    {
        public int? SourceUnitId { get; }

        protected SourcedTargetedActionData(
            string name,
            int targetUnitId,
            int? sourceUnitId,
            ICondition? condition = null)
            : base(name, targetUnitId, condition)
        {
            SourceUnitId = sourceUnitId;
        }
    }

    public sealed class ExecuteActionGroupAction : GameActionData
    {
        public ActionGroup Group { get; }
        public ExecutionPolicy Policy { get; }

        public ExecuteActionGroupAction(ActionGroup group,
            ExecutionPolicy policy = ExecutionPolicy.ContinueOnFailure)
            : base("ExecuteActionGroup")
        {
            Group = group;
            Policy = policy;
        }
    }
}