using Game.Systems.Job;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Systems.BattleMap
{
    [Serializable]
    public enum MoveType
    {
        Infantry,
        Calvary,
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
        public int cost = 0;
    }

    [Serializable]
    public class CostTable
    {
        public List<CostEntry> costs =new();

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
    }

    [Serializable]
    public class  TileInteractionSet
    {
        public List <ActionData> interactions =new();

        public void AddAction(ActionData action) { interactions.Add(action);}

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
        bool IsWalkable(Vector2Int pos);
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
        public HashSet<Vector2Int> GetReachableTiles(Vector2Int start, int moveRange, MovementProfileData profile)
        {
            var reachable = new HashSet<Vector2Int>();
            var frontier = new Queue<(Vector2Int pos, int remainingMove)>();

            frontier.Enqueue((start, moveRange));
            reachable.Add(start);

            while (frontier.Count > 0)
            {
                var (current, remainingMove) = frontier.Dequeue();

                foreach (var neighbor in grid.GetNeighbors(current))
                {
                    if (!grid.IsWalkable(neighbor))
                        continue;

                    int cost = GetTileCost(grid.TryGetNode(neighbor, out var tile) ? tile : null, profile);

                    if (cost < 0 || cost > remainingMove)
                        continue;

                    if (reachable.Contains(neighbor))
                        continue;

                    // Check if the unit can enter from current to neighbor
                    if (!grid.CanEnter(current, neighbor))
                        continue;

                    reachable.Add(neighbor);
                    frontier.Enqueue((neighbor, remainingMove - cost));
                }
            }

            return reachable;
        }

        private int GetTileCost(TileInstance tile, MovementProfileData profile)
        {
            if (tile == null) return -1;

            var costTable = grid.GetMoveCostTable(tile.MapPosition, tile.MapPosition);
            if (costTable.TryGetCost(profile.MoveType, out var cost))
                return cost;
            return -1;
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
