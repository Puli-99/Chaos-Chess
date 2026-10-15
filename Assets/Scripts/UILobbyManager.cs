using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILobbyManager : MonoBehaviour
{
    [SerializeField] Toggle playerReadyToggle;
    [SerializeField] TMP_Text codeText;
    [SerializeField] TMP_Text playersReadyText;
    [SerializeField] Button menuButton;
    [SerializeField] Image fade;

    bool coroutineStarted;


    void Start()
    {
        StartCoroutine(OpeningScene());

        codeText.text = $"Invite Code: {SessionManager.Instance.Session.Code}";

        playerReadyToggle.onValueChanged.AddListener( async ready =>
        {
            await SessionManager.Instance.SetReadyAsync(ready);
        });

        SessionManager.Instance.OnSessionChanged += RefreshUI;

        menuButton.onClick.AddListener(async () => await SessionManager.Instance.QuitLobby());
    }

    void OnDisable()
    {
        SessionManager.Instance.OnSessionChanged -= RefreshUI;

    }

    void RefreshUI()
    {
        int readyCount = 0;

        foreach (var player in SessionManager.Instance.Session.Players)
        {
            if (player.Properties.TryGetValue("PlayerReady", out var r) && r.Value == "True")
            {
                readyCount++;
            }

            playersReadyText.text = $"{readyCount}/{SessionManager.Instance.Session.MaxPlayers} ready";
        }

        if (readyCount == 2 && coroutineStarted == false)
        {
            coroutineStarted = true;
            StartCoroutine(LoadingScene());
        }
    }

    IEnumerator LoadingScene()
    {
        float duration = 1f;
        float elapsed = 0f;


        Color color = fade.color;
        color.a = 0f;
        fade.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            color.a = Mathf.Clamp01(elapsed / duration);
            fade.color = color;

            yield return null;
        }
    }

    IEnumerator OpeningScene()
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
    }
}