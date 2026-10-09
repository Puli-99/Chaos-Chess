using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILobbyManager : MonoBehaviour
{
    [SerializeField] Toggle playerReadyToggle;
    [SerializeField] TMP_Text codeText;
    [SerializeField] TMP_Text playersReadyText;

    void Start()
    {
        codeText.text = $"Invite Code: {SessionManager.Instance.Session.Code}";

        playerReadyToggle.onValueChanged.AddListener( async ready =>
        {
            await SessionManager.Instance.SetReadyAsync(ready);
        });

        SessionManager.Instance.OnSessionChanged += RefreshUI;
    }

    private void OnDisable()
    {
        SessionManager.Instance.OnSessionChanged -= RefreshUI;
    }

    void RefreshUI()
    {
        int readyCount = 0;
        int total = 0;

        foreach (var player in SessionManager.Instance.Session.Players)
        {
            total++;

            if (player.Properties.TryGetValue("PlayerReady", out var r) && r.Value == "True")
            {
                readyCount++;
            }

            playersReadyText.text = $"{readyCount}/{total} ready";
        }
    }
}