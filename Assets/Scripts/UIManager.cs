using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Image progressBar;
    [SerializeField] GameObject PPTUI;
    [SerializeField] GameObject UI;

    [SerializeField] Image fade;


    void Start()
    {
        StartCoroutine(OpeningScene());
        GameManager.Instance.PPT.RoundResolved += OnRoundResolved; // En Start para esperar a que GameManager le de valor a PPT.
    }

    private void OnRoundResolved(PPTRoundResult result)
    {
        progressBar.fillAmount = 1;
    }

    private void Update()
    {
        OnWin();
    }

    void OnWin()
    {
        if (GameManager.Instance.GameState == GameState.EndGame)
        {
            Debug.Log("The game ended");
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.GameState == GameState.PPT)
        {
            PPTUI.SetActive(true);
            progressBar.fillAmount -= Time.fixedDeltaTime / GameManager.Instance.PPTSelectionTime;
        }

        if (GameManager.Instance.GameState != GameState.PPT)
        {
            PPTUI.SetActive(false);
        }
    }

    IEnumerator OpeningScene() // Agregar countdown, desbloqueo de controller, etc
    {
        float duration = 1f;
        float elapsed = 0f;

        Color color = fade.color;
        color.a = 1f;
        fade.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            color.a = 1f - Mathf.Clamp01(elapsed / duration);
            fade.color = color;

            yield return null;
        }

        color.a = 0f;
        fade.color = color;
        UI.SetActive(false);
    }
}