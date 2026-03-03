
using System;

namespace TRPG.Game.Systems.Items
{
    public enum WeaponType
    {
        Sword,
        Axe,
        Spear,
        Ranged,
        None
    }

    [Serializable]
    public class ItemSave
    {
        public int ItemID;
        public string Name;
        public int uses;
        public bool isEquipped;
    }
}
