using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Core.Stats;
using TRPG.Core.Utility;
using TRPG.Game.Const;
using TRPG.Game.Unit;
#nullable enable
namespace TRPG.Game.Systems.Stats
{
    public sealed class UnitStatSnapshot : IStatSnapshot
    {
        private readonly Dictionary<StatKey, float> _baseValues;
        private readonly Dictionary<StatKey, float> _modifiedValues;

        public UnitStatSnapshot(StatCollection collection)
        {
            _baseValues = collection.All.ToDictionary(s => s.Key, s => s.BaseValue);
            _modifiedValues = collection.All.ToDictionary(s => s.Key, s => s.Value);
        }

        public float Get(StatKey key, StatValueMode mode = StatValueMode.Modified)
            => mode == StatValueMode.Base ? _baseValues[key] : _modifiedValues[key];
    }

    public sealed class UnitStatComponent : IUnitComponent<UnitStatsSave>
    {
        public string Name => "UnitStatComponent";

        // We can save this component
        public bool CanSave => true;
        public StatCollection Stats { get; }

        private readonly Dictionary<StatKey, int> _growthRates = new();
        private readonly IRandomProvider _random;

        public UnitStatComponent(ModifierRuleRegistry registry,IRandomProvider random,UnitStatsSave saveData,
            Func<StatCollection, float>? weaponPower = null,
            Func<StatCollection, float>? spellPower = null)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));

            // Extract base stat values from save
            float vitality = 0;
            float might = 0;
            float finesse = 0;
            float guard = 0;
            float focus = 0;
            float spirit = 0;

            foreach (var stat in saveData.coreStats)
            {
                switch (stat.Key.Id)
                {
                    case 0: vitality = stat.BaseValue; break;
                    case 1: might = stat.BaseValue; break;
                    case 2: finesse = stat.BaseValue; break;
                    case 3: guard = stat.BaseValue; break;
                    case 4: focus = stat.BaseValue; break;
                    case 5: spirit = stat.BaseValue; break;
                }

                _growthRates[stat.Key] = stat.GrowthRate;
            }

            // Create stat collection WITH derived stats auto-bound
            Stats = StatCollectionFactory.CreateUnitStats(
                registry,
                vitality,
                might,
                finesse,
                guard,
                focus,
                spirit,
                weaponPower,
                spellPower
            );
        }

        // ================================
        // Growth
        // ================================
        public void RollGrowth()
        {
            foreach (var (key, rate) in _growthRates)
            {
                if (_random.Next(0, 100) < rate)
                {
                    var stat = Stats.Get(key);
                    stat.SetBase(stat.BaseValue + 1);
                }
            }
        }

        // ================================
        // Value Access
        // ================================
        public float GetValue(StatKey key, bool withModifiers = true)
        {
            var stat = Stats.Get(key);

            return withModifiers
                ? stat.Value
                : stat.BaseValue;
        }

        internal float GetValueInternal(StatKey key, StatValueMode mode)
        {
            var stat = Stats.Get(key);

            return mode == StatValueMode.Base
                ? stat.BaseValue
                : stat.Value;
        }

        // ================================
        // Modifiers
        // ================================
        public ModifierHandle AddModifier(StatKey key, StatModifier modifier)
        {
            return Stats.Get(key).AddModifier(modifier);
        }

        public void RemoveModifier(ModifierHandle handle)
        {
            Stats.Get(handle.TargetStat).RemoveModifier(handle);
        }

        public void RemoveModifiersBySource(StatKey key, object source)
        {
            Stats.Get(key).RemoveModifiersBySource(source);
        }

        public void RemoveAllModifiersBySource(object source)
        {
            foreach (var stat in Stats.All)
            {
                stat.RemoveModifiersBySource(source);
            }
        }

        // ================================
        // Snapshot
        // ================================
        public IStatSnapshot CreateSnapshot()
        {
            return new UnitStatSnapshot(Stats);
        }

        public void Initialize()
        {
            // Any initialization logic here
            // For example, recalc derived stats or reset temporary modifiers
        }

        public void Cleanup()
        {
            // Optional cleanup logic
            // Remove temporary modifiers, snapshots, etc.
        }

        public void LoadData(UnitStatsSave data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            foreach (var statSave in data.coreStats)
            {
                if (Stats.Has(statSave.Key))
                {
                    var stat = Stats.Get(statSave.Key);
                    stat.SetBase(statSave.BaseValue);
                }

                _growthRates[statSave.Key] = statSave.GrowthRate;
            }
        }

        public UnitStatsSave SaveData()
        {
            // Return current stats in save format
            var save = new UnitStatsSave
            {
                coreStats = new System.Collections.Generic.List<StatSave>()
            };

            foreach (var key in new[]
            {
                GameConst.StatKeys.Vitality,
                GameConst.StatKeys.Might,
                GameConst.StatKeys.Finesse,
                GameConst.StatKeys.Guard,
                GameConst.StatKeys.Focus,
                GameConst.StatKeys.Spirit
            })
            {
                var stat = Stats.Get(key);
                _growthRates.TryGetValue(key, out int growthRate);

                save.coreStats.Add(new StatSave
                {
                    Key = key,
                    BaseValue = stat.BaseValue,
                    GrowthRate = growthRate
                });
            }

            return save;
        }
    }

    [Serializable]
    public class StatSave
    {
        public StatKey Key;
        public float BaseValue = 0;
        public int GrowthRate = 0;

        public override string ToString()
        {
            return $"{Key}:{BaseValue} (Growth:{GrowthRate})";
        }
    }

    [Serializable]
    public class UnitStatsSave
    {
        public List<StatSave> coreStats;
    }
}