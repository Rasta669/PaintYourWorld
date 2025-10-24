//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.InputSystem;

//public class PlayerController : MonoBehaviour
//{
//    [Header("Movement Settings")]
//    [SerializeField] private float moveSpeed = 5f;
//    [SerializeField] private float jumpForce = 10f;
//    [SerializeField] private Vector2 playerSize = new Vector2(1f, 1f);

//    [Header("State")]
//    [SerializeField] private bool isGrounded;

//    private Rigidbody2D rb;
//    private BoxCollider2D boxCollider;
//    private SpriteRenderer spriteRenderer;
//    private Animator animator;
//    public GameManager UI;

//    [SerializeField] private LayerMask platformLayerMask;
//    [SerializeField] private VirtualJoystick joystick;

//    [Header("Particle Effects")]
//    public ParticleSystem fallParticle;
//    public ParticleSystem bloodParticle;
//    [SerializeField] private ParticleSystem jumpParticles;
//    [SerializeField] private ParticleSystem landParticles;

//    [Header("Health System")]
//    public int maxHealth = 3;
//    private int currentHealth;
//    private bool isDead = false;

//    private bool wasGrounded;
//    private PaintManager paintManager;
//    private ObstacleSpawner obstacleSpawner;
//    private float horizontalInput;
//    private bool isJumping;
//    private bool isGhostMode = false;
//    private Coroutine ghostRoutine;

//    private RaycastHit2D lastGroundHit;

//    private InputActionAsset inputActions;
//    private InputAction moveAction;
//    private InputAction jumpAction;

//    [Header("Animator Controllers")]
//    [SerializeField] private RuntimeAnimatorController solidAnimator;
//    [SerializeField] private RuntimeAnimatorController transparentAnimator;

//    void Start()
//    {
//        rb = GetComponent<Rigidbody2D>();
//        boxCollider = GetComponent<BoxCollider2D>();
//        spriteRenderer = GetComponent<SpriteRenderer>();
//        animator = GetComponent<Animator>();
//        UI = GameManager.Instance;
//        paintManager = FindObjectOfType<PaintManager>();
//        obstacleSpawner = FindObjectOfType<ObstacleSpawner>();

//        isGrounded = true;
//        wasGrounded = false;
//        isJumping = false;
//        currentHealth = maxHealth;
//        UpdateHealthUI();

//        rb.bodyType = RigidbodyType2D.Dynamic;
//        rb.gravityScale = 1f;
//        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

//        inputActions = GetComponent<PlayerInput>().actions;
//        moveAction = inputActions.FindAction("Player/Move");
//        jumpAction = inputActions.FindAction("Player/Jump");

//        moveAction.Enable();
//        jumpAction.Enable();
//        jumpAction.performed += OnJumpPerformed;
//    }

//    void OnDestroy()
//    {
//        StopAllCoroutines();
//    }

//    public void InitializeInput()
//    {
//        if (inputActions == null)
//            inputActions = GetComponent<PlayerInput>().actions;

//        moveAction = inputActions.FindAction("Player/Move");
//        jumpAction = inputActions.FindAction("Player/Jump");

//        if (moveAction != null) moveAction.Enable();
//        if (jumpAction != null)
//        {
//            jumpAction.Enable();
//            jumpAction.performed -= OnJumpPerformed;
//            jumpAction.performed += OnJumpPerformed;
//        }

//        Debug.Log("🎮 Player input system reinitialized.");
//    }

//    private void OnEnable()
//    {
//        if (inputActions == null && TryGetComponent<PlayerInput>(out var pi))
//            inputActions = pi.actions;

//        if (jumpAction == null)
//            jumpAction = inputActions.FindAction("Player/Jump");
//        if (moveAction == null)
//            moveAction = inputActions.FindAction("Player/Move");

//        if (jumpAction != null)
//        {
//            jumpAction.Enable();
//            jumpAction.performed -= OnJumpPerformed;
//            jumpAction.performed += OnJumpPerformed;
//        }
//        if (moveAction != null)
//            moveAction.Enable();
//    }

//    private void OnJumpPerformed(InputAction.CallbackContext context)
//    {
//        OnJump();
//    }

//    void Update()
//    {
//        ProcessInput();

