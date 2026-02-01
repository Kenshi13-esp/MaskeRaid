using UnityEngine;

public class PlaySoundOnAnimationEvent : MonoBehaviour
{
	public void PlaySound(string soundTypeName)
	{
		if (System.Enum.TryParse(soundTypeName, true, out SoundType soundType))
		{
			SoundManager.PlaySound(soundType);
		}
		else
		{
			Debug.LogError($"Invalid sound type: {soundTypeName}");
		}
	}

	public void PlaySoundWithVolume(string soundData)
	{
		string[] data = soundData.Split(',');
		if (data.Length < 1) return;

		string soundTypeName = data[0].Trim();
		float volume = data.Length > 1 && float.TryParse(data[1].Trim(), out float vol) ? vol : 1f;

		if (System.Enum.TryParse(soundTypeName, true, out SoundType soundType))
		{
			SoundManager.PlaySound(soundType, volume);
		}
		else
		{
			Debug.LogError($"Invalid sound type: {soundTypeName}");
		}
	}
}
