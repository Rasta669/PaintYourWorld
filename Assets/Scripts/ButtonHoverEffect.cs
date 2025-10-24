using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button), typeof(Image))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Pressed Color")]
    [SerializeField] private Color pressedColor = Color.green; // Color to use when pressed

    private Button button;
    private Image image;
    private Color originalColor;
    private bool isHovered = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();

        originalColor = image.color;
        image.enabled = false; // Hide by default
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        image.enabled = true; // Show when hovered
        image.color = originalColor; // Keep default color
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        image.enabled = false; // Hide when hover ends
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Only react if currently hovered
        if (isHovered)
        {
            image.enabled = true; // Make sure it’s visible
            image.color = pressedColor; // Change to pressed color
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Restore to original color, but keep visible if still hovered
        image.color = originalColor;

        if (!isHovered)
            image.enabled = false;
    }
}
