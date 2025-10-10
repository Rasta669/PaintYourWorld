using UnityEngine;
using UnityEngine.UI;

public class VolumeIcon : MonoBehaviour
{
    [SerializeField] private Sprite volumeOnIcon;
    [SerializeField] private Sprite volumeOffIcon;
    [SerializeField] private Button volumeButton;
    [SerializeField] private AudioSource backgroundMusic; // optional if you want to control a specific AudioSource

    private bool isMuted = false;

    void Start()
    {
        // Initialize icon
        UpdateIcon();

        // Add listener to button
        volumeButton.onClick.AddListener(ToggleVolume);
    }

    void ToggleVolume()
    {
        isMuted = !isMuted;

        // Change volume globally
        AudioListener.volume = isMuted ? 0f : 1f;

        // (Optional) If controlling only one AudioSource:
        if (backgroundMusic != null)
            backgroundMusic.mute = isMuted;

        // Update icon
        UpdateIcon();
    }

    void UpdateIcon()
    {
        if (volumeButton != null)
        {
            var image = volumeButton.GetComponent<Image>();
            image.sprite = isMuted ? volumeOffIcon : volumeOnIcon;
        }
    }
}
