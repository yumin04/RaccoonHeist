using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCChase : MonoBehaviour
{
    [Header("References")]
    public Transform player;                    // assign or auto-find
    public SpriteRenderer spriteRenderer;       // assign the SpriteRenderer in inspector
    public Animator animator;

    [Header("Chase Settings")]
    public bool isChasing = false;
    public float chaseSpeed = 4f;
    public float stopDistance = 1.5f;

    [Header("Flip Settings")]
    [Tooltip("How long to lerp when flipping to avoid jitter (0 = instant)")]
    public float flipSmoothingTime = 0.06f;
    [Tooltip("Minimum horizontal speed to consider flipping")]
    public float velocityThreshold = 0.05f;

    private NavMeshAgent agent;
    private Vector3 lastPosition;
    private bool facingRight = true; // current logical facing (true = right)
    private float flipLerp = 0f;
    private float flipTarget = 1f; // 1 = scale.x positive / not flipped, -1 = flipped
    private Vector3 originalSpriteScale;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.updateRotation = false;
        agent.acceleration = 40f;
        agent.angularSpeed = 100f;

        // auto-find player if not set
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // auto-find a SpriteRenderer on children if none assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
                Debug.LogWarning("[NPCChase] No SpriteRenderer assigned/found. Assign it in the inspector.");
            else
                Debug.Log("[NPCChase] Auto-assigned SpriteRenderer: " + spriteRenderer.name);
        }

        if (spriteRenderer != null)
            originalSpriteScale = spriteRenderer.transform.localScale;

        lastPosition = transform.position;
    }
    
    public void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"[NPCChase] Triggered animation: {triggerName}");
        }
    }

    void Update()
    {
        if (player == null) return;

        if (isChasing)
        {
            agent.SetDestination(player.position);

            if (Vector3.Distance(transform.position, player.position) <= stopDistance)
                agent.isStopped = true;
            else
                agent.isStopped = false;
        }
        else
        {
            agent.isStopped = true;
        }

        // Keep the root orientation fixed (you wanted no spinning).
        transform.rotation = Quaternion.identity;

        // ----- Determine horizontal movement (robust) -----
        float horiz = 0f;
        bool methodUsed = false;

        // Method A: use agent.velocity (actual movement)
        if (agent != null)
        {
            Vector3 v = agent.velocity;
            if (Mathf.Abs(v.x) > Mathf.Epsilon || Mathf.Abs(v.z) > Mathf.Epsilon)
            {
                horiz = v.x;
                methodUsed = true;
            }
        }

        // Method B: fallback to agent.desiredVelocity (what the agent is trying to do)
        if (!methodUsed && agent != null)
        {
            Vector3 dv = agent.desiredVelocity;
            if (Mathf.Abs(dv.x) > Mathf.Epsilon || Mathf.Abs(dv.z) > Mathf.Epsilon)
            {
                horiz = dv.x;
                methodUsed = true;
            }
        }

        // Method C: fallback to transform delta (works for manual movement)
        if (!methodUsed)
        {
            Vector3 delta = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 1e-6f);
            horiz = delta.x;
            methodUsed = true;
        }

        // ----- Flip logic with hysteresis and smoothing -----
        if (spriteRenderer != null)
        {
            // Decide desired facing
            if (horiz > velocityThreshold)
            {
                SetFlipTarget(true); // face right (flipped)
            }
            else if (horiz < -velocityThreshold)
            {
                SetFlipTarget(false); // face left (not flipped)
            }


            // Smooth the flip if desired
            if (flipSmoothingTime > 0f)
            {
                // Lerp the X scale sign smoothly - we lerp a scalar and apply sign to X
                flipLerp = Mathf.MoveTowards(flipLerp, flipTarget, Time.deltaTime / flipSmoothingTime);
                float sign = Mathf.Sign(flipLerp);
                Vector3 s = spriteRenderer.transform.localScale;
                s.x = Mathf.Abs(originalSpriteScale.x) * sign;
                spriteRenderer.transform.localScale = s;
            }
            else
            {
                Vector3 s = spriteRenderer.transform.localScale;
                s.x = (facingRight ? Mathf.Abs(originalSpriteScale.x) : -Mathf.Abs(originalSpriteScale.x));
                spriteRenderer.transform.localScale = s;
            }
        }
        else
        {
            // debugging help if no sprite renderer
            // Debug.Log("[NPCChase] spriteRenderer is null - can't flip sprite.");
        }

        lastPosition = transform.position;
    }

    private void SetFlipTarget(bool flip)
    {
        if (flip)
        {
            if (!facingRight)
                return; // already left
            facingRight = false;
            flipTarget = -1f;
            flipLerp = 1f; // start from 1 => move toward -1
        }
        else
        {
            if (facingRight)
                return; // already right
            facingRight = true;
            flipTarget = 1f;
            flipLerp = -1f; // start from -1 => move toward 1
        }
    }
    public void StartChase()
    {
        isChasing = true;
    }

    public void StopChase()
    {
        isChasing = false;
        if (agent != null)
            agent.isStopped = true;
    }

}
