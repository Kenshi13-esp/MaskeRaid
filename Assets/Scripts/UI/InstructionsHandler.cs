using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra a pantalla completa la lamina de instrucciones de la mascara recien equipada.
///
/// El canvas se crea una unica vez y luego solo se activa y desactiva, en lugar de construirlo y
/// destruirlo entero en cada recompensa. La espera usa tiempo real para que la camara lenta del
/// dash no alargue su duracion en pantalla.
/// </summary>
public class InstructionsHandler : MonoBehaviour
{
    private const int CanvasSortingOrder = 99;
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    [Header("Instructions Settings")]
    [SerializeField] private float displayDuration = 4f;

    [Header("UI Settings")]
    [SerializeField] private Sprite instructionsSprite;
    [SerializeField] private Vector2 imageSize = new Vector2(800f, 600f);

    private GameObject instructionsCanvas;
    private Image instructionsImage;
    private Coroutine hideRoutine;

    /// <summary>Cambia la lamina que se mostrara en la siguiente llamada a ShowInstructions.</summary>
    public void SetInstructionsSprite(Sprite sprite)
    {
        instructionsSprite = sprite;
    }

    /// <summary>Muestra la lamina y la oculta sola tras la duracion configurada.</summary>
    public void ShowInstructions()
    {
        if (instructionsSprite == null) return;

        EnsureCanvas();

        instructionsImage.sprite = instructionsSprite;
        instructionsCanvas.SetActive(true);

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        if (instructionsCanvas != null) instructionsCanvas.SetActive(false);

        hideRoutine = null;
    }

    private void EnsureCanvas()
    {
        if (instructionsCanvas != null) return;

        instructionsCanvas = new GameObject("InstructionsCanvas");

        Canvas canvas = instructionsCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        CanvasScaler canvasScaler = instructionsCanvas.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("InstructionsImage");
        imageObject.transform.SetParent(instructionsCanvas.transform, false);

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = imageSize;

        instructionsImage = imageObject.AddComponent<Image>();
        instructionsImage.preserveAspect = true;
        instructionsImage.raycastTarget = false;

        // La lamina crece y rebota al aparecer. Va en la imagen y no en el canvas porque el
        // Canvas gobierna su propio RectTransform y la escala del raiz no se respeta.
        imageObject.AddComponent<PopUpAppearAnimation>();

        instructionsCanvas.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instructionsCanvas != null) Destroy(instructionsCanvas);
    }
}
