// ============================================================
// TRPG.Game.Combat — Combat Formulas
// ============================================================
// All formulas are pure functions implementing IFormula<TInput,TResult>.
// No state. No EventBus. No side effects.
// Input structs carry everything needed. Output is a single result.
//
// Formula summary:
//
//   Hit Chance  = AttackerHit - DefenderAvoid + TriangleHitBonus
//                 Clamped 0–100 before roll.
//
//   Damage      = AttackerPhysAtk - DefenderPhysDef + TriangleDmgBonus
//                 Minimum 1. Multiplied by CritMultiplier if crit.
//
//   Crit Chance = AttackerCritChance (FIN × 0.5)
//                 Clamped 0–100.
//
//   Follow-Up   = AttackerSpeed - DefenderSpeed >= FollowUpThreshold
//                 Default threshold: 5 (configurable via GameConst)
// ============================================================

using System;
using TRPG.Core.Utility;

namespace TRPG.Game.TacticalBattle
{
    // --------------------------------------------------------
    // Input structs
    // --------------------------------------------------------

    public readonly struct HitFormulaInput
    {
        public readonly int AttackerHit;
        public readonly int DefenderAvoid;
        public readonly int TriangleHitBonus;   // From WeaponTriangle (+/-15 or 0)
        public readonly int TerrainHitBonus;    // From terrain system (future use, default 0)
        public readonly int BonusHit;           // From skills/effects accumulated this combat

        public HitFormulaInput(
            int attackerHit,
            int defenderAvoid,
            int triangleHitBonus = 0,
            int terrainHitBonus = 0,
            int bonusHit = 0)
        {
            AttackerHit = attackerHit;
            DefenderAvoid = defenderAvoid;
            TriangleHitBonus = triangleHitBonus;
            TerrainHitBonus = terrainHitBonus;
            BonusHit = bonusHit;
        }
    }

    public readonly struct DamageFormulaInput
    {
        public readonly int AttackerAttack;     // PhysicalAttack or MagicAttack
        public readonly int DefenderDefense;    // PhysicalDefense or MagicDefense
        public readonly int TriangleDamageBonus;
        public readonly int BonusDamage;        // From skills/effects accumulated this combat
        public readonly bool IsCritical;

        public DamageFormulaInput(
            int attackerAttack,
            int defenderDefense,
            int triangleDamageBonus = 0,
            int bonusDamage = 0,
            bool isCritical = false)
        {
            AttackerAttack = attackerAttack;
            DefenderDefense = defenderDefense;
            TriangleDamageBonus = triangleDamageBonus;
            BonusDamage = bonusDamage;
            IsCritical = isCritical;
        }
    }

    public readonly struct CritFormulaInput
    {
        public readonly int AttackerCritChance; // FIN * 0.5, floored
        public readonly int BonusCrit;          // From skills/effects

        public CritFormulaInput(int attackerCritChance, int bonusCrit = 0)
        {
            AttackerCritChance = attackerCritChance;
            BonusCrit = bonusCrit;
        }
    }

    public readonly struct FollowUpFormulaInput
    {
        public readonly int AttackerSpeed;
        public readonly int DefenderSpeed;
        public readonly int Threshold; // Default 5

        public FollowUpFormulaInput(int attackerSpeed, int defenderSpeed, int threshold = 5)
        {
            AttackerSpeed = attackerSpeed;
            DefenderSpeed = defenderSpeed;
            Threshold = threshold;
        }
    }

    // --------------------------------------------------------
    // Formulas
    // --------------------------------------------------------

    /// <summary>
    /// Calculates final hit percentage (0–100).
    /// AttackerHit - DefenderAvoid + triangle + terrain + bonus.
    /// Clamped to [0, 100].
    /// </summary>
    public sealed class HitFormula : IFormula<HitFormulaInput, int>
    {
        public int Calculate(HitFormulaInput input)
        {
            int raw = input.AttackerHit
                    - input.DefenderAvoid
                    + input.TriangleHitBonus
                    + input.TerrainHitBonus
                    + input.BonusHit;

            return Math.Clamp(raw, 0, 100);
        }
    }

    /// <summary>
    /// Calculates final damage.
    /// Attack - Defense + triangle + bonus. Minimum 1.
    /// Multiplied by CritMultiplier (1.5x) if critical.
    /// Result is always at least 1.
    /// </summary>
    public sealed class DamageFormula : IFormula<DamageFormulaInput, int>
    {
        private readonly float _critMultiplier;

        public DamageFormula(float critMultiplier = 1.5f)
        {
            _critMultiplier = critMultiplier;
        }

        public int Calculate(DamageFormulaInput input)
        {
            int raw = input.AttackerAttack
                    - input.DefenderDefense
                    + input.TriangleDamageBonus
                    + input.BonusDamage;

            // Minimum 1 before crit so crits are never 0
            raw = Math.Max(1, raw);

            if (input.IsCritical)
                raw = (int)MathF.Round(raw * _critMultiplier);

            return raw;
        }
    }

    /// <summary>
    /// Calculates final crit percentage (0–100).
    /// CritChance + bonus. Clamped to [0, 100].
    /// </summary>
    public sealed class CritFormula : IFormula<CritFormulaInput, int>
    {
        public int Calculate(CritFormulaInput input)
        {
            int raw = input.AttackerCritChance + input.BonusCrit;
            return Math.Clamp(raw, 0, 100);
        }
    }

    /// <summary>
    /// Determines whether a follow-up attack occurs.
    /// Attacker Speed - Defender Speed >= threshold (default 5).
    /// </summary>
    public sealed class FollowUpFormula : IFormula<FollowUpFormulaInput, bool>
    {
        public bool Calculate(FollowUpFormulaInput input)
        {
            return (input.AttackerSpeed - input.DefenderSpeed) >= input.Threshold;
        }
    }
}