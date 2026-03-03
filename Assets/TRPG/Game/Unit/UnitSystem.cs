
using System;
using System.Collections.Generic;
using TRPG.Core.Database;
using TRPG.Core.Event;
using TRPG.Game.Const;
using TRPG.Game.Systems.Stats;
using TRPG.Game.Systems.TRPGEfect;
using TRPG.Game.TacticalBattle;
#nullable enable
namespace TRPG.Game.Unit
{
    // =========================================================
    // IUnitComponent — unchanged contract, kept here for clarity
    // =========================================================

    public interface IUnitComponent<TData>
    {
        string Name { get; }
        bool CanSave { get; }

        void Initialize();
        void Cleanup();
        void LoadData(TData data);
        TData SaveData();
    }


    // =========================================================
    // TRPGUnit — component container
    // =========================================================
    // Design rules:
    //   • TRPGUnit owns its components. Nothing outside should new them.
    //   • Components are retrieved by type, not by string.
    //   • Stats and Health are first-class properties for convenience,
    //     but they are still stored in the component registry.
    //   • TRPGUnit does not know about combat, AI, or rendering.
    // =========================================================

    public sealed class TRPGUnit
    {
        // --- Identity ---
        public int Id { get; }
        public string Name { get; }

        // --- First-class shortcuts (registered as components internally) ---
        public UnitStatComponent Stats { get; }
        public UnitHealthComponent Health { get; }
        public UnitEffectComponent Effects { get; }

        public List<IPreStrikeModifier> PreStrikeModifiers { get; } = new();

        // --- Component registry ---
        private readonly Dictionary<Type, object> _components = new();

        internal TRPGUnit(int id, string name, UnitStatComponent stats, UnitHealthComponent health)
        {
            Id = id;
            Name = name;
            Stats = stats;
            Health = health;

            // Register first-class components so GetComponent<T>() finds them too
            _components[typeof(UnitStatComponent)] = stats;
            _components[typeof(UnitHealthComponent)] = health;
        }

        /// <summary>
        /// Registers an additional component.
        /// Each component type may only be registered once.
        /// </summary>
        public void AddComponent<TData>(IUnitComponent<TData> component)
        {
            var type = component.GetType();

            if (_components.ContainsKey(type))
                throw new InvalidOperationException(
                    $"Component of type {type.Name} is already registered on unit {Name} ({Id}).");

            _components[type] = component;
        }

        /// <summary>
        /// Retrieves a component by its concrete type.
        /// Returns null if not present.
        /// </summary>
        public T? GetComponent<T>() where T : class
        {
            _components.TryGetValue(typeof(T), out var component);
            return component as T;
        }

        /// <summary>
        /// Returns true if the unit has a component of the given type.
        /// </summary>
        public bool HasComponent<T>() where T : class
            => _components.ContainsKey(typeof(T));

        /// <summary>
        /// Calls Initialize() on all registered components.
        /// Called once after the unit is fully constructed.
        /// </summary>
        public void Initialize()
        {
            Stats.Initialize();
            Health.Initialize();
        }

        /// <summary>
        /// Calls Cleanup() on all registered components.
        /// Called when the unit is removed from play.
        /// </summary>
        public void Cleanup()
        {
            Stats.Cleanup();
            Health.Cleanup();
        }

        public override string ToString() => $"[TRPGUnit] {Name} (Id:{Id})";
    }

    // =========================================================
    // UnitHealthComponent
    // =========================================================
    // Owns current HP as runtime state.
    // Max HP is always read from the stat system (derived HP stat).
    //
    // Why separate from stats:
    //   • HP max is a formula (VIT × 4 + 18).
    //   • Current HP is not a formula — it's mutable runtime state.
    //   • Mixing them causes dirty-cache bugs and leaks combat state
    //     into the stat layer.
    // =========================================================

    public sealed class UnitHealthComponent : IUnitComponent<HealthSave>
    {
        public string Name => "UnitHealthComponent";
        public bool CanSave => true;

