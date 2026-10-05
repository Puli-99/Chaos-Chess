using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class MovementSystem
{
    readonly Board board;
    readonly List<MoveOrder> active = new ();
    readonly float conflictWindow;
    public event Action<ChessPiece, ChessPiece> ConflictDetected;
    public event Action<MoveOrder> StepStarted;
    public event Action<MoveOrder, ChessPiece> StepCompleted; // captured puede ser null
    public event Action<MoveOrder, MoveEndReason> OrderEnded;
    public event Action<ChessPiece> PieceCaptured;

    public MovementSystem(Board board, float simulteanityWindow)
    {
        this.board = board;
        conflictWindow = simulteanityWindow;
    }

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
            foreach (MoveOrder order in active)
                if (!order.InConflict && order.StepArrivalTime <= now && (next == null || order.StepArrivalTime < next.StepArrivalTime))
                    next = order;

            if (next == null) break;
            if (CompleteStep(next)) break;
        }
    }

    void BeginStep(MoveOrder order, float startTime)
    {
        order.StepStartTime = startTime;
        order.StepArrivalTime = startTime + order.Piece.SecondsPerCell;
        StepStarted?.Invoke(order);
    }

    bool CompleteStep(MoveOrder order)
    {
        ChessPiece piece = order.Piece;
        Vector2Int cell = order.NextCell;

        MoveOrder rival = FindSimultaneous(order);

        if (rival != null)
        {
            if (rival.Piece.Side == piece.Side)
            {
                // Aliadas: ambas anulan su movimiento y quedan en sus casillas
                End(order, MoveEndReason.BlockedByAlly);
                End(rival, MoveEndReason.BlockedByAlly);
                return false;
            }

            order.InConflict = true;
            rival.InConflict = true;
            ConflictDetected?.Invoke(piece, rival.Piece);
            return true;
        }

        ChessPiece occupant = board.GetNode(cell).Occupant;

        if (occupant != null && occupant.Side == piece.Side)
        {
            End(order, MoveEndReason.BlockedByAlly);
            return false;
        }

        if (piece.Type == PieceType.Pawn && order.Direction.x == 0 && occupant != null)
        {
            End(order, MoveEndReason.BlockedByEnemy);
            return false;
        }

        if (piece.Type == PieceType.Pawn && order.Direction.x != 0 && occupant == null)
        {
            End(order, MoveEndReason.NoTarget);
            return false;
        }

        ChessPiece captured = board.MovePieceTo(piece, cell);
        StepCompleted?.Invoke(order, captured);

        if (captured != null)
        {
            MoveOrder capturedOrder = FindOrder(captured);
            if (capturedOrder != null) End(capturedOrder, MoveEndReason.WasCaptured);
            PieceCaptured?.Invoke(captured);
            End(order, MoveEndReason.CapturedTarget);
            return false;
        }

        if (cell == order.Destination)
        {
            End(order, MoveEndReason.Arrived);
            return false;
        }

        BeginStep(order, order.StepArrivalTime);
        return false;
    }

    MoveOrder FindSimultaneous(MoveOrder order)
    {
        foreach (MoveOrder other in active)
        {
            if (other == order || other.InConflict) continue;
            if (Mathf.Abs(other.StepArrivalTime - order.StepArrivalTime) > conflictWindow) continue;

            bool sameCell = other.NextCell == order.NextCell;
            bool swap = other.NextCell == order.Piece.Position
                     && order.NextCell == other.Piece.Position;

            if (sameCell || swap) return other;
        }
        return null;
    }

    public void ResolveConflict(ChessPiece winnerPiece, ChessPiece loserPiece)
    {
        MoveOrder winner = FindOrder(winnerPiece);
        MoveOrder loser = FindOrder(loserPiece);
        if (winner == null || loser == null) return;

        if (winnerPiece.Type == PieceType.Pawn && winner.Direction.x == 0)
        {
            // Evitar que el peon coma con mov hacia adelante, pero que haga algo digno de haber ganado el PPT
        }

        board.RemovePiece(loserPiece);
        End(loser, MoveEndReason.WasCaptured);
        PieceCaptured?.Invoke(loserPiece);

        board.MovePieceTo(winnerPiece, winner.NextCell);
        StepCompleted?.Invoke(winner, loserPiece);
        End(winner, MoveEndReason.CapturedTarget);
    }

    void End(MoveOrder order, MoveEndReason reason)
    {
        active.Remove(order);
        OrderEnded?.Invoke(order, reason);
    }

    MoveOrder FindOrder(ChessPiece piece) => active.Find(o => o.Piece == piece);
}