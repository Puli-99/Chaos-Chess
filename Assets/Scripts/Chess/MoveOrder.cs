using UnityEngine;

public enum MoveEndReason { Arrived, BlockedByAlly, BlockedByEnemy, CapturedTarget, WasCaptured, NoTarget }

public class MoveOrder
{
    public ChessPiece Piece { get; }
    public Vector2Int Destination { get; }
    public Vector2Int Direction { get; }

    // Paso actual: la pieza sigue en Piece.Position hasta que el fantasma llegue a NextCell
    public Vector2Int NextCell => Piece.Position + Direction;
    public float StepStartTime { get; set; }
    public float StepArrivalTime { get; set; }

    public MoveOrder(ChessPiece piece, Vector2Int destination, Vector2Int direction)
    {
        Piece = piece;
        Destination = destination;
        Direction = direction;
    }
}