//        if (!wasGrounded && isGrounded)
//        {
//            OnLanded();
//        }
//        wasGrounded = isGrounded;

//        UpdateAnimations();
//    }

//    void FixedUpdate()
//    {
//        MovePlayer();
//        CheckGrounded();
//        ApplyPlatformEffects();
//    }

//    private void ProcessInput()
//    {
//        if (joystick != null && joystick.IsDragging)
//        {
//            horizontalInput = joystick.InputVector.x;
//        }
//        else
//        {
//            Vector2 moveInput = moveAction.ReadValue<Vector2>();
//            horizontalInput = moveInput.x;
//        }
//    }

//    private void OnJump()
//    {
//        if (isGrounded && !isJumping)
//        {
//            StartJump(jumpForce);
//        }
//    }

//    private void MovePlayer()
//    {
//        transform.Translate(Vector2.right * horizontalInput * moveSpeed * Time.fixedDeltaTime);

//        if (horizontalInput > 0)
//        {
//            transform.localScale = new Vector3(1, 1, 1);
//            AudioManager.Instance.PlayWalkSound();
//        }
//        else if (horizontalInput < 0)
//        {
//            transform.localScale = new Vector3(-1, 1, 1);
//            AudioManager.Instance.PlayWalkSound();
//        }
//    }

//    private void StartJump(float force)
//    {
//        if (rb == null) return;

//        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
//        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
//        AudioManager.Instance.PlayJumpSound();

//        if (jumpParticles != null) jumpParticles.Play();

//        isJumping = true;
//        isGrounded = false;
//    }

//    private void OnLanded()
//    {
//        if (landParticles != null)
//        {
//            landParticles.Play();
//        }

//        isJumping = false;
//    }

//    private void UpdateAnimations()
//    {
//        if (animator != null)
//        {
//            animator.SetBool("Jump", isJumping);

//            PaintStroke platformUnder = GetPlatformUnder();
//            bool isOnYellowPaint = platformUnder != null && platformUnder.PaintType == "yellow" && isGrounded;

//            if (isGrounded && !isJumping)
//            {
//                animator.SetBool("Run", Mathf.Abs(horizontalInput) > 0 && !isOnYellowPaint);
//                animator.SetBool("FastRun", Mathf.Abs(horizontalInput) > 0 && isOnYellowPaint);
//            }
//            else
//            {
//                animator.SetBool("Run", false);
//                animator.SetBool("FastRun", false);
//            }
//        }
//    }

//    public void SetTransparentMode(bool transparent)
//    {
//        if (animator == null) return;

//        animator.runtimeAnimatorController = transparent ? transparentAnimator : solidAnimator;
//    }

//    private void CheckGrounded()
//    {
//        lastGroundHit = Physics2D.BoxCast(
//            transform.position,
//            boxCollider.size * 0.95f,
//            0f,
//            Vector2.down,
//            0.2f,
//            platformLayerMask
//        );

//        if (lastGroundHit.collider != null && rb.linearVelocity.y <= 0f)
//        {
//            isGrounded = true;
//        }
//        else
//        {
//            isGrounded = false;
//        }
//    }

//    public PaintStroke GetPlatformUnder()
//    {
//        if (isGrounded && lastGroundHit.collider != null)
//        {
//            return lastGroundHit.collider.GetComponent<PaintStroke>();
//        }
//        return null;
//    }

//    public void ApplyPlatformEffects()
//    {
//        PaintStroke platformUnder = GetPlatformUnder();

//        if (platformUnder != null && isGrounded && !isJumping)
//        {
//            switch (platformUnder.PaintType)
//            {
//                case "blue":
//                    StartJump(platformUnder.BounceFactor);
//                    break;

//                case "yellow":
//                    moveSpeed += platformUnder.SpeedBoost;
//                    StartCoroutine(ResetSpeedAfterDelay(0.5f));
//                    break;

//                case "ghost":
//                    if (!isGhostMode)
//                    {
//                        ghostRoutine = StartCoroutine(EnableGhostMode(3f));
//                    }
//                    break;
//            }
//        }
//    }

//    private IEnumerator EnableGhostMode(float duration)
//    {
//        isGhostMode = true;
//        SetTransparentMode(true);
//        Debug.Log("👻 Ghost mode activated!");

