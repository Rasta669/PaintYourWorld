using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public AudioMixer audioMixer; // Drag & Drop your Unity Audio Mixer

    public UIDocument uiDocument;
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider effectsVolumeSlider;

    // Music
    public AudioSource menuMusic;
    public AudioSource gameplayMusic;
    public AudioSource pauseMusic;
    public AudioSource gameOverMusic;
    public AudioSource gameWinMusic;

    // Sound effects
    public AudioSource walkSound;
    public AudioSource jumpSound;
    public AudioSource dieSound;
    public AudioSource keyAcquiredSound;
    public AudioSource enemyWalkSound;
    public AudioSource enemyRunSound;
    public AudioSource clickSound;
    public AudioSource waterSound;
    public AudioSource climbSound;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensures persistence across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("Music", Mathf.Log10(volume) * 20);
    }

    public void SetEffectsVolume(float volume)
    {
        audioMixer.SetFloat("Music/Effects", Mathf.Log10(volume) * 20);
    }

    public void PlayMenuMusic() => PlayMusic(menuMusic);
    public void PlayGameplayMusic() => PlayMusic(gameplayMusic);
    public void PlayPauseMusic() => PlayMusic(pauseMusic);
    public void PlayGameOverMusic() => PlayMusic(gameOverMusic);
    public void PlayGameWinMusic()
    {
        Debug.Log("Game Win Music Triggered!"); // Debug Log
        PlayMusic(gameWinMusic);

    }
    private void PlayMusic(AudioSource music)
    {
        if (music != null)
        {
            StopAllMusic();
            music.Play();
        }
        else
        {
            Debug.LogError("Music source is not assigned.");
        }
    }

    public void StopAllMusic()
    {
        menuMusic.Stop();
        gameplayMusic.Stop();
        pauseMusic.Stop();
        gameOverMusic.Stop();
        gameWinMusic.Stop();
    }



    public void PlayWalkSound() => PlaySound(walkSound);
    public void PlayJumpSound() => PlaySound(jumpSound);
    public void PlayDieSound() => PlaySound(dieSound);
    public void PlayKeyAcquiredSound() => PlaySound(keyAcquiredSound);
    public void PlayEnemyWalkSound() => PlaySound(enemyWalkSound);
    public void PlayEnemyRunSound() => PlaySound(enemyRunSound);
    public void PlayClimbSound() => PlaySound(climbSound);
    public void PlayClickSound() => PlaySound(clickSound);
    public void PlayWaterSound() => PlaySound(waterSound);

    private void PlaySound(AudioSource sound)
    {
        if (sound != null)
        {
            sound.Play();
        }
        else
        {
            Debug.LogWarning("Sound source is missing.");
        }
    }
}

