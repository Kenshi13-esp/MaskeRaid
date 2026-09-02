using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Aviso en pantalla al equipar una mascara nueva. Reacciona al evento del
/// <see cref="PlayerMaskController"/> en lugar de comprobar el estado cada frame.
/// </summary>
public class PowerDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Vacio = se busca el jugador por tag")]
    [SerializeField] private PlayerMaskController playerMaskController;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI maskNameText;
    [SerializeField] private TextMeshProUGUI maskDescriptionText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Configuracion")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeSpeed = 2f;

    private Coroutine displayRoutine;

    private void Awake()
    {
        if (playerMaskController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerMaskController = player.GetComponent<PlayerMaskController>();
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (playerMaskController != null) playerMaskController.MaskEquipped += OnMaskEquipped;
    }

    private void OnDisable()
    {
        if (playerMaskController != null) playerMaskController.MaskEquipped -= OnMaskEquipped;
    }

    private void OnMaskEquipped(MaskDefinition mask)
    {
        if (mask == null) return;

        if (maskNameText != null) maskNameText.text = $"NUEVA MASCARA: {mask.MaskName}";
        if (maskDescriptionText != null) maskDescriptionText.text = mask.Description;

        if (canvasGroup == null) return;

        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(displayDuration);

        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        displayRoutine = null;
    }
}
