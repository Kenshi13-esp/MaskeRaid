using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Escucha la accion Pause del Input System para poder pausar con teclado (Esc) y con mando
/// (boton Start), no solo con el boton del HUD.
/// </summary>
public class PauseInput : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PauseManager pauseManager;

    [Tooltip("PlayerInput del jugador. Es la fuente preferente de las acciones")]
    [SerializeField] private PlayerInput playerInput;

    [Tooltip("Asset de acciones de reserva si no hay PlayerInput asignado")]
    [SerializeField] private InputActionAsset controls;

    [Header("Accion")]
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string pauseActionName = "Pause";

    private InputAction pauseAction;

    private void Awake()
    {
        if (pauseManager == null) pauseManager = GetComponent<PauseManager>();
    }

    private void OnEnable()
    {
        pauseAction = ResolvePauseAction();

        if (pauseAction == null)
        {
            Debug.LogWarning($"[PauseInput] No se encontro la accion '{actionMapName}/{pauseActionName}'.", this);
            return;
        }

        pauseAction.performed += OnPausePerformed;
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        if (pauseAction == null) return;

        pauseAction.performed -= OnPausePerformed;
        pauseAction = null;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (pauseManager == null || GamePause.IsGameFinished) return;

        // Con un menu abierto, Start y Escape cierran el panel que tiene el foco igual que el
        // boton de atras, para que ambos botones se comporten igual con submenus anidados.
        if (MenuPanel.RequestBack()) return;

        // Escape esta enlazado a Pausa y tambien a Cancelar en la UI: sin esta reserva la misma
        // pulsacion cerraria el menu por atras y lo volveria a abrir por pausa.
        if (!MenuPanel.ConsumeFrame()) return;

        pauseManager.TogglePause();
    }

    private InputAction ResolvePauseAction()
    {
        InputActionAsset asset = playerInput != null && playerInput.actions != null ? playerInput.actions : controls;
        if (asset == null) return null;

        InputActionMap actionMap = asset.FindActionMap(actionMapName, false);
        return actionMap != null ? actionMap.FindAction(pauseActionName, false) : null;
    }
}
