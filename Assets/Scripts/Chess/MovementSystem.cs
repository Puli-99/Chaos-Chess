using System;
using System.Collections.Generic;
using UnityEngine;

public class MovementSystem
{
    readonly Board board;
    readonly List<MoveOrder> active = new List<MoveOrder>();

    public event Action<MoveOrder> StepStarted;
    public event Action<MoveOrder, ChessPiece> StepCompleted; // captured puede ser null
    public event Action<MoveOrder, MoveEndReason> OrderEnded;
    public event Action<ChessPiece> PieceCaptured;

    public MovementSystem(Board board) => this.board = board;

    public bool IsMoving(ChessPiece piece) => FindOrder(piece) != null;

    public bool TryStartOrder(ChessPiece piece, Vector2Int destination, float now)
    {
        if (IsMoving(piece)) return false; // PROVISORIO, ver punto 1
        if (!piece.GetPotentialMoves(board).Contains(destination)) return false;

        // Solo válido para piezas en línea recta (torre, alfil, reina, rey)
        Vector2Int delta = destination - piece.Position;
        Vector2Int dir = piece.Type == PieceType.Knight ? delta : new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));

        MoveOrder order = new (piece, destination, dir);
        active.Add(order);
        BeginStep(order, now);
        return true;
    }

    public void Tick(float now)
    {
        // Procesa las llegadas en orden cronológico, aunque un frame largo abarque varios pasos
        while (true)
        {
            MoveOrder next = null;
            foreach (MoveOrder o in active)
                if (o.StepArrivalTime <= now && (next == null || o.StepArrivalTime < next.StepArrivalTime))
                    next = o;

            if (next == null) break;
            CompleteStep(next);
        }
    }

    void BeginStep(MoveOrder order, float startTime)
    {
        order.StepStartTime = startTime;
        order.StepArrivalTime = startTime + order.Piece.SecondsPerCell;
        StepStarted?.Invoke(order);
    }

    void CompleteStep(MoveOrder order)
    {
        ChessPiece piece = order.Piece;
        Vector2Int cell = order.NextCell;
        ChessPiece occupant = board.GetNode(cell).Occupant;

        if (occupant != null && occupant.Side == piece.Side)
        {
            End(order, MoveEndReason.BlockedByAlly);
            return;
        }

        if (piece.Type == PieceType.Pawn && order.Direction.x == 0 && occupant != null)
        {
            End(order, MoveEndReason.BlockedByEnemy);
            return;
        }

        if (piece.Type == PieceType.Pawn && order.Direction.x != 0 && occupant == null)
        {
            End(order, MoveEndReason.NoTarget);
            return;
        }

        ChessPiece captured = board.MovePieceTo(piece, cell);
        StepCompleted?.Invoke(order, captured);

        if (captured != null)
        {
            MoveOrder capturedOrder = FindOrder(captured);
            if (capturedOrder != null) End(capturedOrder, MoveEndReason.WasCaptured);
            PieceCaptured?.Invoke(captured);
            End(order, MoveEndReason.CapturedTarget);
            return;
        }

        if (cell == order.Destination)
        {
            End(order, MoveEndReason.Arrived);
            return;
        }

        // El siguiente paso arranca en el instante exacto de la llegada, para no acumular deriva
        BeginStep(order, order.StepArrivalTime);
    }

    void End(MoveOrder order, MoveEndReason reason)
    {
        active.Remove(order);
        OrderEnded?.Invoke(order, reason);
    }

    MoveOrder FindOrder(ChessPiece piece) => active.Find(o => o.Piece == piece);
}