//        if (spriteRenderer != null)
//        {
//            Color c = spriteRenderer.color;
//            c.a = 0.5f;
//            spriteRenderer.color = c;
//        }

//        Collider2D playerCollider = GetComponent<Collider2D>();
//        if (obstacleSpawner != null && playerCollider != null)
//        {
//            List<GameObject> activeObstacles = obstacleSpawner.GetActiveObstacles();
//            foreach (GameObject obstacle in activeObstacles)
//            {
//                if (obstacle != null)
//                {
//                    Collider2D obsCol = obstacle.GetComponent<Collider2D>();
//                    if (obsCol != null)
//                    {
//                        Physics2D.IgnoreCollision(playerCollider, obsCol, true);
//                    }
//                }
//            }
//        }

//        yield return new WaitForSeconds(duration);

//        if (obstacleSpawner != null && playerCollider != null)
//        {
//            List<GameObject> activeObstacles = obstacleSpawner.GetActiveObstacles();
//            foreach (GameObject obstacle in activeObstacles)
//            {
//                if (obstacle != null)
//                {
//                    Collider2D obsCol = obstacle.GetComponent<Collider2D>();
//                    if (obsCol != null)
//                    {
//                        Physics2D.IgnoreCollision(playerCollider, obsCol, false);
//                    }
//                }
//            }
//        }

//        if (spriteRenderer != null)
//        {
//            Color c = spriteRenderer.color;
//            c.a = 1f;
//            spriteRenderer.color = c;
//        }

//        isGhostMode = false;
//        SetTransparentMode(false);
//        Debug.Log("👻 Ghost mode ended!");
//    }

//    private IEnumerator ResetSpeedAfterDelay(float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        moveSpeed = 5f;
//    }

//    public void ResetPosition()
//    {
//        transform.position = new Vector3(150f, 100f, 0f);
//        rb.linearVelocity = Vector2.zero;
//        isGrounded = false;
//        isJumping = false;
//    }

//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Paint"))
//        {
//            ContactPoint2D contact = collision.GetContact(0);
//            if (contact.normal.y > 0.5f && rb.linearVelocity.y <= 0f)
//            {
//                isGrounded = true;
//            }
//        }

//        if (collision.gameObject.CompareTag("obstacle") && !isGhostMode)
//        {
//            AudioManager.Instance.PlayDieSound();
//            TakeDamage();
//        }

//        if (collision.gameObject.CompareTag("gameOver"))
//        {
//            AudioManager.Instance.PlayDieSound();
//            isDead = true;
//            currentHealth = 0;
//            UI.GameOver();
//        }
//    }

//    private void OnCollisionExit2D(Collision2D collision)
//    {
//        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Paint"))
//        {
//            // Rely on CheckGrounded
//        }
//    }

//    private void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.blue;
//        Gizmos.DrawWireCube(transform.position, playerSize);
//    }

//    private void UpdateHealthUI()
//    {
//        if (UI != null)
//        {
//            UI.UpdatePlayerHealth(currentHealth);
//        }
//    }

//    public bool IsDead()
//    {
//        return isDead;
//    }

//    public void ResetHealth()
//    {
//        currentHealth = maxHealth;
//        isDead = false;
//        UpdateHealthUI();
//    }

//    public void Heal(int amount)
//    {
//        if (isDead) return;
//        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
//        UpdateHealthUI();
//    }

//    public int GetHealth()
//    {
//        return currentHealth;
//    }

//    private void TakeDamage()
//    {
//        if (isDead) return;
//        AudioManager.Instance.PlayDieSound();
//        currentHealth = Mathf.Max(0, currentHealth - 1);
//        UpdateHealthUI();

