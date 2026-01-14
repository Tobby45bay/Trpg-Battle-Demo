using Game.Systems.BattleMap;
using Game.Systems.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBattleConfig", menuName = "Battle/Battle Config")]
public class BattleConfigSO : ScriptableObject
{
    public TilemapData MapData;
    public int PlayerUnitLimt;
    public List<Vector2Int> StatingTiles;
    public List<UnitSaveData> UnitsOnMap;
}
