using UnityEngine;
using UnityEngine.UI;

public enum GameState { None, Lobby, AllReady, ChessPlay, PPT, EndGame }

public class GameManager : MonoBehaviour
{
    [SerializeField] GameState gameState;
    [SerializeField] BoardSpace space;
    [SerializeField] ChessPiece[] pieces;
    public GameState GameState => gameState;
    public Board Board { get; private set; }
    public BoardSpace Space => space;
    public GameClock Clock { get; } = new GameClock();
    public MovementSystem Movement { get; private set; }

    [SerializeField] float simultaneityWindow = 0.15f;
    [SerializeField] float pptSelectionTime = 5f;
    public float PPTSelectionTime => pptSelectionTime;

    public PPTSystem PPT { get; private set; }


    public static GameManager Instance { get; private set; }
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

        InitializingSystems();
    }

    void FixedUpdate()
    {
        if (gameState == GameState.ChessPlay)
        {
            Clock.Advance(Time.fixedDeltaTime);
            Movement.Tick(Clock.Now);
        }

        if (gameState == GameState.PPT)
        {
            PPT.Tick(Time.fixedDeltaTime);
        }
    }


    void InitializingSystems()
    {

        Movement = new MovementSystem(Board, simultaneityWindow);
        PPT = new PPTSystem(pptSelectionTime, side => 0f); // Reemplazar el 0 por latencia medida por el servidor para ese bando

        Movement.PieceCaptured += piece => KingEndGame(piece);
        Movement.PieceCaptured += piece => Destroy(piece.gameObject);
        Movement.ConflictDetected += OnConflictDetected;

        PPT.MatchResolved += OnPPTResolved;
    }

    //Futuro ServerRpc
    public bool RequestMove(ChessPiece piece, Vector2Int destination, PieceColor requester)
    {
        if (gameState != GameState.ChessPlay) return false;

        if(piece.Side != requester) return false;

        return Movement.TryStartOrder(piece, destination, Clock.Now);
    }

    public bool RequestPPTSelection(int roundId, PPTChoice choice, PieceColor requester)
    => gameState == GameState.PPT && PPT.TrySelect(roundId, choice, requester);

    void ChangeGameState(GameState newState)
    {
        gameState = newState;
    }

    void KingEndGame(ChessPiece piece)
    {
        if (piece.Type == PieceType.King)
        {
            string victory = piece.Side == PieceColor.White ? "Negras ganan" : "Blancas Ganan";
            Debug.Log(victory);
            ChangeGameState(GameState.EndGame);
        }
    }

    void OnConflictDetected(ChessPiece a, ChessPiece b)
    {
        gameState = GameState.PPT;
        PPT.StartMatch(a, b);
    }

    void OnPPTResolved(ChessPiece winner, ChessPiece loser)
    {
        Movement.ResolveConflict(winner, loser);
        gameState = GameState.ChessPlay;
    }
}