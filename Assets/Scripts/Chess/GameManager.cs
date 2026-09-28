using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] BoardSpace space;
    [SerializeField] ChessPiece[] pieces;
    public Board Board { get; private set; }
    public BoardSpace Space => space;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Board = new Board();

        foreach (ChessPiece piece in pieces)
        {
            Board.PlacePiece(piece, piece.StartPosition);
            piece.transform.position = space.GridToWorld(piece.StartPosition);
        }
    }

    private void Start()
    {
        // Iniciar todas las piezas
    }

    //Futuro ServerRpc
    public bool RequestMove(ChessPiece piece, Vector2Int destination)
    {
        if (!Board.TryMovePiece(piece, destination, out ChessPiece captured)) return false;

        if (captured != null) Destroy(captured.gameObject);
        piece.transform.position = space.GridToWorld(destination);
        return true;
    }
}
