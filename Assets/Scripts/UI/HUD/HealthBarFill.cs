using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica el relleno de una barra de vida respetando el estilo visual configurado en el Image.
///
/// Unity solo tiene en cuenta <see cref="Image.fillAmount"/> cuando el tipo de imagen es
/// <see cref="Image.Type.Filled"/>; con Sliced o Tiled (los que conservan los bordes del sprite
/// 9-slice de los marcos del HUD) escribir fillAmount no cambia nada en pantalla. Por eso el
/// vaciado se resuelve recortando el ancho del RectTransform cuando la imagen no es Filled.
/// </summary>
public class HealthBarFill
{
    private const float StretchEpsilon = 0.0001f;

    private Image image;
    private RectTransform rectTransform;
    private bool usesImageFillAmount;
    private bool usesAnchorWidth;
    private float baseAnchorMinX;
    private float baseAnchorMaxX;
    private float baseWidth;

    /// <summary>Indica si hay una imagen valida a la que aplicar el relleno.</summary>
    public bool IsValid => image != null;

    /// <summary>Ultimo relleno normalizado aplicado. -1 mientras no se ha aplicado ninguno.</summary>
    public float Current { get; private set; } = -1f;

    /// <summary>Cachea el modo de vaciado sin alterar el estilo elegido en el inspector.</summary>
    public void Initialize(Image fillImage)
    {
        image = fillImage;
        Current = -1f;

        if (image == null) return;

        rectTransform = image.rectTransform;
        usesImageFillAmount = image.type == Image.Type.Filled;

        if (usesImageFillAmount)
        {
            // Con Filled si conviene fijar direccion y origen: la barra debe vaciarse desde la derecha.
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillCenter = true;
            return;
        }

        baseAnchorMinX = rectTransform.anchorMin.x;
        baseAnchorMaxX = rectTransform.anchorMax.x;
        usesAnchorWidth = baseAnchorMaxX - baseAnchorMinX > StretchEpsilon;

        if (usesAnchorWidth) return;

        // Sin estiramiento horizontal se recorta el ancho fijo, que exige pivote a la izquierda
        // para que la barra se coma desde la derecha en lugar de encogerse hacia el centro.
        baseWidth = rectTransform.rect.width;

        Vector2 pivot = rectTransform.pivot;
        if (Mathf.Approximately(pivot.x, 0f)) return;

        pivot.x = 0f;
        rectTransform.pivot = pivot;
    }

    /// <summary>Aplica un relleno normalizado (0 = vacia, 1 = llena).</summary>
    public void Apply(float fill)
    {
        Current = fill;

        if (image == null) return;

        if (usesImageFillAmount)
        {
            image.fillAmount = fill;
            return;
        }

        if (rectTransform == null) return;

        if (usesAnchorWidth)
        {
            Vector2 anchorMax = rectTransform.anchorMax;
            anchorMax.x = baseAnchorMinX + (baseAnchorMaxX - baseAnchorMinX) * fill;
            rectTransform.anchorMax = anchorMax;
            return;
        }

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseWidth * fill);
    }

    /// <summary>Convierte vida absoluta en relleno normalizado.</summary>
    public static float ResolveFill(int currentHP, int maxHP)
    {
        return maxHP <= 0 ? 0f : Mathf.Clamp01((float)currentHP / maxHP);
    }
}
