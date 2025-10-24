using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PaintManager : MonoBehaviour
{
    [Serializable]
    public class ColorProperty
    {
        public string name;
        public float bounceFactor;
        public float duration;
        public float speedBoost;
        public Color paintColor;
        public float lifetime = -1f;
    }

    // Struct to cache platform components
    private struct PlatformData
    {
        public GameObject Platform;
        public PaintStroke Stroke;
        public SpriteRenderer Renderer;
        public float PositionX; // Cache x-position for job
    }

    [SerializeField] private float brushSize = 20f;
    [SerializeField] private float brushHeight = 5f;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Transform platformsParent;
    [SerializeField] private string selectedColor = "purple";
    [SerializeField] private GameObject player;
    [SerializeField] private ParticleSystem paintParticles;
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private float paintParticleLifetime = 3f;
    [SerializeField] private int poolSize = 50; // Number of platforms to pool
    [SerializeField] private float recycleDistance = 20f; // Distance from camera left boundary to recycle platforms

    private Queue<GameObject> platformPool; // Object pool for platforms
    private List<PlatformData> activeBrushStrokes; // Tracks active platforms with components
    private Animator playerAnimator;
    private Camera mainCamera;
    private NativeArray<float> creationTimes;
    private NativeArray<float> durations;
    private NativeArray<float> lifetimes;
    private NativeArray<bool> isTemporary;
    private NativeArray<float> newAlphas;
    private NativeArray<bool> shouldDestroy;
    private NativeArray<float> positionsX; // Store platform x-positions for job
    private int arrayCapacity; // Tracks current NativeArray capacity

    [SerializeField]
    private Dictionary<string, ColorProperty> colorProperties = new Dictionary<string, ColorProperty>()
    {
        { "purple", new ColorProperty { name = "Platform", paintColor = new Color(0.55f, 0.27f, 0.68f, 1f), lifetime = 30f } },
        { "blue", new ColorProperty { name = "Bouncy", bounceFactor = 6f, paintColor = new Color(0.2f, 0.6f, 0.9f, 1f), lifetime = 30f } },
        { "red", new ColorProperty { name = "Temporary", duration = 3f, paintColor = new Color(0.9f, 0.3f, 0.2f, 1f), lifetime = 30f } },
        { "yellow", new ColorProperty { name = "Speed", speedBoost = 1.1f, paintColor = new Color(0.95f, 0.77f, 0.06f, 1f), lifetime = 30f } },
        { "ghost", new ColorProperty { name = "Ghost", paintColor = new Color(0.9f, 0.9f, 0.9f, 0.8f), lifetime = 30f } },
        { "brown", new ColorProperty { name = "Blocker", paintColor = new Color(0.36f, 0.25f, 0.2f, 1f), lifetime = 30f } },
    };

    public event Action<GameObject> OnPaintApplied;

    private void Awake()
    {
        // Initialize object pool and active platforms list
        platformPool = new Queue<GameObject>();
        activeBrushStrokes = new List<PlatformData>();
        InitializeObjectPool();
    }

    private void Start()
    {
        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogWarning("Player GameObject does not have an Animator component!");
            }
        }
        else
        {
            Debug.LogWarning("Player reference is not set in PaintManager!");
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }

        if (paintParticles != null)
        {
            var main = paintParticles.main;
            main.loop = false;
            paintParticles.Stop();
        }

        // Initialize NativeArrays with initial capacity
        arrayCapacity = poolSize;
        creationTimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
        durations = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
        lifetimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
        isTemporary = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
        newAlphas = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
        shouldDestroy = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
        positionsX = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
    }

    private void OnDestroy()
    {
        // Dispose NativeArrays
        if (creationTimes.IsCreated) creationTimes.Dispose();
        if (durations.IsCreated) durations.Dispose();
        if (lifetimes.IsCreated) lifetimes.Dispose();
        if (isTemporary.IsCreated) isTemporary.Dispose();
        if (newAlphas.IsCreated) newAlphas.Dispose();
        if (shouldDestroy.IsCreated) shouldDestroy.Dispose();
        if (positionsX.IsCreated) positionsX.Dispose();
    }

    private void InitializeObjectPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject platform = Instantiate(platformPrefab, platformsParent);
            platform.SetActive(false);
            platformPool.Enqueue(platform);
        }
        Debug.Log($"Object pool initialized with {poolSize} platforms.");
    }

    private GameObject GetPlatformFromPool()
    {
        if (platformPool.Count > 0)
        {
            GameObject platform = platformPool.Dequeue();
            platform.SetActive(true);
            return platform;
        }
        else
        {
            GameObject platform = Instantiate(platformPrefab, platformsParent);
            Debug.LogWarning("Object pool exhausted, instantiating new platform.");
            return platform;
        }
    }

    private void ReturnPlatformToPool(GameObject platform)
    {
        platform.SetActive(false);
        platformPool.Enqueue(platform);
    }

    public void SetPlayer(GameObject newPlayer)
    {
        if (newPlayer == null)
        {
            Debug.LogError("Cannot set player: newPlayer is null");
            return;
        }

        player = newPlayer;
        playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            playerAnimator = player.AddComponent<Animator>();
            Debug.LogWarning("Player did not have an Animator; one was added automatically.");
        }

        Debug.Log($"Player set to {player.name}");
    }

    private void Update()
    {
        HandleInput();
        UpdateTemporaryPlatforms();
    }

    private void HandleInput()
    {
        float facingDirection = player.transform.localScale.x;

        if (IsPointerOverUI())
        {
            StopSpellAnimation();
            return;
        }

        bool isPainting = false;
        Vector3 paintPos = Vector3.zero;

        if (!Application.isMobilePlatform)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
            {
                paintPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                paintPos.z = 0;

                if (IsPositionAheadOfPlayer(paintPos, facingDirection))
                {
                    isPainting = true;
                    if (playerAnimator != null && !playerAnimator.GetBool("Spell"))
                    {
                        playerAnimator.SetBool("Spell", true);
                        AudioManager.Instance.PlayWaterSound();
                    }
                    ApplyPaint(paintPos);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                StopSpellAnimation();
            }
        }
        else if (Touchscreen.current != null)
        {
            bool isTouchPressed = Touchscreen.current.primaryTouch.press.isPressed;

            if (isTouchPressed)
            {
                paintPos = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
                paintPos.z = 0;

                if (IsPositionAheadOfPlayer(paintPos, facingDirection))
                {
                    isPainting = true;
                    if (playerAnimator != null && !playerAnimator.GetBool("Spell"))
                    {
                        playerAnimator.SetBool("Spell", true);
                        AudioManager.Instance.PlayWaterSound();
                    }
                    ApplyPaint(paintPos);
                }
            }
            else
            {
                StopSpellAnimation();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetPaintColor("purple");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetPaintColor("blue");
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetPaintColor("red");
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetPaintColor("yellow");
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetPaintColor("ghost");
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetPaintColor("brown");

        if (!isPainting)
        {
            StopSpellAnimation();
        }
    }

    private bool IsPointerOverUI()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
#elif UNITY_IOS || UNITY_ANDROID
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        else
            return false;
#else
        return false;
#endif
    }

    private void StopSpellAnimation()
    {
        if (playerAnimator != null && playerAnimator.GetBool("Spell"))
        {
            playerAnimator.SetBool("Spell", false);
        }
    }

    private bool IsPositionAheadOfPlayer(Vector3 paintPosition, float facingDirection)
    {
        Vector3 playerPos = player.transform.position;
        return facingDirection > 0 ? paintPosition.x > playerPos.x : paintPosition.x < playerPos.x;
    }

    private void UpdateTemporaryPlatforms()
    {
        if (activeBrushStrokes.Count == 0) return;

        int count = activeBrushStrokes.Count;

        // Resize NativeArrays if needed
        if (count > arrayCapacity)
        {
            // Dispose old arrays
            if (creationTimes.IsCreated) creationTimes.Dispose();
            if (durations.IsCreated) durations.Dispose();
            if (lifetimes.IsCreated) lifetimes.Dispose();
            if (isTemporary.IsCreated) isTemporary.Dispose();
            if (newAlphas.IsCreated) newAlphas.Dispose();
            if (shouldDestroy.IsCreated) shouldDestroy.Dispose();
            if (positionsX.IsCreated) positionsX.Dispose();

            // Allocate new arrays with increased capacity
            arrayCapacity = Mathf.Max(count, arrayCapacity * 2);
            creationTimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
            durations = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
            lifetimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
            isTemporary = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
            newAlphas = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
            shouldDestroy = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
            positionsX = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
            Debug.Log($"Resized NativeArrays to capacity {arrayCapacity}");
        }

        // Populate job data
        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - platformsParent.position.z);
        float cameraLeftBoundary = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance)).x;
        for (int i = 0; i < count; i++)
        {
            var data = activeBrushStrokes[i];
            if (data.Platform == null) continue;

            var stroke = data.Stroke;
            creationTimes[i] = stroke.CreationTime;
            durations[i] = stroke.Duration;
            lifetimes[i] = stroke.Lifetime;
            isTemporary[i] = stroke.IsTemporary;
            positionsX[i] = data.PositionX;
        }

        // Schedule job
        var job = new PaintFadeJob
        {
            currentTime = Time.time,
            creationTimes = creationTimes,
            durations = durations,
            lifetimes = lifetimes,
            isTemporary = isTemporary,
            newAlphas = newAlphas,
            shouldDestroy = shouldDestroy,
            cameraLeftBoundary = cameraLeftBoundary,
            recycleDistance = recycleDistance,
            positionsX = positionsX
        };

        JobHandle handle = job.Schedule(count, 64);
        handle.Complete();

        // Apply results
        for (int i = count - 1; i >= 0; i--)
        {
            var data = activeBrushStrokes[i];
            if (data.Platform == null)
            {
                activeBrushStrokes.RemoveAt(i);
                continue;
            }

            if (shouldDestroy[i])
            {
                ReturnPlatformToPool(data.Platform);
                activeBrushStrokes.RemoveAt(i);
            }
            else
            {
                var color = data.Renderer.color;
                color.a = newAlphas[i];
                data.Renderer.color = color;
            }
        }
    }

    public void SetSelectedColor(string color)
    {
        selectedColor = color;
    }

    public GameObject ApplyPaint(Vector3 position)
    {
        GameObject newPlatform = GetPlatformFromPool();
        newPlatform.transform.position = position;
        newPlatform.transform.rotation = Quaternion.identity;
        newPlatform.name = $"{selectedColor}Platform";
        newPlatform.tag = "Paint";
        newPlatform.layer = LayerMask.NameToLayer("Platforms");
        newPlatform.transform.localScale = new Vector3(brushSize / 10f, brushHeight / 10f, 1f);

        PaintStroke stroke = newPlatform.GetComponent<PaintStroke>();
        if (stroke == null)
        {
            stroke = newPlatform.AddComponent<PaintStroke>();
        }
        stroke.Initialize(selectedColor, GetColorProperties(selectedColor));

        SpriteRenderer renderer = newPlatform.GetComponent<SpriteRenderer>();
        if (renderer != null && colorProperties.TryGetValue(selectedColor, out ColorProperty props))
        {
            renderer.color = props.paintColor;

            if (paintParticles != null)
            {
                paintParticles.transform.position = position;
                var main = paintParticles.main;
                main.startColor = props.paintColor;
                paintParticles.Play();
            }
        }

        BoxCollider2D collider = newPlatform.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = newPlatform.AddComponent<BoxCollider2D>();
        }

        // Cache components and position in PlatformData
        activeBrushStrokes.Add(new PlatformData
        {
            Platform = newPlatform,
            Stroke = stroke,
            Renderer = renderer,
            PositionX = position.x
        });

        CreatePaintParticles(position, selectedColor);
        OnPaintApplied?.Invoke(newPlatform);
        return newPlatform;
    }

    private void CreatePaintParticles(Vector3 position, string colorType)
    {
        int particleCount = GetParticleCountForColor(colorType);
        GameObject particleSystemObj = Instantiate(particlePrefab, position, Quaternion.identity);
        ParticleSystem particles = particleSystemObj.GetComponent<ParticleSystem>();

        if (particles != null && colorProperties.TryGetValue(colorType, out ColorProperty props))
        {
            var main = particles.main;
            main.startColor = props.paintColor;
            main.loop = false;
            main.startLifetime = paintParticleLifetime;

            switch (colorType)
            {
                case "red":
                    main.startLifetime = Mathf.Min(0.5f, paintParticleLifetime);
                    break;

                case "yellow":
                    var velocity = particles.velocityOverLifetime;
                    velocity.enabled = true;
                    velocity.space = ParticleSystemSimulationSpace.World;

                    var xCurve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(1f, 5f));
                    velocity.x = new ParticleSystem.MinMaxCurve(2.0f, xCurve);

                    var yCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
                    velocity.y = new ParticleSystem.MinMaxCurve(0f, yCurve);
                    break;

                case "ghost":
                    main.startLifetime = Mathf.Min(0.5f, paintParticleLifetime);
                    break;
            }

            var emission = particles.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));
            particles.Play();
        }

        Destroy(particleSystemObj, paintParticleLifetime + 0.5f);
    }

    private int GetParticleCountForColor(string colorType)
    {
        switch (colorType)
        {
            case "blue": return 12;
            case "red": return 8;
            case "yellow": return 10;
            case "purple": return 10;
            case "ghost": return 6;
            case "brown": return 14;
            default: return 8;
        }
    }

    private ColorProperty GetColorProperties(string colorType)
    {
        if (colorProperties.TryGetValue(colorType, out ColorProperty property))
        {
            return property;
        }
        return new ColorProperty { name = "Default" };
    }

    public bool SetPaintColor(string colorName)
    {
        if (colorProperties.ContainsKey(colorName))
        {
            selectedColor = colorName;
            Debug.Log($"Paint color changed to {colorName}");
            return true;
        }
        Debug.LogWarning($"Invalid paint color: {colorName}");
        return false;
    }

    public List<GameObject> GetActiveBrushStrokes()
    {
        activeBrushStrokes.RemoveAll(data => data.Platform == null);
        return activeBrushStrokes.ConvertAll(data => data.Platform);
    }

    public void ClearAllPaint()
    {
        foreach (var data in activeBrushStrokes)
        {
            if (data.Platform != null)
            {
                ReturnPlatformToPool(data.Platform);
            }
        }
        activeBrushStrokes.Clear();
        Debug.Log("All paint cleared from the world");
    }
}

