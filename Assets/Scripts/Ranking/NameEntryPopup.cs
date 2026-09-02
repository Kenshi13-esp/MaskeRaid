using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pop up de introduccion de nombre, en plan marcador de arcade. Sale al pulsar Jugar y, al
/// confirmar, guarda el nombre en <see cref="PlayerSession"/> y carga la escena de juego.
///
/// El nombre se filtra a mayusculas y solo letras y numeros mientras se escribe, para que lo que
/// se ve en el campo sea exactamente lo que acabara en la tabla del ranking.
/// </summary>
public class NameEntryPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameField;

    [Tooltip("Boton que confirma el nombre y empieza la partida")]
    [SerializeField] private Button confirmButton;

    [Tooltip("Boton que cierra el pop up sin jugar")]
    [SerializeField] private Button closeButton;

    [Header("Escena")]
    [Tooltip("Escena que se carga al confirmar el nombre")]
    [SerializeField] private string sceneToLoad = "GameScene";

    private bool isSanitizing;

    private void OnEnable()
    {
        if (nameField != null)
        {
            nameField.characterLimit = PlayerSession.MaxNameLength;
            nameField.onValueChanged.AddListener(OnNameChanged);
            nameField.onSubmit.AddListener(OnNameSubmitted);

            nameField.SetTextWithoutNotify(PlayerSession.PlayerName);

            // El foco se pide un fotograma despues: MenuPanel tambien elige seleccion al
            // activarse el panel, y si se hace en el mismo fotograma gana el boton.
            StartCoroutine(FocusFieldNextFrame());
        }

        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (closeButton != null) closeButton.onClick.AddListener(Cancel);

        UpdateConfirmState();
    }

    private void OnDisable()
    {
        if (nameField != null)
        {
            nameField.onValueChanged.RemoveListener(OnNameChanged);
            nameField.onSubmit.RemoveListener(OnNameSubmitted);
        }

        if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
        if (closeButton != null) closeButton.onClick.RemoveListener(Cancel);
    }

    /// <summary>
    /// Guarda el nombre escrito y arranca la partida. Es publica para poder llamarla tambien
    /// desde el OnClick de un boton de la escena.
    /// </summary>
    public void Confirm()
    {
        string playerName = PlayerSession.Sanitize(nameField != null ? nameField.text : string.Empty);

        if (string.IsNullOrEmpty(playerName)) return;

        PlayerSession.PlayerName = playerName;

        // El estado global puede venir sucio de una partida anterior terminada, que deja el
        // tiempo congelado: sin esto la escena nueva arrancaria parada.
        GamePause.ResetState();

        SceneManager.LoadScene(sceneToLoad);
    }

    /// <summary>Cierra el pop up sin empezar la partida.</summary>
    public void Cancel()
    {
        gameObject.SetActive(false);
    }

    private void OnNameChanged(string value)
    {
        // El propio filtrado reescribe el campo y vuelve a disparar el evento: la bandera corta
        // esa segunda vuelta.
        if (isSanitizing) return;

        string sanitized = PlayerSession.Sanitize(value);

        if (sanitized != value)
        {
            isSanitizing = true;
            nameField.SetTextWithoutNotify(sanitized);
            nameField.caretPosition = sanitized.Length;
            isSanitizing = false;
        }

        UpdateConfirmState();
    }

    private void OnNameSubmitted(string value)
    {
        Confirm();
    }

    /// <summary>Deja el cursor en el campo para poder escribir sin tener que pulsarlo.</summary>
    private System.Collections.IEnumerator FocusFieldNextFrame()
    {
        yield return null;

        if (nameField == null || !nameField.gameObject.activeInHierarchy) yield break;

        nameField.Select();
        nameField.ActivateInputField();
        nameField.caretPosition = nameField.text.Length;
    }

    /// <summary>Un nombre vacio no vale como marca, asi que el boton se apaga hasta que haya uno.</summary>
    private void UpdateConfirmState()
    {
        if (confirmButton == null) return;

        string playerName = PlayerSession.Sanitize(nameField != null ? nameField.text : string.Empty);

        confirmButton.interactable = !string.IsNullOrEmpty(playerName);
    }
}
