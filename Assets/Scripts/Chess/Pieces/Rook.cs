using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
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
}