public class PaintStroke : MonoBehaviour
{
    public string PaintType { get; private set; }
    public float BounceFactor { get; private set; }
    public float SpeedBoost { get; private set; }
    public bool IsTemporary { get; private set; }
    public bool IsGhost { get; private set; }
    public float Duration { get; private set; }
    public float RemainingTime { get; private set; }
    private float creationTime;
    private float lifetime = 30f;
    public float Lifetime => lifetime;
    public float CreationTime => creationTime;

    public void Initialize(string paintType, PaintManager.ColorProperty properties)
    {
        PaintType = paintType;
        creationTime = Time.time;

        if (properties != null)
        {
            lifetime = properties.lifetime;

            switch (paintType)
            {
                case "blue":
                    BounceFactor = properties.bounceFactor;
                    break;
                case "red":
                    IsTemporary = true;
                    Duration = properties.duration;
                    RemainingTime = Duration;
                    break;
                case "yellow":
                    SpeedBoost = properties.speedBoost;
                    break;
                case "ghost":
                    IsGhost = true;
                    break;
                case "brown":
                    gameObject.tag = "Blocker";
                    break;
            }
        }
    }

    private void Update()
    {
        if (IsTemporary)
        {
            RemainingTime = Duration - (Time.time - creationTime);
        }
    }
}

