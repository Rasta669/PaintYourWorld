using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;
using System.Collections;
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


    [SerializeField] private float brushSize = 20f;
    [SerializeField] private float brushHeight = 5f;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Transform platformsParent;
    [SerializeField] private string selectedColor = "purple";
    [SerializeField] private GameObject player;
    [SerializeField] private ParticleSystem paintParticles;
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private float paintParticleLifetime = 3f; // NEW PARAMETER for particle lifetime

    [SerializeField]
    private Dictionary<string, ColorProperty> colorProperties = new Dictionary<string, ColorProperty>()
    {
        { "purple", new ColorProperty { name = "Platform", paintColor = new Color(0.55f, 0.27f, 0.68f, 1f), lifetime = 30f } },
        { "blue", new ColorProperty { name = "Bouncy", bounceFactor = 6f, paintColor = new Color(0.2f, 0.6f, 0.9f, 1f), lifetime = 30f } },
        { "red", new ColorProperty { name = "Temporary", duration = 3f, paintColor = new Color(0.9f, 0.3f, 0.2f, 1f), lifetime = 30f } },
        { "yellow", new ColorProperty { name = "Speed", speedBoost = 1.5f, paintColor = new Color(0.95f, 0.77f, 0.06f, 1f), lifetime = 30f } },
        { "ghost", new ColorProperty { name = "Ghost", paintColor = new Color(0.9f, 0.9f, 0.9f, 0.8f), lifetime = 30f } },
        { "brown", new ColorProperty { name = "Blocker", paintColor = new Color(0.36f, 0.25f, 0.2f, 1f), lifetime = 30f } },

    };

    //[SerializeField] private List<HiddenPath> hiddenPaths = new List<HiddenPath>();

    private List<GameObject> brushStrokes = new List<GameObject>();
    private Animator playerAnimator;

    public event Action<GameObject> OnPaintApplied;
    //public event Action<HiddenPath> OnPathRevealed;

    private void Start()
    {
        //InitializeHiddenPaths();

        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogError("Player GameObject does not have an Animator component!");
            }
        }
        else
        {
            Debug.LogError("Player reference is not set in PaintManager!");
        }

        if (paintParticles != null)
        {
            var main = paintParticles.main;
            main.loop = false;
            paintParticles.Stop();
        }
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
        if (brushStrokes.Count == 0) return;

        int count = brushStrokes.Count;
        var creationTimes = new NativeArray<float>(count, Allocator.TempJob);
        var durations = new NativeArray<float>(count, Allocator.TempJob);
        var lifetimes = new NativeArray<float>(count, Allocator.TempJob);
        var isTemporary = new NativeArray<bool>(count, Allocator.TempJob);
        var newAlphas = new NativeArray<float>(count, Allocator.TempJob);
        var shouldDestroy = new NativeArray<bool>(count, Allocator.TempJob);

        for (int i = 0; i < count; i++)
        {
            var brush = brushStrokes[i];
            if (brush == null) continue;

            var stroke = brush.GetComponent<PaintStroke>();
            if (stroke == null) continue;

            creationTimes[i] = stroke.CreationTime;
            durations[i] = stroke.Duration;
            lifetimes[i] = stroke.Lifetime;
            isTemporary[i] = stroke.IsTemporary;
        }

        var job = new PaintFadeJob
        {
            currentTime = Time.time,
            creationTimes = creationTimes,
            durations = durations,
            lifetimes = lifetimes,
            isTemporary = isTemporary,
            newAlphas = newAlphas,
            shouldDestroy = shouldDestroy
        };

        JobHandle handle = job.Schedule(count, 64);
        handle.Complete();

        for (int i = count - 1; i >= 0; i--)
        {
            var brush = brushStrokes[i];
            if (brush == null)
            {
                brushStrokes.RemoveAt(i);
                continue;
            }

            if (shouldDestroy[i])
            {
                Destroy(brush);
                brushStrokes.RemoveAt(i);
            }
            else
            {
                var renderer = brush.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    var color = renderer.color;
                    color.a = newAlphas[i];
                    renderer.color = color;
                }
            }
        }

        creationTimes.Dispose();
        durations.Dispose();
        lifetimes.Dispose();
        isTemporary.Dispose();
        newAlphas.Dispose();
        shouldDestroy.Dispose();
    }



    public GameObject ApplyPaint(Vector3 position)
    {
        GameObject newPlatform = Instantiate(platformPrefab, position, Quaternion.identity, platformsParent);
        newPlatform.name = $"{selectedColor}Platform";
        newPlatform.tag = "Paint";
        newPlatform.layer = LayerMask.NameToLayer("Platforms");
        newPlatform.transform.localScale = new Vector3(brushSize / 10f, brushHeight / 10f, 1f);

        PaintStroke stroke = newPlatform.AddComponent<PaintStroke>();
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

        CreatePaintParticles(position, selectedColor);
        brushStrokes.Add(newPlatform);
        //CheckHiddenPathInteraction(position);
        OnPaintApplied?.Invoke(newPlatform);
        return newPlatform;
    }


    //private void CreatePaintParticles(Vector3 position, string colorType)
    //{
    //    int particleCount = GetParticleCountForColor(colorType);
    //    GameObject particleSystemObj = Instantiate(particlePrefab, position, Quaternion.identity);
    //    ParticleSystem particles = particleSystemObj.GetComponent<ParticleSystem>();

    //    if (particles != null && colorProperties.TryGetValue(colorType, out ColorProperty props))
    //    {
    //        var main = particles.main;
    //        main.startColor = props.paintColor;
    //        main.loop = false;

    //        // Ensure particles don’t live longer than expected
    //        main.startLifetime = paintParticleLifetime;

    //        switch (colorType)
    //        {
    //            case "red":
    //                main.startLifetime = Mathf.Min(0.5f, paintParticleLifetime);
    //                break;
    //            case "yellow":
    //                var velocity = particles.velocityOverLifetime;
    //                velocity.enabled = true;
    //                velocity.space = ParticleSystemSimulationSpace.World;

    //                var xCurve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(1f, 5f));
    //                velocity.x = new ParticleSystem.MinMaxCurve(2.0f, xCurve);

    //                var yCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
    //                velocity.y = new ParticleSystem.MinMaxCurve(0f, yCurve);
    //                break;
    //        }

    //        var emission = particles.emission;
    //        emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));
    //        particles.Play(); // ← force it to play
    //    }

    //    Destroy(particleSystemObj, paintParticleLifetime + 0.5f); // Give time for all particles to finish
    //}


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
        brushStrokes.RemoveAll(item => item == null);
        return brushStrokes;
    }

    //public List<HiddenPath> GetRevealedPaths()
    //{
    //    return hiddenPaths.FindAll(path => path.revealed);
    //}

    public void ClearAllPaint()
    {
        foreach (var stroke in brushStrokes)
        {
            if (stroke != null)
            {
                Destroy(stroke);
            }
        }
        brushStrokes.Clear();
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

            // Schedule destruction if lifetime is set
            if (lifetime > 0f)
            {
                Destroy(gameObject, lifetime);
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

    public void Execute(int index)
    {
        float alpha = 1f;
        bool destroy = false;

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

        newAlphas[index] = alpha;
        shouldDestroy[index] = destroy;
    }
}
