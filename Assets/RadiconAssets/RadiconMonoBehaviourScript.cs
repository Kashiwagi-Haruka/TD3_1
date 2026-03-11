using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RadiconMonoBehaviourScript : MonoBehaviour {
    [Header("Driving")]
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float reverseSpeedMultiplier = 0.55f;
    [SerializeField] private float turnSpeed = 120f;

    [Header("Stability")]
    [SerializeField] private float dragOnGround = 2.2f;
    [SerializeField] private float angularDragOnGround = 4f;
    [SerializeField] private float downforce = 8f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private Rigidbody rb;

    private void Awake () {
        rb = GetComponent<Rigidbody>();

        if (groundCheck == null) {
            groundCheck = transform;
            }
        }

    private void FixedUpdate () {
        float throttle = Input.GetAxisRaw("Vertical");
        float steer = Input.GetAxisRaw("Horizontal");
        bool isGrounded = IsGrounded();

        ApplyThrottle(throttle, isGrounded);
        ApplySteering(steer, throttle, isGrounded);
        ApplyStability(isGrounded);
        }

    private void ApplyThrottle (float throttle, bool isGrounded) {
        if (!isGrounded) {
            return;
            }

        float directionMultiplier = throttle >= 0f ? 1f : reverseSpeedMultiplier;
        float targetMaxSpeed = maxSpeed * directionMultiplier;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (Mathf.Abs(forwardSpeed) >= targetMaxSpeed && Mathf.Sign(forwardSpeed) == Mathf.Sign(throttle)) {
            return;
            }

        Vector3 force = transform.forward * throttle * acceleration;
        rb.AddForce(force, ForceMode.Acceleration);
        }

    private void ApplySteering (float steer, float throttle, bool isGrounded) {
        if (!isGrounded) {
            return;
            }

        float movingFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float steerFactor = Mathf.Lerp(0.55f, 1f, movingFactor);

        if (Mathf.Abs(throttle) < 0.05f && rb.linearVelocity.magnitude < 0.35f) {
            steerFactor *= 0.4f;
            }

        float turnThisFrame = steer * turnSpeed * steerFactor * Time.fixedDeltaTime;
        Quaternion delta = Quaternion.Euler(0f, turnThisFrame, 0f);
        rb.MoveRotation(rb.rotation * delta);
        }

    private void ApplyStability (bool isGrounded) {
        rb.linearDamping = isGrounded ? dragOnGround : 0.1f;
        rb.angularDamping = isGrounded ? angularDragOnGround : 0.5f;

        if (isGrounded) {
            rb.AddForce(-transform.up * downforce, ForceMode.Acceleration);
            }
        }

    private bool IsGrounded () {
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
        }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected () {
        Transform checkTarget = groundCheck == null ? transform : groundCheck;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkTarget.position, groundCheckRadius);
        }
#endif
    }
