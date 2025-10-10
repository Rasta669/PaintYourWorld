
using System.Collections;
using System.Collections.Generic;
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

    // References
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private VirtualJoystick joystick; // Reference to the virtual joystick

    // Visual effects
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
    private float horizontalInput;
    private bool isJumping; // Now controls both animation and blue paint logic
    private bool isGhostMode = false;
    private Coroutine ghostRoutine;


    // New Input System references
    private InputActionAsset inputActions;
    private InputAction moveAction;
    private InputAction jumpAction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        UI = GameManager.Instance;
        paintManager = FindObjectOfType<PaintManager>();

        isGrounded = true;
        wasGrounded = false;
        isJumping = false;
        currentHealth = maxHealth;
        UpdateHealthUI();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Initialize Input Actions
        inputActions = GetComponent<PlayerInput>().actions; // Assumes PlayerInput component on this GameObject
        moveAction = inputActions.FindAction("Player/Move");
        jumpAction = inputActions.FindAction("Player/Jump");

        // Enable the actions
        moveAction.Enable();
        jumpAction.Enable();

        // Subscribe to the jump action
        jumpAction.performed += OnJumpPerformed;
    }

    //void OnDestroy()
    //{
    //    // Clean up to avoid memory leaks
    //    moveAction.Disable();
    //    jumpAction.Disable();
    //}

    void OnDestroy()
    {
        // Safely stop coroutines to prevent them from accessing destroyed data
        StopAllCoroutines();

        // Unsubscribe from input events safely
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Disable();
        }

        if (moveAction != null)
        {
            moveAction.Disable();
        }

        // Prevent null access on destruction
        UI = null;
        paintManager = null;
        joystick = null;
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
        // Prioritize joystick input if available, otherwise use Input System
        if (joystick != null && joystick.IsDragging)
        {
            horizontalInput = joystick.InputVector.x; // Use joystick input
        }
        else
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            horizontalInput = moveInput.x; // Use Input System for gamepad/keyboard
        }

        // The jump input is handled via the performed callback (OnJump), so no need to check here
    }

    private void OnJump()
    {
        // Check conditions and trigger jump
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
        if (rb == null) return; // prevent crash if object was destroyed

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

        isJumping = false; // End jumping state unless blue paint triggers it again
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            //animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
            //animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("Jump", isJumping); // Use bool for jump state

            PaintStroke platformUnder = GetPlatformUnder();
            bool isOnYellowPaint = platformUnder != null && platformUnder.PaintType == "yellow" && isGrounded;

            if (isGrounded && !isJumping)
            {
                //animator.SetFloat("HorizontalSpeed", Mathf.Abs(horizontalInput));
                animator.SetBool("Run", Mathf.Abs(horizontalInput) > 0 && !isOnYellowPaint);
                animator.SetBool("FastRun", Mathf.Abs(horizontalInput) > 0 && isOnYellowPaint);
            }
            else
            {
                animator.SetBool("Run", false);
                animator.SetBool("FastRun", false);
            }
        }
    }

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            boxCollider.size * 0.95f,
            0f,
            Vector2.down,
            0.2f,
            platformLayerMask
        );

        if (hit.collider != null && rb.linearVelocity.y <= 0f)
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
        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            boxCollider.size * 0.95f,
            0f,
            Vector2.down,
            0.2f,
            platformLayerMask
        );

        if (hit.collider != null)
        {
            return hit.collider.GetComponent<PaintStroke>();
        }
        return null;
    }

    //public void ApplyPlatformEffects()
    //{
    //    PaintStroke platformUnder = GetPlatformUnder();

    //    if (platformUnder != null && isGrounded && !isJumping)
    //    {
    //        switch (platformUnder.PaintType)
    //        {
    //            case "blue":
    //                StartJump(platformUnder.BounceFactor); // Auto-jump on blue paint
    //                break;

    //            case "yellow":
    //                moveSpeed += platformUnder.SpeedBoost;
    //                StartCoroutine(ResetSpeedAfterDelay(0.5f));
    //                break;
    //        }
    //    }
    //}

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
                        ghostRoutine = StartCoroutine(EnableGhostMode(3f)); // You can tweak duration
                    }
                    break;
            }
        }
    }

    private IEnumerator EnableGhostMode(float duration)
    {
        isGhostMode = true;
        Debug.Log("👻 Ghost mode activated!");

        // Make the player semi-transparent for visual feedback
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0.5f;
            spriteRenderer.color = c;
        }

        // Ignore collisions with all obstacles
        Collider2D playerCollider = GetComponent<Collider2D>();
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("obstacle");
        foreach (GameObject obstacle in obstacles)
        {
            Collider2D obsCol = obstacle.GetComponent<Collider2D>();
            if (obsCol != null)
            {
                Physics2D.IgnoreCollision(playerCollider, obsCol, true);
            }
        }

        yield return new WaitForSeconds(duration);

        // Restore collisions
        foreach (GameObject obstacle in obstacles)
        {
            if (obstacle != null)
            {
                Collider2D obsCol = obstacle.GetComponent<Collider2D>();
                if (obsCol != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, obsCol, false);
                }
            }
        }

        // Restore transparency
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        isGhostMode = false;
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

        if (collision.gameObject.CompareTag("obstacle"))
        {
            AudioManager.Instance.PlayDieSound();
            TakeDamage();
            //isDead = true;
            //UI.GameOver();
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
        //bloodParticle.Play();

        if (currentHealth <= 0)
        {
            isDead = true;
            UI.GameOver();
        }
        //else
        //{
        //    characterAnimator.PlayHurtAnimation();
        //}
    }
}