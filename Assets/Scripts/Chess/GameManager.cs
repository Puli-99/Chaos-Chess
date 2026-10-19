using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { None, Lobby, AllReady, ChessPlay, PPT, EndGame }

public class GameManager : NetworkBehaviour
{
    #region Variables

    [Header("Board references")]
    [SerializeField] BoardSpace space;
    [SerializeField] ChessPiece[] pieces;

    // Propiedades
    [SerializeField] GameState gameState;
    public GameState GameState => gameState;
    bool isLocalScene;
    public bool IsLocalScene => isLocalScene;
    public Board Board { get; private set; }
    public BoardSpace Space => space;
    public GameClock Clock { get; } = new GameClock();
    public MovementSystem Movement { get; private set; }

    [Header("PPT variables")]
    [SerializeField] float simultaneityWindow = 0.15f;
    [SerializeField] float pptSelectionTime = 5f;
    public float PPTSelectionTime => pptSelectionTime;

    public PPTSystem PPT { get; private set; }

    bool IsAuthority => isLocalScene || IsServer;

    #endregion

    #region Singleton + Initialization

    public static GameManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Board = new Board();

        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].SetId(i);
            Board.PlacePiece(pieces[i], pieces[i].StartPosition);
            pieces[i].transform.position = space.GridToWorld(pieces[i].StartPosition);
        }

        isLocalScene = SceneManager.GetActiveScene().name == "LocalGameScene";
        InitializingSystems();
    }

    void FixedUpdate()
    {
        if (gameState == GameState.ChessPlay)
        {
            Clock.Advance(Time.fixedDeltaTime);
            if (IsAuthority) Movement.Tick(Clock.Now);
        }

        if (gameState == GameState.PPT && IsAuthority)
            PPT.Tick(Time.fixedDeltaTime);
    }

    void InitializingSystems()
    {

        Movement = new MovementSystem(Board, simultaneityWindow);
        PPT = new PPTSystem(pptSelectionTime, side => 0f); // Reemplazar el 0 por latencia medida por el servidor para ese bando
        Movement.PieceCaptured += piece => Destroy(piece.gameObject);

        if (isLocalScene) SubscriptionToGameStateChangingEvents();
    }

    void OnDisable()
    {
        Movement.PieceCaptured -= piece => Destroy(piece.gameObject);
        if (isLocalScene) DesubscriptionToGameStateChangingEvents();
    }

    void SubscriptionToGameStateChangingEvents()
    {
        Movement.PieceCaptured += piece => KingEndGame(piece);
        Movement.ConflictDetected += OnConflictDetected;
        PPT.MatchResolved += OnPPTResolved;
    }

    void DesubscriptionToGameStateChangingEvents()
    {
        Movement.PieceCaptured -= piece => KingEndGame(piece);
        Movement.ConflictDetected -= OnConflictDetected;
        PPT.MatchResolved -= OnPPTResolved;
    }

    #endregion

    // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    #region Requests (Local and Online)

    ChessPiece Piece(int id) => id >= 0 && id < pieces.Length ? pieces[id] : null;
    PieceColor GetSideFromClientID(ulong clientId) => clientId == NetworkManager.ServerClientId ? PieceColor.White : PieceColor.Black;


    [Rpc(SendTo.Server)] // se manda el request al server, host lo replica de inmediato, client no (replica en Region Online/Chess)
    public void RequestMoveRpc(int pieceId, Vector2Int destination, RpcParams rpcParams = default)
    {
        PieceColor side = GetSideFromClientID(rpcParams.Receive.SenderClientId);
        ChessPiece piece = Piece(pieceId);
        if (piece == null) return;

        RequestMove(piece, destination, side);
    }


    public bool RequestMove(ChessPiece piece, Vector2Int destination, PieceColor requester)
    {
        if (gameState != GameState.ChessPlay) return false;

        if(piece.Side != requester) return false;

        return Movement.TryStartOrder(piece, destination, Clock.Now);
    }


    [Rpc(SendTo.Server)]
    public void RequestPPTSelectionRpc(int roundId, PPTChoice choice, RpcParams rpcParams = default) // lo mismo que RequestMoveRpc (replica del cliente en Region Online/PPT)
    {
        PieceColor side = GetSideFromClientID(rpcParams.Receive.SenderClientId);
        RequestPPTSelection(roundId, choice, side);
    }

    public bool RequestPPTSelection(int roundId, PPTChoice choice, PieceColor requester) => gameState == GameState.PPT && PPT.TrySelect(roundId, choice, requester);

    #endregion

    // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    #region Changing Game State
    void ChangeGameState(GameState newState)
    {
        if(gameState == GameState.EndGame) return;
        gameState = newState;
        if (IsServer) ReplicateGameStateRpc(newState);
    }

    [Rpc(SendTo.NotServer)]
    void ReplicateGameStateRpc(GameState newState) => gameState = newState;

    void KingEndGame(ChessPiece piece)
    {
        if (piece.Type == PieceType.King)
        {
            string victory = piece.Side == PieceColor.White ? "Negras ganan" : "Blancas Ganan"; // Cambiar esto por evento
            Debug.Log(victory);
            ChangeGameState(GameState.EndGame);
        }
    }

    void OnConflictDetected(ChessPiece a, ChessPiece b)
    {
        ChangeGameState(GameState.PPT);
        PPT.StartMatch(a, b);
    }

    void OnPPTResolved(ChessPiece winner, ChessPiece loser)
    {
        Movement.ResolveConflict(winner, loser);
        ChangeGameState(GameState.ChessPlay);
    }
    #endregion

    // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    #region Online

    #region Events Suscriptions
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        SubscriptionToGameStateChangingEvents();

        // Eventos de replica para el cliente
        Movement.StepStarted += OnServerStepStarted;
        Movement.StepCompleted += OnServerStepCompleted;
        Movement.OrderEnded += OnServerOrderEnded;
        Movement.PieceCaptured += OnServerPieceCaptured;

        PPT.RoundStarted += OnServerRoundStarted;
        PPT.SelectionAccepted += OnServerSelectionAccepted;
        PPT.RoundResolved += OnServerRoundResolved;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        DesubscriptionToGameStateChangingEvents();

        // Eventos de replica para el cliente
        Movement.StepStarted -= OnServerStepStarted;
        Movement.StepCompleted -= OnServerStepCompleted;
        Movement.OrderEnded -= OnServerOrderEnded;
        Movement.PieceCaptured -= OnServerPieceCaptured;

        PPT.RoundStarted -= OnServerRoundStarted;
        PPT.SelectionAccepted -= OnServerSelectionAccepted;
        PPT.RoundResolved -= OnServerRoundResolved;
    }

    #endregion

    // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    #region Chess


    void OnServerStepStarted(MoveOrder o) => ReplicateStepStartedRpc(o.Piece.Id, o.Destination, o.Direction, o.StepStartTime, o.StepArrivalTime, Clock.Now);

    void OnServerStepCompleted(MoveOrder o, ChessPiece captured) => ReplicateStepCompletedRpc(o.Piece.Id, o.Piece.Position);

    void OnServerOrderEnded(MoveOrder o, MoveEndReason reason) => ReplicateOrderEndedRpc(o.Piece.Id, reason);

    void OnServerPieceCaptured(ChessPiece p) => ReplicatePieceCapturedRpc(p.Id);


    // Replicando con MovementSystem lo que paso en servidor
    [Rpc(SendTo.NotServer)]
    void ReplicateStepStartedRpc(int pieceId, Vector2Int destination, Vector2Int direction, float start, float arrival, float serverNow)
    {
        ChessPiece piece = Piece(pieceId);
        if (piece == null) return;
        Clock.Set(serverNow); // el reloj del cliente se alinea con el del servidor
        Movement.ReplicateStepStarted(piece, destination, direction, start, arrival);
    }

    [Rpc(SendTo.NotServer)]
    void ReplicateStepCompletedRpc(int pieceId, Vector2Int cell)
    {
        ChessPiece piece = Piece(pieceId);
        if (piece == null) return;
        Movement.ReplicateStepCompleted(piece, cell);
    }

    [Rpc(SendTo.NotServer)]
    void ReplicateOrderEndedRpc(int pieceId, MoveEndReason reason)
    {
        ChessPiece piece = Piece(pieceId);
        if (piece == null) return;
        Movement.ReplicateOrderEnded(piece, reason);
    }

    [Rpc(SendTo.NotServer)]
    void ReplicatePieceCapturedRpc(int pieceId)
    {
        ChessPiece piece = Piece(pieceId);
        if (piece == null) return;
        Movement.ReplicatePieceCaptured(piece);
    }

    #endregion

    // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    #region PPT

    void OnServerRoundStarted(PPTRound round) => ReplicateRoundStartedRpc(round.RoundId, round.Duration, round.SideA, round.SideB);
    void OnServerSelectionAccepted(PieceColor side) => ReplicateSelectionAcceptedRpc(side);
    void OnServerRoundResolved(PPTRoundResult result) => ReplicateRoundResolvedRpc(result.SideA, result.SideB, result.ChoiceA, result.ChoiceB, result.IsDraw);

    // Replicando en cliente lo que paso en servidor
    [Rpc(SendTo.NotServer)]
    void ReplicateRoundStartedRpc(int roundId, float duration, PieceColor sideA, PieceColor sideB) => PPT.ReplicateRoundStarted(new PPTRound { RoundId = roundId, Duration = duration, SideA = sideA, SideB = sideB });

    [Rpc(SendTo.NotServer)]
    void ReplicateSelectionAcceptedRpc(PieceColor side) => PPT.ReplicateSelectionAccepted(side);

    [Rpc(SendTo.NotServer)]
    void ReplicateRoundResolvedRpc(PieceColor sideA, PieceColor sideB, PPTChoice choiceA, PPTChoice choiceB, bool isDraw) => PPT.ReplicateRoundResolved(new PPTRoundResult
    {
        SideA = sideA, SideB = sideB, ChoiceA = choiceA, ChoiceB = choiceB, IsDraw = isDraw
    });

    #endregion

    #endregion
}