using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float volume = 0.7f;
    
    private AudioSource musicSource;
    
    private void Awake()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = musicClip;
        musicSource.volume = volume;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }
    
    private void Start()
    {
        if (musicClip != null)
        {
            musicSource.Play();
        }
    }
    
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }
}
