using Game.Systems.Item;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Systems.Combat
{
    [CreateAssetMenu(menuName = "EffectInstance System/EffectInstance/Attack Data")]
    public class AttackData : ScriptableObject
    {
        public int AttackId;
        [Header("Requirements")]
        public WeaponType RequiredWeapon;

        public List<Tag> tags;

        [Header("Targeting")]
        public TargetData TargetData;

        [Header("Primary Damage")]
        public DamageEffectData PrimaryDamage;

        [Header("Primary Effects (On Hit)")]
        public List<ActionData> OnHitEffects;

        [Header("Critical Effects")]
        public List<ActionData> OnCriticalEffects;

        [Header("Secondary Effects")]
        public SecondaryEffectData SecondaryEffects;

        public int UseCost;
    }

    [System.Serializable]
    public class SecondaryEffectData
    {
        public int BaseChance; // additive
        public List<ActionData> Effects;
    }
}


