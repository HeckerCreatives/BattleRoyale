using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector2Int gridPos;
    public Vector3 worldPos;
    public bool walkable;

    public int gCost;
    public int hCost;
    public int FCost => gCost + hCost;
    public Node parent;

    public Node(Vector2Int gridPos, Vector3 worldPos, bool walkable)
    {
        this.gridPos = gridPos;
        this.worldPos = worldPos;
        this.walkable = walkable;
    }
}
