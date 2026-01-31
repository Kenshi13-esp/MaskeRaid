using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerDashController2D playerDashController;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI powerNameText;
    [SerializeField] private TextMeshProUGUI powerDescriptionText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Configuración")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeSpeed = 2f;
    
    private DashAbility lastAbility;
    private float displayTimer;
    private bool isShowing;

    private void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (playerDashController == null) return;
        
        DashAbility currentAbility = playerDashController.GetCurrentDashAbility();
        
        if (currentAbility != null && currentAbility != lastAbility)
        {
            ShowPowerNotification(currentAbility);
            lastAbility = currentAbility;
        }
        
        if (isShowing)
        {
            displayTimer -= Time.deltaTime;
            
            if (canvasGroup != null)
            {
                if (displayTimer > 0f)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
                }
                else
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
                    
                    if (canvasGroup.alpha <= 0f)
                    {
                        isShowing = false;
                    }
                }
            }
        }
    }

    private void ShowPowerNotification(DashAbility ability)
    {
        if (powerNameText != null)
        {
            powerNameText.text = $"¡NUEVO PODER: {ability.AbilityName}!";
        }
        
        if (powerDescriptionText != null)
        {
            powerDescriptionText.text = ability.Description;
        }
        
        displayTimer = displayDuration;
        isShowing = true;
    }
}
