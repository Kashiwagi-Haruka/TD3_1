using UnityEngine;

public class EnemyMonoBehaviourScript : MonoBehaviour {
    [Header("Wander")]
    // 移動速度
    [SerializeField] float moveSpeed = 2f;
    // turnSpeed
    [SerializeField] float turnSpeed = 5f;
    // wanderRadius
    [SerializeField] float wanderRadius = 6f;
    // minIdleTime
    [SerializeField] float minIdleTime = 0.6f;
    // maxIdleTime
    [SerializeField] float maxIdleTime = 2f;
    // stopDistance
    [SerializeField] float stopDistance = 0.2f;

    [Header("Sikai")]
    // sikai
    [SerializeField] Transform sikai;
    // sikaiForwardOffset
    [SerializeField] float sikaiForwardOffset = 1.0f;
    // sikaiDownOffset
    [SerializeField] float sikaiDownOffset = 0.0f;

    [Header("Dark Aura")]
    // 色
    // Color の処理
    [SerializeField] Color auraColor = new Color(0f, 0f, 0f, 0.9f);
    // auraSize
    [SerializeField] float auraSize = 0.12f;
    // auraLifetime
    [SerializeField] float auraLifetime = 0.6f;
    // auraRateOverTime
    [SerializeField] float auraRateOverTime = 45f;
    // auraRadius
    [SerializeField] float auraRadius = 0.35f;
  
    // auraRadiusScaleMultiplier
    [SerializeField] float auraRadiusScaleMultiplier = 0.75f;
    // auraHeightScaleMultiplier
    [SerializeField] float auraHeightScaleMultiplier = 1.35f;
    // auraBottomOffsetMultiplier
    [SerializeField] float auraBottomOffsetMultiplier = 0.55f;
    // auraUpwardSpeed
    [SerializeField] float auraUpwardSpeed = 0.7f;
    // auraGravity
    [SerializeField] float auraGravity = 0.9f;


    // originPosition
    Vector3 originPosition;
    // targetPosition
    Vector3 targetPosition;
    // idleTimer
    float idleTimer;
    // isMovementPaused
    bool isMovementPaused;
    // auraParticleSystem
    ParticleSystem auraParticleSystem;

    // SetMovementPaused の処理
    public void SetMovementPaused (bool isPaused) {
        isMovementPaused = isPaused;
        }

    // シーン開始時の初期化
    void Start () {
        originPosition = transform.position;
        ResolveSikaiIfNeeded();
        EnsureDarkAura();
        UpdateSikaiPosition();
        PickNextTarget();
        }

    // 毎フレームの入力処理や状態更新
    // 毎フレームの処理
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
    // 衝突した相手に応じた処理
    // 衝突した相手に応じた処理
    void OnCollisionEnter (Collision collision) {
        TryFillPortalNoise(collision.collider);
        }

    // トリガーに触れた相手に応じた処理
    // トリガー接触時の処理
    void OnTriggerEnter (Collider other) {
        TryFillPortalNoise(other);
        }

    // 試行満たすポータルノイズを試行
    // TryFillPortalNoise の処理
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

    // picknext対象を決定
    // PickNextTarget の処理
    void PickNextTarget () {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPosition = originPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

    // ondrawgizmosselected
    // OnDrawGizmosSelected の処理
    void OnDrawGizmosSelected () {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Application.isPlaying ? originPosition : transform.position, wanderRadius);
        }

    // ResolveSikaiIfNeeded の処理
    void ResolveSikaiIfNeeded () {
        if (sikai != null) {
            return;
            }

        Transform child = transform.Find("sikai");
        if (child != null) {
            sikai = child;
            }
        }


    // EnsureDarkAura の処理
    void EnsureDarkAura () {
        if (auraParticleSystem != null) {
            return;
            }

        Transform auraTransform = transform.Find("DarkAuraParticles");
        if (auraTransform != null) {
            auraParticleSystem = auraTransform.GetComponent<ParticleSystem>();
            if (auraParticleSystem != null) {
                ConfigureAuraShape();
                auraParticleSystem.Play();
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
        main.gravityModifier = auraGravity;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = auraParticleSystem.emission;
        emission.rateOverTime = auraRateOverTime;

        ConfigureAuraShape();

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

    // ConfigureAuraShape の処理
    void ConfigureAuraShape () {
        if (auraParticleSystem == null) {
            return;
            }

        ParticleSystem.MainModule main = auraParticleSystem.main;
        main.startSpeed = auraUpwardSpeed;
        main.gravityModifier = auraGravity;

        Vector3 enemyScale = transform.lossyScale;
        float horizontalExtent = Mathf.Max(enemyScale.x, enemyScale.z) * auraRadiusScaleMultiplier;
        float verticalExtent = enemyScale.y * auraHeightScaleMultiplier;

        Transform auraTransform = auraParticleSystem.transform;
        auraTransform.localPosition = Vector3.down * ( enemyScale.y * auraBottomOffsetMultiplier );

        ParticleSystem.ShapeModule shape = auraParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(
            Mathf.Max(auraRadius * 2f, horizontalExtent),
            Mathf.Max(auraSize, verticalExtent),
            Mathf.Max(auraRadius * 2f, horizontalExtent)
        );

        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = auraParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(auraUpwardSpeed);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);
        }


    // UpdateSikaiPosition の処理
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
