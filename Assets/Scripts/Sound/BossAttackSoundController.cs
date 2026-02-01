using UnityEngine;

public class BossAttackSoundController : MonoBehaviour
{
	[SerializeField] private SoundType attackSoundType;
	[SerializeField] private AudioClip attackLoopClip;
	[Range(0f, 1f)]
	[SerializeField] private float attackVolume = 1f;
	
	private AudioSource loopAudioSource;

	private void Awake()
	{
		loopAudioSource = gameObject.AddComponent<AudioSource>();
		loopAudioSource.loop = true;
		loopAudioSource.playOnAwake = false;
		loopAudioSource.volume = attackVolume;
		
		if (attackLoopClip != null)
		{
			loopAudioSource.clip = attackLoopClip;
		}
	}

	public void StartAttackSound()
	{
		if (loopAudioSource != null && attackLoopClip != null && !loopAudioSource.isPlaying)
		{
			loopAudioSource.Play();
		}
	}

	public void StopAttackSound()
	{
		if (loopAudioSource != null && loopAudioSource.isPlaying)
		{
			loopAudioSource.Stop();
		}
	}
}
