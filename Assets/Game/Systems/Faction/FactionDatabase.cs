using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Systems.Faction
{
    [CreateAssetMenu(menuName = "Unit System/Faction Database")]
    public class FactionDatabase : ScriptableObject
    {
        public List<FactionData> factions;

        private Dictionary<int, FactionData> lookupTable;

        public void Initialize()
        {
            lookupTable = new Dictionary<int, FactionData>();
            foreach (var e in factions)
                lookupTable[e.factionId] = e;
        }

        public FactionData GetFaction(int id)
        {
            if (lookupTable == null) Initialize();
            return lookupTable.TryGetValue(id, out var e) ? e : null;
        }

        public FactionRelationshipTag GetRelationshipTag(int a, int b) 
        {
            var f = GetFaction(a);
            return f.GetFactionRelationship(b);
        }



    }

    public static class FactionExtension
    {
        public static bool IsAlly(this FactionDatabase db, int a, int b)
            => db.GetRelationshipTag(a, b) == FactionRelationshipTag.isAlly;
        public static bool IsEnemy(this FactionDatabase db, int a, int b)
            => db.GetRelationshipTag(a,b) == FactionRelationshipTag.isEnemy;
        public static bool IsNeutral(this FactionDatabase db, int a, int b)
            => db.GetRelationshipTag(a, b) == FactionRelationshipTag.isNeutral;
    }



}

