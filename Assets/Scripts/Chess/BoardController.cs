using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] InputActionReference pointAction;
    [SerializeField] InputActionReference selectAction;
    [SerializeField] InputActionReference deselectAction;
    [SerializeField] GameObject highlightPrefab;        // movimientos posibles ahora mismo
    [SerializeField] GameObject highlightPotentialPrefab;  // movimientos que dependen de que el tablero cambie

    Vector3 lastHit;

    ChessPiece selected;
    List<Vector2Int> currentMoves = new(); // posibles ahora (GetValidMoves)
    List<Vector2Int> potentialMoves = new(); // todas las alternativas (GetPotentialMoves); incluye las actuales
    readonly List<GameObject> highlights = new();

    // Se marca cuando el Board cambia; el refresco real se hace una vez por frame en LateUpdate.
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
        //deselectAction.action.performed += OnDeselect;

    }

    void OnDisable()
    {
        selectAction.action.performed -= OnSelect;
    }

    // GameManager crea el Board en su Awake, por eso la suscripción va en Start (igual que en Ghost).
    void Start()
    {
        Board.Changed += OnBoardChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) Board.Changed -= OnBoardChanged;
    }

    void OnBoardChanged() => boardChanged = true;

    void LateUpdate()
    {
        if (!boardChanged) return;
        boardChanged = false;
        RefreshSelection();
    }

    void OnSelect(InputAction.CallbackContext context)
    {
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

    #region Visual Debug

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(lastHit, 0.1f);
    }
    #endregion

    void HandleClick(Vector2Int cell)
    {
        // Esta rama va primero a propósito: una pieza enemiga en movimiento sigue ocupando
        // su casilla lógica, así que sigue siendo un objetivo válido de captura.
        // Se valida contra todas las alternativas, no solo las posibles ahora mismo.
        if (selected != null && potentialMoves.Contains(cell))
        {
            GameManager.Instance.RequestMove(selected, cell);
            Deselect();
            return;
        }

        ChessPiece piece = Board.GetNode(cell).Occupant;
        if (piece != null && !Movement.IsMoving(piece)) Select(piece);
        else Deselect();
    }

    void Select(ChessPiece piece)
    {
        selected = piece;
        currentMoves = piece.GetValidMoves(Board);
        potentialMoves = piece.GetPotentialMoves(Board);
        RebuildHighlights();
    }

    // Recalcula los movimientos de la pieza seleccionada tras un cambio en el tablero.
    void RefreshSelection()
    {
        if (selected == null) return;

        // Si la capturaron, el tablero ya no la tiene en su casilla.
        if (Board.GetNode(selected.Position)?.Occupant != selected)
        {
            Deselect();
            return;
        }

        List<Vector2Int> updatedCurrent = selected.GetValidMoves(Board);
        List<Vector2Int> updatedPotential = selected.GetPotentialMoves(Board);

        // Hay que comparar las dos listas: las alternativas pueden ser las mismas y aun así
        // haber cambiado cuáles son posibles ahora (y por lo tanto el prefab de cada casilla).
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
}