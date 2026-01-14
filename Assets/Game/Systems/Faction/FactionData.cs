using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game.Systems.Faction
{
    [CreateAssetMenu(menuName = "Unit System/Faction Data")]
    public class FactionData : ScriptableObject
    {
        public int factionId;
        public string factionName;
        public List<FactionRelationship> relationships = new();

        public void SetRelationship(int targetFactionId, FactionRelationshipTag relationshipTag)
        {
            var rel = relationships.FirstOrDefault(r => r.otherFactionId == targetFactionId);
            if (rel == null)
            {
                relationships.Add(new FactionRelationship(targetFactionId, relationshipTag));
            }
            else
            {
                rel.relationshipTag = relationshipTag;
            }
        }

        public FactionRelationshipTag GetFactionRelationship(int targetFactionId)
        {
            var rel = relationships.Find(x => x.otherFactionId == targetFactionId);
            return rel.relationshipTag;
        }
    }
}
