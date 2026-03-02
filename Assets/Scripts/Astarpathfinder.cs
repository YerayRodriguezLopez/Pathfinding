using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    public static List<Node> FindPath(
        Node[,] nodeMatrix,
        int startX, int startY,
        int endX, int endY,
        out List<Node> closedVisited,
        out List<Node> openVisited)
    {
        closedVisited = new List<Node>();
        openVisited = new List<Node>();

        // Reset all nodes
        int rows = nodeMatrix.GetLength(0);
        int cols = nodeMatrix.GetLength(1);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
            {
                nodeMatrix[i, j].GCost = float.MaxValue;
                nodeMatrix[i, j].NodeParent = null;
            }

        Node startNode = nodeMatrix[startX, startY];
        Node endNode = nodeMatrix[endX, endY];

        // Open list ordered by F = G + H
        List<Node> openList = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.GCost = 0f;
        openList.Add(startNode);
        openVisited.Add(startNode);

        while (openList.Count > 0)
        {
            // Pick node with lowest F cost
            Node current = GetLowestF(openList);

            if (current == endNode)
            {
                // Reconstruct path
                return ReconstructPath(endNode);
            }

            openList.Remove(current);
            closedSet.Add(current);
            closedVisited.Add(current);

            foreach (Way way in current.WayList)
            {
                Node neighbour = way.NodeDestiny;

                if (closedSet.Contains(neighbour))
                    continue;

                float tentativeG = current.GCost + way.Cost;

                if (tentativeG < neighbour.GCost)
                {
                    neighbour.GCost = tentativeG;
                    neighbour.NodeParent = current;

                    if (!openList.Contains(neighbour))
                    {
                        openList.Add(neighbour);
                        openVisited.Add(neighbour);
                    }
                }
            }
        }

        // No path found
        return null;
    }

    private static Node GetLowestF(List<Node> list)
    {
        Node best = list[0];
        foreach (Node n in list)
        {
            if ((n.GCost + n.Heuristic) < (best.GCost + best.Heuristic))
                best = n;
        }
        return best;
    }

    private static List<Node> ReconstructPath(Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current);
            current = current.NodeParent;
        }
        path.Reverse();
        return path;
    }
}