using Game.Systems.Stat;
using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Systems.Effect
{
    public abstract class EffectTemplate : ScriptableObject
    {
        public abstract void Build(EffectSO effect);
    }




}

