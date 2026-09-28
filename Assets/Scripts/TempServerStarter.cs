using System;
using UnityEngine;
using UnityEngine.UI;

public class TempServerStarter : MonoBehaviour
{
    [SerializeField] Button startHostButton;
    [SerializeField] Button startClientButton;

    public event Action onStartHost;
    public event Action onStartClient;

    void Start()
    {
        startHostButton.onClick.AddListener(() => onStartHost?.Invoke());
        startClientButton.onClick.AddListener(() => onStartClient?.Invoke());

        onStartHost += SessionManager.Instance.StartHost;
        onStartClient += SessionManager.Instance.StartClient;

    }
}
