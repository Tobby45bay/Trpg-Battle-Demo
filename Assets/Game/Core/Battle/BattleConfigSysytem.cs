using Game.Systems.BattleMap;
using Game.Systems.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Battle
{
    [Serializable]
    public class BattleConfigData
    {
        public TilemapData mapData;
        public int PlayerUnitLimt;
        public List<Vector2Int> StartingTiles;
        public List<UnitSaveData> UnitsInMap;
    }


    public class BattleLoader
    {
        private BattleConfigData currentConfig;
        private BattleManager _battleManager;

        public void Start(BattleConfigData config,BattleManager battleManager)
        {
            _battleManager = battleManager;
            currentConfig = config;
        }

    }
}
