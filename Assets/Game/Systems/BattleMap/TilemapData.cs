using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.BattleMap
{
    [CreateAssetMenu(fileName = "NewTileMap", menuName = "Tilemap/TileMapData")]
    public class TilemapData : ScriptableObject
    {
        public int width = 16;
        public int height = 16;

        public List<Sprite> sprites;

        // Unity can serialize 1D arrays. Not jagged arrays.
        public TileCellData[] cells;

        [Serializable]
        public struct TileCellData
        {
            public int tileId;     // reference into TileDatabase
            public int spriteId;   // index into sprites list
        }

        // --- Initialization ---
        public void InitCells()
        {
            if (cells == null || cells.Length != width * height)
            {
                cells = new TileCellData[width * height];
            }
        }

        // --- Helpers ---
        public int Index(int x, int y) => y * width + x;

        public TileCellData GetCell(int x, int y)
        {
            return cells[Index(x, y)];
        }

        public void SetCell(int x, int y, TileCellData data)
        {
            cells[Index(x, y)] = data;
        }
    }
}
