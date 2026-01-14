using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Game.Systems.Item
{
    [CreateAssetMenu(menuName = "Item System/Items/Weapon")]
    public class WeaponSO : ItemSO
    {
        [Header("Weapon Properties")]
        public WeaponType WeaponType;
        public int DefaultAttack;

        public int minRange = 1;
        public int maxRange = 1;

        [Header("Trigger")]
        public TriggerGroup onEquip;
        public TriggerGroup onRemove;
        public TriggerGroup onAttack;
        public TriggerGroup onDefense;
    }
}