[BurstCompile]
public struct PaintFadeJob : IJobParallelFor
{
    public float currentTime;
    [ReadOnly] public NativeArray<float> creationTimes;
    [ReadOnly] public NativeArray<float> durations;
    [ReadOnly] public NativeArray<float> lifetimes;
    [ReadOnly] public NativeArray<bool> isTemporary;
    [WriteOnly] public NativeArray<float> newAlphas;
    [WriteOnly] public NativeArray<bool> shouldDestroy;
    public float cameraLeftBoundary; // Camera's left viewport boundary in world space
    public float recycleDistance; // Distance threshold for recycling
    [ReadOnly] public NativeArray<float> positionsX; // Platform x-positions

    public void Execute(int index)
    {
        float alpha = 1f;
        bool destroy = false;

        // Check lifetime and temporary duration
        if (isTemporary[index])
        {
            float remaining = durations[index] - (currentTime - creationTimes[index]);
            if (remaining <= 0)
            {
                alpha = 0f;
                destroy = true;
            }
            else
            {
                alpha = math.clamp(remaining / durations[index], 0f, 1f);
            }
        }
        else if (lifetimes[index] > 0f && currentTime - creationTimes[index] >= lifetimes[index])
        {
            alpha = 0f;
            destroy = true;
        }

        // Check if platform is left of the camera's left boundary minus recycleDistance
        if (!destroy && positionsX[index] < cameraLeftBoundary - recycleDistance)
        {
            alpha = 0f;
            destroy = true;
        }

        newAlphas[index] = alpha;
        shouldDestroy[index] = destroy;
    }
}






