using System.Collections.Generic;
using UnityEngine;

public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }

public enum PieceColor { White, Black }

public abstract class ChessPiece : MonoBehaviour
{
    [SerializeField] PieceColor side;
    [SerializeField] PieceType type;
    [SerializeField] [Tooltip("Values from 0 to 7")] Vector2Int startPosition;
    [SerializeField] float secondsPerCell = 1f;
    public float SecondsPerCell => secondsPerCell;

    public PieceColor Side => side;
    public PieceType Type => type;
    public Vector2Int StartPosition => startPosition;
    public Vector2Int Position { get; set; }   // solo Board debería escribirla


    public abstract List<Vector2Int> GetValidMoves(Board board);
    public virtual List<Vector2Int> GetPotentialMoves(Board board) => GetValidMoves(board);

    protected List<Vector2Int> GetLineMoves(Board board, Vector2Int[] directions)
    {
        List<Vector2Int> moves = new();
        foreach (Vector2Int dir in directions)
            for (Vector2Int cell = Position + dir; board.InBounds(cell); cell += dir)
                moves.Add(cell);
        return moves;
    }
}