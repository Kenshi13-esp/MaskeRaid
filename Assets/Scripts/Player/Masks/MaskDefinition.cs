using UnityEngine;

/// <summary>
/// Mascara de un boss. Define que dash gana el jugador al equiparla, con que ajustes y con
/// que aspecto. Anadir un boss nuevo al juego es crear su mascara y su
/// <see cref="DashMoveBase"/>: no hace falta tocar el jugador.
/// </summary>
[CreateAssetMenu(fileName = "New Mask", menuName = "Boss Rush/Mask")]
public class MaskDefinition : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField] private string maskName = "Mascara";

    [TextArea]
    [SerializeField] private string description = "";

    [Tooltip("Sprite de la mascara para mostrar en la UI")]
    [SerializeField] private Sprite maskSprite;

    [Header("Dash")]
    [Tooltip("Dash que otorga esta mascara al jugador")]
    [SerializeField] private DashMoveKind dashMoveKind = DashMoveKind.Basic;

    [Tooltip("Ajustes del dash cuando lo ejecuta el jugador")]
    [SerializeField] private DashProfile dashProfile;

    [Header("Mejora permanente")]
    [Tooltip("Cargas extra del especial que esta mascara anade para el resto de la partida. " +
             "Se acumulan y se conservan al cambiar de mascara")]
    [Min(0)]
    [SerializeField] private int extraSpecialCharges = 0;

    [Header("Visuales del jugador")]
    [Tooltip("Animator del jugador con esta mascara. Vacio = mantiene el original")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    [Tooltip("Pantalla de instrucciones que se muestra al equipar la mascara")]
    [SerializeField] private Sprite instructionsSprite;

    public string MaskName => maskName;
    public string Description => description;
    public Sprite MaskSprite => maskSprite;
    public DashMoveKind DashMoveKind => dashMoveKind;
    public DashProfile DashProfile => dashProfile;

    /// <summary>Cargas extra del especial que esta mascara anade de forma permanente.</summary>
    public int ExtraSpecialCharges => Mathf.Max(0, extraSpecialCharges);
    public RuntimeAnimatorController AnimatorController => animatorController;
    public Sprite InstructionsSprite => instructionsSprite;
}
