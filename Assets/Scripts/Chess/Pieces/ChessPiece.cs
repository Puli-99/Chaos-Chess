using System.Collections.Generic;
using UnityEngine;

public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }

public enum PieceColor { White, Black }

public abstract class ChessPiece : MonoBehaviour
{
    [SerializeField] PieceColor side;
    [SerializeField] [Tooltip("Values from 0 to 7")] Vector2Int startPosition;

    public PieceColor Side => side;
    public Vector2Int StartPosition => startPosition;
    public Vector2Int Position { get; set; }   // solo Board debería escribirla


    public abstract List<Vector2Int> GetValidMoves(Board board);
}