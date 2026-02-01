using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VolumeController : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private GameObject sliderObject;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button volumeButton;

    [Header("Volume Settings")]
    [SerializeField] private float defaultVolume = 0.5f;

    private const string VOLUME_PREF_KEY = "MasterVolume";

    private void Awake()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (volumeButton == null)
        {
            volumeButton = GetComponent<Button>();
        }

        if (volumeButton != null)
        {
            volumeButton.onClick.RemoveAllListeners();
        }
    }

    private void Start()
    {
        LoadVolume();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (sliderObject != null && !IsPointerOverSlider(eventData))
        {
            bool newState = !sliderObject.activeSelf;
            sliderObject.SetActive(newState);
        }
    }

    private bool IsPointerOverSlider(PointerEventData eventData)
    {
        if (sliderObject == null || !sliderObject.activeSelf)
        {
            return false;
        }

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        if (sliderRect == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(sliderRect, eventData.position, eventData.pressEventCamera);
    }

    public void ToggleSlider()
    {
        if (sliderObject != null)
        {
            bool newState = !sliderObject.activeSelf;
            sliderObject.SetActive(newState);
        }
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        SaveVolume(value);
    }

    private void LoadVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, defaultVolume);
        
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }
        
        AudioListener.volume = savedVolume;
    }

    private void SaveVolume(float volume)
    {
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, volume);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }
    }
}
