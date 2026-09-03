using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Presenta la historia inicial del juego en una pantalla negra con texto blanco.
/// Todo el texto aparece en una sola pagina con efecto maquina de escribir; pulsar
/// cualquier tecla acelera la escritura y, una vez completo, una pulsacion mas carga
/// la escena de juego.
/// </summary>
public class LorePresenter : MonoBehaviour
{
    private const string PlayerNamePlaceholder = "{PLAYER_NAME}";
    private const string ContinueHintText = "Press any key to continue...";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI loreText;
    [SerializeField] private TextMeshProUGUI continueHint;

    [Header("Configuracion")]
    [Tooltip("Caracteres por segundo del efecto maquina de escribir")]
    [SerializeField] private float charactersPerSecond = 45f;

    [Tooltip("Escena que se carga al terminar la presentacion")]
    [SerializeField] private string nextSceneName = "GameScene";

    private readonly string[] paragraphs =
    {
        "<i>Journal of <color=#4D9FFF>{PLAYER_NAME}</color>.</i>",
        "After countless centuries of searching, I finally spot the shores of the forgotten island on the horizon. I come in search of the tomb of <color=#FF3333>Qetza</color>, the legendary warrior.",
        "I have not traveled to the ends of the world for glory or riches, but for something more important than my own life: vengeance. I seek the mask of this legendary warrior, the one who mercilessly ravaged my civilization, to strip him of his power.",
        "However, I fear this will be no easy task.",
        "To reach the tomb of <color=#FF3333>Qetza</color>, I must first steal the masks of his two ancestral guardians: <b><color=#FF3333>Oniki</color></b>, the Insatiable, and <b><color=#FF3333>Glorbo</color></b>, the Indestructible. Only by absorbing the powers of their faces will I have the strength to survive the battle that awaits me.",
        "May the gods and the fallen walk with me. The hunt has begun..."
    };

    private bool isTyping;
    private bool waitingForInput;
    private bool skipRequested;

    private void Start()
    {
        if (continueHint != null)
        {
            continueHint.text = ContinueHintText;
            continueHint.gameObject.SetActive(false);
        }

        StartCoroutine(ShowFullLore());
    }

    private void Update()
    {
        if (!AnyKeyPressed()) return;

        if (isTyping)
        {
            skipRequested = true;
        }
        else if (waitingForInput)
        {
            waitingForInput = false;
            LoadNextScene();
        }
    }

    /// <summary>Construye el texto completo uniendo todos los parrafos y lo muestra con efecto maquina de escribir.</summary>
    private IEnumerator ShowFullLore()
    {
        isTyping = true;
        skipRequested = false;

        string fullText = BuildFullText();
        loreText.text = fullText;
        loreText.ForceMeshUpdate();

        int totalCharacters = loreText.textInfo.characterCount;
        loreText.maxVisibleCharacters = 0;

        float delay = 1f / charactersPerSecond;

        for (int i = 0; i <= totalCharacters; i++)
        {
            if (skipRequested)
            {
                loreText.maxVisibleCharacters = totalCharacters;
                break;
            }

            loreText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        loreText.maxVisibleCharacters = totalCharacters;
        isTyping = false;

        // Esperar un frame para que la pulsacion de saltar no se interprete como avanzar
        yield return null;
        skipRequested = false;

        waitingForInput = true;

        if (continueHint != null) continueHint.gameObject.SetActive(true);
    }

    /// <summary>Une todos los parrafos en un unico texto con separacion entre ellos.</summary>
    private string BuildFullText()
    {
        paragraphs[0] = paragraphs[0].Replace(PlayerNamePlaceholder, PlayerSession.PlayerName);

        const string paragraphSeparator = "\n\n";
        return string.Join(paragraphSeparator, paragraphs);
    }

    /// <summary>Comprueba si se ha pulsado cualquier tecla, boton del raton o boton del mando.</summary>
    private bool AnyKeyPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame
            || Gamepad.current.buttonEast.wasPressedThisFrame)) return true;
        return false;
    }

    private void LoadNextScene()
    {
        GamePause.ResetState();
        SceneManager.LoadScene(nextSceneName);
    }
}
