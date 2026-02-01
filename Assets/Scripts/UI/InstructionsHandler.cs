using System.Collections;
using UnityEngine;

public class InstructionsHandler : MonoBehaviour
{
    [Header("Instructions Settings")]
    [SerializeField] private float displayDuration = 4f;
    
    [Header("UI Settings")]
    [SerializeField] private Sprite instructionsSprite;
    [SerializeField] private Vector2 imageSize = new Vector2(800f, 600f);
    
    private GameObject instructionsCanvasInstance;
    private bool isShowing = false;
    
    public void SetInstructionsSprite(Sprite sprite)
    {
        instructionsSprite = sprite;
    }
    
    public void ShowInstructions()
    {
        if (isShowing || instructionsSprite == null)
        {
            Debug.LogWarning("[InstructionsHandler] Ya se están mostrando instrucciones o no hay sprite asignado.");
            return;
        }
        
        isShowing = true;
        
        Debug.Log("[InstructionsHandler] Mostrando instrucciones de Glorbo...");
        
        ShowInstructionsScreen();
        
        StartCoroutine(HideInstructionsAfterDelay());
    }
    
    private void ShowInstructionsScreen()
    {
        GameObject canvasObj = new GameObject("InstructionsCanvas");
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        
        var canvasScaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        GameObject instructionsImage = new GameObject("InstructionsImage");
        instructionsImage.transform.SetParent(canvasObj.transform, false);
        
        RectTransform rectTransform = instructionsImage.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = imageSize;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        var image = instructionsImage.AddComponent<UnityEngine.UI.Image>();
        
        if (instructionsSprite != null)
        {
            image.sprite = instructionsSprite;
            image.preserveAspect = true;
            Debug.Log("[InstructionsHandler] Sprite de instrucciones asignado correctamente.");
        }
        else
        {
            Debug.LogWarning("[InstructionsHandler] No hay sprite asignado.");
            image.color = new Color(0.2f, 0.5f, 1f, 0.8f);
        }
        
        instructionsCanvasInstance = canvasObj;
    }
    
    private IEnumerator HideInstructionsAfterDelay()
    {
        Debug.Log($"[InstructionsHandler] Ocultando instrucciones en {displayDuration} segundos...");
        
        yield return new WaitForSeconds(displayDuration);
        
        Debug.Log("[InstructionsHandler] Ocultando instrucciones...");
        
        if (instructionsCanvasInstance != null)
        {
            Destroy(instructionsCanvasInstance);
        }
        
        isShowing = false;
    }
    
    private void OnDestroy()
    {
        if (instructionsCanvasInstance != null)
        {
            Destroy(instructionsCanvasInstance);
        }
    }
}
