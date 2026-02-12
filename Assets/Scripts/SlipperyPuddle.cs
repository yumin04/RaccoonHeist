using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SlipperyPuddle : MonoBehaviour
{
    [Header("Slippery Settings")]
    public Vector3 slideDirection = new Vector3(0, 0, 1); // set this in Inspector to match hallway
    public float slideForce = 20f;  // stronger force to push up ramp
    public float maxSpeed = 12f;    // prevents overshooting
    public PhysicsMaterial slipperyMaterial; // set a low-friction material (Dynamic Friction = 0, Static Friction = 0, Combine = Minimum)

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
        Rigidbody rb = collision.rigidbody;

        if (pm != null)
            pm.canMove = false;

        // optional: assign low-friction material for smooth sliding
        if (slipperyMaterial != null)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.material = slipperyMaterial;
        }

        // Normalize slide direction in world space
        Vector3 worldDir = transform.TransformDirection(slideDirection.normalized);

        // give initial push
        rb.AddForce(worldDir * slideForce, ForceMode.VelocityChange);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        // Assume grounded if we have any upward-facing contact normal
        bool grounded = false;
        Vector3 averagedNormal = Vector3.zero;

        foreach (var contact in collision.contacts)
        {
            averagedNormal += contact.normal;
            if (contact.normal.y > 0.3f) grounded = true; // ignore walls
        }

        averagedNormal.Normalize();

        // Only apply forces and velocity clamping if grounded
        if (grounded)
        {
            // Base world direction
            Vector3 worldDir = transform.TransformDirection(slideDirection.normalized);
            worldDir = Vector3.ProjectOnPlane(worldDir, averagedNormal).normalized;
            rb.AddForce(worldDir * slideForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

            // Clamp velocity only while grounded
            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        // If not grounded, do nothing - let physics handle the trajectory
    }


    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.canMove = true;

        // reset friction
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.material = null;
    }
}