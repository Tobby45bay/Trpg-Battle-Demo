using Game.Systems.Job;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Game.Core.Game.GameManager;
using static UnityEngine.UI.CanvasScaler;

namespace Game.Systems.BattleMap
{
    [Serializable]
    public enum MoveType
    {
        Infantry,
        Mounted,
        Flying,
        Armored,
        None
    }

    public enum TerrainInteraction
    {
        Normal,
        ReducedCost,
        Ignore,
        Blocked
    }


    [Serializable]
    public class CostEntry
    {
        public MoveType moveType;
        public TerrainInteraction TerrainInteraction;
        public int cost = 0;
    }

    [Serializable]
    public class CostTable
    {
        public List<CostEntry> costs = new();

        public bool TryGetCost(MoveType moveType, out int cost)
        {
            for (int i = 0; i < costs.Count; i++)
            {
                if (costs[i].moveType == moveType)
                {
                    cost = costs[i].cost;
                    return true;
                }
            }

            cost = default;
            return false;
        }

        public bool TryGetTerrianInteractin(MoveType moveType, out TerrainInteraction terrainInteraction)
        {
            for (int i = 0; i < costs.Count; i++)
            {
                if (costs[i].moveType == moveType)
                {
                    terrainInteraction = costs[i].TerrainInteraction;
                    return true;
                }
            }

            terrainInteraction = default;
            return false;
        }
    }

    [Serializable]
    public class TileInteractionSet
    {
        public List<ActionData> interactions = new();

        public void AddAction(ActionData action) { interactions.Add(action); }

        public void Interact(TriggerContext context)
        {
            foreach (var action in interactions)
            {
                action.Execute(context);
            }
        }
    }

    public class TileInstance
    {
        public Vector2Int MapPosition;
        public int TileId;
        public readonly TileData tileData;
        public Sprite sprite;
        public bool IsVisable;
        public int? UnitOnTile;
        public bool IsOccupied => UnitOnTile >= 0;
        public TileInteractionSet TileInterface;
        public TileView TileView;

        public TileInstance(TileData tileData, Vector2Int tilePos, int mapWidth)
        {
            this.tileData = tileData;
            MapPosition = tilePos;

            // Unique deterministic ID based on position
            TileId = tilePos.y * mapWidth + tilePos.x;
        }
    }

