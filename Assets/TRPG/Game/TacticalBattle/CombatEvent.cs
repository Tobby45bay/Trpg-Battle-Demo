// ============================================================
// TRPG.Game.Combat — Combat Event Structs
// ============================================================
// All combat events are pure data (IEvent marker interface).
// They carry snapshots of state at the moment they're fired.
//
// Naming convention:
//   *DeclaredEvent  — intent, before resolution
//   *InitiatedEvent — a side begins acting
//   *CheckEvent     — a resolution point (result TBD)
//   *HitEvent       — outcome confirmed: hit
//   *MissedEvent    — outcome confirmed: miss
//   *AppliedEvent   — values written to game state
//   *EndedEvent     — phase is over, cleanup
// ============================================================

using TRPG.Core.Event;

namespace TRPG.Game.TacticalBattle
{
    // --------------------------------------------------------
    // Combat scope — outermost wrapper
    // --------------------------------------------------------

    /// <summary>
    /// Fired when an attacker selects a target and combat begins.
    /// Pre-combat effects (Guard stance, counter prep) hook here.
    /// </summary>
    public struct CombatDeclaredEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;

        public CombatDeclaredEvent(int attackerId, int defenderId)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
        }
    }

    /// <summary>
    /// Fired after all pre-combat effects resolve.
    /// Attacker-side skills (+damage, accuracy, first-strike) hook here.
    /// </summary>
    public struct AttackInitiatedEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        /// <summary>True for the follow-up second strike.</summary>
        public bool IsFollowUp;
    }

    /// <summary>
    /// Fired after AttackInitiatedEvent.
    /// Defender-side skills (damage reduction, dodge boost, shield) hook here.
    /// </summary>
    public struct DefenseInitiatedEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        public bool IsFollowUp;
    }

    /// <summary>
    /// Fired to resolve hit/miss.
    /// Contains final hit chance after all modifiers.
    /// Weapon triangle and terrain bonuses are baked in at this point.
    /// </summary>
    public struct HitCheckEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        /// <summary>Final hit percentage (0–100+). Clamped by orchestrator before roll.</summary>
        public int HitChance;
        public bool IsFollowUp;
    }

    /// <summary>Fired when the hit roll fails.</summary>
    public struct AttackMissedEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        public bool IsFollowUp;
    }

    /// <summary>Fired when the hit roll succeeds, before damage.</summary>
    public struct AttackHitEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        public bool IsFollowUp;
    }

    /// <summary>
    /// Fired to resolve crit/no-crit.
    /// Contains final crit chance after modifiers.
    /// </summary>
    public struct CriticalCheckEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        public int CritChance;
        public bool IsFollowUp;
    }

    /// <summary>Fired when crit roll succeeds. Multiplier is applied here.</summary>
    public struct AttackCriticalEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        public bool IsFollowUp;
    }

    /// <summary>
    /// Fired after damage is calculated and written to HP.
    /// Lifesteal, thorns, reflect hook here.
    /// </summary>
    public struct DamageAppliedEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        /// <summary>Raw damage before HP clamping.</summary>
        public int RawDamage;
        /// <summary>Actual HP lost (may be less than RawDamage if near death).</summary>
        public int ActualDamage;
        public bool WasCritical;
        public bool IsFollowUp;
    }

    /// <summary>
    /// Fired to check whether a follow-up attack should occur.
    /// Speed delta is pre-calculated; listeners may override the result.
    /// </summary>


    /// <summary>
    /// Fired when a unit's HP reaches 0.
    /// Death effects, soul skills, EXP gain hook here.
    /// NOTE: UnitHealthComponent also fires its own UnitDefeatedEvent
    /// when HP hits 0. This event is the *combat-scoped* version —
    /// fired by the orchestrator to signal the combat consequence.
    /// </summary>
    public struct CombatUnitDefeatedEvent : IEventContext
    {
        public int DefeatedUnitId;
        public int KillerUnitId;
    }

    /// <summary>
    /// Fired when the full combat exchange is over.
    /// Temporary stances, duration ticks, end-of-combat effects hook here.
    /// </summary>
    public struct CombatEndedEvent : IEventContext
    {
        public int AttackerId;
        public int DefenderId;
        public bool AttackerDefeated;
        public bool DefenderDefeated;
    }

    public struct CombatTimeInitializedEvent : IEventContext
    {
        public int UnitId;
        public int BaseTime;
        public int ModifiedTime;
    }

    public struct AttackCostCalculatedEvent : IEventContext
    {
        public int UnitId;
        public int BaseCost;
        public int ModifiedCost;
    }
}