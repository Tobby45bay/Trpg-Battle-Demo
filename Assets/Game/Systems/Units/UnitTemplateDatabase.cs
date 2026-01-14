using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Systems.Units
{
    [CreateAssetMenu(menuName = "Unit System/Unit Database")]
    public class UnitTemplateDatabase : ScriptableObject
    {
        public List<UnitTemplate> templates;

        private Dictionary<int, UnitTemplate> lookupTable;

        public void Initialize()
        {
            lookupTable = new Dictionary<int, UnitTemplate>();
            foreach (var e in templates)
                lookupTable[e.id] = e;
        }

        public UnitTemplate GetTeamplate(int id)
        {
            if (lookupTable == null) Initialize();
            return lookupTable.TryGetValue(id, out var e) ? e : null;
        }


    }
}
