using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    public LayerMask obstacleMask;

    private Node[,] grid;

    void Awake()
    {
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPoint = GetWorldPosition(x, y);
                bool walkable = !Physics.CheckSphere(worldPoint, cellSize * 0.4f, obstacleMask);
                grid[x, y] = new Node(new Vector2Int(x, y), worldPoint, walkable);
            }
        }
    }

    public Node GetNodeFromWorld(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.z / cellSize);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return grid[x, y];
    }

    public List<Node> GetNeighbors(Node node)
    {
        var neighbors = new List<Node>();
        var dirs = new Vector2Int[] {
            new(0, 1), new(1, 0), new(0, -1), new(-1, 0)
        };

        foreach (var dir in dirs)
        {
            int nx = node.gridPos.x + dir.x;
            int ny = node.gridPos.y + dir.y;

            if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                neighbors.Add(grid[nx, ny]);
        }

        return neighbors;
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSize + cellSize / 2, 0, y * cellSize + cellSize / 2);
    }

    private void OnDrawGizmos()
    {
        if (grid == null) return;

        foreach (Node node in grid)
        {
            Gizmos.color = node.walkable ? Color.blue : Color.red;
            Gizmos.DrawCube(node.worldPos, Vector3.one * (cellSize - 0.1f));
        }
    }
}
