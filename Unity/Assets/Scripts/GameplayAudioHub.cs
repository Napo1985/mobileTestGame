using UnityEngine;

/// <summary>
/// Optional SFX/music hooks: assign AudioClips in the inspector on the same GameObject as GameBootstrap,
/// or leave empty for silent play. Uses two AudioSources (SFX one-shots + optional looping music).
/// </summary>
[DefaultExecutionOrder(-50)]
public class GameplayAudioHub : MonoBehaviour
{
    public static GameplayAudioHub Instance { get; private set; }

    [Header("Sources (auto-created if null)")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource musicSource;

    [Header("SFX (optional)")]
    [SerializeField] AudioClip shootClip;
    [SerializeField] AudioClip hitEnemyClip;
    [SerializeField] AudioClip enemyDeathClip;
    [SerializeField] AudioClip pickupClip;
    [SerializeField] AudioClip playerHurtClip;
    [SerializeField] AudioClip gameOverClip;
    [SerializeField] AudioClip uiClickClip;
    [SerializeField] AudioClip waveClearClip;

    [Header("Music (optional loop)")]
    [SerializeField] AudioClip gameplayMusicLoop;
    [Range(0f, 1f)] [SerializeField] float musicVolume = 0.35f;

    [Range(0f, 1f)] [SerializeField] float sfxVolume = 0.85f;

    void Awake()
    {
        Instance = this;
        EnsureSources();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureSources()
    {
        if (sfxSource == null)
        {
            var go = new GameObject("SFX");
            go.transform.SetParent(transform, false);
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }

        if (musicSource == null)
        {
            var go = new GameObject("Music");
            go.transform.SetParent(transform, false);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }
    }

    /// <summary>Call from GameBootstrap after camera/scene ready; starts loop if clip assigned.</summary>
    public void StartGameplayMusicIfConfigured()
    {
        EnsureSources();
        if (gameplayMusicLoop == null || musicSource == null)
            return;
        musicSource.clip = gameplayMusicLoop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }

    public void PlayShoot() => PlayOne(sfxSource, shootClip, 0.55f * sfxVolume);
    public void PlayHitEnemy() => PlayOne(sfxSource, hitEnemyClip, 0.45f * sfxVolume);
    public void PlayEnemyDeath() => PlayOne(sfxSource, enemyDeathClip, 0.65f * sfxVolume);
    public void PlayPickup() => PlayOne(sfxSource, pickupClip, 0.7f * sfxVolume);
    public void PlayPlayerHurt() => PlayOne(sfxSource, playerHurtClip, 0.75f * sfxVolume);
    public void PlayGameOver() => PlayOne(sfxSource, gameOverClip, 0.9f * sfxVolume);
    public void PlayUiClick() => PlayOne(sfxSource, uiClickClip, 0.6f * sfxVolume);
    public void PlayWaveClear() => PlayOne(sfxSource, waveClearClip, 0.7f * sfxVolume);

    static void PlayOne(AudioSource src, AudioClip clip, float vol)
    {
        if (src == null || clip == null || vol <= 0f)
            return;
        src.PlayOneShot(clip, vol);
    }
}
