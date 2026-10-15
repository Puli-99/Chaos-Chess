using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MatchType { None, FlagCapturem, Deathmatch } // Ejemplo de la clase

public class SessionManager : MonoBehaviour
{
    NetworkManager NetManager => NetworkManager.Singleton;
    public event Action OnSessionChanged;
    public event Action OnCreateLobby;
    public event Action<string> OnCreateLobbyException;
    public Func<string> message;
    public event Action OnJoiningLobby;
    public event Action OnJoinedLobby;
    public event Action<string> OnJoinLobbyException;
    ISession session;
    public ISession Session => session;
    bool isStartingGame = false;

    const string c_MenuScene = "MenuScene";
    const string c_GameScene = "OnlineGameScene";
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
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();

            //Debug.Log("Sign in succeed: " + AuthenticationService.Instance.PlayerId);
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

            OnCreateLobby?.Invoke();

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


            OnJoinedLobby?.Invoke();

           // Debug.Log($"Player Name: {session.CurrentPlayer.Properties[c_Key_PlayerName].Value}");
           // Debug.Log($"MatchType : {session.Properties[c_Key_MatchType].Value}");
           // Debug.Log("Session Code: " + session.Code); // cambiar esto a UI

        }
        catch (Exception e)
        {
            OnCreateLobbyException?.Invoke("Could not create lobby. Try again"); // cambiar esto a un switch de tipos de excepcion
            Debug.LogException(e);
        }
    }    

    public async Task JoinLobbyWithCode(string joinCode, string playerName = default)
    {
        try
        {
            if (!await InitializeUGS()) Debug.LogError("UGS not initialized");

            OnJoiningLobby?.Invoke();

            playerName ??= AuthenticationService.Instance.PlayerId;

            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

            SuscribeEvents();

            session.CurrentPlayer.SetProperty(c_Key_PlayerName, new PlayerProperty(playerName, VisibilityPropertyOptions.Public));
            await session.SaveCurrentPlayerDataAsync();

            OnJoinedLobby?.Invoke();
        }
        catch (Exception e)
        {
            string message = e.Message switch // bloque temporal hasta encontrar una mejor forma de catchear tipo de excepcion
            {
                var m when m.Contains("Argument should be non-null, non-empty & not only whitespaces.")
                    => "Empty code",

                var m when m.Contains("contains an invalid character")
                    => "Invalid join code",

                var m when m.Contains("lobby not found")
                    => "Lobby not found",

                _ => "Could not join lobby. Try again"
            };

            Debug.LogException(e);
            OnJoinLobbyException?.Invoke(message);        }
    }

    public async Task QuitLobby()
    {
        await session.LeaveAsync();
        SceneManager.LoadScene(c_MenuScene, LoadSceneMode.Single);
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

    async void Session_Changed()
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

        if (readyCount != session.MaxPlayers) return;

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

    #region Callbacks

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

    void AuthenticationCallbacks()
    {
        AuthenticationService.Instance.SignInFailed += (err) => { Debug.Log("Sign in failed: " + err); };
        AuthenticationService.Instance.SignedOut += () => { Debug.Log("Player signed out."); };
        AuthenticationService.Instance.Expired += () => { Debug.Log("Player session could not be refreshed and expired."); };
    }
    #endregion
}