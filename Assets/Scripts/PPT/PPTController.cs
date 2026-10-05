using UnityEngine;

public class PPTController : MonoBehaviour
{
    [SerializeField] PieceColor localSide;
    public PieceColor LocalSide { get => localSide; set => localSide = value; }
    GameManager gm;
    int currentRound;

    void Start()
    {
        gm = GameManager.Instance;
        gm.PPT.RoundStarted += OnRoundStarted;

        localSide = GetComponent<BoardController>().LocalSide;
    }

    void OnDestroy()
    {
        if (gm != null) gm.PPT.RoundStarted -= OnRoundStarted;
    }

    void OnRoundStarted(PPTRound round) => currentRound = round.RoundId;


    public void ChooseRock() => Choose(PPTChoice.Rock);
    public void ChoosePaper() => Choose(PPTChoice.Paper);
    public void ChooseScissors() => Choose(PPTChoice.Scissors);

    void Choose(PPTChoice choice) => gm.RequestPPTSelection(currentRound, choice, localSide);
}