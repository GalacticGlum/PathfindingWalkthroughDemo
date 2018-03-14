using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class Pathfinder
{
    public Node<Tile> StartNode { get; }
    public Node<Tile> EndNode { get; }

    public bool SearchIsComplete => openSet.Count <= 0;
    public bool HasFoundPath => Path != null;

    public TilePath Path { get; private set; }

    private readonly PathfindingPriorityQueue<Node<Tile>> openSet;
    private readonly HashSet<Node<Tile>> closedSet;
    private readonly Dictionary<Node<Tile>, Node<Tile>> cameFrom;

    public Pathfinder(Node<Tile> startNode, Node<Tile> endNode)
    {
        StartNode = startNode;
        EndNode = endNode;

        closedSet = new HashSet<Node<Tile>>();
        openSet = new PathfindingPriorityQueue<Node<Tile>>(1);
        openSet.Enqueue(StartNode, 0);

        cameFrom = new Dictionary<Node<Tile>, Node<Tile>>();

        StartNode.GCost = 0;
        StartNode.HCost = CalculateHeuristicCost(StartNode, EndNode);
    }

    public TilePath FindPath()
    {
        while (openSet.Count > 0)
        {
            ExecutePathfindingStep();
            if (HasFoundPath) break;
        }

        return Path;
    }

    public void ExecutePathfindingStep()
    {
        Node<Tile> currentNode = openSet.Dequeue();
        if (currentNode == EndNode)
        {
            ConstructPath();
            return;
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
            neighbor.HCost = CalculateHeuristicCost(neighbor, EndNode);

            openSet.EnqueueOrUpdate(neighbor, neighbor.FCost);
        }
    }

    private void ConstructPath()
    {
        // At this point Current IS the goal.
        // What we want to do is walk backwards through the came from map, 
        // until we reach the our starting node.
        Queue<Tile> totalPath = new Queue<Tile>();
        Node<Tile> currentNode = EndNode;

        totalPath.Enqueue(currentNode.Data);
        while (cameFrom.ContainsKey(currentNode))
        {
            currentNode = cameFrom[currentNode];
            totalPath.Enqueue(currentNode.Data);
        }

        Path = new TilePath(totalPath.Reverse());
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

    private static float CalculateHeuristicCost(Node<Tile> a, Node<Tile> b)
    {
        if (b == null) return 0;

        Vector2Int delta = a.Data.Position - b.Data.Position;
        return Mathf.Sqrt(Mathf.Pow(delta.x, 2) + Mathf.Pow(delta.y, 2));
    }

    public ReadOnlyCollection<Node<Tile>> GetOpenSet() => new ReadOnlyCollection<Node<Tile>>(openSet.ToList());
    public ReadOnlyCollection<Node<Tile>> GetClosedSet() => new ReadOnlyCollection<Node<Tile>>(closedSet.ToList());

    public bool IsInOpenSet(Node<Tile> node) => openSet.Contains(node);
    public bool IsInClosedSet(Node<Tile> node) => closedSet.Contains(node);
}