//        if (currentHealth <= 0)
//        {
//            isDead = true;
//            UI.GameOver();
//        }
//    }
//}


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Vector2 playerSize = new Vector2(1f, 1f);

    [Header("State")]
    [SerializeField] private bool isGrounded;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    public GameManager UI;

    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private VirtualJoystick joystick;

    [Header("Particle Effects")]
    public ParticleSystem fallParticle;
    public ParticleSystem bloodParticle;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem landParticles;

    [Header("Health System")]
    public int maxHealth = 3;
    private int currentHealth;
    private bool isDead = false;

    private bool wasGrounded;
    private PaintManager paintManager;
    private ObstacleSpawner obstacleSpawner;
    private float horizontalInput;
    private bool isJumping;
    private bool isGhostMode = false;
    private Coroutine ghostRoutine;
    private float ghostModeEndTime;

    private RaycastHit2D lastGroundHit;

    private InputActionAsset inputActions;
    private InputAction moveAction;
    private InputAction jumpAction;

    // Animator parameter IDs
    private static readonly int JumpParamID = Animator.StringToHash("Jump");
    private static readonly int RunParamID = Animator.StringToHash("Run");
    private static readonly int FastRunParamID = Animator.StringToHash("FastRun");
    private static readonly int IsGhostParamID = Animator.StringToHash("IsGhost");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        UI = GameManager.Instance;
        paintManager = FindObjectOfType<PaintManager>();
        obstacleSpawner = FindObjectOfType<ObstacleSpawner>();

        isGrounded = true;
        wasGrounded = false;
        isJumping = false;
        currentHealth = maxHealth;
        UpdateHealthUI();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        inputActions = GetComponent<PlayerInput>().actions;
        moveAction = inputActions.FindAction("Player/Move");
        jumpAction = inputActions.FindAction("Player/Jump");

        moveAction.Enable();
        jumpAction.Enable();
        jumpAction.performed += OnJumpPerformed;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void InitializeInput()
    {
        if (inputActions == null)
            inputActions = GetComponent<PlayerInput>().actions;

        moveAction = inputActions.FindAction("Player/Move");
        jumpAction = inputActions.FindAction("Player/Jump");

        if (moveAction != null) moveAction.Enable();
        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.performed += OnJumpPerformed;
        }

        Debug.Log("🎮 Player input system reinitialized.");
    }

    private void OnEnable()
    {
        if (inputActions == null && TryGetComponent<PlayerInput>(out var pi))
            inputActions = pi.actions;

        if (jumpAction == null)
            jumpAction = inputActions.FindAction("Player/Jump");
        if (moveAction == null)
            moveAction = inputActions.FindAction("Player/Move");

        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.performed += OnJumpPerformed;
        }
        if (moveAction != null)
            moveAction.Enable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        OnJump();
    }

    void Update()
    {
        ProcessInput();

        if (!wasGrounded && isGrounded)
        {
            OnLanded();
        }
        wasGrounded = isGrounded;

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        MovePlayer();
        CheckGrounded();
        ApplyPlatformEffects();
    }

    private void ProcessInput()
    {
        if (joystick != null && joystick.IsDragging)
        {
            horizontalInput = joystick.InputVector.x;
        }
        else
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            horizontalInput = moveInput.x;
        }
    }

    private void OnJump()
    {
        if (isGrounded && !isJumping)
        {
            StartJump(jumpForce);
        }
    }

    private void MovePlayer()
    {
        transform.Translate(Vector2.right * horizontalInput * moveSpeed * Time.fixedDeltaTime);

        if (horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
            AudioManager.Instance.PlayWalkSound();
        }
        else if (horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            AudioManager.Instance.PlayWalkSound();
        }
    }

    private void StartJump(float force)
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        AudioManager.Instance.PlayJumpSound();

        if (jumpParticles != null) jumpParticles.Play();

        isJumping = true;
        isGrounded = false;
    }

    private void OnLanded()
    {
        if (landParticles != null)
        {
            landParticles.Play();
        }

        isJumping = false;
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool(JumpParamID, isJumping);

            PaintStroke platformUnder = GetPlatformUnder();
            bool isOnYellowPaint = platformUnder != null && platformUnder.PaintType == "yellow" && isGrounded;

            if (isGrounded && !isJumping)
            {
                animator.SetBool(RunParamID, Mathf.Abs(horizontalInput) > 0 && !isOnYellowPaint);
                animator.SetBool(FastRunParamID, Mathf.Abs(horizontalInput) > 0 && isOnYellowPaint);
            }
            else
            {
                animator.SetBool(RunParamID, false);
                animator.SetBool(FastRunParamID, false);
            }

            animator.SetBool(IsGhostParamID, isGhostMode);
        }
    }

    private void CheckGrounded()
    {
        lastGroundHit = Physics2D.BoxCast(
            transform.position,
            boxCollider.size * 0.95f,
            0f,
            Vector2.down,
            0.2f,
            platformLayerMask
        );

        if (lastGroundHit.collider != null && rb.linearVelocity.y <= 0f)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    public PaintStroke GetPlatformUnder()
    {
        if (isGrounded && lastGroundHit.collider != null)
        {
            return lastGroundHit.collider.GetComponent<PaintStroke>();
        }
        return null;
    }

    public void ApplyPlatformEffects()
    {
        PaintStroke platformUnder = GetPlatformUnder();

        if (platformUnder != null && isGrounded && !isJumping)
        {
            switch (platformUnder.PaintType)
            {
                case "blue":
                    StartJump(platformUnder.BounceFactor);
                    break;

                case "yellow":
                    moveSpeed += platformUnder.SpeedBoost;
                    StartCoroutine(ResetSpeedAfterDelay(0.5f));
                    break;

                case "ghost":
                    if (!isGhostMode)
                    {
                        ExtendOrStartGhostMode(3f);
                    }
                    break;
            }
        }
    }

    private void ExtendOrStartGhostMode(float duration)
    {
        if (isGhostMode)
        {
            ghostModeEndTime = Mathf.Max(ghostModeEndTime, Time.time + duration);
        }
        else
        {
            ghostRoutine = StartCoroutine(EnableGhostMode(duration));
        }
    }

    private IEnumerator EnableGhostMode(float duration)
    {
        isGhostMode = true;
        ghostModeEndTime = Time.time + duration;
        Debug.Log("👻 Ghost mode activated!");

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0.5f;
            spriteRenderer.color = c;
        }

        Collider2D playerCollider = GetComponent<Collider2D>();
        List<Collider2D> obstacleColliders = new List<Collider2D>();
        if (obstacleSpawner != null && playerCollider != null)
        {
            List<GameObject> activeObstacles = obstacleSpawner.GetActiveObstacles();
            foreach (GameObject obstacle in activeObstacles)
            {
                if (obstacle != null)
                {
                    Collider2D obsCol = obstacle.GetComponent<Collider2D>();
                    if (obsCol != null)
                    {
                        obstacleColliders.Add(obsCol);
                        Physics2D.IgnoreCollision(playerCollider, obsCol, true);
                    }
                }
            }
        }

        while (Time.time < ghostModeEndTime)
        {
            yield return null;
        }

        if (obstacleSpawner != null && playerCollider != null)
        {
            foreach (Collider2D obsCol in obstacleColliders)
            {
                if (obsCol != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, obsCol, false);
                }
            }
        }

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        isGhostMode = false;
        ghostRoutine = null;
        Debug.Log("👻 Ghost mode ended!");
    }

    private IEnumerator ResetSpeedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        moveSpeed = 5f;
    }

    public void ResetPosition()
    {
        transform.position = new Vector3(150f, 100f, 0f);
        rb.linearVelocity = Vector2.zero;
        isGrounded = false;
        isJumping = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Paint"))
        {
            ContactPoint2D contact = collision.GetContact(0);
            if (contact.normal.y > 0.5f && rb.linearVelocity.y <= 0f)
            {
                isGrounded = true;
            }
        }

        if ((collision.gameObject.CompareTag("obstacle") || collision.gameObject.CompareTag("bird")) &&!isGhostMode )
        {
            AudioManager.Instance.PlayDieSound();
            TakeDamage();
        }

        if(isGhostMode && collision.gameObject.CompareTag("bird"))
        {
            AudioManager.Instance.PlayDieSound();
            TakeDamage();
        }

        if (collision.gameObject.CompareTag("gameOver"))
        {
            AudioManager.Instance.PlayDieSound();
            isDead = true;
            currentHealth = 0;
            UI.GameOver();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Paint"))
        {
            // Rely on CheckGrounded
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, playerSize);
    }

    private void UpdateHealthUI()
    {
        if (UI != null)
        {
            UI.UpdatePlayerHealth(currentHealth);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    private void TakeDamage()
    {
        if (isDead) return;
        AudioManager.Instance.PlayDieSound();
        currentHealth = Mathf.Max(0, currentHealth - 1);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            isDead = true;
            UI.GameOver();
        }
    }
}
