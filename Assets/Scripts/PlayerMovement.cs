using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float airControlMultiplier = 0.6f;
    public float stopLerpFactor = 0.15f;

    [Header("References")]
    public SpriteRenderer sr;
    public Animator animator;

    private Rigidbody rb;
    private Vector3 inputDir;

    [Header("Standing State")]
    public bool onHindLegs = false;
    public bool wasOnHindLegsBeforePickup = false;

    [Header("Step Climbing Settings")]
    public float maxStepHeight = 0.4f;         // Maximum height the player can step over
    public float stepCheckDistance = 0.3f;     // Distance to check forward for steps
    
    [Header("Pulling Mode")]
    public bool isPulling = false;
    public float pullSpeed = 2f;   // speed when dragging
    private Vector3 lastMoveDir = Vector3.zero;
    public Transform pullPos;   
    
    [Header("Sprinting")]
    public float sprintMultiplier = 1.6f;
    private bool isSprinting = false;
    
    [Header("Jump Settings")]
    public float jumpForce = 6f;
    public float coyoteTime = 0.15f;  // allows jumping shortly after leaving ground
    public float jumpCooldown = 0.2f; // small delay between jumps
    private float lastGroundedTime;
    private float lastJumpTime;
    private bool jumpQueued;
    
    [Header("Trampoline Auto-Bounce")]
    public float defaultTrampolineBounce = 6f; // small automatic bounce
    public float boostedTrampolineBounce = 12f; // bounce when holding space
    public float trampolineCooldown = 0.1f;    // prevent repeated bounces
    private float lastTrampolineBounceTime;
    
    [Header("Slippery / Movement Control")]
    public bool canMove = true; // player can move normally
    
    public string[] noJumpTags = { "NoJump" };
    public string[] noPullTags = { "NoPull" };
    
    public GroundChecker groundChecker;
    [Header("Trampoline Layer")]
    public LayerMask trampolineLayer; // assign in Inspector

    

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleInput();
        HandleJumpInput();
        HandleAnimations();
        HandleFlip();
    }

    void FixedUpdate()
    {
        MovePlayer();

        if (groundChecker.isGrounded)
            rb.AddForce(Vector3.down * 5f, ForceMode.Acceleration);

        HandleStepClimbing();

        HandleTrampolineBounce(); // ✅ apply auto-bounce here
    }


    void HandleInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(x, 0f, z).normalized;

        if (inputDir.magnitude < 0.1f)
            inputDir = Vector3.zero;

        // Sprint toggle
        isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDir.magnitude > 0.1f && !isPulling;

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            bool isHolding = GetComponent<ItemPickup>().HoldingObject();
            onHindLegs = isHolding ? true : !onHindLegs;
            animator.SetBool("isStanding", onHindLegs);
        }
        
        // ✅ Jump input (auto-repeat while holding)
        jumpQueued = Input.GetButton("Jump");
        
    }


    void MovePlayer()
    {
        if (!canMove) 
        {
            inputDir = Vector3.zero;
            return;
        }

        float control = groundChecker.isGrounded ? 1f : airControlMultiplier;

        // ✅ Apply sprint multiplier if sprinting
        float currentSpeed = isPulling ? pullSpeed : (isSprinting ? speed * sprintMultiplier : speed);

        Vector3 targetVelocity = inputDir * currentSpeed * control;
    
        if (isPulling && pullPos != null)
        {
            Vector3 toPull = (pullPos.position - transform.position).normalized;
            float dot = Vector3.Dot(targetVelocity.normalized, toPull);

            if (dot >= 0f)
            {
                targetVelocity -= toPull * Vector3.Dot(targetVelocity, toPull);
            }
        }

        if (groundChecker.isGrounded)
        {
            if (Physics.Raycast(
                    transform.position + Vector3.up * 0.1f,
                    Vector3.down,
                    out RaycastHit hit,
                    groundChecker.groundRayLength,
                    groundChecker.groundMask))
            {
                targetVelocity = Vector3.ProjectOnPlane(targetVelocity, hit.normal);
            }
        }


        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, stopLerpFactor);
        rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);
    }
    
    void HandleJumpInput()
    {
        // Track coyote-time window
        if (groundChecker.isGrounded)
            lastGroundedTime = Time.time;

        bool canJump = (Time.time - lastGroundedTime <= coyoteTime)
                       && (Time.time - lastJumpTime >= jumpCooldown);

        // Skip if on NoJump surface
        // If we're standing on a trampoline, don't use the normal jump path.
        // Trampoline bounces are handled exclusively in HandleTrampolineBounce(). 
        if (groundChecker.isGrounded && groundChecker.IsOnLayer(trampolineLayer))
        {
            // Clear normal jump queue so Update/Fixed don't both apply forces
            jumpQueued = false;
            return;
        }

        // Determine jump force
        float appliedJumpForce = jumpForce;

        // Check if standing on trampoline layer
        if (groundChecker.IsOnLayer(trampolineLayer))
        {
            appliedJumpForce = Input.GetButton("Jump") ? boostedTrampolineBounce : defaultTrampolineBounce;
        }

        if (jumpQueued && canJump)
        {
            // Reset vertical velocity to avoid crazy bounces
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;

            // Apply the jump / bounce force
            rb.AddForce(Vector3.up * appliedJumpForce, ForceMode.VelocityChange);

            // Trigger animation
            if (animator != null)
                //animator.SetTrigger("jump");

            lastJumpTime = Time.time;
            jumpQueued = false;
        }

        // Reset queued jump if too late
        if (jumpQueued && (Time.time - lastGroundedTime > coyoteTime))
            jumpQueued = false;
    }
    
    void HandleTrampolineBounce()
    {
        // Check if player is grounded on trampoline
        if (!groundChecker.isGrounded) return;
        if (!groundChecker.IsOnLayer(trampolineLayer)) return;

        // Prevent multiple bounces per second
        if (Time.time - lastTrampolineBounceTime < trampolineCooldown) return;

        // Determine bounce force
        float bounceForce = Input.GetButton("Jump") ? boostedTrampolineBounce : defaultTrampolineBounce;

        // Reset vertical velocity to prevent stacking
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        // Apply upward force
        rb.AddForce(Vector3.up * bounceForce, ForceMode.VelocityChange);

        // Trigger animation
        //if (animator != null)
        //    animator.SetTrigger("jump");

        lastTrampolineBounceTime = Time.time;
    }

    
    void BounceOnTrampoline(float force)
    {
        lastJumpTime = Time.time;

        // Cancel downward velocity to ensure consistent bounce
        Vector3 velocity = rb.linearVelocity;
        if (velocity.y < 0f)
            velocity.y = 0f;

        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * force, ForceMode.VelocityChange);

        // Animator trigger (optional)
        if (animator != null)
            animator.SetTrigger("jump");
    }

    
    void Jump(float force)
    {
        lastJumpTime = Time.time;

        // Cancel downward velocity to ensure consistent jump height
        Vector3 velocity = rb.linearVelocity;

// Reset vertical velocity completely before applying jump
        velocity.y = 0f;
        rb.linearVelocity = velocity;

// Apply jump/bounce force
        rb.AddForce(Vector3.up * force, ForceMode.VelocityChange);


        // Set animator
        if (animator != null)
            animator.SetTrigger("jump");
    }

    
    
    void HandleAnimations()
    {
        animator.SetFloat("xVelocity", Mathf.Abs(inputDir.x));
        animator.SetFloat("zVelocity", Mathf.Abs(inputDir.z));
        //animator.SetBool("isSprinting", isSprinting); // ✅ for sprint animation
    }


    void HandleFlip()
    {
        if (isPulling) return; // do not flip while pulling

        if (inputDir.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = (inputDir.x < 0) ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }


    // ✅ Step Climbing Function
    void HandleStepClimbing()
    {
        if (!groundChecker.isGrounded || inputDir.magnitude == 0) return;

        Vector3 moveDir = inputDir.normalized;
        float capsuleRadius = 0.3f;
        float padding = 0.02f; // small buffer to ensure the player clears the step

        // Bottom and top of capsule at current height (foot level)
        Vector3 bottom = transform.position + Vector3.up * 0.05f;
        Vector3 top = bottom + Vector3.up * 0.05f; // thin slice at feet

        // 1) Check if horizontal movement at foot level is blocked
        bool blocked = Physics.CapsuleCast(bottom, top, capsuleRadius, moveDir, stepCheckDistance, groundChecker.groundMask);

        if (!blocked) return;

        // 2) Check if space above the obstacle is free for stepping (at maxStepHeight)
        Vector3 stepCheckBottom = bottom + Vector3.up * maxStepHeight;
        Vector3 stepCheckTop = top + Vector3.up * maxStepHeight;

        if (!Physics.CapsuleCast(stepCheckBottom, stepCheckTop, capsuleRadius, moveDir, stepCheckDistance, groundChecker.groundMask))
        {
            // Safe to step up → move Rigidbody by maxStepHeight + padding
            rb.position += Vector3.up * (maxStepHeight + padding);
            rb.position += moveDir * 0.05f; // small forward nudge
        }
    }

    public Vector3 GetMovementDirection() => inputDir;

    public void SetStanding(bool state)
    {
        onHindLegs = state;
        animator.SetBool("isStanding", state);
    }
}