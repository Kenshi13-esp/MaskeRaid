using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Control de volumen maestro del menu: muestra u oculta el slider y guarda el valor.
/// Responde tanto al clic del raton como al boton de aceptar del mando.
/// El volumen se escribe en PlayerPrefs en memoria en cada cambio, pero el volcado a disco
/// (PlayerPrefs.Save) se aplaza al cerrar el panel o el juego, porque hacerlo en cada
/// fotograma mientras se arrastra el slider provocaba una escritura a disco por frame.
/// </summary>
public class VolumeController : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private const string VolumePrefKey = "MasterVolume";

    [Header("UI References")]
    [SerializeField] private GameObject sliderObject;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button volumeButton;

    [Header("Volume Settings")]
    [SerializeField] private float defaultVolume = 0.5f;

    private RectTransform sliderRect;
    private MenuPanel sliderPanel;
    private bool hasUnsavedVolume;

    private void Awake()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (volumeButton == null) volumeButton = GetComponent<Button>();
        if (volumeButton != null) volumeButton.onClick.RemoveAllListeners();

        if (sliderObject == null) return;

        sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderPanel = sliderObject.GetComponent<MenuPanel>();

        if (sliderPanel != null) sliderPanel.BackRequested += FlushVolume;
    }

    private void Start()
    {
        LoadVolume();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (sliderObject == null || IsPointerOverSlider(eventData)) return;

        ToggleSlider();
    }

    /// <summary>Aceptar con mando o teclado sobre el boton de volumen.</summary>
    public void OnSubmit(BaseEventData eventData)
    {
        ToggleSlider();
    }

    /// <summary>Muestra u oculta el slider de volumen.</summary>
    public void ToggleSlider()
    {
        if (sliderObject == null) return;

        bool showSlider = !sliderObject.activeSelf;
        sliderObject.SetActive(showSlider);

        if (!showSlider) FlushVolume();
    }

    private bool IsPointerOverSlider(PointerEventData eventData)
    {
        if (sliderRect == null || !sliderObject.activeSelf) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(sliderRect, eventData.position, eventData.pressEventCamera);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;

        PlayerPrefs.SetFloat(VolumePrefKey, value);
        hasUnsavedVolume = true;
    }

    private void LoadVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, defaultVolume);

        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(savedVolume);

        AudioListener.volume = savedVolume;
    }

    private void FlushVolume()
    {
        if (!hasUnsavedVolume) return;

        hasUnsavedVolume = false;
        PlayerPrefs.Save();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) FlushVolume();
    }

    private void OnApplicationQuit()
    {
        FlushVolume();
    }

    private void OnDisable()
    {
        FlushVolume();
    }

    private void OnDestroy()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        if (sliderPanel != null) sliderPanel.BackRequested -= FlushVolume;
    }
}
