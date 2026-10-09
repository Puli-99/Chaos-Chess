using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Image progressBar;
    [SerializeField] GameObject UI;

    void Start()
    {
        GameManager.Instance.PPT.RoundResolved += OnRoundResolved; // En Start para esperar a que GameManager le de valor a PPT.
    }

    private void OnRoundResolved(PPTRoundResult result)
    {
        progressBar.fillAmount = 1;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.GameState == GameState.PPT)
        {
            UI.SetActive(true);
            progressBar.fillAmount -= Time.fixedDeltaTime / GameManager.Instance.PPTSelectionTime;
        }

        if (GameManager.Instance.GameState != GameState.PPT)
        {
            UI.SetActive(false);
        }
    }
}