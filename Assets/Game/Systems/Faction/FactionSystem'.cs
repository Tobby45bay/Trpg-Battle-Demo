using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Systems.Faction
{
    public enum FactionRelationshipTag
    {
        isAlly,
        isNeutral,
        isEnemy
    }
    [Serializable]
    public class FactionRelationship
    {
        public int otherFactionId;
        public FactionRelationshipTag relationshipTag;

        public FactionRelationship(int targetFactionId, FactionRelationshipTag relationshipTag)
        {
            this.otherFactionId = targetFactionId;
            this.relationshipTag = relationshipTag;
        }
    }
}

