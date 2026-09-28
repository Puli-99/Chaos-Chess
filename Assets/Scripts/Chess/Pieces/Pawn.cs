using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetValidMoves(Board board)
    {
        List<Vector2Int> moves = new();

        Vector2Int direction = Side == PieceColor.White ? Vector2Int.up : Vector2Int.down;        

        Vector2Int target = Position + direction;

        if (board.InBounds(target) && board.GetNode(target).IsFree)
        {
            moves.Add(target);

            if (Position == StartPosition)
            {
                Vector2Int doubleTarget = target + direction;

                if (board.InBounds(doubleTarget) && board.GetNode(doubleTarget).IsFree)
                {
                    moves.Add(doubleTarget);
                }
            }
        }


        Vector2Int[] attackDirections =
        {
            direction + Vector2Int.right, direction + Vector2Int.left,
        };

        foreach (Vector2Int attackDir in attackDirections)
        {
            Vector2Int attackTarget = Position + attackDir;

            if (!board.InBounds(attackTarget)) continue;

            Node attackNode = board.GetNode(attackTarget);            

            if (!attackNode.IsFree && attackNode.Occupant.Side != Side)
            {
                moves.Add(attackTarget);
            }
        }

        return moves;
    }
}