using System;
using System.IO;
using System.Linq;
using System.Text;
using Bezi;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Escribe el JSON de un asset .inputactions y lo reimporta en el sitio.
///
/// Existe porque los assets del Input System se guardan como JSON en texto plano y no se
/// pueden reparar con las utilidades genericas de ScriptableObject: esas solo ven las
/// propiedades publicas de <see cref="InputAction"/>, que no incluyen el nombre, asi que
/// sobrescriben las acciones dejandolas sin nombre y rompen el asset. Sobrescribir el
/// archivo en su sitio conserva su GUID y, con el, la referencia del PlayerInput y los
/// eventos de accion ya cableados en las escenas.
/// </summary>
public static class InputActionsAssetWriter
{
    private const string InputActionsExtension = ".inputactions";
    private const string AssetsFolderPrefix = "Assets/";

    /// <summary>
    /// Sobrescribe un asset .inputactions con el JSON indicado y lo reimporta. Valida el
    /// JSON antes de tocar el disco: si alguna accion se quedara sin nombre, aborta.
    /// </summary>
    /// <param name="assetPath">Ruta relativa al proyecto, por ejemplo "Assets/Controls.input.inputactions".</param>
    /// <param name="json">Contenido JSON completo del asset.</param>
    [BeziAction(
        "Overwrites an .inputactions asset with raw JSON and reimports it in place, preserving the asset GUID so scene references and already wired action events keep working. Validates the JSON first and rejects it if any action would end up without a name.",
        RequireApproval = "Overwrite the .inputactions asset with new JSON?")]
    public static string WriteInputActionsAsset(string assetPath, string json)
    {
        string normalizedPath = ValidateAssetPath(assetPath);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("El JSON del asset no puede estar vacio.", nameof(json));
        }

        ValidateJson(json);

        string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), normalizedPath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException(
                $"No existe el asset '{normalizedPath}'. Este metodo sobrescribe assets existentes para conservar su GUID.",
                absolutePath);
        }

        File.WriteAllText(absolutePath, json, new UTF8Encoding(false));

        AssetDatabase.ImportAsset(
            normalizedPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        return DescribeAsset(normalizedPath);
    }

    /// <summary>
    /// Devuelve un resumen legible de los mapas, acciones y bindings de un asset .inputactions.
    /// </summary>
    /// <param name="assetPath">Ruta relativa al proyecto del asset a inspeccionar.</param>
    [BeziAction(
        "Returns a readable summary of the action maps, actions and bindings of an .inputactions asset as Unity actually imported it. Use to verify the asset after editing it.",
        IsReadOnly = true)]
    public static string DescribeInputActionsAsset(string assetPath)
    {
        return DescribeAsset(ValidateAssetPath(assetPath));
    }

    private static string ValidateAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException("La ruta del asset no puede estar vacia.", nameof(assetPath));
        }

        string normalizedPath = assetPath.Replace('\\', '/').TrimStart('/');

        int assetsIndex = normalizedPath.IndexOf(AssetsFolderPrefix, StringComparison.OrdinalIgnoreCase);
        if (assetsIndex > 0)
        {
            // Acepta tambien rutas absolutas del workspace, del tipo "/MiProyecto/Assets/...".
            normalizedPath = normalizedPath.Substring(assetsIndex);
        }

        if (!normalizedPath.StartsWith(AssetsFolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"La ruta debe empezar por '{AssetsFolderPrefix}'. Recibida: '{assetPath}'.", nameof(assetPath));
        }

        if (!normalizedPath.EndsWith(InputActionsExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"La ruta debe terminar en '{InputActionsExtension}'. Recibida: '{assetPath}'.", nameof(assetPath));
        }

        return normalizedPath;
    }

    /// <summary>
    /// Comprueba que el JSON se parsea y que ninguna accion se queda sin nombre. Es la
    /// salvaguarda contra el fallo que rompio este asset: una accion sin nombre hace que
    /// PlayerInput lance ArgumentNullException al activarse.
    /// </summary>
    private static void ValidateJson(string json)
    {
        InputActionAsset parsed = null;

        try
        {
            parsed = InputActionAsset.FromJson(json);

            if (parsed.actionMaps.Count == 0)
            {
                throw new ArgumentException("El JSON no define ningun action map.", nameof(json));
            }

            foreach (InputActionMap map in parsed.actionMaps)
            {
                if (string.IsNullOrEmpty(map.name))
                {
                    throw new ArgumentException("Hay un action map sin nombre en el JSON.", nameof(json));
                }

                if (map.actions.Count == 0)
                {
                    throw new ArgumentException($"El action map '{map.name}' no tiene ninguna accion.", nameof(json));
                }

                foreach (InputAction action in map.actions)
                {
                    if (string.IsNullOrEmpty(action.name))
                    {
                        throw new ArgumentException(
                            $"Hay una accion sin nombre en el action map '{map.name}'. PlayerInput fallaria al activarse.",
                            nameof(json));
                    }
                }
            }
        }
        finally
        {
            if (parsed != null) UnityEngine.Object.DestroyImmediate(parsed);
        }
    }

    private static string DescribeAsset(string normalizedPath)
    {
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(normalizedPath);

        if (asset == null)
        {
            throw new InvalidOperationException($"Unity no pudo importar '{normalizedPath}' como InputActionAsset.");
        }

        var summary = new StringBuilder();
        summary.AppendLine($"{normalizedPath} (GUID {AssetDatabase.AssetPathToGUID(normalizedPath)})");

        foreach (InputActionMap map in asset.actionMaps)
        {
            summary.AppendLine($"Map '{map.name}' [{map.id}]");

            foreach (InputAction action in map.actions)
            {
                string bindings = string.Join(", ", action.bindings
                    .Where(binding => !binding.isComposite)
                    .Select(binding => binding.path));

                summary.AppendLine($"  {action.name} ({action.type}) [{action.id}] -> {bindings}");
            }
        }

        foreach (InputControlScheme scheme in asset.controlSchemes)
        {
            summary.AppendLine($"Scheme '{scheme.name}' group '{scheme.bindingGroup}'");
        }

        return summary.ToString();
    }
}
