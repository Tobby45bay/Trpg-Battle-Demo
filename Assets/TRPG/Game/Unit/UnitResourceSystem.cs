using System;
using TRPG.Core.Action;
using TRPG.Core.Conditions;
using TRPG.Core.Event;
using TRPG.Game.Const;
using TRPG.Game.Data.GameActions;
using TRPG.Game.Systems.Stats;
using TRPG.Game.TacticalBattle;

namespace TRPG.Game.Unit
{
    /// <summary>
    /// Manages action points (AP) and magic points (MP) for a unit.
    /// 
    /// Current values are runtime state.
    /// Max values are derived from the stat system:
    ///   - Max AP = VIT + FOC × 4
    ///   - Max MP = FOC × 6
    /// 
    /// Resources are replenished at the start of each turn or through abilities.
    /// Similar to UnitHealthComponent but for two independent resources.
    /// </summary>
    public sealed class UnitResourceComponent : IUnitComponent<ResourceSave>
    {
        public string Name => "UnitResourceComponent";
        public bool CanSave => true;

        private readonly UnitStatComponent _stats;
        private readonly EventBus _bus;
        private readonly int _unitId;

        private int _currentAP;
        private int _currentMP;

        /// <summary>
        /// Current action points available this turn.
        /// </summary>
        public int CurrentAP => _currentAP;

        /// <summary>
        /// Current magic points available.
        /// Max MP is derived from stats and never decreases mid-turn.
        /// </summary>
        public int CurrentMP => _currentMP;

        /// <summary>
        /// Max AP is always derived from the stat system.
        /// Never cache externally — stat modifiers can change it.
        /// Formula: VIT + FOC × 4
        /// </summary>
        public int MaxAP
        {
            get
            {
                var Ap = _stats.GetValue(GameConst.StatKeys.PhysicalCapacity);
                return (int)(Ap);
            }
        }

        /// <summary>
        /// Max MP is always derived from the stat system.
        /// Never cache externally — stat modifiers can change it.
        /// Formula: FOC × 6
        /// </summary>
        public int MaxMP
        {
            get
            {
                var Mp = _stats.GetValue(GameConst.StatKeys.MagicCapacity);
                return (int)(Mp);
            }
        }

        /// <summary>
        /// True if this unit has any AP available.
        /// </summary>
        public bool HasAP => _currentAP > 0;

        /// <summary>
        /// True if this unit has any MP available.
        /// </summary>
        public bool HasMP => _currentMP > 0;

        /// <summary>
        /// True if AP is at maximum.
        /// </summary>
        public bool IsAPFull => _currentAP >= MaxAP;

        /// <summary>
        /// True if MP is at maximum.
        /// </summary>
        public bool IsMPFull => _currentMP >= MaxMP;

        /// <summary>
        /// Percentage of AP remaining (0-100).
        /// </summary>
        public int APPercent => MaxAP > 0 ? (_currentAP * 100) / MaxAP : 0;

        /// <summary>
        /// Percentage of MP remaining (0-100).
        /// </summary>
        public int MPPercent => MaxMP > 0 ? (_currentMP * 100) / MaxMP : 0;

