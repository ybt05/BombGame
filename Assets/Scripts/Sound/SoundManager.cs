using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource seSource;

    [Header("Clips")]
    public AudioClip bgmClip;
    public AudioClip actionSE;
    public AudioClip clickSE;
    public AudioClip deathSE;
    public AudioClip bombSE;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ← これが必須
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== BGM =====
    public void PlayBGM()
    {
        if (bgmClip == null) return;

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // ===== SE =====
    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;

        seSource.PlayOneShot(clip);
    }
    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SetSEVolume(float volume)
    {
        seSource.volume = volume;
    }
    
}