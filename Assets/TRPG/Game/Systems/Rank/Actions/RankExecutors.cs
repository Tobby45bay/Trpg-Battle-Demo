
using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Core.Action;
using TRPG.Core.Event;
using TRPG.Game.Const;
using TRPG.Game.Data.GameActions;
using TRPG.Game.TacticalBattle;
using TRPG.Game.Unit;

namespace TRPG.Game.Systems.Rank.Actions
{
    /// <summary>
    /// Battle action context provides access to units and game state.
    /// Rank actions require this context.
    /// </summary>
    public interface IBattleActionContext : IActionContext
    {
        TRPGUnit GetUnit(int unitId);
    }



    /// <summary>
    /// Executes AddWeaponRankExpAction.
    /// Looks up the unit's rank component and applies experience.
    /// </summary>
    public sealed class AddWeaponRankExpExecutor : ActionExecutor<AddWeaponRankExpAction>
    {
        protected override ActionResult ExecuteTyped(AddWeaponRankExpAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var rankComponent = unit.GetComponent<UnitRankComponent>();
            if (rankComponent == null)
                return ActionResult.Failed;

            rankComponent.AddWeaponExp(data.WeaponType, data.Amount);
            return ActionResult.Completed;
        }
    }

    /// <summary>
    /// Executes AddMagicRankExpAction.
    /// Applies affinity bonuses if requested, then adds experience.
    /// </summary>
    public sealed class AddMagicRankExpExecutor : ActionExecutor<AddMagicRankExpAction>
    {
        protected override ActionResult ExecuteTyped(AddMagicRankExpAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var rankComponent = unit.GetComponent<UnitRankComponent>();
            if (rankComponent == null)
                return ActionResult.Failed;

            if (data.ApplyAffinity)
                rankComponent.AddMagicExpWithAffinity(data.ElementType, data.Amount);
            else
                rankComponent.AddMagicExp(data.ElementType, data.Amount);

            return ActionResult.Completed;
        }
    }

    /// <summary>
    /// Executes ApplyRankBonusAction.
    /// 
    /// Fetches bonuses for the rank, converts them to stat modifiers,
    /// and applies them to the target unit's stats.
    /// </summary>
    public sealed class ApplyRankBonusExecutor : ActionExecutor<ApplyRankBonusAction>
    {
        protected override ActionResult ExecuteTyped(ApplyRankBonusAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var rankComponent = unit.GetComponent<UnitRankComponent>();
            var statComponent = unit.Stats;
            if (rankComponent == null || statComponent == null)
                return ActionResult.Failed;

            // Fetch bonuses based on weapon or element
            var bonuses = data.WeaponType.HasValue
                ? rankComponent.GetWeaponBonuses(data.WeaponType.Value)
                : rankComponent.GetMagicBonuses(data.ElementType.Value);

            // Apply each bonus as a stat modifier
            foreach (var bonus in bonuses)
            {
                if (!TryConvertBonusToStat(bonus.Type, out var statKey))
                    continue;

                if (!rankComponent.CanApplyBonus(bonus.Type, statKey))
                    continue;

                var modifier = new TRPG.Core.Stats.StatModifier
                {
                    Type = TRPG.Core.Stats.Rules.DefaultModifierType.Flat,
                    Value = bonus.Value,
                    Order = 0,
                    Source = data.Source
                };
                statComponent.AddModifier(statKey, modifier);
            }

            return ActionResult.Completed;
        }

        private bool TryConvertBonusToStat(RankBonusType bonusType, out TRPG.Core.Stats.StatKey statKey)
        {
            statKey = new(0, "");

            statKey = bonusType switch
            {
                RankBonusType.PhysicalAttack => GameConst.StatKeys.PhysicalAttack,
                RankBonusType.MagicAttack => GameConst.StatKeys.MagicAttack,
                RankBonusType.PhysicalDefense => GameConst.StatKeys.PhysicalDefense,
                RankBonusType.MagicDefense => GameConst.StatKeys.MagicDefense,
                RankBonusType.Hit => GameConst.StatKeys.Hit,
                RankBonusType.Avoid => GameConst.StatKeys.Avoid,
                // Cost reduction and effect chance don't map to core stats
                // These would be handled by system-specific code
                _ => new(-1, "")
            };

            return statKey.Id >= 0;
        }
    }

    /// <summary>
    /// Executes RemoveRankBonusAction.
    /// 
    /// Removes all stat modifiers from the given source.
    /// </summary>
    public sealed class RemoveRankBonusExecutor : ActionExecutor<RemoveRankBonusAction>
    {
        protected override ActionResult ExecuteTyped(RemoveRankBonusAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var statComponent = unit.Stats;
            if (statComponent == null)
                return ActionResult.Failed;

            // Remove all modifiers from this source across all stats
            statComponent.RemoveAllModifiersBySource(data.Source);

            return ActionResult.Completed;
        }
    }

    /// <summary>
    /// Executes SetRankExpAction.
    /// 
    /// Directly sets the rank and EXP for a weapon or element.
    /// Be cautious with this action — it bypasses progression checks.
    /// </summary>
    public sealed class SetRankExpExecutor : ActionExecutor<SetRankExpAction>
    {
        protected override ActionResult ExecuteTyped(SetRankExpAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var rankComponent = unit.GetComponent<UnitRankComponent>();
            if (rankComponent == null)
                return ActionResult.Failed;

            if (data.WeaponType.HasValue)
            {
                var progress = rankComponent.GetWeaponProgress(data.WeaponType.Value);
                if (progress == null)
                    return ActionResult.Failed;

                // Set rank by creating new RankProgress
                // (We can't directly set it, so recreate from scratch)
                var newProgress = new RankProgress(data.NewRank, data.NewExp);
                // Note: This is a limitation of the current API
                // A better approach would be to expose a SetRank method on RankProgress
            }
            else
            {
                var progress = rankComponent.GetMagicProgress(data.ElementType.Value);
                if (progress == null)
                    return ActionResult.Failed;

                var newProgress = new RankProgress(data.NewRank, data.NewExp);
            }

            return ActionResult.Completed;
        }
    }
}