//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Burst;
//using Unity.Mathematics;
//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.InputSystem;

//public class PaintManager : MonoBehaviour
//{
//    [Serializable]
//    public class ColorProperty
//    {
//        public string name;
//        public float bounceFactor;
//        public float duration;
//        public float speedBoost;
//        public Color paintColor;
//        public float lifetime = -1f;
//    }

//    // Struct to cache platform components
//    private struct PlatformData
//    {
//        public GameObject Platform;
//        public PaintStroke Stroke;
//        public SpriteRenderer Renderer;
//        public float PositionX; // Cache x-position for job
//    }

//    [SerializeField] private float brushSize = 20f;
//    [SerializeField] private float brushHeight = 5f;
//    [SerializeField] private GameObject platformPrefab;
//    [SerializeField] private GameObject particlePrefab;
//    [SerializeField] private Transform platformsParent;
//    [SerializeField] private string selectedColor = "purple";
//    [SerializeField] private GameObject player;
//    [SerializeField] private ParticleSystem paintParticles;
//    [SerializeField] private VirtualJoystick joystick;
//    [SerializeField] private float paintParticleLifetime = 3f;
//    [SerializeField] private int poolSize = 50; // Number of platforms to pool
//    [SerializeField] private float recycleDistance = 20f; // Distance from camera left boundary to recycle platforms

