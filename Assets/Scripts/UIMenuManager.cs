using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

public class UIMenuManager : MonoBehaviour
{
    [Header("Interactables")]
    [SerializeField] Button tutorialButton;
    [SerializeField] Button createLobbyButton;
    [SerializeField] Button joinLobbyButton;
    [SerializeField] TMP_InputField lobbyCodeInputField;

    [Header("Input References")]
    [SerializeField] InputActionReference submitAction;

    [Header("Error box")]
    [SerializeField] GameObject errorImage;
    [SerializeField] TMP_Text errorText;

    [Header("Loading effects")]
    [SerializeField] bl_LoadingEffect loadingCircle;
    [SerializeField] Image fade;

    const string c_LobbyScene = "LobbyScene";
    const string c_TutorialScene = "LocalGameScene";


    void Start()
    {
        StartCoroutine(OpeningScene());
        createLobbyButton.onClick.AddListener(async () => await SessionManager.Instance.InitializeLobbyAsync());
        joinLobbyButton.onClick.AddListener(async () => await SessionManager.Instance.JoinLobbyWithCode(lobbyCodeInputField.text));
    }

    void OnEnable()
    {
        submitAction.action.performed += OnSubmit;

        SessionManager.Instance.OnCreateLobby += OnLoading;
        SessionManager.Instance.OnCreateLobbyException += OnLoadingFailed;

        SessionManager.Instance.OnJoiningLobby += OnLoading;
        SessionManager.Instance.OnJoinedLobby += LoadScene;
        SessionManager.Instance.OnJoinLobbyException += OnLoadingFailed;
    }

    void OnDisable()
    {
        submitAction.action.performed -= OnSubmit;

        SessionManager.Instance.OnCreateLobby -= OnLoading;
        SessionManager.Instance.OnCreateLobbyException -= OnLoadingFailed;

        SessionManager.Instance.OnJoiningLobby -= OnLoading;
        SessionManager.Instance.OnJoinedLobby -= LoadScene;
        SessionManager.Instance.OnJoinLobbyException -= OnLoadingFailed;
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

    void LoadScene()
    {
        StartCoroutine(LoadingScene(c_LobbyScene));
    }

    void OnLoadingFailed(string exception)
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
        StartCoroutine(LoadingScene(c_TutorialScene));
    }

    IEnumerator LoadingScene(string scene)
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


        if (loadingCircle.isLoading)
            loadingCircle.isLoading = false;

        SceneManager.LoadScene(scene);
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