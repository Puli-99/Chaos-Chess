using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField] Button tutorialButton;
    [SerializeField] Button createLobbyButton;
    [SerializeField] Button joinLobbyButton;
    [SerializeField] TMP_InputField lobbyCodeInputField;
    [SerializeField] InputActionReference submitAction;
    [SerializeField] bl_LoadingEffect loadingCircle;
    [SerializeField] GameObject errorImage;
    [SerializeField] TMP_Text errorText;

    void Start()
    {
        createLobbyButton.onClick.AddListener(async () => await SessionManager.Instance.InitializeLobbyAsync());
        joinLobbyButton.onClick.AddListener(async () => await SessionManager.Instance.JoinLobbyWithCode(lobbyCodeInputField.text));

    }

    void OnEnable()
    {
        submitAction.action.performed += OnSubmit;

        SessionManager.Instance.OnCreateLobby += OnLoading;
        SessionManager.Instance.OnCreateLobbyException += OnLoadingFailed;

        SessionManager.Instance.OnJoinLobby += OnLoading;
        SessionManager.Instance.OnJoinLobbyException += OnLoadingFailed;
    }

    void OnDisable()
    {
        submitAction.action.performed -= OnSubmit;
        SessionManager.Instance.OnCreateLobby -= OnLoading;
        SessionManager.Instance.OnCreateLobbyException -= OnLoadingFailed;
    }

    void OnSubmit(InputAction.CallbackContext context)
    {
        if (lobbyCodeInputField.text.IsNullOrEmpty()) return;

        if (lobbyCodeInputField.isFocused)
        {
            joinLobbyButton.onClick?.Invoke();
        }
    }

    void OnLoading()
    {
        ChangeButtonsEnability();
        loadingCircle.isLoading = true;
    }

    void  OnLoadingFailed(string exception)
    {
        errorImage.SetActive(true);
        errorText.text = exception;
        ChangeButtonsEnability();        
        loadingCircle.isLoading = false;
    }

    void ChangeButtonsEnability()
    {
        tutorialButton.enabled = !tutorialButton.enabled;
        createLobbyButton.enabled = !createLobbyButton.enabled;
        joinLobbyButton.enabled = !joinLobbyButton.enabled;
    }

    public void CloseErrorBox()
    {
        errorImage.SetActive(false);
        errorText.text = "";
    }

    public void TutorialScene()
    {
        SceneManager.LoadScene("LocalGameScene");
    }
}