//    private Queue<GameObject> platformPool; // Object pool for platforms
//    private List<PlatformData> activeBrushStrokes; // Tracks active platforms with components
//    private Animator playerAnimator;
//    private Camera mainCamera;
//    private NativeArray<float> creationTimes;
//    private NativeArray<float> durations;
//    private NativeArray<float> lifetimes;
//    private NativeArray<bool> isTemporary;
//    private NativeArray<float> newAlphas;
//    private NativeArray<bool> shouldDestroy;
//    private NativeArray<float> positionsX; // Store platform x-positions for job
//    private int arrayCapacity; // Tracks current NativeArray capacity

//    [SerializeField]
//    private Dictionary<string, ColorProperty> colorProperties = new Dictionary<string, ColorProperty>()
//    {
//        { "purple", new ColorProperty { name = "Platform", paintColor = new Color(0.55f, 0.27f, 0.68f, 1f), lifetime = 30f } },
//        { "blue", new ColorProperty { name = "Bouncy", bounceFactor = 6f, paintColor = new Color(0.2f, 0.6f, 0.9f, 1f), lifetime = 30f } },
//        { "red", new ColorProperty { name = "Temporary", duration = 3f, paintColor = new Color(0.9f, 0.3f, 0.2f, 1f), lifetime = 30f } },
//        { "yellow", new ColorProperty { name = "Speed", speedBoost = 1.5f, paintColor = new Color(0.95f, 0.77f, 0.06f, 1f), lifetime = 30f } },
//        { "ghost", new ColorProperty { name = "Ghost", paintColor = new Color(0.9f, 0.9f, 0.9f, 0.8f), lifetime = 30f } },
//        { "brown", new ColorProperty { name = "Blocker", paintColor = new Color(0.36f, 0.25f, 0.2f, 1f), lifetime = 30f } },
//    };

//    public event Action<GameObject> OnPaintApplied;

//    private void Awake()
//    {
//        // Initialize object pool and active platforms list
//        platformPool = new Queue<GameObject>();
//        activeBrushStrokes = new List<PlatformData>();
//        InitializeObjectPool();
//    }

//    private void Start()
//    {
//        if (player != null)
//        {
//            playerAnimator = player.GetComponent<Animator>();
//            if (playerAnimator == null)
//            {
//                Debug.LogWarning("Player GameObject does not have an Animator component!");
//            }
//        }
//        else
//        {
//            Debug.LogWarning("Player reference is not set in PaintManager!");
//        }

//        mainCamera = Camera.main;
//        if (mainCamera == null)
//        {
//            Debug.LogError("Main Camera not found!");
//        }

//        if (paintParticles != null)
//        {
//            var main = paintParticles.main;
//            main.loop = false;
//            paintParticles.Stop();
//        }

//        // Initialize NativeArrays with initial capacity
//        arrayCapacity = poolSize;
//        creationTimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//        durations = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//        lifetimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//        isTemporary = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
//        newAlphas = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//        shouldDestroy = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
//        positionsX = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//    }

//    private void OnDestroy()
//    {
//        // Dispose NativeArrays
//        if (creationTimes.IsCreated) creationTimes.Dispose();
//        if (durations.IsCreated) durations.Dispose();
//        if (lifetimes.IsCreated) lifetimes.Dispose();
//        if (isTemporary.IsCreated) isTemporary.Dispose();
//        if (newAlphas.IsCreated) newAlphas.Dispose();
//        if (shouldDestroy.IsCreated) shouldDestroy.Dispose();
//        if (positionsX.IsCreated) positionsX.Dispose();
//    }

//    private void InitializeObjectPool()
//    {
//        for (int i = 0; i < poolSize; i++)
//        {
//            GameObject platform = Instantiate(platformPrefab);
//            platform.transform.SetParent(platformsParent, false);
//            platform.SetActive(false);
//            platformPool.Enqueue(platform);
//        }
//        Debug.Log($"Object pool initialized with {poolSize} platforms.");
//    }

//    private GameObject GetPlatformFromPool()
//    {
//        if (platformPool.Count > 0)
//        {
//            GameObject platform = platformPool.Dequeue();
//            platform.SetActive(true);
//            return platform;
//        }
//        else
//        {
//            GameObject platform = Instantiate(platformPrefab);
//            platform.transform.SetParent(platformsParent, false);
//            Debug.LogWarning("Object pool exhausted, instantiating new platform.");
//            return platform;
//        }
//    }

//    private void ReturnPlatformToPool(GameObject platform)
//    {
//        platform.SetActive(false);
//        platformPool.Enqueue(platform);
//    }

//    public void SetPlayer(GameObject newPlayer)
//    {
//        if (newPlayer == null)
//        {
//            Debug.LogError("Cannot set player: newPlayer is null");
//            return;
//        }

//        player = newPlayer;
//        playerAnimator = player.GetComponent<Animator>();
//        if (playerAnimator == null)
//        {
//            playerAnimator = player.AddComponent<Animator>();
//            Debug.LogWarning("Player did not have an Animator; one was added automatically.");
//        }

