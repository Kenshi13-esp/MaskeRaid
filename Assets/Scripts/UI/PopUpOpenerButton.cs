using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abre o cierra un pop up al pulsar el boton que lleva este componente. La escucha se engancha
/// por codigo en lugar de por UnityEvent del inspector, asi que la referencia al panel es un
/// campo normal y no se pierde al mover objetos de sitio.
/// </summary>
[RequireComponent(typeof(Button))]
public class PopUpOpenerButton : MonoBehaviour
{
    [Tooltip("Panel que se muestra u oculta al pulsar")]
    [SerializeField] private GameObject popUp;

    [Tooltip("True para mostrar el panel, false para ocultarlo")]
    [SerializeField] private bool show = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null) button.onClick.AddListener(Toggle);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(Toggle);
    }

    /// <summary>Aplica el cambio de visibilidad configurado sobre el panel.</summary>
    public void Toggle()
    {
        if (popUp == null)
        {
            Debug.LogWarning($"[PopUpOpener] '{name}' no tiene panel asignado.", this);
            return;
        }

        popUp.SetActive(show);
    }
}
