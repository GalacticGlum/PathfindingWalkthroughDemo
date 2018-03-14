using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tilegraph
{
    public Dictionary<Tile, Node<Tile>> Nodes { get; }
    private readonly WorldController worldController;

    public Tilegraph(WorldController worldController)
    {
        this.worldController = worldController;
        Nodes = new Dictionary<Tile, Node<Tile>>(worldController.Width * worldController.Height);

        for (int x = 0; x < worldController.Width; x++)
        {
            for (int y = 0; y < worldController.Height; y++)
            {
                Tile tile = worldController.GetTileAt(x, y);
                Nodes[tile] = new Node<Tile>(tile);
            }
        }

        foreach (Node<Tile> node in Nodes.Values)
        {
            GenerateEdges(node);
        }
    }

    public void Regenerate(Tile tile)
    {
        if (tile == null) return;

        Node<Tile> node = Nodes[tile];
        GenerateEdges(node);

        // Regenerate edges for all adjacent tiles as well
        foreach (Tile neighbour in worldController.GetNeighbours(tile, true))
        {
            if(neighbour == null) continue;
            GenerateEdges(Nodes[neighbour]);
        }
    }

    public TilePath FindPath(Tile startTile, Tile endTile)
    {
        if (startTile == null || endTile == null) return null;
        if (!Nodes.ContainsKey(startTile))
        {
            Debug.LogError("Tilegraph::FindPath: The starting tile (param: Tile startTile) isn't in the list of nodes!");
            return null;
        }

        Node<Tile> startNode = Nodes[startTile];
        if (!Nodes.ContainsKey(endTile))
        {
            Debug.LogError("Tilegraph::FindPath: The goal tile (param: Tile endTile) isn't in the list of nodes!");
            return null;
        }

        Node<Tile> endNode = Nodes[endTile];

        Pathfinder pathfinder = new Pathfinder(startNode, endNode);
        return pathfinder.FindPath();
    }

    private void GenerateEdges(Node<Tile> node)
    {
        Tile[] neighbours = worldController.GetNeighbours(node.Data, true);
        node.Edges = (from neighbour in neighbours where neighbour != null && neighbour.MovementCost > 0 && !IsClippingCorner(node.Data, neighbour) select new Edge<Tile>(neighbour.MovementCost, Nodes[neighbour])).ToArray();
    }

    private bool IsClippingCorner(Tile current, Tile neighbour)
    {
        Vector2Int delta = current.Position - neighbour.Position;
        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 2) return false;
        if (worldController.GetTileAt(current.Position.x - delta.x, current.Position.y).MovementCost == 0) return true;

        return worldController.GetTileAt(current.Position.x, current.Position.y - delta.y).MovementCost == 0;
    }
}