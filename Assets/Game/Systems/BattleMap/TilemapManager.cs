using Game.Core.Battle;
using Game.Core.Game;
using Game.Systems.Effect;
using Game.Systems.Trigger;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static Game.Systems.BattleMap.TilemapData;

namespace Game.Systems.BattleMap
{
    public class TilemapManager : MonoBehaviour,IGridProvider<TileInstance>
    {
        [Header("Unity Tilemap")]
        public Tilemap tilemap;
        private TileBase[] tilePalette;

        [Header("Data")]
        private TileDatabase tileDatabase;
        private TilemapData mapData;

        [Header("Runtime State")]
        private readonly Dictionary<Vector2Int, TileInstance> tiles = new();
        public Dictionary<int, Vector2Int> units = new();
        public Dictionary<int , UnitView> unitsViews = new();
        private BattleMapCamera battleCamera;                           

        public TileCursor cursor;
        public CursorController cursorController;
        public GameObject CursorPrefab;

        public float tileUnitSize = 1f;
        public GameObject unitPrefab;
        public GameObject TileViewPrefab;

        // Reference to all systems
        private BattleSystems battleSystems;
        private GameManager gameManager;

        // ---------------------------------------------------------------
        // INITIALIZATION
        // ---------------------------------------------------------------

        public void Initialize(GameManager gm, BattleSystems systems)
        {
            tileDatabase = gm.TileDb;
            battleSystems = systems;
            gameManager = gm;
            gm.triggerDispatcher.Register(new TilemapListener());

            // Grid
            if (!TryGetComponent<Grid>(out _))
            {
                Grid grid = gameObject.AddComponent<Grid>();
                grid.cellSize = Vector3.one;
            }

            // Tilemap
            if (!TryGetComponent<Tilemap>(out tilemap))
            {
                tilemap = gameObject.AddComponent<Tilemap>();
                if (GetComponent<TilemapRenderer>() == null)
                    gameObject.AddComponent<TilemapRenderer>();
            }

            // Cursor
            var cursorInstance = Instantiate(CursorPrefab, transform);
            cursor = new TileCursor(Vector2Int.zero);
            cursorController = new CursorController(this, cursor);


            if (cursorInstance.TryGetComponent(out CursorView cursorView))
            {
                cursorView.tilemapManager = this;
                cursorView.Bind(cursor);
            }
            else
            {
                Debug.LogError("CursorPrefab is missing CursorView!");
                return;
            }

            // Camera
            if (Camera.main != null &&
                Camera.main.TryGetComponent<BattleMapCamera>(out var battleCamera))
            {
                this.battleCamera = battleCamera;
                this.battleCamera.Initialize(cursorView.transform, tilemap);
            }
        }

        // ---------------------------------------------------------------
        // LOAD MAP
        // ---------------------------------------------------------------

        public void LoadMap(TilemapData tilemapData)
        {
            mapData = tilemapData;

            GenerateTilePaletteFromSprites();


            tiles.Clear();  // ResetSelection dictionary

            for (int y = 0; y < tilemapData.height; y++)
            {
                for (int x = 0; x < tilemapData.width; x++)
                {
                    Vector2Int pos = new(x, y);

                    // Get cell data
                    TilemapData.TileCellData cell = tilemapData.GetCell(x, y);

                    // Get tile data from database
                    TileData tileData = tileDatabase.GetTileData(cell.tileId);

                    // Create TileInstance and store in dictionary
                    TileInstance instance = new(tileData, pos,tilemapData.width);
                    tiles[pos] = instance;

                    // Set sprite on Tilemap
                    if (cell.spriteId >= 0 && cell.spriteId < tilePalette.Length)
                        tilemap.SetTile((Vector3Int)pos, tilePalette[cell.spriteId]);

                    var tileViewGO = Instantiate(TileViewPrefab, transform);
                    var tileView = tileViewGO.GetComponent<TileView>();

                    tileViewGO.transform.position =
                        GetWorldPosition(pos);

                    instance.TileView = tileView;
                    tileView.Bind(instance);
                    tileView.SetHighlight(HightlightState.None);
                }
            }

            battleCamera?.RefreshBounds();
        }
        public void GenerateTilePaletteFromSprites()
        {
            if (mapData == null || mapData.sprites == null || mapData.sprites.Count == 0)
            {
                Debug.LogWarning("No sprites in mapData to generate tile palette.");
                return;
            }

            tilePalette = new TileBase[mapData.sprites.Count];

            for (int i = 0; i < mapData.sprites.Count; i++)
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = mapData.sprites[i];
                tilePalette[i] = tile;
            }

            Debug.Log($"Generated {tilePalette.Length} Tiles from sprites.");
        }

