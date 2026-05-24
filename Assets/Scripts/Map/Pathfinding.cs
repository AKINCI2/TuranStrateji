using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance;

    void Awake()
    {
        Instance = this;
    }

    public List<HexCell> FindPath(HexCell start, HexCell target)
    {
        if (start == null || target == null) return null;
        if (!start.IsWalkable() || !target.IsWalkable()) return null;
        if (start == target) return new List<HexCell>();

        List<HexCell> openSet = new List<HexCell>();
        HashSet<HexCell> closedSet = new HashSet<HexCell>();

        ResetPathfindingValues(start);
        start.gCost = 0;
        start.hCost = GetDistance(start, target);
        start.CalculateFCost();

        openSet.Add(start);

        while (openSet.Count > 0)
        {
            HexCell current = GetLowestFCost(openSet);

            if (current == target)
            {
                return RetracePath(start, target);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (HexCell neighbor in current.neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor)) continue;
                if (!neighbor.IsWalkable()) continue;

                int movementCost = neighbor.GetMovementCost();
                if (movementCost == int.MaxValue)
                    continue;

                int tentativeGCost = current.gCost + movementCost;

                if (tentativeGCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = GetDistance(neighbor, target);
                    neighbor.CalculateFCost();
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    HexCell GetLowestFCost(List<HexCell> list)
    {
        HexCell lowest = list[0];
        foreach (HexCell cell in list)
        {
            if (cell.fCost < lowest.fCost ||
                cell.fCost == lowest.fCost && cell.hCost < lowest.hCost)
            {
                lowest = cell;
            }
        }
        return lowest;
    }

    List<HexCell> RetracePath(HexCell start, HexCell end)
    {
        List<HexCell> path = new List<HexCell>();
        HexCell current = end;

        while (current != start)
        {
            if (current == null || current.parent == null)
                return null;

            path.Add(current);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    int GetDistance(HexCell a, HexCell b)
    {
        return a.GetDistance(b);
    }

    void ResetPathfindingValues(HexCell start)
    {
        HashSet<HexCell> visited = new HashSet<HexCell>();
        Queue<HexCell> queue = new Queue<HexCell>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            HexCell current = queue.Dequeue();

            current.gCost = int.MaxValue;
            current.hCost = 0;
            current.fCost = int.MaxValue;
            current.parent = null;

            foreach (HexCell neighbor in current.neighbors)
            {
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
}