        public UnitResourceComponent(int unitId, UnitStatComponent stats, EventBus bus)
        {
            _unitId = unitId;
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        // ================================
        // AP Management
        // ================================

        /// <summary>
        /// Spends AP from the current pool.
        /// Clamps to 0. Enqueues ResourceChangedEvent.
        /// Returns actual AP spent.
        /// </summary>
        public int SpendAP(int amount)
        {
            if (amount <= 0 || !HasAP)
                return 0;

            int before = _currentAP;
            _currentAP = Math.Max(0, _currentAP - amount);
            int actual = before - _currentAP;

            _bus.Enqueue(new ResourceChangedEvent
            {
                UnitId = _unitId,
                ResourceType = ResourceType.AP,
                Delta = -actual,
                CurrentValue = _currentAP,
                MaxValue = MaxAP
            });

            return actual;
        }

        /// <summary>
        /// Restores AP to the unit.
        /// Clamps to MaxAP. Returns actual AP restored.
        /// </summary>
        public int RestoreAP(int amount)
        {
            if (amount <= 0)
                return 0;

            int before = _currentAP;
            _currentAP = Math.Min(MaxAP, _currentAP + amount);
            int actual = _currentAP - before;

            if (actual > 0)
            {
                _bus.Enqueue(new ResourceChangedEvent
                {
                    UnitId = _unitId,
                    ResourceType = ResourceType.AP,
                    Delta = actual,
                    CurrentValue = _currentAP,
                    MaxValue = MaxAP
                });
            }

            return actual;
        }

        /// <summary>
        /// Refills AP to maximum. Typically called at turn start.
        /// </summary>
        public void RestoreAPFull()
        {
            int maxAP = MaxAP;
            _currentAP = maxAP;

            _bus.Enqueue(new ResourceChangedEvent
            {
                UnitId = _unitId,
                ResourceType = ResourceType.AP,
                Delta = maxAP - _currentAP,
                CurrentValue = _currentAP,
                MaxValue = MaxAP
            });
        }

        /// <summary>
        /// Checks if the unit has enough AP for an action.
        /// </summary>
        public bool CanSpendAP(int amount) => amount > 0 && _currentAP >= amount;

        // ================================
        // MP Management
        // ================================

        /// <summary>
        /// Spends MP from the current pool.
        /// Clamps to 0. Enqueues ResourceChangedEvent.
        /// Returns actual MP spent.
        /// </summary>
        public int SpendMP(int amount)
        {
            if (amount <= 0 || !HasMP)
                return 0;

            int before = _currentMP;
            _currentMP = Math.Max(0, _currentMP - amount);
            int actual = before - _currentMP;

            _bus.Enqueue(new ResourceChangedEvent
            {
                UnitId = _unitId,
                ResourceType = ResourceType.MP,
                Delta = -actual,
                CurrentValue = _currentMP,
                MaxValue = MaxMP
            });

            return actual;
        }

        /// <summary>
        /// Restores MP to the unit.
        /// Clamps to MaxMP. Returns actual MP restored.
        /// </summary>
        public int RestoreMP(int amount)
        {
            if (amount <= 0)
                return 0;

            int before = _currentMP;
            _currentMP = Math.Min(MaxMP, _currentMP + amount);
            int actual = _currentMP - before;

            if (actual > 0)
            {
                _bus.Enqueue(new ResourceChangedEvent
                {
                    UnitId = _unitId,
                    ResourceType = ResourceType.MP,
                    Delta = actual,
                    CurrentValue = _currentMP,
                    MaxValue = MaxMP
                });
            }

            return actual;
        }

        /// <summary>
        /// Refills MP to maximum.
        /// </summary>
        public void RestoreMPFull()
        {
            int maxMP = MaxMP;
            _currentMP = maxMP;

            _bus.Enqueue(new ResourceChangedEvent
            {
                UnitId = _unitId,
                ResourceType = ResourceType.MP,
                Delta = maxMP - _currentMP,
                CurrentValue = _currentMP,
                MaxValue = MaxMP
            });
        }

        /// <summary>
        /// Checks if the unit has enough MP for an action.
        /// </summary>
        public bool CanSpendMP(int amount) => amount > 0 && _currentMP >= amount;

        // ================================
        // Batch Operations
        // ================================

        /// <summary>
        /// Refills both AP and MP to maximum.
        /// Called at turn start or after full restoration effects.
        /// </summary>
        public void RestoreAllFull()
        {
            RestoreAPFull();
            RestoreMPFull();
        }

        /// <summary>
        /// Resets both resources to zero.
        /// Generally not used in normal gameplay, but useful for certain effects.
        /// </summary>
        public void DrainAll()
        {
            if (_currentAP > 0)
            {
                int lost = _currentAP;
                _currentAP = 0;
                _bus.Enqueue(new ResourceChangedEvent
                {
                    UnitId = _unitId,
                    ResourceType = ResourceType.AP,
                    Delta = -lost,
                    CurrentValue = _currentAP,
                    MaxValue = MaxAP
                });
            }

            if (_currentMP > 0)
            {
                int lost = _currentMP;
                _currentMP = 0;
                _bus.Enqueue(new ResourceChangedEvent
                {
                    UnitId = _unitId,
                    ResourceType = ResourceType.MP,
                    Delta = -lost,
                    CurrentValue = _currentMP,
                    MaxValue = MaxMP
                });
            }
        }

        // ================================
        // IUnitComponent
        // ================================

        public void Initialize()
        {
            // On first init, fill resources to max
            _currentAP = MaxAP;
            _currentMP = MaxMP;
        }

        public void Cleanup() { }

        public void LoadData(ResourceSave data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Clamp on load in case max values changed since the save
            _currentAP = Math.Clamp(data.CurrentAP, 0, MaxAP);
            _currentMP = Math.Clamp(data.CurrentMP, 0, MaxMP);
        }

        public ResourceSave SaveData()
        {
            return new ResourceSave
            {
                CurrentAP = _currentAP,
                CurrentMP = _currentMP
            };
        }
    }

    // =========================================================
    // Save Data
    // =========================================================

    /// <summary>
    /// Serializable data for saving/loading resource state.
    /// </summary>
    [System.Serializable]
    public class ResourceSave
    {
        public int CurrentAP;
        public int CurrentMP;
    }

    // =========================================================
    // Events
    // =========================================================

    /// <summary>
    /// Fired whenever AP or MP changes (spend or restore).
    /// Delta is negative for spending, positive for restoring.
    /// </summary>
    public struct ResourceChangedEvent : Core.Event.IEventContext
    {
        public int UnitId;
        public ResourceType ResourceType;
        public int Delta;
        public int CurrentValue;
        public int MaxValue;
    }

    /// <summary>
    /// Identifies which resource changed.
    /// </summary>
    public enum ResourceType
    {
        AP,
        MP
    }

    /// <summary>
    /// Fired when a resource is fully depleted.
    /// Useful for triggering special effects or state changes.
    /// </summary>
    public struct ResourceDepletedEvent : Core.Event.IEventContext
    {
        public int UnitId;
        public ResourceType ResourceType;
    }
}