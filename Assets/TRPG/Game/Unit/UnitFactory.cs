using System;
using System.Collections;
using System.Collections.Generic;
using TRPG.Core.Database;
using TRPG.Core.Event;
using TRPG.Core.Stats;
using TRPG.Core.Utility;
using TRPG.Game.Systems.Rank;
using TRPG.Game.Systems.Stats;
using UnityEngine;
#nullable enable

namespace TRPG.Game.Unit
{
    public class UnitTemplate : IDataEntry
    {
        public int ID { get; }
        public string Name { get; }
        public UnitStatsSave Stats { get; }
        public UnitRankSaveData Ranks { get; }

        public UnitTemplate(int id, string name, UnitStatsSave unitStats, UnitRankSaveData unitRanks)
        {
            ID = id;
            Name = name;
            Stats = unitStats;
            Ranks = unitRanks;
        }
    }

    // =========================================================
    // UnitFactory
    // =========================================================
    // Responsible for:
    //   1. Building a TRPGUnit from a UnitTemplate
    //   2. Wiring dependencies (registry, random, bus)
    //   3. Calling Initialize() before returning
    //
    // Does NOT assign IDs — the caller controls ID allocation.
    // (IDs come from GameConst.Id.UnitStart and up.)
    // =========================================================

    public sealed class UnitFactory
    {
        private readonly ModifierRuleRegistry _registry;
        private readonly IRandomProvider _random;
        private readonly EventBus _bus;

        public UnitFactory(
            ModifierRuleRegistry registry,
            IRandomProvider random,
            EventBus bus)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>
        /// Creates a fully initialized TRPGUnit from a template.
        /// 
        /// weaponPower / spellPower are optional dynamic lookups
        /// injected by the equipment system when it's built.
        /// Pass null for now — derived stats will use 0.
        /// </summary>
        public TRPGUnit Create(
            int id,
            UnitTemplate template,
            Func<StatCollection, float>? weaponPower = null,
            Func<StatCollection, float>? spellPower = null)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            // 1. Build stat component from template's save data
            var statComponent = new UnitStatComponent(
                _registry,
                _random,
                template.Stats,
                weaponPower,
                spellPower
            );

            // 2. Build health component — depends on stats for MaxHP
            var healthComponent = new UnitHealthComponent(id, statComponent, _bus);

            // 3. Assemble unit
            var unit = new TRPGUnit(id, template.Name, statComponent, healthComponent);

            // 4. Initialize all components (fills HP to max, etc.)
            unit.Initialize();

            return unit;
        }
    }
}