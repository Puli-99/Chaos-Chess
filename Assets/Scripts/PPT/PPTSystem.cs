using System;

public enum PPTChoice { None, Rock, Paper, Scissors }

public class PPTRound
{
    public int RoundId;
    public float Duration;
    public PieceColor SideA, SideB;
}

public class PPTRoundResult
{
    public PieceColor SideA, SideB;
    public PPTChoice ChoiceA, ChoiceB;
    public bool IsDraw;
}

public class PPTSystem
{
    readonly float selectionTime;
    readonly Func<PieceColor, float> extraTime; // compensación por latencia, según el bando

    ChessPiece pieceA, pieceB;
    PieceColor sideA, sideB;
    PPTChoice choiceA, choiceB;     // privadas: nunca salen de acá hasta cerrar la ronda
    float elapsed, deadlineA, deadlineB;
    int roundId;

    public bool IsActive { get; private set; }

    public event Action<PPTRound> RoundStarted;
    public event Action<PieceColor> SelectionAccepted;       // la UI de ESE bando confirma
    public event Action<PPTRoundResult> RoundResolved;       // ambas elecciones (revelación)
    public event Action<ChessPiece, ChessPiece> MatchResolved; // ganador, perdedor

    public PPTSystem(float selectionTime, Func<PieceColor, float> extraTime = null)
    {
        this.selectionTime = selectionTime;
        this.extraTime = extraTime ?? (_ => 0f);
    }

    public void StartMatch(ChessPiece a, ChessPiece b)
    {
        pieceA = a; pieceB = b;
        sideA = a.Side; sideB = b.Side;
        IsActive = true;
        StartRound();
    }

    public bool TrySelect(int round, PPTChoice choice, PieceColor requester)
    {
        if (!IsActive || round != roundId || choice == PPTChoice.None) return false;

        if (requester == sideA)
        {
            if (choiceA != PPTChoice.None) return false;   // el primer clic cuenta
            choiceA = choice;
        }
        else if (requester == sideB)
        {
            if (choiceB != PPTChoice.None) return false;
            choiceB = choice;
        }
        else return false;

        SelectionAccepted?.Invoke(requester);
        TryResolveRound();
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;
        elapsed += deltaTime;
        if (choiceA == PPTChoice.None && elapsed >= deadlineA) choiceA = RandomChoice();
        if (choiceB == PPTChoice.None && elapsed >= deadlineB) choiceB = RandomChoice();
        TryResolveRound();
    }

    void StartRound()
    {
        roundId++;
        elapsed = 0f;
        choiceA = choiceB = PPTChoice.None;
        deadlineA = selectionTime + extraTime(sideA);
        deadlineB = selectionTime + extraTime(sideB);

        RoundStarted?.Invoke(new PPTRound
        {
            RoundId = roundId,
            Duration = selectionTime,
            SideA = sideA,
            SideB = sideB
        });
    }

    void TryResolveRound()
    {
        if (choiceA == PPTChoice.None || choiceB == PPTChoice.None) return;

        int outcome = Compare(choiceA, choiceB); // 0 empate, 1 gana A, -1 gana B

        RoundResolved?.Invoke(new PPTRoundResult
        {
            SideA = sideA,
            SideB = sideB,
            ChoiceA = choiceA,
            ChoiceB = choiceB,
            IsDraw = outcome == 0
        });

        if (outcome == 0) { StartRound(); return; }

        IsActive = false;
        if (outcome > 0) MatchResolved?.Invoke(pieceA, pieceB);
        else MatchResolved?.Invoke(pieceB, pieceA);
    }

    static int Compare(PPTChoice a, PPTChoice b)
    {
        if (a == b) return 0;
        bool aWins = (a == PPTChoice.Rock && b == PPTChoice.Scissors)
                  || (a == PPTChoice.Paper && b == PPTChoice.Rock)
                  || (a == PPTChoice.Scissors && b == PPTChoice.Paper);
        return aWins ? 1 : -1;
    }

    static PPTChoice RandomChoice() => (PPTChoice)UnityEngine.Random.Range(1, 4);
}