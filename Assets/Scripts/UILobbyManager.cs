using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILobbyManager : MonoBehaviour
{
    [SerializeField] Toggle playerReadyToggle;
    [SerializeField] TMP_Text codeText;
    [SerializeField] TMP_Text playersReadyText;
    [SerializeField] TMP_Text connectedPlayersText;
    [SerializeField] Button menuButton;

    void Start()
    {
        codeText.text = $"Invite Code: {SessionManager.Instance.Session.Code}";

        playerReadyToggle.onValueChanged.AddListener( async ready =>
        {
            await SessionManager.Instance.SetReadyAsync(ready);
        });

        SessionManager.Instance.OnSessionChanged += RefreshUI;

        menuButton.onClick.AddListener(async () => await SessionManager.Instance.QuitLobby());
    }

    private void Update()
    {
        connectedPlayersText.text = SessionManager.Instance.Session.PlayerCount.ToString();
    }

    private void OnDisable()
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
    }
}