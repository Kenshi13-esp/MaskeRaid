using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puente entre los eventos de animacion y el <see cref="SoundManager"/>: los clips llaman a
/// estos metodos pasando el nombre del <see cref="SoundType"/> como texto.
///
/// Los nombres se resuelven con una tabla creada una sola vez, en lugar de con Enum.TryParse
/// en cada evento de animacion, que compara sin distinguir mayusculas y asigna memoria en
/// cada llamada.
/// </summary>
public class PlaySoundOnAnimationEvent : MonoBehaviour
{
	private const char VolumeSeparator = ',';

	private static readonly Dictionary<string, SoundType> SoundTypesByName = BuildSoundTypeLookup();

	/// <summary>Reproduce el sonido indicado por nombre con su volumen por defecto.</summary>
	public void PlaySound(string soundTypeName)
	{
		if (!TryResolveSoundType(soundTypeName, out SoundType soundType)) return;

		SoundManager.PlaySound(soundType);
	}

	/// <summary>Reproduce un sonido con volumen indicado como "NOMBRE_DEL_SONIDO,0.5".</summary>
	public void PlaySoundWithVolume(string soundData)
	{
		if (string.IsNullOrEmpty(soundData)) return;

		string[] data = soundData.Split(VolumeSeparator);

		if (!TryResolveSoundType(data[0], out SoundType soundType)) return;

		float volume = data.Length > 1 && float.TryParse(data[1].Trim(), out float parsedVolume) ? parsedVolume : 1f;

		SoundManager.PlaySound(soundType, volume);
	}

	private static bool TryResolveSoundType(string soundTypeName, out SoundType soundType)
	{
		soundType = default;

		if (string.IsNullOrEmpty(soundTypeName)) return false;

		if (SoundTypesByName.TryGetValue(soundTypeName.Trim(), out soundType)) return true;

		Debug.LogError($"Invalid sound type: {soundTypeName}");
		return false;
	}

	private static Dictionary<string, SoundType> BuildSoundTypeLookup()
	{
		SoundType[] values = (SoundType[])Enum.GetValues(typeof(SoundType));
		Dictionary<string, SoundType> lookup = new Dictionary<string, SoundType>(values.Length, StringComparer.OrdinalIgnoreCase);

		foreach (SoundType value in values)
		{
			lookup[value.ToString()] = value;
		}

		return lookup;
	}
}
