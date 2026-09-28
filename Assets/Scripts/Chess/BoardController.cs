using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] InputActionReference pointAction;
    [SerializeField] InputActionReference selectAction;
    [SerializeField] GameObject highlightPrefab;

    Vector3 lastHit;


    ChessPiece selected;
    List<Vector2Int> currentMoves = new List<Vector2Int>();
    readonly List<GameObject> highlights = new List<GameObject>();

    Board Board => GameManager.Instance.Board;
    BoardSpace Space => GameManager.Instance.Space;

    void OnEnable()
    {
        pointAction.action.Enable();
        selectAction.action.Enable();
        selectAction.action.performed += OnSelect;
    }

    void OnDisable()
    {
        selectAction.action.performed -= OnSelect;
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
        if (selected != null && currentMoves.Contains(cell))
        {
            GameManager.Instance.RequestMove(selected, cell);
            Deselect();
            return;
        }

        ChessPiece piece = Board.GetNode(cell).Occupant;
        if (piece != null) Select(piece);
        else Deselect();
    }

    void Select(ChessPiece piece)
    {
        ClearHighlights();
        selected = piece;
        currentMoves = piece.GetValidMoves(Board);

        foreach (Vector2Int move in currentMoves)
            highlights.Add(Instantiate(highlightPrefab, Space.GridToWorld(move), Quaternion.Euler(90,0,0)));
    }

    void Deselect()
    {
        selected = null;
        currentMoves = new List<Vector2Int>();
        ClearHighlights();
    }

    void ClearHighlights()
    {
        foreach (GameObject h in highlights) Destroy(h);
        highlights.Clear();
    }
}