        // ---------------------------------------------------------------
        // UNIT REGISTRATION / MOVEMENT
        // ---------------------------------------------------------------
        public void RegisterUnit(int unitInstanceId,int unitTemplateld, Vector2Int position)
        {
            if (!tiles.ContainsKey(position))
            {
                Debug.LogError($"Attempted to register unit at invalid tile {position}");
                return;
            }

            if (units.ContainsKey(unitInstanceId))
                return;

            SetUnitToTile(tiles[position], unitInstanceId);
            units[unitInstanceId] = position;

            var unitGO = Instantiate(unitPrefab,GetWorldPosition(position),Quaternion.identity);
            var unitView = unitGO.GetComponent<UnitView>();
            Sprite sprite = gameManager.UnitSpriteDb.GetBattleSprite(unitTemplateld);
            unitView.Initialize(unitInstanceId, sprite);

            unitsViews[unitInstanceId] = unitView;
        }

        public void MoveUnit(int unitId, Vector2Int targetPos)
        {
            if (!units.TryGetValue(unitId, out Vector2Int currentPos))
                return;

            if (!tiles.ContainsKey(targetPos)) return;

            TileInstance startTile = tiles[currentPos];
            TileInstance endTile = tiles[targetPos];

            RemoveUnitFromTile(startTile, unitId);
            SetUnitToTile(endTile, unitId);

            if(unitsViews.TryGetValue(unitId, out UnitView unitView))
            {
                unitView.MoveToTile(GetWorldPosition(targetPos));
            }

            units[unitId] = targetPos;
        }

        private void SetUnitToTile(TileInstance tileInstance, int unitId)
        {
            tileInstance.UnitOnTile = unitId;

            var actions = tileInstance.tileData.onUnitEnter.Actions;

            if (actions == null || actions.Count <= 0)
                return;

            TriggerExitOrEnter(unitId, tileInstance, tileInstance.tileData.onUnitEnter);
        }

        private void RemoveUnitFromTile(TileInstance tileInstance, int unitId)
        {
            tileInstance.UnitOnTile = -1;

            var actions = tileInstance.tileData.onUnitExit.Actions;
            if (actions == null || actions.Count <= 0)
                return;

            TriggerExitOrEnter(unitId, tileInstance, tileInstance.tileData.onUnitExit);
        }

        // ---------------------------------------------------------------
        // TRIGGER DISPATCHING
        // ---------------------------------------------------------------
        private void TriggerExitOrEnter(int unitId, TileInstance tile,TriggerGroup tg)
        {
            var ctx = new EffectContext()
            {
                battleSystems = battleSystems,
                triggerCall = tg.TriggerCall,
                SourceUnitId = unitId,
                TargetUnitId = unitId,
                SourceCode= GameManager.SourceType.Tile,
                SourceId = tile.TileId,
                SourceRefCode = GameManager.SourceType.Tile,
                SourceRefId = tile.TileId,
            };

            ActionHelper.ExecuteActions(tg.Actions, ctx);
        }

        // ---------------------------------------------------------------
        // Tile
        // ---------------------------------------------------------------
        public bool TryGetTile(Vector2Int pos, out TileInstance tile) => tiles.TryGetValue(pos, out tile);
        public bool HasTile(Vector2Int pos) => tiles.ContainsKey(pos);

        static readonly Vector2Int[] Dirs =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        public bool TryGetNode(Vector2Int pos, out TileInstance tile) => tiles.TryGetValue(pos, out tile);

        public IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            foreach(var d in Dirs)
            {
                var p = pos + d;
                if(tiles.ContainsKey(p))
                    yield return p;
            }
        }

        public CostTable GetMoveCostTable(Vector2Int from, Vector2Int to)
        {
            return tiles[to].tileData.costTable;
        }

        public bool IsWalkable(Vector2Int pos) => tiles.TryGetValue(pos, out var t) && t.tileData.isPassable;
        public bool CanEnter(Vector2Int from, Vector2Int to)
        {
            if (!tiles.TryGetValue(to, out var tile))
                return false;

            if (!tile.tileData.isPassable)
                return false;

            if (tile.UnitOnTile.HasValue) return false;

            return true;
        }
        public Vector3 GetWorldPosition(Vector2Int pos)
        {
            return tilemap.GetCellCenterWorld((Vector3Int)pos);
        }
    }

    public static class TilemapHelper
    {
        public static void SetHightlightOfTiles(List<TileInstance> tiles, HightlightState hightlight)
        {
            foreach(TileInstance tile in tiles)
            {
                if (tile.TileView != null)
                    tile.TileView.SetHighlight(hightlight);
            }
        }

    }
}
