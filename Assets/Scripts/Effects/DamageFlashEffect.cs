using System.Collections;
using UnityEngine;

public class DamageFlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.white;
    
    private SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    private Material flashMaterial;
    private Coroutine flashCoroutine;
    
    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalMaterials = new Material[spriteRenderers.Length];
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalMaterials[i] = spriteRenderers[i].material;
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
    
    private IEnumerator FlashCoroutine()
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr != null)
            {
                sr.material = flashMaterial;
                sr.color = flashColor;
            }
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && originalMaterials[i] != null)
            {
                spriteRenderers[i].material = originalMaterials[i];
                spriteRenderers[i].color = Color.white;
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