        private readonly UnitStatComponent _stats;
        private readonly EventBus _bus;
        private readonly int _unitId;

        public int CurrentHP { get; private set; }

        /// <summary>
        /// Max HP is always derived from the stat system.
        /// Never cache this externally — stat modifiers can change it.
        /// </summary>
        public int MaxHP => (int)_stats.GetValue(GameConst.StatKeys.HP);

        public bool IsAlive => CurrentHP > 0;
        public bool IsAtFullHP => CurrentHP >= MaxHP;

        public UnitHealthComponent(int unitId, UnitStatComponent stats, EventBus bus)
        {
            _unitId = unitId;
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        // --------------------------------
        // Modification
        // --------------------------------

        /// <summary>
        /// Applies damage to the unit.
        /// Clamps to 0. Enqueues UnitDefeatedEvent if HP hits 0.
        /// Returns actual damage dealt.
        /// </summary>
        public int TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return 0;

            int before = CurrentHP;
            CurrentHP = Math.Max(0, CurrentHP - amount);
            int actual = before - CurrentHP;

            _bus.Enqueue(new HPChangedEvent
            {
                UnitId = _unitId,
                Delta = -actual,
                CurrentHP = CurrentHP,
                MaxHP = MaxHP
            });

            if (CurrentHP <= 0)
            {
                _bus.Enqueue(new UnitDefeatedEvent { UnitId = _unitId });
            }

            return actual;
        }

        /// <summary>
        /// Heals the unit.
        /// Clamps to MaxHP. Returns actual HP restored.
        /// </summary>
        public int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return 0;

            int before = CurrentHP;
            CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
            int actual = CurrentHP - before;

            if (actual > 0)
            {
                _bus.Enqueue(new HPChangedEvent
                {
                    UnitId = _unitId,
                    Delta = actual,
                    CurrentHP = CurrentHP,
                    MaxHP = MaxHP
                });
            }

            return actual;
        }

        /// <summary>
        /// Sets HP to MaxHP. Used at battle start or full restore effects.
        /// </summary>
        public void FullRestore()
        {
            CurrentHP = MaxHP;

            _bus.Enqueue(new HPChangedEvent
            {
                UnitId = _unitId,
                Delta = 0,
                CurrentHP = CurrentHP,
                MaxHP = MaxHP
            });
        }

        // --------------------------------
        // IUnitComponent
        // --------------------------------

        public void Initialize()
        {
            // On first init, fill HP to max
            CurrentHP = MaxHP;
        }

        public void Cleanup(){}

        public void LoadData(HealthSave data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Clamp on load in case MaxHP changed since the save was made
            CurrentHP = Math.Clamp(data.CurrentHP, 0, MaxHP);
        }

        public HealthSave SaveData()
        {
            return new HealthSave { CurrentHP = CurrentHP };
        }
    }

    // =========================================================
    // Save data
    // =========================================================

    [Serializable]
    public class HealthSave
    {
        public int CurrentHP;
    }

    // =========================================================
    // Events fired by UnitHealthComponent
    // =========================================================

    public interface IUnitEvent : Core.Event.IEventContext
    {
        TRPGUnit Unit { get; }
    }

    /// <summary>
    /// Fired whenever current HP changes (damage or heal).
    /// Delta is negative for damage, positive for healing.
    /// </summary>
    public struct HPChangedEvent : Core.Event.IEventContext
    {
        public int UnitId;
        public int Delta;
        public int CurrentHP;
        public int MaxHP;
    }

    /// <summary>
    /// Fired when CurrentHP reaches 0.
    /// Distinct from CombatEndedEvent — this is unit-scoped, not combat-scoped.
    /// Combat layer listens to this to trigger its own UnitDefeated flow.
    /// </summary>
    public struct UnitDefeatedEvent : Core.Event.IEventContext
    {
        public int UnitId;
    }
}