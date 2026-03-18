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
    [SerializeField] float sikaiDownOffset = 0.0f;

    [Header("Dark Aura")]
    [SerializeField] Color auraColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField] float auraSize = 0.12f;
    [SerializeField] float auraLifetime = 0.6f;
    [SerializeField] float auraRateOverTime = 45f;
    [SerializeField] float auraRadius = 0.35f;
    [SerializeField] float auraUpwardSpeed = 0.15f;

    Vector3 originPosition;
    Vector3 targetPosition;
    float idleTimer;
    bool isMovementPaused;
    ParticleSystem auraParticleSystem;

    public void SetMovementPaused (bool isPaused) {
        isMovementPaused = isPaused;
        }

    void Start () {
        originPosition = transform.position;
        ResolveSikaiIfNeeded();
        EnsureDarkAura();
        UpdateSikaiPosition();
        PickNextTarget();
        }

    void Update () {
        UpdateSikaiPosition();

        if (isMovementPaused) {
            return;
            }

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


    void EnsureDarkAura () {
        if (auraParticleSystem != null) {
            return;
            }

        Transform auraTransform = transform.Find("DarkAuraParticles");
        if (auraTransform != null) {
            auraParticleSystem = auraTransform.GetComponent<ParticleSystem>();
            if (auraParticleSystem != null) {
                return;
                }
            }

        GameObject auraObject = new GameObject("DarkAuraParticles");
        auraObject.transform.SetParent(transform, false);
        auraObject.transform.localPosition = Vector3.zero;

        auraParticleSystem = auraObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = auraParticleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startColor = auraColor;
        main.startSize = auraSize;
        main.startLifetime = auraLifetime;
        main.startSpeed = auraUpwardSpeed;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = auraParticleSystem.emission;
        emission.rateOverTime = auraRateOverTime;

        ParticleSystem.ShapeModule shape = auraParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = auraRadius;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = auraParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new[] {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.black, 1f),
            },
            new[] {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.2f, 0.75f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        colorOverLifetime.color = fadeGradient;

        auraParticleSystem.Play();
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
        sikai.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        }
    }
