using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] BoardSpace space;
    [SerializeField] ChessPiece[] pieces;
    public Board Board { get; private set; }
    public BoardSpace Space => space;
    public GameClock Clock { get; } = new GameClock();
    public MovementSystem Movement { get; private set; }

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

        Movement = new MovementSystem(Board);
        Movement.PieceCaptured += piece => Destroy(piece.gameObject);
    }

    void FixedUpdate()
    {
        Clock.Advance(Time.fixedDeltaTime);
        Movement.Tick(Clock.Now);
    }

    //Futuro ServerRpc
    public bool RequestMove(ChessPiece piece, Vector2Int destination) => Movement.TryStartOrder(piece, destination, Clock.Now);
}