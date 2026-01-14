using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Item
{
    public abstract class ItemSO : ScriptableObject
    {
        [Header("Identity")]
        public int itemId;
        public string itemName;
        [TextArea]
        public string description;

        public ItemType type;

        public int maxUse = -1;
    }
}

