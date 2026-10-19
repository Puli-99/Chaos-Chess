using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BoardController : MonoBehaviour
{
    [SerializeField] PieceColor localSide;
    public PieceColor LocalSide { get => localSide; set => localSide = value; }

    [SerializeField] Camera cam;
    [SerializeField] InputActionReference pointAction;
    [SerializeField] InputActionReference selectAction;
    [SerializeField] InputActionReference deselectAction;
    [SerializeField] GameObject highlightPrefab;
    [SerializeField] GameObject highlightPotentialPrefab;

    Vector3 lastHit;

    ChessPiece selected;
    List<Vector2Int> currentMoves = new();
    List<Vector2Int> potentialMoves = new();
    readonly List<GameObject> highlights = new();
    bool boardChanged;

    Board Board => GameManager.Instance.Board;
    BoardSpace Space => GameManager.Instance.Space;
    MovementSystem Movement => GameManager.Instance.Movement;

    void OnEnable()
    {
        pointAction.action.Enable();
        selectAction.action.Enable();
        deselectAction.action.Enable();

        selectAction.action.performed += OnSelect;
        deselectAction.action.performed += OnDeselect;

    }

    void OnDisable()
    {
        selectAction.action.performed -= OnSelect;
        deselectAction.action.performed -= OnDeselect;
        Board.Changed -= OnBoardChanged;
    }

    void Start()
    {
        Board.Changed += OnBoardChanged;

        if (!GameManager.Instance.IsLocalScene)
        {
            localSide = NetworkManager.Singleton.IsHost? PieceColor.White: PieceColor.Black;
        }
    }

    void LateUpdate()
    {
        if (!boardChanged) return;
        boardChanged = false;
        RefreshSelection();
    }

    void OnBoardChanged() => boardChanged = true;

    void OnSelect(InputAction.CallbackContext context)
    {
        if(GameManager.Instance.GameState != GameState.ChessPlay) return;

        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Space.TryRaycastBoard(ray, out Vector3 hit))
        {
            Deselect();
            return;
        }

        Vector2Int cell = Space.WorldToGrid(hit);
        lastHit = hit;

        if (!Board.InBounds(cell)) { Deselect(); return; }
        HandleClick(cell);
    }

    void OnDeselect(InputAction.CallbackContext context)
    {
        Deselect();
    }

    void HandleClick(Vector2Int cell)
    {
        if (selected != null && potentialMoves.Contains(cell))
        {
            if (!GameManager.Instance.IsLocalScene)
            {
                GameManager.Instance.RequestMoveRpc(selected.Id, cell);
            }
            else
            {
                GameManager.Instance.RequestMove(selected, cell, localSide);
            }

            Deselect();
            return;
        }

        ChessPiece piece = Board.GetNode(cell).Occupant;
        if (piece != null && !Movement.IsMoving(piece) && piece.Side == localSide) Select(piece);
        else Deselect();
    }

    void Select(ChessPiece piece)
    {
        selected = piece;
        currentMoves = piece.GetValidMoves(Board);
        potentialMoves = piece.GetPotentialMoves(Board);
        RebuildHighlights();
    }

    void RefreshSelection()
    {
        if (selected == null) return;

        if (Board.GetNode(selected.Position)?.Occupant != selected)
        {
            Deselect();
            return;
        }

        List<Vector2Int> updatedCurrent = selected.GetValidMoves(Board);
        List<Vector2Int> updatedPotential = selected.GetPotentialMoves(Board);

        if (updatedCurrent.SequenceEqual(currentMoves) && updatedPotential.SequenceEqual(potentialMoves)) return;

        currentMoves = updatedCurrent;
        potentialMoves = updatedPotential;
        RebuildHighlights();
    }

    void RebuildHighlights()
    {
        ClearHighlights();
        foreach (Vector2Int move in potentialMoves)
        {
            GameObject prefab = currentMoves.Contains(move) ? highlightPrefab : highlightPotentialPrefab;
            highlights.Add(Instantiate(prefab, Space.GridToWorld(move), Quaternion.Euler(90, 0, 0)));
        }
    }

    void Deselect()
    {
        selected = null;
        currentMoves = new List<Vector2Int>();
        potentialMoves = new List<Vector2Int>();
        ClearHighlights();
    }

    void ClearHighlights()
    {
        foreach (GameObject h in highlights) Destroy(h);
        highlights.Clear();
    }

    #region Visual Debug

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(lastHit, 0.1f);
    }
    #endregion
}