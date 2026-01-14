using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.BattleMap
{
    public class UnitView : MonoBehaviour
    {
        public int unitId;
        public SpriteRenderer spriteRenderer;

        public void Initialize(int id, Sprite sprite)
        {
            unitId = id;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
        }

        public void MoveToTile(Vector3 wordPos)
        {
            transform.position = wordPos;
        }

    }
}

