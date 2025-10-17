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



    public void SetMusicVolume(float value)
    {
        // Slider range 0 → 1
        // Map to decibels (-80 dB = silence, 0 dB = full volume, +10 dB = louder boost)
        float dB;

        if (value <= 0.0001f)
            dB = -80f; // completely mute
        else
            dB = Mathf.Lerp(-30f, 10f, value); // you can adjust range here

        audioMixer.SetFloat("Music", dB);
        Debug.Log($"🎵 Music Volume: Slider={value}, dB={dB}");
    }

    public void SetEffectsVolume(float value)
    {
        float dB;

        if (value <= 0.0001f)
            dB = -80f;
        else
            dB = Mathf.Lerp(-30f, 10f, value); // same loudness curve

        audioMixer.SetFloat("Effects", dB);
        Debug.Log($"💥 Effects Volume: Slider={value}, dB={dB}");
    }


    //public void SetEffectsVolume(float volume)
    //{
    //    audioMixer.SetFloat("Effects", Mathf.Log10(volume) * 20);
    //}


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

