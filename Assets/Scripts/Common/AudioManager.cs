using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Sources")]
    public AudioSource sfxSource;   
    public AudioSource musicSource; 
    [Header("Clips")]
    public AudioClip sfxEnemyHit;
    public AudioClip sfxPlayerHit;
    public AudioClip sfxPotionUse;
    public AudioClip sfxObjectiveHit;
    public AudioClip sfxThrow;
    public AudioClip sfxBounce;
    public AudioClip bgm;

    [Header("Volume")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.6f;

    [Header("Anti-spam")]
    [Tooltip("同一帧内多次 PlayOneShot 的最小间隔（秒）")]
    public float minSfxInterval = 0.01f;

    float lastSfxTime = -999f;

    void Awake()
    {
        
        if (transform.parent != null)
            transform.SetParent(null, true);

        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = sfxVolume;
        }

        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;

            EnsureBgmPlaying();
        }
    }

    void EnsureBgmPlaying()
    {
        if (musicSource == null) return;
        if (bgm == null) return;

        if (musicSource.clip != bgm)
            musicSource.clip = bgm;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void SetSfxEnabled(bool enabled)
    {
        if (sfxSource != null) sfxSource.mute = !enabled;
    }

    public void SetMusicEnabled(bool enabled)
    {
        if (musicSource != null) musicSource.mute = !enabled;

        
        if (enabled) EnsureBgmPlaying();
    }

    void PlayOneShotSafe(AudioClip clip, float volMul = 1f)
    {
        if (clip == null || sfxSource == null) return;

        
        if (Time.unscaledTime - lastSfxTime < minSfxInterval) return;
        lastSfxTime = Time.unscaledTime;

        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volMul));
    }

    public void PlayEnemyHit(float volMul = 1f) => PlayOneShotSafe(sfxEnemyHit, volMul);
    public void PlayPlayerHit(float volMul = 1f) => PlayOneShotSafe(sfxPlayerHit, volMul);
    public void PlayPotionUse(float volMul = 1f) => PlayOneShotSafe(sfxPotionUse, volMul);
    public void PlayObjectiveHit(float volMul = 1f) => PlayOneShotSafe(sfxObjectiveHit, volMul);
    public void PlayThrow(float volMul = 1f) => PlayOneShotSafe(sfxThrow, volMul);
    public void PlayBounce(float volMul = 1f) => PlayOneShotSafe(sfxBounce, volMul);
}
