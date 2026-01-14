using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.BattleMap
{
    [CreateAssetMenu(menuName = "Tilemap/Tile Data")]
    public class TileData: ScriptableObject
    {
        public int tileId;
        public string tileName;
        public CostTable costTable;
        public bool isPassable = true;

        public TriggerGroup onUnitEnter;
        public TriggerGroup onUnitExit;
    }
}
