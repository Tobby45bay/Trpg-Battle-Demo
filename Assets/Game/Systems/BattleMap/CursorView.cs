using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.BattleMap
{
    public class CursorView : MonoBehaviour
    {
        [SerializeField] Transform visual;
        public TilemapManager tilemapManager;

        public TileCursor Cursor { get; private set; }

        public void Bind(TileCursor cursor)
        {
            Cursor = cursor;
        }

        private void LateUpdate()
        {
            if (Cursor == null) return;

            visual.position = Vector3.Lerp
                (visual.position,tilemapManager.GetWorldPosition(Cursor.Position), Time.deltaTime * 20f);
        }
    }
}

