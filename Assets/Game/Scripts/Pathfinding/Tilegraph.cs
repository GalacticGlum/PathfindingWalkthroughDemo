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
        HashSet<Node<Tile>> closedSet = new HashSet<Node<Tile>>();
        PathfindingPriorityQueue<Node<Tile>> openSet = new PathfindingPriorityQueue<Node<Tile>>(1);
        openSet.Enqueue(startNode, 0);

        Dictionary<Node<Tile>, Node<Tile>> cameFrom = new Dictionary<Node<Tile>, Node<Tile>>();

        startNode.GCost = 0;
        startNode.FCost = CalculateHeuristicCost(startNode, endNode);

        while (openSet.Count > 0)
        {
            TilePath result = ExecutePathfindingStep(openSet, closedSet, cameFrom, endNode);
            if (result != null) return result;
        }

        // At this point, we haven't found a path.
        return null;
    }

    private static TilePath ExecutePathfindingStep(PathfindingPriorityQueue<Node<Tile>> openSet, 
        HashSet<Node<Tile>> closedSet, Dictionary<Node<Tile>, Node<Tile>> cameFrom, Node<Tile> endNode)
    {
        Node<Tile> currentNode = openSet.Dequeue();
        if (currentNode == endNode)
        {
            return ConstructPath(cameFrom, currentNode);
        }

        closedSet.Add(currentNode);
        foreach (Edge<Tile> neighbouringEdge in currentNode.Edges)
        {
            Node<Tile> neighbor = neighbouringEdge.Node;
            if (closedSet.Contains(neighbor)) continue;

            float movementCostToNeighbor = neighbor.Data.MovementCost * DistanceBetween(currentNode, neighbor);
            float tentativeGScore = currentNode.GCost + movementCostToNeighbor;

            if (openSet.Contains(neighbor) && tentativeGScore >= neighbor.GCost) continue;

            cameFrom[neighbor] = currentNode;
            neighbor.GCost = tentativeGScore;
            neighbor.FCost = neighbor.GCost + CalculateHeuristicCost(neighbor, endNode);

            openSet.EnqueueOrUpdate(neighbor, neighbor.FCost);
        }

        return null;
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

    private static float CalculateHeuristicCost(Node<Tile> a, Node<Tile> b)
    {
        if (b == null) return 0;

        Vector2Int delta = a.Data.Position - b.Data.Position;
        return Mathf.Sqrt(Mathf.Pow(delta.x, 2) + Mathf.Pow(delta.y, 2));
    }

    private static float DistanceBetween(Node<Tile> a, Node<Tile> b)
    {
        Vector2Int delta = a.Data.Position - b.Data.Position;
        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1) return 1;

        // Diagonal neighbours have a distance of 1.41421356237 (square root of 2)
        if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.x) == 1) return 1.41421356237f;

        // Otherwise, do the actual math.
        return Mathf.Sqrt(Mathf.Pow(delta.x, 2) + Mathf.Pow(delta.y, 2));
    }

    private static TilePath ConstructPath(IDictionary<Node<Tile>, Node<Tile>> cameFrom, Node<Tile> current)
    {
        // At this point Current IS the goal.
        // What we want to do is walk backwards through the came from map, 
        // until we reach the our starting node.
        Queue<Tile> totalPath = new Queue<Tile>();
        totalPath.Enqueue(current.Data);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Enqueue(current.Data);
        }

        return new TilePath(totalPath.Reverse());
    }
}