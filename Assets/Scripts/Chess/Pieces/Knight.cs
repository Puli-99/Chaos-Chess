using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
   static readonly Vector2Int[] Directions =
    {        
        new(2,1), new(-2,1), new(2,-1), new(-2,-1),
        new(1,2), new(-1,2), new (1,-2), new(-1,-2)
    };

    public override List<Vector2Int> GetValidMoves(Board board)
    {
        List<Vector2Int> moves = new();

        foreach (Vector2Int dir in Directions)
        {
            Vector2Int target = Position + dir;

            if (!board.InBounds(target)) continue;

            Node node = board.GetNode(target);

            if (node.IsFree)
            {
                moves.Add(target);
            }
            else if (node.Occupant.Side != Side)
            {
                moves.Add(target);
            }            
        }

        return moves;
    }

    public override List<Vector2Int> GetPotentialMoves(Board board)
    {
        List<Vector2Int> moves = new();
        foreach (Vector2Int dir in Directions)
            if (board.InBounds(Position + dir)) moves.Add(Position + dir);
        return moves;
    }
}