    public interface IGridProvider<TNode>
    {
        bool TryGetNode(Vector2Int pos, out TNode node);
        IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos);
        CostTable GetMoveCostTable(Vector2Int from, Vector2Int to);
        bool IsWalkable(Vector2Int pos, MoveType moveType);
        bool CanEnter(Vector2Int from, Vector2Int to);
    }

    public class Pathfinder
    {
        public enum TargetShape
        {
            Single,
            Square,
            Rectangle,
            Diamond,
        }

        private readonly IGridProvider<TileInstance> grid;

        public Pathfinder(IGridProvider<TileInstance> grid)
        {
            this.grid = grid;
        }

        public Dictionary<Vector2Int, int> GetReachableTiles(Vector2Int start, int moveRange, MovementProfileData movementProfileData)
        {
            // Track best remaining movement per tile (FE-style)
            var bestRemaining = new Dictionary<Vector2Int, int>();
            var frontier = new Queue<(Vector2Int pos, int remainingMove)>();

            frontier.Enqueue((start, moveRange));
            bestRemaining[start] = moveRange;

            while (frontier.Count > 0)
            {
                var (current, remaining) = frontier.Dequeue();

                foreach (var neighbor in grid.GetNeighbors(current))
                {
                    // Entry rules (doors, cliffs, unit blocking, etc.)
                    if (!grid.CanEnter(current, neighbor))
                        continue;

                    // Movement type rules (flying, mounted, etc.)
                    if (!grid.IsWalkable(neighbor, movementProfileData.MoveType))
                        continue;

                    if (!grid.TryGetNode(neighbor, out var tile))
                        continue;

                    int cost = GetTileCost(
                        tile,
                        movementProfileData.MoveType,
                        movementProfileData.TerrainInteraction);

                    if (cost < 0 || cost > remaining)
                        continue;

                    int nextRemaining = remaining - cost;

                    // Only keep the best path to each tile
                    if (bestRemaining.TryGetValue(neighbor, out int best) &&
                        best >= nextRemaining)
                        continue;

                    bestRemaining[neighbor] = nextRemaining;
                    frontier.Enqueue((neighbor, nextRemaining));
                }
            }

            bestRemaining.TryAdd(start, moveRange);

            return bestRemaining;
        }

        public List<Vector2Int> GetAttackableTiles(Vector2Int origin, int minRange, int maxRange)
        {
            var open = new Queue<Vector2Int>();
            var visited = new Dictionary<Vector2Int, int>();
            var inRange = new HashSet<Vector2Int>();

            open.Enqueue(origin);
            visited[origin] = 0;

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                int dist = visited[current];

                if (dist >= maxRange)
                    continue;

                foreach (var neighbor in grid.GetNeighbors(current)) // ✅ FIX
                {
                    int nextDist = dist + 1;

                    if (nextDist > maxRange)
                        continue;

                    if (visited.ContainsKey(neighbor))
                        continue;

                    visited[neighbor] = nextDist;
                    open.Enqueue(neighbor);

                    if (nextDist >= minRange)
                    {
                        inRange.Add(neighbor);
                    }
                }
            }

            return inRange.ToList();
        }


        public List<Vector2Int> GetAttackableTiles(List<Vector2Int> origins,int minRange, int maxRange)
        {
            var open = new Queue<Vector2Int>();
            var visited = new Dictionary<Vector2Int, int>();
            var inRange = new HashSet<Vector2Int>();

            // Seed BFS with all origins
            foreach (var origin in origins)
            {
                if (!visited.ContainsKey(origin))
                {
                    open.Enqueue(origin);
                    visited[origin] = 0;
                }
            }

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                int dist = visited[current];

                if (dist >= maxRange)
                    continue;

                foreach (var neighbor in grid.GetNeighbors(current))
                {
                    int nextDist = dist + 1;

                    if (nextDist > maxRange)
                        continue;

                    // Keep shortest distance to each tile
                    if (visited.TryGetValue(neighbor, out int best) &&
                        best <= nextDist)
                        continue;

                    visited[neighbor] = nextDist;
                    open.Enqueue(neighbor);

                    if (nextDist >= minRange)
                    {
                        inRange.Add(neighbor);
                    }
                }
            }

            return inRange.ToList();
        }



        private int GetTileCost(TileInstance tile, MoveType moveType, TerrainInteraction terrainInteraction)
        {
            if (tile == null)
                return -1;

            var costTable = grid.GetMoveCostTable(
                tile.MapPosition,
                tile.MapPosition);

            // No cost entry = cannot traverse
            if (!costTable.TryGetCost(moveType, out int baseCost))
                return -1;

            int movementCost = baseCost;

            switch (terrainInteraction)
            {
                case TerrainInteraction.Normal:
                    // Use base cost
                    break;

                case TerrainInteraction.ReducedCost:
                    movementCost = Mathf.Max(1, baseCost / 2);
                    break;

                case TerrainInteraction.Ignore:
                    movementCost = 1;
                    break;

                case TerrainInteraction.Blocked:
                    return -1;
            }

            return movementCost;
        }
    }


    public class CursorController
    {
        private readonly TilemapManager tm;
        private readonly TileCursor cursor;

        public CursorController(TilemapManager tm, TileCursor cursor)
        {
            this.tm = tm;
            this.cursor = cursor;
        }

        public void Move(Vector2Int dir)
        {
            var next = cursor.Position + dir;
            if (tm.HasTile(next))
                cursor.Set(next);
        }

        public void SnapTo(Vector2Int pos)
        {
            if (tm.HasTile(pos))
                cursor.Set(pos);
        }

        public bool TryGetHoveredTile(out TileInstance tile)
        {
            return tm.TryGetTile(cursor.Position, out tile);
        }

        public bool TryGetHoveredUnit(out int unitId)
        {
            unitId = -1;
            if (!TryGetHoveredTile(out var tile)) return false;

            if (!tile.IsOccupied) return false;

            unitId = (int)tile.UnitOnTile;
            return true;
        }

        public Vector2Int GetCursorPostion() => cursor.Position;
    }

    public class TileCursor
    {
        public Vector2Int Position { get; private set; }

        public TileCursor(Vector2Int startPos)
        {
            Position = startPos;
        }

        internal void Set(Vector2Int pos)
        {
            Position = pos;
        }
    }

    public class TilemapListener : TriggerListener
    {
        public TilemapListener()
        {
            system = "Tilemap";
            priority = 0;
        }
        public override void Trigger(TriggerContext context)
        {

        }
    }


}