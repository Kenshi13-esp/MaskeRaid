using UnityEngine;

public enum SoundType
{
	ONI_SPAWN,
	ONI_ATTACK,
	ONI_DEATH,
	GLORBO_SPAWN,
	GLORBO_ATTACK,
	GLORBO_DEATH,
	QETZA_SPAWN,
	QETZA_ATTACK_1,
	QETZA_ATTACK_2,
	QETZA_GROUND_SLAM,
	QETZA_DEATH,
	PLAYER_CHARGE,
	PLAYER_ATTACK,
	PLAYER_HIT,
	BOSS_HIT,
	BOSS_PHASE_CHANGE,
	MUSIC,
	PLAY,
	VICTORY,
}

[System.Serializable]
public class Sound
{
	public SoundType soundType;
	public AudioClip clip;
	[Range(0f, 1f)]
	public float defaultVolume = 1f;
}

public class SoundManager : MonoBehaviour
{
	private static SoundManager instance;
	
	[SerializeField] private Sound[] sounds;
	[SerializeField] private int audioSourcePoolSize = 5;
	
	private AudioSource[] audioSourcePool;
	private int currentAudioSourceIndex = 0;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}
		
		InitializeAudioSourcePool();
	}

	private void InitializeAudioSourcePool()
	{
		audioSourcePool = new AudioSource[audioSourcePoolSize];
		for (int i = 0; i < audioSourcePoolSize; i++)
		{
			audioSourcePool[i] = gameObject.AddComponent<AudioSource>();
		}
	}

	public static void PlaySound(SoundType soundType, float volume = 1f)
	{
		if (instance == null) return;
		instance.PlaySoundInternal(soundType, volume);
	}

	private void PlaySoundInternal(SoundType soundType, float volume)
	{
		Sound sound = System.Array.Find(sounds, s => s.soundType == soundType);
		
		if (sound == null || sound.clip == null)
		{
			Debug.LogWarning($"Sound {soundType} not found or has no clip assigned!");
			return;
		}

		AudioSource audioSource = GetNextAudioSource();
		audioSource.volume = sound.defaultVolume * volume;
		audioSource.PlayOneShot(sound.clip);
	}

	private AudioSource GetNextAudioSource()
	{
		AudioSource source = audioSourcePool[currentAudioSourceIndex];
		currentAudioSourceIndex = (currentAudioSourceIndex + 1) % audioSourcePoolSize;
		return source;
	}
}
