using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [Range(0f, 1f)] public float parallaxEffect = 0.5f;

    private float startPosition;
    private float textureUnitSizeX;

    void Start()
    {
        startPosition = transform.position.x;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        textureUnitSizeX = spriteRenderer.bounds.size.x;
    }

    void Update()
    {
        float distance = (cameraTransform.position.x * parallaxEffect);
        transform.position = new Vector3(startPosition + distance, transform.position.y, transform.position.z);

        float temp = (cameraTransform.position.x * (1 - parallaxEffect));
        if (temp > startPosition + textureUnitSizeX) startPosition += textureUnitSizeX;
        else if (temp < startPosition - textureUnitSizeX) startPosition -= textureUnitSizeX;
    }
}
