using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Item
{
    [CreateAssetMenu(menuName = "Item System/Database/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemSO> items;

        private Dictionary<int, ItemSO> lookup;

        public void Initialize()
        {
            lookup = new Dictionary<int, ItemSO>();
            foreach(ItemSO item in items)
            {
                lookup[item.itemId] = item;
            }
        }

        public ItemSO GetItem(int itemId)
        {
            return lookup.TryGetValue(itemId, out var item) ? item : null;
        }
    }

}