//        Debug.Log($"Player set to {player.name}");
//    }

//    private void Update()
//    {
//        HandleInput();
//        UpdateTemporaryPlatforms();
//    }

//    private void HandleInput()
//    {
//        float facingDirection = player.transform.localScale.x;

//        if (IsPointerOverUI())
//        {
//            StopSpellAnimation();
//            return;
//        }

//        bool isPainting = false;
//        Vector3 paintPos = Vector3.zero;

//        if (!Application.isMobilePlatform)
//        {
//            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
//            {
//                paintPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//                paintPos.z = 0;

//                if (IsPositionAheadOfPlayer(paintPos, facingDirection))
//                {
//                    isPainting = true;
//                    if (playerAnimator != null && !playerAnimator.GetBool("Spell"))
//                    {
//                        playerAnimator.SetBool("Spell", true);
//                        AudioManager.Instance.PlayWaterSound();
//                    }
//                    ApplyPaint(paintPos);
//                }
//            }

//            if (Input.GetMouseButtonUp(0))
//            {
//                StopSpellAnimation();
//            }
//        }
//        else if (Touchscreen.current != null)
//        {
//            bool isTouchPressed = Touchscreen.current.primaryTouch.press.isPressed;

//            if (isTouchPressed)
//            {
//                paintPos = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
//                paintPos.z = 0;

//                if (IsPositionAheadOfPlayer(paintPos, facingDirection))
//                {
//                    isPainting = true;
//                    if (playerAnimator != null && !playerAnimator.GetBool("Spell"))
//                    {
//                        playerAnimator.SetBool("Spell", true);
//                        AudioManager.Instance.PlayWaterSound();
//                    }
//                    ApplyPaint(paintPos);
//                }
//            }
//            else
//            {
//                StopSpellAnimation();
//            }
//        }

//        if (Input.GetKeyDown(KeyCode.Alpha1)) SetPaintColor("purple");
//        if (Input.GetKeyDown(KeyCode.Alpha2)) SetPaintColor("blue");
//        if (Input.GetKeyDown(KeyCode.Alpha3)) SetPaintColor("red");
//        if (Input.GetKeyDown(KeyCode.Alpha4)) SetPaintColor("yellow");
//        if (Input.GetKeyDown(KeyCode.Alpha5)) SetPaintColor("ghost");
//        if (Input.GetKeyDown(KeyCode.Alpha6)) SetPaintColor("brown");

//        if (!isPainting)
//        {
//            StopSpellAnimation();
//        }
//    }

//    private bool IsPointerOverUI()
//    {
//#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
//        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
//#elif UNITY_IOS || UNITY_ANDROID
//        if (EventSystem.current == null) return false;
//        if (Input.touchCount > 0)
//            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
//        else
//            return false;
//#else
//        return false;
//#endif
//    }

//    private void StopSpellAnimation()
//    {
//        if (playerAnimator != null && playerAnimator.GetBool("Spell"))
//        {
//            playerAnimator.SetBool("Spell", false);
//        }
//    }

//    private bool IsPositionAheadOfPlayer(Vector3 paintPosition, float facingDirection)
//    {
//        Vector3 playerPos = player.transform.position;
//        return facingDirection > 0 ? paintPosition.x > playerPos.x : paintPosition.x < playerPos.x;
//    }

//    private void UpdateTemporaryPlatforms()
//    {
//        if (activeBrushStrokes.Count == 0) return;

//        int count = activeBrushStrokes.Count;

//        // Resize NativeArrays if needed
//        if (count > arrayCapacity)
//        {
//            // Dispose old arrays
//            if (creationTimes.IsCreated) creationTimes.Dispose();
//            if (durations.IsCreated) durations.Dispose();
//            if (lifetimes.IsCreated) lifetimes.Dispose();
//            if (isTemporary.IsCreated) isTemporary.Dispose();
//            if (newAlphas.IsCreated) newAlphas.Dispose();
//            if (shouldDestroy.IsCreated) shouldDestroy.Dispose();
//            if (positionsX.IsCreated) positionsX.Dispose();

//            // Allocate new arrays with increased capacity
//            arrayCapacity = Mathf.Max(count, arrayCapacity * 2);
//            creationTimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//            durations = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//            lifetimes = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//            isTemporary = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
//            newAlphas = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//            shouldDestroy = new NativeArray<bool>(arrayCapacity, Allocator.TempJob);
//            positionsX = new NativeArray<float>(arrayCapacity, Allocator.TempJob);
//            Debug.Log($"Resized NativeArrays to capacity {arrayCapacity}");
//        }

