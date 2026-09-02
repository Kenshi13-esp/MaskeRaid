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
    
    private GameObject victoryCanvasInstance;
    private bool victoryTriggered = false;
    private GameObject gameSceneUI;
    
    public void SetExitSprite(Sprite sprite)
    {
        exitSprite = sprite;
    }
    
    public void TriggerVictory()
    {
        if (victoryTriggered) return;
        
        victoryTriggered = true;
        
        Debug.Log("[VictoryHandler] ¡VICTORIA! Mostrando pantalla de victoria...");
        
        SoundManager.PlaySound(SoundType.VICTORY);
        
        GamePause.SetGameFinished(true);
        
        DisableGameSceneUI();
        
        ShowVictoryScreen();
        
        StartCoroutine(ReturnToMainMenuAfterDelay());
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
