using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TempServerStarter : MonoBehaviour
{
    [SerializeField] Button createLobbyButton;
    [SerializeField] Button joinLobbyButton;
    [SerializeField] TMP_InputField lobbyCodeInputField;
    [SerializeField] InputActionReference submitAction;

    void Start()
    {
        createLobbyButton.onClick.AddListener(async () => await SessionManager.Instance.InitializeLobbyAsync());
        joinLobbyButton.onClick.AddListener(async () => await SessionManager.Instance.JoinLobbyWithCode(lobbyCodeInputField.text));
    }

    void OnEnable()
    {
        submitAction.action.performed += OnSubmit;
    }

    void OnDisable()
    {
        submitAction.action.performed -= OnSubmit;
    }

    void OnSubmit(InputAction.CallbackContext context)
    {
        if (lobbyCodeInputField.isFocused)
        {
            joinLobbyButton.onClick?.Invoke();
        }
    }
}
