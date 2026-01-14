using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Systems.Units
{
    [CreateAssetMenu(fileName = "UnitSpriteDatabase", menuName = "Unit System/UnitSpriteDatabase")]
    public class UnitSpriteDatabase : ScriptableObject
    {
        [Serializable]
        public struct UnitSprites
        {
            public int unitId;
            public Sprite battleSprite;
            public Sprite portraitSprite;
        }

        public List<UnitSprites> unitSprites;

        public Sprite GetBattleSprite(int unitId)
        {
            var entry = unitSprites.Find(u => u.unitId == unitId);
            return entry.battleSprite;
        }
    }
}