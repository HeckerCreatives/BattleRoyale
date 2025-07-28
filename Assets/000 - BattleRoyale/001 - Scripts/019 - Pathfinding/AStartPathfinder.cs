using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStartPathfinder
{
    public static List<Node> FindPath(GridManager gridManager, Vector3 startWorld, Vector3 targetWorld)
    {
        var grid = gridManager;
        Node start = grid.GetNodeFromWorld(startWorld);
        Node target = grid.GetNodeFromWorld(targetWorld);

        if (!start.walkable || !target.walkable)
            return null;

        var openSet = new List<Node> { start };
        var closedSet = new HashSet<Node>();

        start.gCost = 0;
        start.hCost = GetDistance(start, target);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            foreach (var node in openSet)
                if (node.FCost < current.FCost || (node.FCost == current.FCost && node.hCost < current.hCost))
                    current = node;

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == target)
                return RetracePath(start, target);

            foreach (var neighbor in grid.GetNeighbors(current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                int newCost = current.gCost + GetDistance(current, neighbor);
                if (newCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCost;
                    neighbor.hCost = GetDistance(neighbor, target);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    static int GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridPos.x - b.gridPos.x);
        int dy = Mathf.Abs(a.gridPos.y - b.gridPos.y);
        return dx + dy;
    }

    static List<Node> RetracePath(Node start, Node end)
    {
        var path = new List<Node>();
        Node current = end;
        while (current != start)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }
}
