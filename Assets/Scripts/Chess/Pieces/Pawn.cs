using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    static readonly Vector2Int[] WhiteAttacks = { new(1, 1), new(-1, 1) };
    static readonly Vector2Int[] BlackAttacks = { new(1, -1), new(-1, -1) };

    Vector2Int Forward => Side == PieceColor.White ? Vector2Int.up : Vector2Int.down;
    Vector2Int[] AttackDirections => Side == PieceColor.White ? WhiteAttacks : BlackAttacks;

    public override List<Vector2Int> GetValidMoves(Board board)
    {
        List<Vector2Int> moves = new();

        Vector2Int target = Position + Forward;

        if (board.InBounds(target) && board.GetNode(target).IsFree)
        {
            moves.Add(target);

            if (Position == StartPosition)
            {
                Vector2Int doubleTarget = target + Forward;

                if (board.InBounds(doubleTarget) && board.GetNode(doubleTarget).IsFree)
                {
                    moves.Add(doubleTarget);
                }
            }
        }

        foreach (Vector2Int attackDir in AttackDirections)
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

    public override List<Vector2Int> GetPotentialMoves(Board board)
    {
        List<Vector2Int> moves = new();

        Vector2Int target = Position + Forward;

        if (board.InBounds(target))
        {
            moves.Add(target);

            Vector2Int doubleTarget = target + Forward;

            if (Position == StartPosition && board.InBounds(doubleTarget))
            {
                moves.Add(doubleTarget);
            }
        }

        foreach (Vector2Int attackDir in AttackDirections)
        {
            Vector2Int attackTarget = Position + attackDir;

            if (board.InBounds(attackTarget)) moves.Add(attackTarget);
        }

        return moves;
    }
}