//        // Populate job data
//        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - platformsParent.position.z);
//        float cameraLeftBoundary = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance)).x;
//        for (int i = 0; i < count; i++)
//        {
//            var data = activeBrushStrokes[i];
//            if (data.Platform == null) continue;

//            var stroke = data.Stroke;
//            creationTimes[i] = stroke.CreationTime;
//            durations[i] = stroke.Duration;
//            lifetimes[i] = stroke.Lifetime;
//            isTemporary[i] = stroke.IsTemporary;
//            positionsX[i] = data.PositionX;
//        }

//        // Schedule job
//        var job = new PaintFadeJob
//        {
//            currentTime = Time.time,
//            creationTimes = creationTimes,
//            durations = durations,
//            lifetimes = lifetimes,
//            isTemporary = isTemporary,
//            newAlphas = newAlphas,
//            shouldDestroy = shouldDestroy,
//            cameraLeftBoundary = cameraLeftBoundary,
//            recycleDistance = recycleDistance,
//            positionsX = positionsX
//        };

//        JobHandle handle = job.Schedule(count, 64);
//        handle.Complete();

//        // Apply results
//        for (int i = count - 1; i >= 0; i--)
//        {
//            var data = activeBrushStrokes[i];
//            if (data.Platform == null)
//            {
//                activeBrushStrokes.RemoveAt(i);
//                continue;
//            }

//            if (shouldDestroy[i])
//            {
//                ReturnPlatformToPool(data.Platform);
//                activeBrushStrokes.RemoveAt(i);
//            }
//            else
//            {
//                var color = data.Renderer.color;
//                color.a = newAlphas[i];
//                data.Renderer.color = color;
//            }
//        }
//    }

//    public void SetSelectedColor(string color)
//    {
//        selectedColor = color;
//    }

//    public GameObject ApplyPaint(Vector3 position)
//    {
//        GameObject newPlatform = GetPlatformFromPool();
//        newPlatform.transform.position = position;
//        newPlatform.transform.rotation = Quaternion.identity;
//        newPlatform.name = $"{selectedColor}Platform";
//        newPlatform.tag = "Paint";
//        newPlatform.layer = LayerMask.NameToLayer("Platforms");
//        newPlatform.transform.localScale = new Vector3(brushSize / 10f, brushHeight / 10f, 1f);

//        PaintStroke stroke = newPlatform.GetComponent<PaintStroke>();
//        if (stroke == null)
//        {
//            stroke = newPlatform.AddComponent<PaintStroke>();
//        }
//        stroke.Initialize(selectedColor, GetColorProperties(selectedColor));

//        SpriteRenderer renderer = newPlatform.GetComponent<SpriteRenderer>();
//        if (renderer != null && colorProperties.TryGetValue(selectedColor, out ColorProperty props))
//        {
//            renderer.color = props.paintColor;

//            if (paintParticles != null)
//            {
//                paintParticles.transform.position = position;
//                var main = paintParticles.main;
//                main.startColor = props.paintColor;
//                paintParticles.Play();
//            }
//        }

//        BoxCollider2D collider = newPlatform.GetComponent<BoxCollider2D>();
//        if (collider == null)
//        {
//            collider = newPlatform.AddComponent<BoxCollider2D>();
//        }

//        // Cache components and position in PlatformData
//        activeBrushStrokes.Add(new PlatformData
//        {
//            Platform = newPlatform,
//            Stroke = stroke,
//            Renderer = renderer,
//            PositionX = position.x
//        });

//        CreatePaintParticles(position, selectedColor);
//        OnPaintApplied?.Invoke(newPlatform);
//        return newPlatform;
//    }

//    private void CreatePaintParticles(Vector3 position, string colorType)
//    {
//        int particleCount = GetParticleCountForColor(colorType);
//        GameObject particleSystemObj = Instantiate(particlePrefab, position, Quaternion.identity);
//        ParticleSystem particles = particleSystemObj.GetComponent<ParticleSystem>();

//        if (particles != null && colorProperties.TryGetValue(colorType, out ColorProperty props))
//        {
//            var main = particles.main;
//            main.startColor = props.paintColor;
//            main.loop = false;
//            main.startLifetime = paintParticleLifetime;

//            switch (colorType)
//            {
//                case "red":
//                    main.startLifetime = Mathf.Min(0.5f, paintParticleLifetime);
//                    break;

//                case "yellow":
//                    var velocity = particles.velocityOverLifetime;
//                    velocity.enabled = true;
//                    velocity.space = ParticleSystemSimulationSpace.World;

//                    var xCurve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(1f, 5f));
//                    velocity.x = new ParticleSystem.MinMaxCurve(2.0f, xCurve);

//                    var yCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
//                    velocity.y = new ParticleSystem.MinMaxCurve(0f, yCurve);
//                    break;

//                case "ghost":
//                    main.startLifetime = Mathf.Min(0.5f, paintParticleLifetime);
//                    break;
//            }

