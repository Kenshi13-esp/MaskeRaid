using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryHandler : MonoBehaviour
{
    [Header("Victory Settings")]
    [SerializeField] private float delayBeforeRedirect = 3f;
    
    [Header("UI Settings")]
    [SerializeField] private Sprite exitSprite;
    [SerializeField] private Vector2 exitImageSize = new Vector2(1296f, 324f);

    [Header("Marca de la partida")]
    [Tooltip("Muestra el tiempo conseguido y el puesto del ranking bajo el cartel de victoria")]
    [SerializeField] private bool showRunTime = true;

    [SerializeField] private float runTimeFontSize = 64f;
    [SerializeField] private float runTimeVerticalOffset = -240f;
    
    private GameObject victoryCanvasInstance;
    private bool victoryTriggered = false;
    private GameObject gameSceneUI;
    private string runTimeText;
    private int runPosition = -1;
    
    public void SetExitSprite(Sprite sprite)
    {
        exitSprite = sprite;
    }
    
    public void TriggerVictory()
    {
        if (victoryTriggered) return;
        
        victoryTriggered = true;
        
        Debug.Log("[VictoryHandler] ¡VICTORIA! Mostrando pantalla de victoria...");

        // Se para y se registra el cronometro antes de tocar la UI: apagar /UI deshabilita el
        // cronometro y perderiamos la referencia sin haber guardado la marca.
        StopAndRecordRun();

        SoundManager.PlaySound(SoundType.VICTORY);
        
        GamePause.SetGameFinished(true);
        
        DisableGameSceneUI();
        
        ShowVictoryScreen();
        
        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    /// <summary>Detiene el cronometro de la partida y guarda la marca en el ranking local.</summary>
    private void StopAndRecordRun()
    {
        RunTimer timer = RunTimer.Active;
        if (timer == null) return;

        runPosition = timer.StopAndRecord();
        runTimeText = timer.ElapsedText;
    }
    
    private void DisableGameSceneUI()
    {
        GameObject uiObject = GameObject.Find("UI");
        if (uiObject != null)
        {
            gameSceneUI = uiObject;
            gameSceneUI.SetActive(false);
            Debug.Log("[VictoryHandler] UI de GameScene desactivado.");
        }
    }
    
    private void ShowVictoryScreen()
    {
        GameObject canvasObj = new GameObject("VictoryCanvas");
        DontDestroyOnLoad(canvasObj);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        var canvasScaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        GameObject exitImage = new GameObject("YouWinImage");
        exitImage.transform.SetParent(canvasObj.transform, false);
        
        RectTransform rectTransform = exitImage.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = exitImageSize;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        var image = exitImage.AddComponent<UnityEngine.UI.Image>();

        // El cartel crece y rebota al aparecer. Va en la imagen y no en el canvas porque el
        // Canvas gobierna su propio RectTransform y la escala del raiz no se respeta.
        exitImage.AddComponent<PopUpAppearAnimation>();
        
        if (exitSprite != null)
        {
            image.sprite = exitSprite;
            image.preserveAspect = true;
            Debug.Log("[VictoryHandler] Sprite 'you_win' asignado correctamente.");
        }
        else
        {
            Debug.LogWarning("[VictoryHandler] No hay sprite asignado. Usando color verde.");
            image.color = Color.green;
        }
        
        victoryCanvasInstance = canvasObj;

        if (showRunTime) CreateRunTimeLabel(canvasObj.transform);
    }

    /// <summary>
    /// Anade bajo el cartel el tiempo conseguido y el puesto del ranking. El texto se crea por
    /// codigo porque el canvas de victoria tambien se construye en tiempo de ejecucion.
    /// </summary>
    private void CreateRunTimeLabel(Transform canvasTransform)
    {
        if (string.IsNullOrEmpty(runTimeText)) return;

        GameObject labelObject = new GameObject("RunTimeLabel");
        labelObject.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = labelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, runTimeVerticalOffset);
        rectTransform.sizeDelta = new Vector2(1200f, 200f);

        TMPro.TextMeshProUGUI label = labelObject.AddComponent<TMPro.TextMeshProUGUI>();
        label.fontSize = runTimeFontSize;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.text = runPosition > 0
            ? $"{PlayerSession.PlayerName}   {runTimeText}\nPUESTO {runPosition}"
            : $"{PlayerSession.PlayerName}   {runTimeText}";

        labelObject.AddComponent<PopUpAppearAnimation>();
    }
    
    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        Debug.Log($"[VictoryHandler] Redirigiendo al Main Menu en {delayBeforeRedirect} segundos...");
        
        yield return new WaitForSecondsRealtime(delayBeforeRedirect);
        
        Debug.Log("[VictoryHandler] Cargando Main Menu...");
        
        GamePause.ResetState();
        
        if (victoryCanvasInstance != null)
        {
            Destroy(victoryCanvasInstance);
        }
        
        SceneManager.LoadScene("MainMenu");
    }
    
    private void OnDestroy()
    {
        if (victoryCanvasInstance != null)
        {
            Destroy(victoryCanvasInstance);
        }
        
        if (gameSceneUI != null)
        {
            gameSceneUI.SetActive(true);
        }
    }
}
