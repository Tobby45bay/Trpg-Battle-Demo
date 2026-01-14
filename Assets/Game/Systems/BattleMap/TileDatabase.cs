using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.BattleMap
{
    [CreateAssetMenu(menuName = "Tilemap/Tile Database")]
    public class TileDatabase : ScriptableObject
    {
        public List<TileData> tiles = new();
        private Dictionary<int, TileData> lookupTable;

        public void Initialize()
        {
            lookupTable = new Dictionary<int, TileData>();
            foreach(var tile in tiles)
            {
                if(tile == null) continue;
                if (!lookupTable.ContainsKey(tile.tileId))
                    lookupTable.Add(tile.tileId, tile);
                else
                    Debug.LogWarning($"Duplicate Tile ID detected:{tile.tileId}");
            }
        }

        public TileData GetTileData(int tileId)
        {
            if(lookupTable == null) Initialize();
            lookupTable.TryGetValue(tileId, out TileData tileData);
            return tileData;
        }

        public List<TileData> GetAllTiles() => tiles;
    }
}
