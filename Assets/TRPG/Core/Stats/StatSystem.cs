using System;
using System.Collections.Generic;


/// <summary>
/// Core stat system.
/// </summary>
/// <remarks>
/// This namespace contains a standalone stat engine.
/// It performs numeric stat calculations only and has no knowledge of:
/// - Units
/// - EffectConts
/// - Skills
/// - Combat
/// - Game rules
///
/// All semantic meaning is defined by higher-level systems.
/// </remarks>
namespace TRPG.Core.Stats
{
    /// <summary>
    /// Identifies a stat within the stat system.
    /// StatKeys are value-based and compared by Id only.
    /// </summary>
    /// <remarks>
    /// StatKey has no behavior and no semantic meaning beyond identity.
    /// Game-specific meaning (e.g. "Attack", "Speed") is defined outside
    /// the stat core.
    /// </remarks>
    public readonly struct StatKey : IEquatable<StatKey>
    {
        /// <summary>
        /// Unique identifier for the stat.
        /// </summary>
        public readonly int Id;
        /// <summary>
        /// Human-readable name, used for debugging and UI.
        /// </summary>
        public readonly string Name;

        public StatKey(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public bool Equals(StatKey other) => Id == other.Id;

        public override int GetHashCode() => Id;
        public override string ToString() => Name;
    }

    public readonly struct ModifierTypeKey : IEquatable<ModifierTypeKey>
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int Order;
        public ModifierTypeKey(int id, string name,int order)
        {
            Id = id;
            Name = name;
            Order = order;
        }

        public bool Equals(ModifierTypeKey other) => Id == other.Id;

        public override int GetHashCode() => Id;
        public override string ToString() => Name;
    }

    public interface IStat
    {
        public StatKey Key { get; }
        float BaseValue {  get; }
        float Value { get; }
        void SetBase(float value);
        ModifierHandle AddModifier(StatModifier modifier);
        void RemoveModifier(ModifierHandle handle);
        void RemoveModifiersBySource(object source);
    }

    /// <summary>
    /// Represents a single modification applied to a stat.
    /// </summary>
    /// <remarks>
    /// StatModifiers are passive data objects.
    /// They contain no logic and are applied via ModifierRules.
    /// 
    /// The Source field is an opaque identity token used only for
    /// grouping and removal. The stat system never inspects it.
    /// </remarks>
    public sealed class StatModifier
    {
        /// <summary>
        /// Unique runtime identifier for this modifier instance.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Determines how the modifier affects the stat value.
        /// </summary>
        public ModifierTypeKey Type;

        /// <summary>
        /// The numeric value passed to the modifier rule.
        /// Interpretation depends on the rule.
        /// </summary>
        public float Value;

        /// <summary>
        /// Determines application order relative to other modifiers.
        /// Lower values are applied first.
        /// </summary>
        public int Order;

        /// <summary>
        /// Opaque source identifier used for removal and ownership tracking.
        /// The stat system does not inspect or interpret this value.
        /// </summary>
        public object Source;
    }

    /// <summary>
    /// Defines how a stat modifier alters a stat value.
    /// </summary>
    /// <remarks>
    /// ModifierHandler rules are pure functions.
    /// They must not store state or depend on external systems.
    /// </remarks>
    public interface IModifierRule
    {
        /// <summary>
        /// Applies the modifier to the current stat calculation.
        /// </summary>
        /// <param name="value">Current stat value.</param>
        /// <param name="percentAddAccumulator">
        /// Accumulates additive percentage modifiers to be applied once.
        /// </param>
        /// <param name="modifierValue">Value defined by the StatModifier.</param>
        void Apply(ref float value, ref float percentAddAccumulator, float modifierValue);
    }

    public sealed class ModifierHandle
    {
        public Guid Id { get; }
        public StatKey TargetStat { get; }

        internal ModifierHandle(Guid id, StatKey targetStat)
        {
            Id = id;
            TargetStat = targetStat;
        }
    }


    /// <summary>
    /// Global registry mapping modifier types to modifier rules.
    /// </summary>
    /// <remarks>
    /// This registry should be populated during system initialization
    /// and treated as immutable during gameplay.
    /// </remarks>
    public sealed class ModifierRuleRegistry
    {
        private readonly Dictionary<ModifierTypeKey, IModifierRule> _rules = new();

        public void Register(ModifierTypeKey key, IModifierRule rule)
        {
            _rules[key] = rule;
        }

        public IModifierRule Get(ModifierTypeKey key)
        {
            if (!_rules.TryGetValue(key, out var rule))
                throw new Exception($"No rule registered for modifier type {key}");

            return rule;
        }
    }


    /// <summary>
    /// Holds a collection of stats indexed by StatKey.
    /// </summary>
    /// <remarks>
    /// StatCollection owns stat instances and controls access to them.
    /// The collection structure cannot be modified externally.
    /// </remarks>
    public sealed class StatCollection
    {
        private readonly ModifierRuleRegistry _registry;
        private readonly Dictionary<StatKey, IStat> _stats = new();

