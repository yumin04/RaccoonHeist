using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrampolineScript2 : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float objectBounceForce = 10f;     // non-player objects
    public float bounceCooldown = 0.1f;

    private float lastBounceTime;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        GameObject obj = collision.gameObject;

        // Ignore the player completely — handled in PlayerMovement
        if (obj.CompareTag("Player")) return;

        // Prevent multiple bounces per second
        if (Time.time - lastBounceTime < bounceCooldown) return;

        // Reset vertical velocity for consistency
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        // Apply force
        rb.AddForce(Vector3.up * objectBounceForce, ForceMode.VelocityChange);

        lastBounceTime = Time.time;
    }
}