//            var emission = particles.emission;
//            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));
//            particles.Play();
//        }

//        Destroy(particleSystemObj, paintParticleLifetime + 0.5f);
//    }

//    private int GetParticleCountForColor(string colorType)
//    {
//        switch (colorType)
//        {
//            case "blue": return 12;
//            case "red": return 8;
//            case "yellow": return 10;
//            case "purple": return 10;
//            case "ghost": return 6;
//            case "brown": return 14;
//            default: return 8;
//        }
//    }

//    private ColorProperty GetColorProperties(string colorType)
//    {
//        if (colorProperties.TryGetValue(colorType, out ColorProperty property))
//        {
//            return property;
//        }
//        return new ColorProperty { name = "Default" };
//    }

//    public bool SetPaintColor(string colorName)
//    {
//        if (colorProperties.ContainsKey(colorName))
//        {
//            selectedColor = colorName;
//            Debug.Log($"Paint color changed to {colorName}");
//            return true;
//        }
//        Debug.LogWarning($"Invalid paint color: {colorName}");
//        return false;
//    }

//    public List<GameObject> GetActiveBrushStrokes()
//    {
//        activeBrushStrokes.RemoveAll(data => data.Platform == null);
//        return activeBrushStrokes.ConvertAll(data => data.Platform);
//    }

//    public void ClearAllPaint()
//    {
//        foreach (var data in activeBrushStrokes)
//        {
//            if (data.Platform != null)
//            {
//                ReturnPlatformToPool(data.Platform);
//            }
//        }
//        activeBrushStrokes.Clear();
//        Debug.Log("All paint cleared from the world");
//    }
//}

//public class PaintStroke : MonoBehaviour
//{
//    public string PaintType { get; private set; }
//    public float BounceFactor { get; private set; }
//    public float SpeedBoost { get; private set; }
//    public bool IsTemporary { get; private set; }
//    public bool IsGhost { get; private set; }
//    public float Duration { get; private set; }
//    public float RemainingTime { get; private set; }
//    private float creationTime;
//    private float lifetime = 30f;
//    public float Lifetime => lifetime;
//    public float CreationTime => creationTime;

//    public void Initialize(string paintType, PaintManager.ColorProperty properties)
//    {
//        PaintType = paintType;
//        creationTime = Time.time;

//        if (properties != null)
//        {
//            lifetime = properties.lifetime;

//            switch (paintType)
//            {
//                case "blue":
//                    BounceFactor = properties.bounceFactor;
//                    break;
//                case "red":
//                    IsTemporary = true;
//                    Duration = properties.duration;
//                    RemainingTime = Duration;
//                    break;
//                case "yellow":
//                    SpeedBoost = properties.speedBoost;
//                    break;
//                case "ghost":
//                    IsGhost = true;
//                    break;
//                case "brown":
//                    gameObject.tag = "Blocker";
//                    break;
//            }
//        }
//    }

//    private void Update()
//    {
//        if (IsTemporary)
//        {
//            RemainingTime = Duration - (Time.time - creationTime);
//        }
//    }
//}

//[BurstCompile]
//public struct PaintFadeJob : IJobParallelFor
//{
//    public float currentTime;
//    [ReadOnly] public NativeArray<float> creationTimes;
//    [ReadOnly] public NativeArray<float> durations;
//    [ReadOnly] public NativeArray<float> lifetimes;
//    [ReadOnly] public NativeArray<bool> isTemporary;
//    [WriteOnly] public NativeArray<float> newAlphas;
//    [WriteOnly] public NativeArray<bool> shouldDestroy;
//    public float cameraLeftBoundary; // Camera's left viewport boundary in world space
//    public float recycleDistance; // Distance threshold for recycling
//    [ReadOnly] public NativeArray<float> positionsX; // Platform x-positions

//    public void Execute(int index)
//    {
//        float alpha = 1f;
//        bool destroy = false;

//        // Check lifetime and temporary duration
//        if (isTemporary[index])
//        {
//            float remaining = durations[index] - (currentTime - creationTimes[index]);
//            if (remaining <= 0)
//            {
//                alpha = 0f;
//                destroy = true;
//            }
//            else
//            {
//                alpha = math.clamp(remaining / durations[index], 0f, 1f);
//            }
//        }
//        else if (lifetimes[index] > 0f && currentTime - creationTimes[index] >= lifetimes[index])
//        {
//            alpha = 0f;
//            destroy = true;
//        }

//        // Check if platform is left of the camera's left boundary minus recycleDistance
//        if (!destroy && positionsX[index] < cameraLeftBoundary - recycleDistance)
//        {
//            alpha = 0f;
//            destroy = true;
//        }

//        newAlphas[index] = alpha;
//        shouldDestroy[index] = destroy;
//    }
//}