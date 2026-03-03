using System;
using System.Collections;
using System.Collections.Generic;
using TRPG.Core.Stats;
using TRPG.Game.Const;
using UnityEngine;
#nullable enable
namespace TRPG.Game.Systems.Stats
{
    public static class StatCollectionFactory
    {
        /// <summary>
        /// Creates a StatCollection for a unit with auto-bound derived stats.
        /// Base stats are initialized to 0 unless provided in the optional parameters.
        /// </summary>
        public static StatCollection CreateUnitStats(
            ModifierRuleRegistry registry,
            float vitality = 0,
            float might = 0,
            float finesse = 0,
            float guard = 0,
            float focus = 0,
            float spirit = 0,
            Func<StatCollection, float>? weaponPower = null,
            Func<StatCollection, float>? spellPower = null
        )
        {
            var stats = new StatCollection(registry);

            // --- Base stats ---
            stats.Add(GameConst.StatKeys.Vitality, vitality);
            stats.Add(GameConst.StatKeys.Might, might);
            stats.Add(GameConst.StatKeys.Finesse, finesse);
            stats.Add(GameConst.StatKeys.Guard, guard);
            stats.Add(GameConst.StatKeys.Focus, focus);
            stats.Add(GameConst.StatKeys.Spirit, spirit);

            // --- Derived stats ---
            var derivedHP = new DerivedStat(
                GameConst.StatKeys.HP,
                sc => 18 + sc.Get(GameConst.StatKeys.Vitality).Value * 4,
                stats,
                registry
            );

            var derivedPhysicalAttack = new DerivedStat(
                GameConst.StatKeys.PhysicalAttack,
                sc => sc.Get(GameConst.StatKeys.Might).Value +
                      (weaponPower?.Invoke(sc) ?? 0),
                stats,
                registry
            );

            var derivedMagicAttack = new DerivedStat(
                GameConst.StatKeys.MagicAttack,
                sc => sc.Get(GameConst.StatKeys.Focus).Value +
                      (spellPower?.Invoke(sc) ?? 0),
                stats,
                registry
            );

            var derivedPhysicalDefense = new DerivedStat(
                GameConst.StatKeys.PhysicalDefense,
                sc => sc.Get(GameConst.StatKeys.Guard).Value,
                stats,
                registry
            );

            var derivedMagicDefense = new DerivedStat(
                GameConst.StatKeys.MagicDefense,
                sc => sc.Get(GameConst.StatKeys.Spirit).Value,
                stats,
                registry
            );

            var derivedSpeed = new DerivedStat(
                GameConst.StatKeys.Speed,
                sc => sc.Get(GameConst.StatKeys.Finesse).Value +
                      sc.Get(GameConst.StatKeys.Focus).Value / 2f,
                stats,
                registry
            );

            var derivedHit = new DerivedStat(
                GameConst.StatKeys.Hit,
                sc => sc.Get(GameConst.StatKeys.Finesse).Value,
                stats,
                registry
            );

            var derivedAvoid = new DerivedStat(
                GameConst.StatKeys.Avoid,
                sc => sc.Get(GameConst.StatKeys.Speed).Value +
                      sc.Get(GameConst.StatKeys.Focus).Value / 2f,
                stats,
                registry
            );

            var derivedPhysicalCapacity = new DerivedStat(
                GameConst.StatKeys.PhysicalCapacity,
                sc => sc.Get(GameConst.StatKeys.Vitality).Value +
                      sc.Get(GameConst.StatKeys.Focus).Value * 4,
                stats,
                registry
            );

            var derivedMagicCapacity = new DerivedStat(
                GameConst.StatKeys.MagicCapacity,
                sc => sc.Get(GameConst.StatKeys.Focus).Value * 6,
                stats,
                registry
            );

            var derivedCritChance = new DerivedStat(
                GameConst.StatKeys.CritChance,
                sc => sc.Get(GameConst.StatKeys.Finesse).Value * 0.5f,
                stats,
                registry
            );

            var derivedCritDamage = new DerivedStat(
                GameConst.StatKeys.CritDamage,
                sc => GameConst.Combat.CritMultiplier, // fixed multiplier
                stats,
                registry
            );

            // --- Replace the placeholder stats with derived stats ---
            stats.AddDerived(derivedHP);
            stats.AddDerived(derivedPhysicalAttack);
            stats.AddDerived(derivedMagicAttack);
            stats.AddDerived(derivedPhysicalDefense);
            stats.AddDerived(derivedMagicDefense);
            stats.AddDerived(derivedSpeed);
            stats.AddDerived(derivedHit);
            stats.AddDerived(derivedAvoid);
            stats.AddDerived(derivedPhysicalCapacity);
            stats.AddDerived(derivedMagicCapacity);
            stats.AddDerived(derivedCritChance);
            stats.AddDerived(derivedCritDamage);

            return stats;
        }
    }
}
