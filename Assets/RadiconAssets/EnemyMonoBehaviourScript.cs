using UnityEngine;

public class EnemyMonoBehaviourScript : MonoBehaviour {
    [Header("Wander")]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float turnSpeed = 5f;
    [SerializeField] float wanderRadius = 6f;
    [SerializeField] float minIdleTime = 0.6f;
    [SerializeField] float maxIdleTime = 2f;
    [SerializeField] float stopDistance = 0.2f;

    [Header("Sikai")]
    [SerializeField] Transform sikai;
    [SerializeField] float sikaiForwardOffset = 1.0f;
    [SerializeField] float sikaiDownOffset = 0.5f;

    Vector3 originPosition;
    Vector3 targetPosition;
    float idleTimer;

    void Start () {
        originPosition = transform.position;
        ResolveSikaiIfNeeded();
        UpdateSikaiPosition();
        PickNextTarget();
        }

    void Update () {
        UpdateSikaiPosition();

        if (idleTimer > 0f) {
            idleTimer -= Time.deltaTime;
            return;
            }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= stopDistance * stopDistance) {
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
            PickNextTarget();
            return;
            }

        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    void OnCollisionEnter (Collision collision) {
        TryFillPortalNoise(collision.collider);
        }

    void OnTriggerEnter (Collider other) {
        TryFillPortalNoise(other);
        }

    void TryFillPortalNoise (Collider hitCollider) {
        if (hitCollider == null) {
            return;
            }

        RadiconMonoBehaviourScript radicon = hitCollider.GetComponentInParent<RadiconMonoBehaviourScript>();
        if (radicon == null) {
            return;
            }

        radicon.FillPortalsWithBlackAndWhiteNoise();
        }

    void PickNextTarget () {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPosition = originPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

    void OnDrawGizmosSelected () {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Application.isPlaying ? originPosition : transform.position, wanderRadius);
        }

    void ResolveSikaiIfNeeded () {
        if (sikai != null) {
            return;
            }

        Transform child = transform.Find("sikai");
        if (child != null) {
            sikai = child;
            }
        }

    void UpdateSikaiPosition () {
        ResolveSikaiIfNeeded();
        if (sikai == null) {
            return;
            }

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f) {
            forward = Vector3.forward;
            } else {
            forward.Normalize();
            }

        sikai.position = transform.position + forward * sikaiForwardOffset + Vector3.down * sikaiDownOffset;
        }
    }