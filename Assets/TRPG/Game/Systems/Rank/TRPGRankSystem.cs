using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Core.Event;
using TRPG.Core.Stats;
using TRPG.Game.Const;
using TRPG.Game.Systems.Items;
using TRPG.Game.Systems.Magic;
using TRPG.Game.Unit;

namespace TRPG.Game.Systems.Rank
{
    public enum RankTier
    {
        E = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5
    }

    public enum RankDomain
    {
        Magic = 0,
        Weapon = 1
    }



    public readonly struct RankCategoryKey
    {
        public readonly RankDomain Domain;
        public readonly int Id;

        public RankCategoryKey(RankDomain domain, int id)
        {
            Domain = domain;
            Id = id;
        }
    }

    public sealed class RankProgress
    {
        public RankTier Rank { get; private set; }
        public int CurrentExp { get; private set; }

        public RankProgress(RankTier startingRank, int currentExp = 0)
        {
            Rank = startingRank;
            CurrentExp = currentExp;
        }

        /// <summary>
        /// Adds experience and ranks up if thresholds are met.
        /// </summary>
        public void AddExp(int amount)
        {
            if (Rank == RankTier.S)
                return;

            CurrentExp += amount;

            while (CurrentExp >= GetThreshold(Rank))
            {
                CurrentExp -= GetThreshold(Rank);
                Rank++;

                if (Rank == RankTier.S)
                {
                    CurrentExp = 0;
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the EXP threshold required to reach the next rank from the given rank.
        /// Formula: 50 × (rank + 1) × (rank + 1)
        /// </summary>
        public static int GetThreshold(RankTier rank)
        {
            int rankValue = (int)rank;
            return 50 * (rankValue + 1) * (rankValue + 1);
        }

        /// <summary>
        /// Sets current EXP directly (for loading from save).
        /// </summary>
        public void SetExp(int amount)
        {
            if (Rank == RankTier.S)
                CurrentExp = 0;
            else
                CurrentExp = Math.Clamp(amount, 0, GetThreshold(Rank));
        }

        public override string ToString() => $"{Rank} ({CurrentExp}/{GetThreshold(Rank)})";
    }

    public sealed class RankBonusDefinition
    {
        public RankTier Rank { get; }
        public IReadOnlyList<RankBonusModifier> Modifiers { get; }

        public RankBonusDefinition(RankTier rank, List<RankBonusModifier> modifiers)
        {
            Rank = rank;
            Modifiers = modifiers.AsReadOnly();
        }
    }

    public sealed class RankBonusModifier
    {
        public RankBonusType Type { get; }
        public int Value { get; }

        public RankBonusModifier(RankBonusType type, int value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString() => $"{Type}: +{Value}";
    }

    public sealed class UnitRankComponent : IUnitComponent<UnitRankSaveData>
    {
        private readonly Dictionary<WeaponType, RankProgress> _weaponProgress;
        private readonly Dictionary<ElementType, RankProgress> _magicProgress;
        private readonly List<MagicAffinity> _magicAffinities = new();

        private EventBus _eventBus;
        private int _ownerUnitId;
        private bool _initialized = false;

        public string Name => "UnitRankComponent";
        public bool CanSave => true;

        /// <summary>
        /// Creates a rank component with available weapons and elements.
        /// Magic affinities can be provided to override starting ranks.
        /// </summary>
        public UnitRankComponent(
            IEnumerable<WeaponType> weaponTypes,
            IEnumerable<ElementType> elementTypes,
            IEnumerable<MagicAffinity> magicAffinities = null)
        {
            _weaponProgress = weaponTypes.ToDictionary(
                w => w,
                w => new RankProgress(RankTier.E));

            _magicProgress = elementTypes.ToDictionary(
                e => e,
                e => new RankProgress(RankTier.E));

            if (magicAffinities != null)
                _magicAffinities.AddRange(magicAffinities);
        }

        /// <summary>
        /// Wires up the component with its owner ID and event bus.
        /// Must be called before processing rank-up events.
        /// </summary>
        public void WireDependencies(int ownerUnitId, EventBus eventBus)
        {
            if (_initialized)
                throw new InvalidOperationException("Component already initialized with dependencies");

            _ownerUnitId = ownerUnitId;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _initialized = true;
        }

        /// <summary>
        /// Initializes rank starting points based on magic affinities.
        /// Called during component initialization.
        /// </summary>
        public void Initialize()
        {
            InitializeRanksFromAffinities();
        }

        public void Cleanup() { }

        // Rank Access

        public RankTier GetWeaponRank(WeaponType type)
        {
            return _weaponProgress.TryGetValue(type, out var progress) ? progress.Rank : RankTier.E;
        }

        public RankTier GetMagicRank(ElementType type)
        {
            return _magicProgress.TryGetValue(type, out var progress) ? progress.Rank : RankTier.E;
        }

        public RankProgress GetWeaponProgress(WeaponType type)
        {
            return _weaponProgress.TryGetValue(type, out var progress) ? progress : null;
        }

        public RankProgress GetMagicProgress(ElementType type)
        {
            return _magicProgress.TryGetValue(type, out var progress) ? progress : null;
        }


        public void AddWeaponExp(WeaponType type, int amount)
        {
            if (!_weaponProgress.TryGetValue(type, out var progress))
                return;

            var oldRank = progress.Rank;
            progress.AddExp(amount);

            if (progress.Rank != oldRank)
            {
                _eventBus?.Enqueue(new WeaponRankUpEvent(_ownerUnitId, type, progress.Rank));
            }
        }

        public void AddMagicExpWithAffinity(ElementType type, int amount)
        {
            var affinity = _magicAffinities
                .FirstOrDefault(a => a.ElementType == type)
                ?.ElementAffinity ?? ElementAffinity.None;

            amount = ApplyAffinityBonus(amount, affinity);
            AddMagicExp(type, amount);
        }

        public void AddMagicExp(ElementType type, int amount)
        {
            if (!_magicProgress.TryGetValue(type, out var progress))
                return;

            var oldRank = progress.Rank;
            progress.AddExp(amount);

            if (progress.Rank != oldRank)
            {
                _eventBus?.Enqueue(new MagicRankUpEvent(_ownerUnitId, type, progress.Rank));
            }
        }

        public IEnumerable<RankBonusModifier> GetWeaponBonuses(WeaponType type)
        {
            var rank = GetWeaponRank(type);
            if (RankBonusDatabase.WeaponBonuses.TryGetValue(type, out var rankDict) &&
                rankDict.TryGetValue(rank, out var definition))
                return definition.Modifiers;

            return Enumerable.Empty<RankBonusModifier>();
        }

        public IEnumerable<RankBonusModifier> GetMagicBonuses(ElementType type)
        {
            var rank = GetMagicRank(type);
            if (RankBonusDatabase.MagicBonuses.TryGetValue(type, out var rankDict) &&
                rankDict.TryGetValue(rank, out var definition))
                return definition.Modifiers;

            return Enumerable.Empty<RankBonusModifier>();
        }
        // Validation & Helpers

        /// <summary>
        /// Validates whether a bonus type can be applied to a target stat.
        /// Prevents applying wrong bonus types to wrong stats.
        /// </summary>
        public bool CanApplyBonus(RankBonusType bonusType, StatKey targetStat)
        {
            return (bonusType, targetStat.Id) switch
            {
                // Physical Attack bonus
                (RankBonusType.PhysicalAttack, 11) => true,
                // Magic Attack bonus
                (RankBonusType.MagicAttack, 12) => true,
                // Physical Defense bonus
                (RankBonusType.PhysicalDefense, 13) => true,
                // Magic Defense bonus
                (RankBonusType.MagicDefense, 14) => true,
                // Hit bonus
                (RankBonusType.Hit, 16) => true,
                // Avoid bonus
                (RankBonusType.Avoid, 17) => true,
                // Cost reduction doesn't directly map to a stat
                (RankBonusType.ApCostReduction, _) => true,
                (RankBonusType.MpCostReduction, _) => true,
                (RankBonusType.EffectChance, _) => true,
                (RankBonusType.ElementResistance, _) => true,
                _ => false
            };
        }

        private void InitializeRanksFromAffinities()
        {
            foreach (var affinity in _magicAffinities)
            {
                var startingRank = GetStartingRank(affinity.ElementAffinity);
                _magicProgress[affinity.ElementType] = new RankProgress(startingRank);
            }
        }

        private RankTier GetStartingRank(ElementAffinity affinity)
        {
            return affinity switch
            {
                ElementAffinity.High => RankTier.C,
                ElementAffinity.Low => RankTier.D,
                _ => RankTier.E
            };
        }

        private int ApplyAffinityBonus(int amount, ElementAffinity elementAffinity)
        {
            return elementAffinity switch
            {
                ElementAffinity.High => (int)(amount * 1.5f), // 50% bonus
                ElementAffinity.Low => (int)(amount * 0.75f), // 25% penalty
                _ => amount
            };
        }

        public void LoadData(UnitRankSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Load weapon ranks
            if (data.WeaponRanks != null)
            {
                foreach (var kvp in data.WeaponRanks)
                {
                    if (_weaponProgress.ContainsKey(kvp.Key))
                    {
                        _weaponProgress[kvp.Key] = new RankProgress(kvp.Value.Rank, kvp.Value.Exp);
                    }
                }
            }

            // Load magic ranks
            if (data.MagicRanks != null)
            {
                foreach (var kvp in data.MagicRanks)
                {
                    if (_magicProgress.ContainsKey(kvp.Key))
                    {
                        _magicProgress[kvp.Key] = new RankProgress(kvp.Value.Rank, kvp.Value.Exp);
                    }
                }
            }
        }

        public UnitRankSaveData SaveData()
        {
            var save = new UnitRankSaveData
            {
                WeaponRanks = _weaponProgress.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value.Rank, kvp.Value.CurrentExp)
                ),
                MagicRanks = _magicProgress.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value.Rank, kvp.Value.CurrentExp)
                )
            };

            return save;
        }
    }

