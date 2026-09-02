using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Une el modulo de UI del Input System con los <see cref="MenuPanel"/> de la escena: enruta el
/// boton de atras al panel abierto, mantiene siempre un elemento seleccionado mientras hay menu
/// y apaga los eventos de navegacion cuando no hay ninguno, para que el boton de dash no pulse
/// botones de la interfaz durante la partida.
/// </summary>
[RequireComponent(typeof(EventSystem))]
[DisallowMultipleComponent]
public class MenuNavigator : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Modulo de entrada de UI. Si esta vacio se busca en este objeto")]
    [SerializeField] private InputSystemUIInputModule inputModule;

    [Header("Comportamiento")]
    [Tooltip("Apaga la navegacion de UI mientras no hay ningun menu abierto")]
    [SerializeField] private bool disableNavigationWithoutMenus = true;

    [Tooltip("Evita que un clic en el fondo deje la interfaz sin seleccion")]
    [SerializeField] private bool keepSelectionOnBackgroundClick = true;

    private EventSystem eventSystem;
    private InputAction cancelAction;

    private void Awake()
    {
        eventSystem = GetComponent<EventSystem>();

        if (inputModule == null) inputModule = GetComponent<InputSystemUIInputModule>();

        if (keepSelectionOnBackgroundClick && inputModule != null) inputModule.deselectOnBackgroundClick = false;
    }

    private void OnEnable()
    {
        cancelAction = inputModule != null && inputModule.cancel != null ? inputModule.cancel.action : null;

        if (cancelAction == null)
        {
            Debug.LogWarning("[MenuNavigator] El modulo de UI no tiene accion Cancel asignada.", this);
        }
        else
        {
            cancelAction.performed += OnCancelPerformed;
        }

        MenuPanel.StackChanged += ApplyMenuState;
        ApplyMenuState();
    }

    private void OnDisable()
    {
        if (cancelAction != null)
        {
            cancelAction.performed -= OnCancelPerformed;
            cancelAction = null;
        }

        MenuPanel.StackChanged -= ApplyMenuState;
    }

    private void Update()
    {
        if (!MenuPanel.AnyOpen) return;

        MenuPanel.EnsureSelection();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        MenuPanel.RequestBack();
    }

    private void ApplyMenuState()
    {
        if (eventSystem == null) return;

        bool anyMenuOpen = MenuPanel.AnyOpen;

        if (disableNavigationWithoutMenus && eventSystem.sendNavigationEvents != anyMenuOpen)
        {
            eventSystem.sendNavigationEvents = anyMenuOpen;
        }

        if (!anyMenuOpen && eventSystem.currentSelectedGameObject != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
