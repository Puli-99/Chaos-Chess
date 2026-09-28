using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new(1,1), new(-1,1), new(1,-1), new(-1,-1)
    };

    public override List<Vector2Int> GetValidMoves(Board board)
    {
        List<Vector2Int> moves = new();

        foreach (Vector2Int dir in Directions)
        {
            Vector2Int target = Position + dir;

            if (!board.InBounds(target)) continue;   // fuera del tablero: saltar a la siguiente dirección

            Node node = board.GetNode(target);

            if (node.IsFree)
            {
                moves.Add(target);
            }
            else if (node.Occupant.Side != Side)
            {
                moves.Add(target);                   // enemigo: se puede capturar
            }
            // pieza propia: no se agrega, y el foreach sigue solo
        }

        return moves;
    }
}