    [Serializable]
    public sealed class UnitRankSaveData
    {
        public Dictionary<WeaponType, (RankTier Rank, int Exp)> WeaponRanks;
        public Dictionary<ElementType, (RankTier Rank, int Exp)> MagicRanks;

        public UnitRankSaveData()
        {
            WeaponRanks = new();
            MagicRanks = new();
        }
    }

    public readonly struct WeaponRankUpEvent : Core.Event.IEventContext
    {
        public int UnitId { get; }
        public WeaponType Type { get; }
        public RankTier NewRank { get; }

        public WeaponRankUpEvent(int unitId, WeaponType type, RankTier newRank)
        {
            UnitId = unitId;
            Type = type;
            NewRank = newRank;
        }

        public override string ToString() => $"TRPGUnit {UnitId} reached {Type} rank {NewRank}";
    }

    public readonly struct MagicRankUpEvent : Core.Event.IEventContext
    {
        public int UnitId { get; }
        public ElementType Type { get; }
        public RankTier NewRank { get; }

        public MagicRankUpEvent(int unitId, ElementType type, RankTier newRank)
        {
            UnitId = unitId;
            Type = type;
            NewRank = newRank;
        }

        public override string ToString() => $"TRPGUnit {UnitId} reached {Type} rank {NewRank}";
    }
}
