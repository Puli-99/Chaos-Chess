using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;

public enum MatchType { None, FlagCapturem, Deathmatch } // Ejemplo de la clase

public class SessionManager : MonoBehaviour
{
    NetworkManager NetManager => NetworkManager.Singleton;
    public event Action OnSessionChanged;
    ISession session;
    public ISession Session => session;
    bool isStartingGame = false;

    const string c_GameScene = "GameScene";
    const string c_LobbyScene = "LobbyScene";
    const string c_Key_MatchType = "MatchType";
    const string c_Key_PlayerName = "PlayerName";
    const string c_Key_PlayerReady = "PlayerReady";


    #region Singleton
    public static SessionManager Instance { private set; get; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    void Start()
    {
        NetManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectedCallback;
        NetManager.OnServerStarted += NetworkManager_OnServerStarted;
        NetManager.OnServerStopped += NetworkManager_OnServerStopped;
    }


    async Task<bool> InitializeUGS()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log("Sign in succeed: " + AuthenticationService.Instance.PlayerId);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    public async Task InitializeLobbyAsync(string hostName = default, int maxPlayers = 2, bool isPrivate = false, MatchType matchType = MatchType.None)
    {
        try
        {
            if (!await InitializeUGS()) Debug.LogError("UGS not initialized");

            hostName ??= AuthenticationService.Instance.PlayerId;

            var options = new SessionOptions
            {
                Name = hostName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate
            };

            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            SuscribeEvents();
            IHostSession hostSession = session as IHostSession;

            if (hostSession != null)
            {
                hostSession.SetProperties(new Dictionary<string, SessionProperty>
                {
                    [c_Key_MatchType] = new SessionProperty(matchType.ToString(), VisibilityPropertyOptions.Public)
                });

                hostSession.CurrentPlayer.SetProperty(c_Key_PlayerName, new PlayerProperty(hostName, VisibilityPropertyOptions.Public));
            }

            await hostSession.SaveCurrentPlayerDataAsync();
            await hostSession.SavePropertiesAsync();

            SceneManager.LoadScene(c_LobbyScene, LoadSceneMode.Single);

            Debug.Log($"Player Name: {session.CurrentPlayer.Properties[c_Key_PlayerName].Value}");
            Debug.Log($"MatchType : {session.Properties[c_Key_MatchType].Value}");
            Debug.Log("Session Code: " + session.Code); // cambiar esto a UI

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }


    public async Task JoinLobbyWithCode(string joinCode, string playerName = default)
    {
        try
        {
            if (!await InitializeUGS()) Debug.LogError("UGS not initialized");
            playerName ??= AuthenticationService.Instance.PlayerId;

            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

            SuscribeEvents();

            session.CurrentPlayer.SetProperty(c_Key_PlayerName, new PlayerProperty(playerName, VisibilityPropertyOptions.Public));
            await session.SaveCurrentPlayerDataAsync();

            SceneManager.LoadScene(c_LobbyScene, LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }


    public async Task SetReadyAsync(bool isReady = true)
    {
        session.CurrentPlayer.SetProperty(c_Key_PlayerReady, new PlayerProperty(isReady.ToString(), VisibilityPropertyOptions.Public));
        await session.SaveCurrentPlayerDataAsync();
    }

    void SuscribeEvents()
    {
        session.Changed += Session_Changed;
    }

    void DesuscribeEvents()
    {
        session.Changed -= Session_Changed;
    }
    private async void Session_Changed()
    {
        // aca va el Relay
        Debug.Log("Players connected: " + session.PlayerCount);
        OnSessionChanged?.Invoke();

        if (!session.IsHost || isStartingGame) return;

        int readyCount = 0;

        foreach (var player in session.Players)
        {
            if (player.Properties.TryGetValue("PlayerReady", out var r) && r.Value == "True")
            {
                readyCount++;
            }
        }

        if (readyCount < session.PlayerCount) return;

        isStartingGame = true;

        try
        {
            var hostSession = session.AsHost();
            hostSession.IsLocked = true;
            await hostSession.SavePropertiesAsync();
            await hostSession.Network.StartRelayNetworkAsync(new RelayNetworkOptions());
            NetManager.SceneManager.LoadScene(c_GameScene, LoadSceneMode.Single);

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void NetworkManager_OnClientConnectedCallback(ulong clientID)
    {
        if (clientID == NetworkManager.ServerClientId)
        {
            Debug.Log("HostConnected");
        }
        else
        {
            Debug.Log("ClientConnected");
        }
    }

    void NetworkManager_OnClientDisconnectedCallback(ulong clientID)
    {
        Debug.Log("Client Disconnected " + clientID);
    }

    void NetworkManager_OnServerStarted()
    {
        Debug.Log("Server Started!");
        NetManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadLevelCompleted;
    }

    void NetworkManager_OnServerStopped(bool obj)
    {
        Debug.Log("Server Stopped");
    }
    void SceneManager_OnLoadLevelCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"All loaded {sceneName}, with: {clientsCompleted.Count}. {clientsTimedOut.Count} timed out");
    }

}