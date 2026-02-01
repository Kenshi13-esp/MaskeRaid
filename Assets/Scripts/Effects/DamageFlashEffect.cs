using System.Collections;
using UnityEngine;

public class DamageFlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.white;
    
    private SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    private Color[] originalColors;
    private Material flashMaterial;
    private Coroutine flashCoroutine;
    
    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalMaterials = new Material[spriteRenderers.Length];
        originalColors = new Color[spriteRenderers.Length];
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalMaterials[i] = spriteRenderers[i].material;
            originalColors[i] = spriteRenderers[i].color;
        }
        
        CreateFlashMaterial();
    }
    
    private void CreateFlashMaterial()
    {
        Shader flashShader = Shader.Find("GUI/Text Shader");
        if (flashShader == null)
        {
            flashShader = Shader.Find("Sprites/Default");
        }
        
        flashMaterial = new Material(flashShader);
        flashMaterial.color = flashColor;
    }
    
    public void Flash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        flashCoroutine = StartCoroutine(FlashCoroutine());
    }
    
    public void UpdateBaseColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }
    }
    
    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalColors[i] = spriteRenderers[i].color;
                spriteRenderers[i].material = flashMaterial;
                spriteRenderers[i].color = flashColor;
            }
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && originalMaterials[i] != null)
            {
                spriteRenderers[i].material = originalMaterials[i];
                spriteRenderers[i].color = originalColors[i];
            }
        }
        
        flashCoroutine = null;
    }
    
    private void OnDestroy()
    {
        if (flashMaterial != null)
        {
            Destroy(flashMaterial);
        }
    }
}
