using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Panel de menu navegable con mando. Mantiene una pila estatica de los paneles abiertos para
/// saber cual debe recibir el boton de atras (O en mando, Escape en teclado), para que siempre
/// haya un elemento seleccionado y para impedir que la navegacion salte a botones que quedan
/// detras del panel abierto.
/// </summary>
[DisallowMultipleComponent]
public class MenuPanel : MonoBehaviour
{
    private const int InitialSelectableCapacity = 32;

    private static readonly List<MenuPanel> OpenPanels = new List<MenuPanel>();

    private static Selectable[] selectableBuffer = new Selectable[InitialSelectableCapacity];
    private static GameObject verifiedSelection;
    private static Selectable verifiedSelectable;
    private static int lastHandledFrame = -1;

    /// <summary>Se dispara cada vez que se abre o se cierra un panel.</summary>
    public static event Action StackChanged;

    /// <summary>Panel que tiene el foco: el ultimo que se ha abierto.</summary>
    public static MenuPanel Top
    {
        get
        {
            for (int i = OpenPanels.Count - 1; i >= 0; i--)
            {
                MenuPanel panel = OpenPanels[i];
                if (panel != null) return panel;

                OpenPanels.RemoveAt(i);
            }

            return null;
        }
    }

    /// <summary>True mientras haya algun panel de menu abierto.</summary>
    public static bool AnyOpen => Top != null;

    [Header("Seleccion")]
    [Tooltip("Elemento que queda seleccionado al abrir el panel")]
    [SerializeField] private GameObject firstSelected;

    [Tooltip("Al cerrar, devuelve la seleccion al elemento que estaba activo antes de abrir")]
    [SerializeField] private bool restorePreviousSelection = true;

    [Header("Atras")]
    [Tooltip("Desactiva este objeto al pulsar atras. Desactivalo si otro script gestiona el cierre")]
    [SerializeField] private bool deactivateOnBack = true;

    [Header("Navegacion")]
    [Tooltip("Impide navegar hacia elementos que estan fuera de este panel mientras esta abierto")]
    [SerializeField] private bool blockNavigationOutside = true;

    /// <summary>Se dispara al pulsar atras sobre este panel, antes de cerrarlo.</summary>
    public event Action BackRequested;

    private readonly List<Selectable> blockedSelectables = new List<Selectable>();
    private readonly List<Navigation.Mode> blockedModes = new List<Navigation.Mode>();

    private GameObject previousSelection;
    private Coroutine selectRoutine;

    private bool CanHandleBack => deactivateOnBack || BackRequested != null;

    /// <summary>
    /// Reserva la pulsacion de atras o pausa de este fotograma. Devuelve false si ya se ha
    /// gestionado, para que una sola tecla (Escape) no cierre y reabra el menu a la vez.
    /// </summary>
    public static bool ConsumeFrame()
    {
        if (lastHandledFrame == Time.frameCount) return false;

        lastHandledFrame = Time.frameCount;
        return true;
    }

    /// <summary>Envia el boton de atras al panel abierto. True si alguien lo ha gestionado.</summary>
    public static bool RequestBack()
    {
        MenuPanel top = Top;
        if (top == null || !top.CanHandleBack) return false;
        if (!ConsumeFrame()) return false;

        top.HandleBack();
        return true;
    }

    /// <summary>Garantiza que el panel abierto tenga un elemento valido seleccionado.</summary>
    public static void EnsureSelection()
    {
        MenuPanel top = Top;
        if (top == null) return;

        top.EnsureOwnSelection();
    }

    private void OnEnable()
    {
        previousSelection = CurrentSelection();

        OpenPanels.Add(this);

        if (blockNavigationOutside) BlockOutsideNavigation();

        selectRoutine = StartCoroutine(SelectFirstAfterLayout());

        StackChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        OpenPanels.Remove(this);

        RestoreOutsideNavigation();
        RestoreSelection();

        StackChanged?.Invoke();
    }

    private IEnumerator SelectFirstAfterLayout()
    {
        yield return null;

        selectRoutine = null;
        ForceOwnSelection();
    }

    private void HandleBack()
    {
        BackRequested?.Invoke();

        if (deactivateOnBack && gameObject.activeSelf) gameObject.SetActive(false);
    }

    /// <summary>Selecciona el primer elemento salvo que ya haya foco dentro del panel.</summary>
    private void ForceOwnSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || !IsSelectable(firstSelected)) return;

        GameObject current = eventSystem.currentSelectedGameObject;
        if (IsSelectable(current) && current.transform.IsChildOf(transform)) return;

        eventSystem.SetSelectedGameObject(firstSelected);
    }

    /// <summary>Recupera la seleccion solo si se ha quedado en un elemento invalido.</summary>
    private void EnsureOwnSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        if (IsSelectable(eventSystem.currentSelectedGameObject)) return;
        if (!IsSelectable(firstSelected)) return;

        eventSystem.SetSelectedGameObject(firstSelected);
    }

    private void RestoreSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        GameObject current = eventSystem.currentSelectedGameObject;
        bool ownedFocus = current == null || current.transform.IsChildOf(transform);
        if (!ownedFocus) return;

        eventSystem.SetSelectedGameObject(null);

        if (restorePreviousSelection && IsSelectable(previousSelection))
        {
            eventSystem.SetSelectedGameObject(previousSelection);
            return;
        }

        MenuPanel top = Top;
        if (top != null) top.ForceOwnSelection();
    }

    private void BlockOutsideNavigation()
    {
        int count = Selectable.allSelectableCount;
        if (selectableBuffer.Length < count) selectableBuffer = new Selectable[count];

        Selectable.AllSelectablesNoAlloc(selectableBuffer);

        for (int i = 0; i < count; i++)
        {
            Selectable selectable = selectableBuffer[i];
            selectableBuffer[i] = null;

            if (selectable == null || selectable.transform.IsChildOf(transform)) continue;

            Navigation navigation = selectable.navigation;
            if (navigation.mode == Navigation.Mode.None) continue;

            blockedSelectables.Add(selectable);
            blockedModes.Add(navigation.mode);

            navigation.mode = Navigation.Mode.None;
            selectable.navigation = navigation;
        }
    }

    private void RestoreOutsideNavigation()
    {
        for (int i = 0; i < blockedSelectables.Count; i++)
        {
            Selectable selectable = blockedSelectables[i];
            if (selectable == null) continue;

            Navigation navigation = selectable.navigation;
            navigation.mode = blockedModes[i];
            selectable.navigation = navigation;
        }

        blockedSelectables.Clear();
        blockedModes.Clear();
    }

    private static GameObject CurrentSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        return eventSystem != null ? eventSystem.currentSelectedGameObject : null;
    }

    /// <summary>True si el objeto se puede seleccionar ahora mismo. Cachea el ultimo consultado.</summary>
    private static bool IsSelectable(GameObject candidate)
    {
        if (candidate == null || !candidate.activeInHierarchy) return false;

        if (candidate != verifiedSelection)
        {
            verifiedSelection = candidate;
            verifiedSelectable = candidate.GetComponent<Selectable>();
        }

        return verifiedSelectable == null || verifiedSelectable.IsInteractable();
    }
}
