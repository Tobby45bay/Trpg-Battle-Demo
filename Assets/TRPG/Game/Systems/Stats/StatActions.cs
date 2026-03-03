using System;
using System.Collections;
using System.Collections.Generic;
using TRPG.Core.Action;
using TRPG.Core.Conditions;
using TRPG.Core.Stats;
using TRPG.Game.Data.GameActions;
using TRPG.Game.TacticalBattle;
#nullable enable
namespace TRPG.Game.Systems.Stats.Actions
{
    public abstract class StatActionData : TargetedActionData
    {
        public StatKey StatKey { get; }

        protected StatActionData( string name,int targetUnitId,
            StatKey statKey,
            ICondition? condition = null)
            : base(name, targetUnitId, condition)
        {
            StatKey = statKey;
        }
    }

    public sealed class AddModifierAction : StatActionData
    {
        public StatModifier Modifier { get; }

        public AddModifierAction(
            int targetUnitId,
            StatKey key,
            StatModifier modifier)
            : base("AddModifier", targetUnitId, key)
        {
            Modifier = modifier;
        }
    }

    public sealed class RemoveModifierAction : StatActionData
    {
        public ModifierHandle Handle { get; }

        public RemoveModifierAction(
            int targetUnitId,
            StatKey key,
            ModifierHandle handle)
            : base("RemoveModifier", targetUnitId,key)
        {
            Handle = handle;
        }
    }
}
namespace TRPG.Game.Systems.Stats.Actions
{
    public sealed class StatActionExecutor : ActionExecutor<StatActionData>
    {
        protected override ActionResult ExecuteTyped(StatActionData data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            if (data is AddModifierAction add)
            {
                unit.Stats.AddModifier(add.StatKey, add.Modifier);
            }
            else if (data is RemoveModifierAction remove)
            {
                unit.Stats.RemoveModifier(remove.Handle);
            }
            else
            {
                return ActionResult.Failed;
            }

            return ActionResult.Completed;
        }
    }
}
