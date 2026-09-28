using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{

    NetworkManager NetManager => NetworkManager.Singleton;
    UnityTransport utp;

    const string c_GameScene = "GameScene";
    const string c_Host_Tag = "HostTag";
    const string c_Client_Tag = "ClientTag";

    [SerializeField] int playersToStartGame;
    [SerializeField] string ipAdress = "127.0.0.1";
    [SerializeField] ushort port = 7777;



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
        utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

        NetManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectedCallback;
        NetManager.OnServerStarted += NetworkManager_OnServerStarted;
        NetManager.OnServerStopped += NetworkManager_OnServerStopped;

        AutoStartFromTags();
    }


    void NetworkManager_OnClientConnectedCallback(ulong clientID)
    {

        string connectionMessage = clientID == NetworkManager.ServerClientId ? "Host Connected " : "Client Connected ";
        Debug.Log(connectionMessage + clientID);


        if (!NetManager.IsServer) { return;}

        if (NetManager.ConnectedClientsList.Count == playersToStartGame)
        {
            NetManager.SceneManager.LoadScene(c_GameScene, LoadSceneMode.Single);
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

    public void StartHost()
    {
        utp.SetConnectionData(ipAdress, port);
        NetManager.StartHost();
    }
    public void StartClient()
    {
        utp.SetConnectionData(ipAdress, port);
        NetManager.StartClient();
    }



#if UNITY_EDITOR

    void AutoStartFromTags()
    {
        var tags = CurrentPlayer.Tags;

        if (tags.Contains(c_Host_Tag))
        {
            StartHost();
        }

        if (tags.Contains(c_Client_Tag))
        {
            StartClient();
        }
    }

#endif




}