using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    static readonly Vector2Int[] Directions =
    {
        new(1,1), new(-1,1), new(1,-1), new(-1,-1)
    };

    public override List<Vector2Int> GetValidMoves(Board board)
    {
        List<Vector2Int> moves = new();

        foreach (Vector2Int dir in Directions)
        {
            Vector2Int current = Position + dir;

            while (board.InBounds(current))
            {
                Node node = board.GetNode(current);

                if (node.IsFree)
                {
                    moves.Add(current);
                }
                else
                {
                    if (node.Occupant.Side != Side) moves.Add(current);
                    break;
                }

                current += dir;
            }
        }

        return moves;
    }

    public override List<Vector2Int> GetPotentialMoves(Board board) => GetLineMoves(board, Directions);
}
