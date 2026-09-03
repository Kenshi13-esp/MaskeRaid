using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Presenta la historia inicial del juego en una pantalla negra con texto blanco.
/// Cada parrafo aparece con efecto maquina de escribir; pulsar cualquier tecla acelera
/// la escritura y, una vez completo, avanza al siguiente parrafo. Al terminar, carga
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
    [SerializeField] private float charactersPerSecond = 30f;

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

    private int currentParagraphIndex;
    private bool isTyping;
    private bool waitingForInput;
    private bool skipRequested;

    private void Start()
    {
        paragraphs[0] = paragraphs[0].Replace(PlayerNamePlaceholder, $"<color=#4D9FFF>{PlayerSession.PlayerName}</color>");

        currentParagraphIndex = 0;

        if (continueHint != null)
        {
            continueHint.text = ContinueHintText;
            continueHint.gameObject.SetActive(false);
        }

        StartCoroutine(ShowParagraph());
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
            Advance();
        }
    }

    /// <summary>Avanza al siguiente parrafo o carga la escena de juego si era el ultimo.</summary>
    private void Advance()
    {
        if (continueHint != null) continueHint.gameObject.SetActive(false);

        currentParagraphIndex++;

        if (currentParagraphIndex >= paragraphs.Length)
        {
            LoadNextScene();
        }
        else
        {
            StartCoroutine(ShowParagraph());
        }
    }

    private IEnumerator ShowParagraph()
    {
        isTyping = true;
        skipRequested = false;

        loreText.text = paragraphs[currentParagraphIndex];
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
