using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Systems.Effect
{
    [CreateAssetMenu(menuName = "EffectInstance System/EffectInstance/EffectInstance Data")]
    public class EffectSO : ScriptableObject
    {
        public int EffectId;
        public string EffectName;
        [TextArea]
        public string description;

        public List<EffectTemplate> Templates = new();

        [HideInInspector]
        public List<TriggerGroup> Triggers = new();

        public int BaseDuration;

        [ContextMenu("Rebuild Effect")]
        public void Rebuild()
        {
            Triggers.Clear();
            foreach (var t in Templates)
                t.Build(this);
        }
    }
}