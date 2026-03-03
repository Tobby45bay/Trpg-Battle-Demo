
using System;
using TRPG.Core.Conditions;
using TRPG.Game.Data.GameActions;
using TRPG.Game.Systems.Items;
using TRPG.Game.Systems.Magic;

namespace TRPG.Game.Systems.Rank.Actions
{
    /// <summary>
    /// Base class for all rank-related actions.
    /// </summary>
    public abstract class RankActionData : TargetedActionData
    {
        protected RankActionData(string name, int targetUnitId, ICondition? condition = null)
            : base(name, targetUnitId, condition) { }
    }

    // =========================================================
    // ADD WEAPON RANK EXP ACTION
    // =========================================================

    /// <summary>
    /// Applies weapon rank experience to a unit.
    /// 
    /// Example: After hitting with Sword, enqueue this action.
    /// Can be conditional (only if critical hit, etc.)
    /// </summary>
    public sealed class AddWeaponRankExpAction : RankActionData
    {
        public WeaponType WeaponType { get; }
        public int Amount { get; }

        public AddWeaponRankExpAction(
            int targetUnitId,
            WeaponType weaponType,
            int amount,
            ICondition? condition = null)
            : base("AddWeaponRankExp", targetUnitId, condition)
        {
            WeaponType = weaponType;
            Amount = Math.Max(0, amount);
        }

        public override string ToString() => $"AddWeaponRankExp[{WeaponType}: +{Amount}] -> TRPGUnit {TargetUnitId}";
    }

    // <summary>
    /// Applies magic rank experience to a unit.
    /// 
    /// Automatically applies affinity bonuses if ApplyAffinity is true.
    /// Example: After casting Fire spell, enqueue this action.
    /// </summary>
    public sealed class AddMagicRankExpAction : RankActionData
    {
        public ElementType ElementType { get; }
        public int Amount { get; }
        public bool ApplyAffinity { get; }

        public AddMagicRankExpAction(
            int targetUnitId,
            ElementType elementType,
            int amount,
            bool applyAffinity = true,
            ICondition? condition = null)
            : base("AddMagicRankExp", targetUnitId, condition)
        {
            ElementType = elementType;
            Amount = Math.Max(0, amount);
            ApplyAffinity = applyAffinity;
        }

        public override string ToString() => $"AddMagicRankExp[{ElementType}: +{Amount}] -> TRPGUnit {TargetUnitId}";
    }

    /// <summary>
    /// Applies rank bonuses as stat modifiers.
    /// 
    /// Supports both weapons and elements.
    /// Source is used for tracking and removal.
    /// Duration can be used for temporary bonuses (0 = permanent).
    /// 
    /// Example: After equipping a Sword, apply all current Sword rank bonuses.
    /// Example: After ranking up, apply new tier bonuses.
    /// </summary>
    public sealed class ApplyRankBonusAction : RankActionData
    {
        public WeaponType? WeaponType { get; }
        public ElementType? ElementType { get; }
        public object Source { get; }
        public int Duration { get; }

        public ApplyRankBonusAction(
            int targetUnitId,
            WeaponType weaponType,
            object source,
            int duration = 0,
            ICondition? condition = null)
            : base("ApplyRankBonus", targetUnitId, condition)
        {
            WeaponType = weaponType;
            ElementType = null;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Duration = Math.Max(0, duration);
        }

        public ApplyRankBonusAction(
            int targetUnitId,
            ElementType elementType,
            object source,
            int duration = 0,
            ICondition? condition = null)
            : base("ApplyRankBonus", targetUnitId, condition)
        {
            WeaponType = null;
            ElementType = elementType;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Duration = Math.Max(0, duration);
        }

        public override string ToString()
        {
            var typeStr = WeaponType?.ToString() ?? ElementType?.ToString() ?? "Unknown";
            return $"ApplyRankBonus[{typeStr}] -> TRPGUnit {TargetUnitId}";
        }
    }

    /// <summary>
    /// Removes rank bonuses applied by a specific source.
    /// 
    /// Used for unequipping weapons, expiring effects, etc.
    /// 
    /// Example: TRPGUnit unequipped Sword → RemoveRankBonusAction(unit, swordSource)
    /// </summary>
    public sealed class RemoveRankBonusAction : RankActionData
    {
        public object Source { get; }

        public RemoveRankBonusAction(
            int targetUnitId,
            object source,
            ICondition? condition = null)
            : base("RemoveRankBonus", targetUnitId, condition)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public override string ToString() => $"RemoveRankBonus[Source: {Source}] -> TRPGUnit {TargetUnitId}";
    }

    /// <summary>
    /// Sets rank progress directly (rank + EXP).
    /// 
    /// Used for:
    /// - Save loading
    /// - Debug/cheat commands
    /// - Quest rewards
    /// - Level-up unlocks
    /// 
    /// Example: "Unlock Sword Rank B on reaching Level 10"
    /// SetRankExpAction(unit, WeaponType.Sword, RankTier.B, 0)
    /// </summary>
    public sealed class SetRankExpAction : RankActionData
    {
        public WeaponType? WeaponType { get; }
        public ElementType? ElementType { get; }
        public RankTier NewRank { get; }
        public int NewExp { get; }

        public SetRankExpAction(
            int targetUnitId,
            WeaponType weaponType,
            RankTier newRank,
            int newExp = 0,
            ICondition? condition = null)
            : base("SetRankExp", targetUnitId, condition)
        {
            WeaponType = weaponType;
            ElementType = null;
            NewRank = newRank;
            NewExp = Math.Max(0, newExp);
        }

        public SetRankExpAction(
            int targetUnitId,
            ElementType elementType,
            RankTier newRank,
            int newExp = 0,
            ICondition? condition = null)
            : base("SetRankExp", targetUnitId, condition)
        {
            WeaponType = null;
            ElementType = elementType;
            NewRank = newRank;
            NewExp = Math.Max(0, newExp);
        }

        public override string ToString()
        {
            var typeStr = WeaponType?.ToString() ?? ElementType?.ToString() ?? "Unknown";
            return $"SetRankExp[{typeStr} -> {NewRank}] -> TRPGUnit {TargetUnitId}";
        }
    }
}
