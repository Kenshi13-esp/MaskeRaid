using System.Collections;
using UnityEngine;

/// <summary>
/// Destello blanco al recibir dano. Sustituye temporalmente el material de los sprites del
/// objeto y restaura el original al terminar.
///
/// Usa sharedMaterial en lugar de material: leer o escribir <c>renderer.material</c> clona el
/// material en tiempo de ejecucion, asi que la version anterior creaba una copia por sprite al
/// arrancar y otra en cada destello, rompiendo el batching. El material del destello tambien es
/// unico y compartido por todos los objetos que parpadean.
/// </summary>
public class DamageFlashEffect : MonoBehaviour
{
    private const string TextShaderName = "GUI/Text Shader";
    private const string SpriteShaderName = "Sprites/Default";

    private static Material sharedFlashMaterial;

    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.white;

    private SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    private Color[] originalColors;
    private WaitForSeconds flashWait;
    private Coroutine flashCoroutine;

    private static Material FlashMaterial
    {
        get
        {
            if (sharedFlashMaterial == null)
            {
                Shader flashShader = Shader.Find(TextShaderName);
                if (flashShader == null) flashShader = Shader.Find(SpriteShaderName);

                sharedFlashMaterial = new Material(flashShader) { name = "SharedDamageFlash" };
            }

            return sharedFlashMaterial;
        }
    }

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalMaterials = new Material[spriteRenderers.Length];
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalMaterials[i] = spriteRenderers[i].sharedMaterial;
            originalColors[i] = spriteRenderers[i].color;
        }

        flashWait = new WaitForSeconds(flashDuration);
    }

    /// <summary>Lanza el destello, reiniciandolo si ya habia uno en curso.</summary>
    public void Flash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    /// <summary>
    /// Vuelve a leer los colores base de los sprites. La usan los cambios de fase, que tinen
    /// al boss de otro color y deben conservarlo al dejar de parpadear.
    /// </summary>
    public void UpdateBaseColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null) originalColors[i] = spriteRenderers[i].color;
        }
    }

    private IEnumerator FlashCoroutine()
    {
        Material flashMaterial = FlashMaterial;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;

            originalColors[i] = spriteRenderers[i].color;
            spriteRenderers[i].sharedMaterial = flashMaterial;
            spriteRenderers[i].color = flashColor;
        }

        yield return flashWait;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null || originalMaterials[i] == null) continue;

            spriteRenderers[i].sharedMaterial = originalMaterials[i];
            spriteRenderers[i].color = originalColors[i];
        }

        flashCoroutine = null;
    }
}