        public StatCollection(ModifierRuleRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public IStat Get(StatKey key)
        {
            if (!_stats.TryGetValue(key, out var stat))
                throw new KeyNotFoundException($"Missing stat: {key}");

            return stat;
        }

        public void Add(StatKey key, float baseValue)
        {
            if (_stats.ContainsKey(key))
                throw new InvalidOperationException($"Stat already exists: {key}");

            _stats[key] = new Stat(key, baseValue, _registry);
        }

        public void AddDerived(DerivedStat derived)
        {
            if (derived == null)
                throw new ArgumentNullException(nameof(derived));

            if (_stats.ContainsKey(derived.Key))
                throw new InvalidOperationException($"Stat already exists: {derived.Key}");

            _stats[derived.Key] = derived;
        }

        public bool Has(StatKey key) => _stats.ContainsKey(key);

        /// <summary>
        /// Enumerates all stats in the collection.
        /// Structure is immutable, individual stats are not.
        /// </summary>
        public IEnumerable<IStat> All => _stats.Values;
    }

    /// <summary>
    /// Represents a single numeric stat with modifiers.
    /// </summary>
    /// <remarks>
    /// Stats are lazily recalculated and cached.
    /// Any change to the base value or modifiers marks the stat dirty.
    /// </remarks>
    public sealed class Stat : IStat
    {
        private readonly StatKey _key;
        public StatKey Key => _key;
        private readonly ModifierRuleRegistry _registry;

        private float _baseValue;
        private float _cachedValue;
        private bool _dirty = true;

        private readonly List<StatModifier> _modifiers = new();

        public float BaseValue => _baseValue;

        public Stat(StatKey key,float baseValue,ModifierRuleRegistry registry)
        {
            _key = key;
            _baseValue = baseValue;
            _registry = registry;
        }

        public float Value
        {
            get
            {
                if (_dirty)
                    Recalculate();

                return _cachedValue;
            }
        }

        

        public void SetBase(float value)
        {
            _baseValue = value;
            _dirty = true;
        }

        // Fully Handle-Based Add
        public ModifierHandle AddModifier(StatModifier modifier)
        {
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            _modifiers.Add(modifier);

            // Sort once on insertion
            _modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));

            _dirty = true;

            return new ModifierHandle(modifier.Id, _key);
        }

        // Remove using handle
        public void RemoveModifier(ModifierHandle handle)
        {
            if (handle == null)
                return;

            if (!handle.TargetStat.Equals(_key))
                return; // safety guard

            _modifiers.RemoveAll(m => m.Id == handle.Id);
            _dirty = true;
        }

        // Optional bulk remove
        public void RemoveModifiersBySource(object source)
        {
            _modifiers.RemoveAll(m => ReferenceEquals(m.Source, source));
            _dirty = true;
        }

        public void ClearModifiers()
        {
            _modifiers.Clear();
            _dirty = true;
        }

        private void Recalculate()
        {
            float value = _baseValue;
            float percentAdd = 0f;

            foreach (var mod in _modifiers)
            {
                var rule = _registry.Get(mod.Type);
                rule.Apply(ref value, ref percentAdd, mod.Value);
            }

            _cachedValue = MathF.Round(value * (1f + percentAdd));
            _dirty = false;
        }
    }

    /// <summary>
    /// A stat whose base value is calculated from other stats, but still
    /// supports modifiers like any other stat.
    /// </summary>
    public sealed class DerivedStat : IStat
    {
        private readonly StatKey _key;
        private readonly StatCollection _collection;
        private readonly Func<StatCollection, float> _formula;
        private readonly ModifierRuleRegistry _registry;

        private float _cachedValue;
        private bool _dirty = true;
        private readonly List<StatModifier> _modifiers = new();

        public StatKey Key => _key;

        public DerivedStat(StatKey key, Func<StatCollection, float> formula,  StatCollection collection,  ModifierRuleRegistry registry)
        {
            _key = key;
            _formula = formula;
            _collection = collection;
            _registry = registry;
        }

        public float BaseValue
        {
            get
            {
                if (_dirty)
                    Recalculate(); // Default null for now
                return _cachedValue;
            }
        }

        public float Value
        {
            get
            {
                if (_dirty)
                    Recalculate();
                return _cachedValue;
            }
        }

        public void SetBase(float value)
        {
            throw new InvalidOperationException("Cannot directly set base value of a derived stat.");
        }

        public ModifierHandle AddModifier(StatModifier modifier)
        {
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            _modifiers.Add(modifier);
            _modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
            _dirty = true;

            return new ModifierHandle(modifier.Id, _key);
        }

        public void RemoveModifier(ModifierHandle handle)
        {
            if (handle == null || !handle.TargetStat.Equals(_key))
                return;

            _modifiers.RemoveAll(m => m.Id == handle.Id);
            _dirty = true;
        }

        public void RemoveModifiersBySource(object source)
        {
            _modifiers.RemoveAll(m => ReferenceEquals(m.Source, source));
            _dirty = true;
        }

        /// <summary>
        /// Recalculates the derived stat based on the formula and modifiers.
        /// </summary>
        private void Recalculate()
        {
            float value = _formula(_collection);
            float percentAdd = 0f;

            foreach (var mod in _modifiers)
            {
                var rule = _registry.Get(mod.Type);
                rule.Apply(ref value, ref percentAdd, mod.Value);
            }

            _cachedValue = MathF.Round(value * (1f + percentAdd));
            _dirty = false;
        }
    }

    public enum StatValueMode
    {
        Base,
        Modified
    }
    public interface IStatSnapshot
    {
        float Get(StatKey key, StatValueMode mode = StatValueMode.Modified);
    }
}