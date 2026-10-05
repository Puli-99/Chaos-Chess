using UnityEngine;
using System;

public class Board
{
    public event Action Changed;
    readonly Node[,] nodes = new Node[8, 8];

    public Board()
    {
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                nodes[x, y] = new Node(new Vector2Int(x, y));
    }

    public bool InBounds(Vector2Int coordinate) => coordinate.x >= 0 && coordinate.x < 8 && coordinate.y >= 0 && coordinate.y < 8;

    public Node GetNode(Vector2Int coordinate) => InBounds(coordinate) ? nodes[coordinate.x, coordinate.y] : null;

    public void RemovePiece(ChessPiece piece)
    {
        Node node = GetNode(piece.Position);
        if (node != null && node.Occupant == piece) node.Occupant = null;
    }

    public void PlacePiece(ChessPiece piece, Vector2Int coord)
    {
        GetNode(coord).Occupant = piece;
        piece.Position = coord;
    }

    public ChessPiece MovePieceTo(ChessPiece piece, Vector2Int destination)
    {
        Node from = GetNode(piece.Position);
        Node to = GetNode(destination);

        ChessPiece captured = to.Occupant;
        from.Occupant = null;
        to.Occupant = piece;
        piece.Position = destination;

        Changed?.Invoke();

        return captured;
    }

}


public class Node
{
    public Vector2Int Coord { get; }
    public ChessPiece Occupant { get; set; }
    public bool IsFree => Occupant == null;

    public Node(Vector2Int coord) 
    {
        Coord = coord; 
    }
}