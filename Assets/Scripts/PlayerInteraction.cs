using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public Transform holdPos;
    public Transform pullPos;
    private GroundChecker groundChecker;

    [Header("Pickup Settings")]
    public float pickUpRange = 5f;
    public float throwForce = 5f;
    public bool canDrop = true;

    [Header("Pull Settings")]
    public Vector3 pickUpSphereOffset = Vector3.zero;
    public float breakForce = 2000f;
    public float damping = 50f;
    public float grabSphereOffset = 1f;

    private GameObject heldObj;
    private Rigidbody heldObjRb;

    private GameObject pulledObj;
    private Rigidbody pulledRb;
    private ConfigurableJoint joint;
    private Vector3 originalLocalPullPos;
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        groundChecker = player.GetComponent<GroundChecker>();
        originalLocalPullPos = pullPos.localPosition;
    }

    void Update()
    {
        // Press E → interact or drop
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsHoldingOrPulling())
            {
                DropOrRelease();
            }
            else
            {
                TryInteract();
            }
        }

        // Press T → throw only if holding
        if (heldObj != null && Input.GetKeyDown(KeyCode.T))
        {
            StopClipping();
            ThrowObject();
        }

        // Keep held object in position
        if (heldObj != null)
            MoveHeldObject();
    }

    // --------------------
    // MAIN INTERACTION LOGIC
    // --------------------
    void TryInteract()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(player.transform.position, pickUpRange);
        GameObject closestPickup = null;
        GameObject closestPull = null;
        float minPickupDist = Mathf.Infinity;
        float minPullDist = Mathf.Infinity;

        foreach (Collider col in nearbyObjects)
        {
            float dist = Vector3.Distance(col.transform.position, player.transform.position);

            if (col.CompareTag("canPickUp") && dist < minPickupDist)
            {
                closestPickup = col.gameObject;
                minPickupDist = dist;
            }
            else if (col.CompareTag("canPull") && dist < minPullDist)
            {
                closestPull = col.gameObject;
                minPullDist = dist;
            }
        }

        // Decide what to interact with (whichever is closer)
        if (closestPickup == null && closestPull == null)
            return;

        if (closestPickup != null && (closestPull == null || minPickupDist <= minPullDist))
        {
            playerMovement.wasOnHindLegsBeforePickup = playerMovement.onHindLegs;
            playerMovement.SetStanding(true);
            PickUpObject(closestPickup);
        }
        else if (closestPull != null)
        {
            PullObject(closestPull);
        }
    }

    // --------------------
    // PICKUP
    // --------------------
    void PickUpObject(GameObject obj)
    {
        Rigidbody rb = obj.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        heldObjRb = rb;
        heldObj = rb.gameObject;

        heldObjRb.isKinematic = true;
        heldObj.transform.parent = holdPos;

        Collider col = heldObj.GetComponent<Collider>();
        if (col != null)
            Physics.IgnoreCollision(col, player.GetComponent<Collider>(), true);

        Debug.Log("Picked up: " + heldObj.name);
    }

    void MoveHeldObject()
    {
        heldObj.transform.position = holdPos.position;
    }

    void DropObject()
    {
        if (heldObj == null) return;

        Collider col = heldObj.GetComponent<Collider>();
        if (col != null)
            Physics.IgnoreCollision(col, player.GetComponent<Collider>(), false);

        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;

        heldObj = null;
        heldObjRb = null;

        playerMovement.SetStanding(playerMovement.wasOnHindLegsBeforePickup);
    }

    void ThrowObject()
    {
        if (heldObj == null) return;

        Collider col = heldObj.GetComponent<Collider>();
        if (col != null)
            Physics.IgnoreCollision(col, player.GetComponent<Collider>(), false);

        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;

        Vector3 moveDir = playerMovement.GetMovementDirection();
        if (moveDir.magnitude < 0.1f)
            moveDir = Vector3.up;

        Vector3 throwDir = (moveDir.normalized + Vector3.up * 1.2f).normalized;
        heldObjRb.AddForce(throwDir * throwForce, ForceMode.Impulse);
        heldObjRb.AddTorque(Random.onUnitSphere * 1f, ForceMode.Impulse);

        playerMovement.SetStanding(playerMovement.wasOnHindLegsBeforePickup);

        heldObj = null;
        heldObjRb = null;
    }

    // --------------------
    // PULL
    // --------------------
    void PullObject(GameObject obj)
    {
        Rigidbody rb = obj.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        // ❌ Prevent pulling if player is NOT grounded
        if (!groundChecker.isGrounded)
        {
            Debug.Log("Can't pull: player is not grounded.");
            return;
        }

        // ✅ Prevent pulling the object you're standing on
        if (groundChecker.currentGround != null)
        {
            Rigidbody groundedRb = groundChecker.currentGround.GetComponentInParent<Rigidbody>();
            if (groundedRb != null && groundedRb == rb)
            {
                Debug.Log("Can't pull the object you're standing on!");
                return;
            }
        }

        // Proceed with pulling
        pulledObj = rb.gameObject;
        pulledRb = rb;

        CreateJoint(pulledObj.GetComponent<Collider>());
        playerMovement.isPulling = true;
        Debug.Log("Started pulling: " + pulledObj.name);
    }



    private void CreateJoint(Collider col)
    {

        // Step 1: find the closest grab point on the object
        Vector3 grabPointWorld = col.ClosestPoint(player.transform.position);

        // Step 2: instant aligning pullPos toward that grab point (smooth, optional)
        if (!IsPlayerAboveObject(col))
        {
            AlignPullPosToGrabPointInstant(col.ClosestPoint(player.transform.position));
        }

        // Step 3: create joint on the object
        joint = pulledObj.AddComponent<ConfigurableJoint>();
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        joint.connectedBody = playerRb;
        joint.autoConfigureConnectedAnchor = false;

        // Step 4: assign anchors using updated positions
        joint.anchor = pulledObj.transform.InverseTransformPoint(grabPointWorld);
        joint.connectedAnchor = player.transform.InverseTransformPoint(pullPos.position);

        // Step 5: set linear limit / motion
        SoftJointLimit softLimit = new SoftJointLimit { limit = 0.05f };
        joint.linearLimit = softLimit;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        // Step 6: allow free rotation for natural swing
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        // Step 7: damping setup to reduce jitter
        JointDrive dampedDrive = new JointDrive
        {
            positionSpring = 0f,
            positionDamper = damping,
            maximumForce = Mathf.Infinity
        };
        joint.xDrive = joint.yDrive = joint.zDrive = dampedDrive;

        // Step 8: safety and special cases
        joint.breakForce = breakForce;
        joint.breakTorque = breakForce;
        joint.enableCollision = true;

        if (pulledObj.GetComponent<HingeJoint>() != null)
        {
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit limit = new SoftJointLimit { limit = 2f };
            joint.linearLimit = limit;
        } 
    }

    bool IsPlayerAboveObject(Collider obj)
    {
        Bounds bounds = obj.bounds;
        Vector3 pos = player.transform.position;
        bool xOverlap = pos.x > bounds.min.x && pos.x < bounds.max.x;
        bool zOverlap = pos.z > bounds.min.z && pos.z < bounds.max.z;
        bool above = pos.y > bounds.max.y;
        return xOverlap && zOverlap && above;
    }

    void AlignPullPosToGrabPointInstant(Vector3 targetPos, float radius = 0.5f)
    {
        float facing = Mathf.Sign(player.transform.localScale.x);
        Vector3 circleCenter = player.transform.position + player.transform.right * grabSphereOffset * facing;
        targetPos.y = pullPos.position.y;
        Vector3 offset = targetPos - circleCenter;
        if (offset.magnitude > radius)
            targetPos = circleCenter + offset.normalized * radius;
        pullPos.position = targetPos;
    }

    IEnumerator ReturnPullPosToOriginal(float duration = 0.2f)
    {
        Vector3 start = pullPos.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            pullPos.localPosition = Vector3.Lerp(start, originalLocalPullPos, t / duration);
            yield return null;
        }
        pullPos.localPosition = originalLocalPullPos;
    }

    void ReleasePulledObject()
    {
        if (joint != null)
            Destroy(joint);

        pulledObj = null;
        pulledRb = null;
        joint = null;
        playerMovement.isPulling = false;
        StartCoroutine(ReturnPullPosToOriginal());
    }

    // --------------------
    // SHARED HELPERS
    // --------------------
    bool IsHoldingOrPulling() => heldObj != null || pulledObj != null;

    void DropOrRelease()
    {
        if (heldObj != null && canDrop)
        {
            StopClipping();
            DropObject();
        }
        else if (pulledObj != null)
        {
            ReleasePulledObject();
        }
    }

    void StopClipping()
    {
        if (heldObj == null) return;
        float clipRange = Vector3.Distance(heldObj.transform.position, transform.position);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, clipRange);
        if (hits.Length > 1)
            heldObj.transform.position = transform.position + Vector3.down * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        Vector3 sphereCenter = player.transform.position + player.transform.TransformVector(pickUpSphereOffset);
        Gizmos.DrawWireSphere(sphereCenter, pickUpRange);

        Gizmos.color = Color.cyan;
        float facing = Mathf.Sign(player.transform.localScale.x);
        Vector3 circleCenter = player.transform.position + player.transform.right * grabSphereOffset * facing;
        Gizmos.DrawWireSphere(circleCenter, 0.5f);
    }
}
