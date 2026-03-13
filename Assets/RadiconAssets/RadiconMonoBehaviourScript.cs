using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RadiconMonoBehaviourScript : MonoBehaviour {
    [Header("Driving")]
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float lateralGrip = 12f;

    [Header("Stability")]
    [SerializeField] private float dragOnGround = 3.2f;
    [SerializeField] private float angularDragOnGround = 8f;
    [SerializeField] private float maxYawAngularSpeed = 2.5f;
    [SerializeField] private float yawSpinDamping = 18f;
    [SerializeField] private bool keepOnGroundPlane = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Portal")]
    [SerializeField] private Transform topPortalTarget;
    [SerializeField] private Transform floatingPortalTarget;
    [SerializeField] private Vector3 topPortalLocalPosition = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector3 floatingPortalLocalPosition = new Vector3(0f, 1.4f, 0.25f);
    [SerializeField] private Vector3 portalLocalScale = new Vector3(0.45f, 0.25f, 1f);
    [SerializeField] private Vector3 portalForwardCameraLocalPosition = new Vector3(0f, 0.35f, 0.85f);
    [SerializeField] private float portalCameraFarClipPlane = 80f;
    [SerializeField] private int portalRenderTextureSize = 512;
    [SerializeField] private Color portalVisibleColor = new Color(0.2f, 0.9f, 1f, 1f);
    [SerializeField] private int portalNoiseTextureSize = 128;
    [SerializeField] private float portalNoiseScrollSpeed = 1.2f;
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float candlePickupRange = 1.5f;
    [SerializeField] private float blockCheckRange = 1.3f;
    [SerializeField] private float interactionHeightOffset = 0.35f;
    [SerializeField] private int particleBurstCount = 25;
    [SerializeField] private float particleLifetime = 0.6f;
    [SerializeField] private float particleSpeed = 2.5f;

    private RousokuMonoBehaviourScript heldCandle;
    private Rigidbody rb;
    private MeshRenderer[] portalRenderers = System.Array.Empty<MeshRenderer>();
    private bool isPortalNoiseActive;
    private Vector2 portalNoiseOffset;
    private void Awake () {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.maxAngularVelocity = maxYawAngularSpeed;

        if (keepOnGroundPlane) {
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            rb.useGravity = false;
            }
        ResolvePortalTargets();
        EnsurePortalPair();

        if (groundCheck == null) {
            groundCheck = transform;
            }
        }

    private void ResolvePortalTargets () {
        topPortalTarget = ResolveSceneTransformReference(topPortalTarget);
        if (!IsSceneTransform(topPortalTarget)) {
            topPortalTarget = transform;
            }

        floatingPortalTarget = ResolveSceneTransformReference(floatingPortalTarget);
        if (!IsSceneTransform(floatingPortalTarget)) {
            floatingPortalTarget = transform;
            }
        }

    private Transform ResolveSceneTransformReference (Transform target) {
        if (IsSceneTransform(target)) {
            return target;
            }

        if (target == null) {
            return null;
            }

        GameObject sceneObjectWithSameName = GameObject.Find(target.name);
        return sceneObjectWithSameName == null ? null : sceneObjectWithSameName.transform;
        }

    private bool IsSceneTransform (Transform target) {
        return target != null && target.gameObject.scene.IsValid();
        }

    private void EnsurePortalPair () {
        Transform topPortalTransform = transform.Find("TopPortal");
        Transform floatingPortalTransform = transform.Find("FloatingPortal");
        Transform portalCameraTransform = transform.Find("PortalForwardCamera");

        GameObject topPortal = topPortalTransform == null
            ? CreatePortalSurface("TopPortal", topPortalTarget, topPortalLocalPosition)
            : topPortalTransform.gameObject;

        GameObject floatingPortal = floatingPortalTransform == null
            ? CreatePortalSurface("FloatingPortal", floatingPortalTarget, floatingPortalLocalPosition)
            : floatingPortalTransform.gameObject;

        RemovePortalCollider(topPortal);
        RemovePortalCollider(floatingPortal);

        ReattachPortal(topPortal.transform, topPortalTarget, topPortalLocalPosition);
        ReattachPortal(floatingPortal.transform, floatingPortalTarget, floatingPortalLocalPosition);

        RenderTexture portalRenderTexture = new RenderTexture(portalRenderTextureSize, portalRenderTextureSize, 16);
        portalRenderTexture.name = $"{name}_PortalRenderTexture";

        BindPortalTexture(topPortal, portalRenderTexture);
        BindPortalTexture(floatingPortal, portalRenderTexture);
        CachePortalRenderers(topPortal, floatingPortal);

        GameObject portalCameraObject = portalCameraTransform == null
            ? new GameObject("PortalForwardCamera")
            : portalCameraTransform.gameObject;

        portalCameraObject.transform.SetParent(transform, false);
        portalCameraObject.transform.localPosition = portalForwardCameraLocalPosition;
        portalCameraObject.transform.localRotation = Quaternion.identity;

        UnityEngine.Camera portalCamera = portalCameraObject.GetComponent<UnityEngine.Camera>();
        if (portalCamera == null) {
            portalCamera = portalCameraObject.AddComponent<UnityEngine.Camera>();
            }

        portalCamera.targetTexture = portalRenderTexture;
        portalCamera.clearFlags = CameraClearFlags.Skybox;
        portalCamera.nearClipPlane = 0.05f;
        portalCamera.farClipPlane = portalCameraFarClipPlane;
        portalCamera.fieldOfView = 65f;
        }

    private void CachePortalRenderers (GameObject topPortal, GameObject floatingPortal) {
        MeshRenderer topPortalRenderer = topPortal == null ? null : topPortal.GetComponent<MeshRenderer>();
        MeshRenderer floatingPortalRenderer = floatingPortal == null ? null : floatingPortal.GetComponent<MeshRenderer>();
        portalRenderers = new[] { topPortalRenderer, floatingPortalRenderer };
        }


    public void FillPortalsWithBlackAndWhiteNoise () {
        Texture2D noiseTexture = BuildBlackAndWhiteNoiseTexture();
        isPortalNoiseActive = true;
        portalNoiseOffset = Vector2.zero;

        foreach (MeshRenderer portalRenderer in portalRenderers) {
            if (portalRenderer == null || portalRenderer.material == null) {
                continue;
                }

            Material portalMaterial = portalRenderer.material;
            portalMaterial.mainTexture = noiseTexture;
            portalMaterial.mainTextureOffset = Vector2.zero;
            if (portalMaterial.HasProperty("_BaseMap")) {
                portalMaterial.SetTexture("_BaseMap", noiseTexture);
                portalMaterial.SetTextureOffset("_BaseMap", Vector2.zero);
                }
            }
        }

    private void UpdatePortalNoise () {
        if (!isPortalNoiseActive || portalRenderers.Length == 0) {
            return;
            }

        portalNoiseOffset.x += portalNoiseScrollSpeed * Time.deltaTime;
        portalNoiseOffset.y += portalNoiseScrollSpeed * 0.35f * Time.deltaTime;

        foreach (MeshRenderer portalRenderer in portalRenderers) {
            if (portalRenderer == null || portalRenderer.material == null) {
                continue;
                }

            Material portalMaterial = portalRenderer.material;
            portalMaterial.mainTextureOffset = portalNoiseOffset;
            if (portalMaterial.HasProperty("_BaseMap")) {
                portalMaterial.SetTextureOffset("_BaseMap", portalNoiseOffset);
                }
            }
        }

    private Texture2D BuildBlackAndWhiteNoiseTexture () {
        int textureSize = Mathf.Max(16, portalNoiseTextureSize);
        Texture2D noiseTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
        Color[] pixels = new Color[textureSize * textureSize];

        for (int index = 0; index < pixels.Length; index++) {
            bool isWhite = Random.value > 0.5f;
            pixels[index] = isWhite ? Color.white : Color.black;
            }

        noiseTexture.SetPixels(pixels);
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        noiseTexture.filterMode = FilterMode.Point;
        noiseTexture.Apply();
        return noiseTexture;
        }
    private GameObject CreatePortalSurface (string portalName, Transform parentTarget, Vector3 localPosition) {
        GameObject portalObject = new GameObject(portalName);
        MeshFilter meshFilter = portalObject.AddComponent<MeshFilter>();
        portalObject.AddComponent<MeshRenderer>();

        Mesh quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        if (quadMesh == null) {
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempQuad);
            }

        meshFilter.sharedMesh = quadMesh;

        ReattachPortal(portalObject.transform, parentTarget, localPosition);
        return portalObject;
        }

    private void RemovePortalCollider (GameObject portalObject) {
        if (portalObject == null) {
            return;
            }

        Collider portalCollider = portalObject.GetComponent<Collider>();
        if (portalCollider != null) {
            Destroy(portalCollider);
            }
        }

    private void ReattachPortal (Transform portalTransform, Transform parentTarget, Vector3 localPosition) {
        if (portalTransform == null) {
            return;
            }

        if (!IsSceneTransform(parentTarget)) {
            parentTarget = transform;
            }

        portalTransform.SetParent(parentTarget, false);
        portalTransform.localPosition = localPosition;
        portalTransform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        portalTransform.localScale = portalLocalScale;
        }

    private void BindPortalTexture (GameObject portalObject, RenderTexture renderTexture) {
        MeshRenderer portalRenderer = portalObject.GetComponent<MeshRenderer>();
        if (portalRenderer == null) {
            return;
            }

        Shader unlitTextureShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitTextureShader == null) {
            unlitTextureShader = Shader.Find("Unlit/Texture");
            }

        Material portalMaterial = unlitTextureShader == null
            ? new Material(Shader.Find("Standard"))
            : new Material(unlitTextureShader);

        portalMaterial.mainTexture = renderTexture;
        portalMaterial.color = portalVisibleColor;

        if (portalMaterial.HasProperty("_BaseColor")) {
            portalMaterial.SetColor("_BaseColor", portalVisibleColor);
            }

        if (portalMaterial.HasProperty("_Cull")) {
            portalMaterial.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

        portalRenderer.material = portalMaterial;
        }
    private void Update () {
        UpdatePortalNoise();
        HandleInteract();
        }

    private void HandleInteract () {
        if (!Input.GetKeyDown(interactKey)) {
            return;
            }

        if (heldCandle == null) {
            TryPickupCandle();
            return;
            }

        if (IsTouchingBlock(out Vector3 blockPoint)) {
            SpawnRedParticles(blockPoint);
            }
        }

    private void TryPickupCandle () {
        Vector3 center = transform.position + Vector3.up * interactionHeightOffset;
        Collider[] nearbyColliders = Physics.OverlapSphere(center, candlePickupRange, ~0, QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        RousokuMonoBehaviourScript nearestCandle = null;

        foreach (Collider nearbyCollider in nearbyColliders) {
            RousokuMonoBehaviourScript candle = nearbyCollider.GetComponentInParent<RousokuMonoBehaviourScript>();
            if (candle == null || candle.IsHeld) {
                continue;
                }

            float distance = Vector3.Distance(center, nearbyCollider.ClosestPoint(center));
            if (distance < closestDistance) {
                closestDistance = distance;
                nearestCandle = candle;
                }
            }

        if (nearestCandle == null) {
            return;
            }

        nearestCandle.AttachTo(transform);
        heldCandle = nearestCandle;
        }

    private bool IsTouchingBlock (out Vector3 hitPoint) {
        Vector3 center = transform.position + Vector3.up * interactionHeightOffset;
        Collider[] nearbyColliders = Physics.OverlapSphere(center, blockCheckRange, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider nearbyCollider in nearbyColliders) {
            if (!nearbyCollider.gameObject.name.Contains("Block")) {
                continue;
                }

            hitPoint = nearbyCollider.ClosestPoint(center);
            return true;
            }

        hitPoint = center + transform.forward * 0.2f;
        return false;
        }

    private void SpawnRedParticles (Vector3 position) {
        GameObject particleObject = new GameObject("RedInteractionParticles");
        particleObject.transform.position = position;

        ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.startColor = Color.red;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = 0.12f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = particleBurstCount;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        particleSystem.Emit(particleBurstCount);
        Destroy(particleObject, particleLifetime + 0.4f);
        }

    private void FixedUpdate () {
        EnforceYawOnlyRotation();

        float throttle = Input.GetAxisRaw("Vertical");
        float steer = -Input.GetAxisRaw("Horizontal");
        bool isGrounded = IsGrounded();

        ApplyPlanarMovement(throttle, isGrounded);
        ApplySteering(steer, isGrounded);
        ApplyStability(isGrounded);
        }
    private void EnforceYawOnlyRotation () {
        Vector3 currentEulerAngles = rb.rotation.eulerAngles;
        rb.MoveRotation(Quaternion.Euler(0f, currentEulerAngles.y, 0f));

        Vector3 angularVelocity = rb.angularVelocity;
        rb.angularVelocity = new Vector3(0f, angularVelocity.y, 0f);
        }

    private void ApplyPlanarMovement (float throttle, bool isGrounded) {
        if (!isGrounded) {
            return;
            }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float currentForwardSpeed = Vector3.Dot(planarVelocity, forward);
        float targetForwardSpeed = throttle * maxSpeed;
        float forwardSpeedDelta = targetForwardSpeed - currentForwardSpeed;

        float maxForwardAccelerationStep = acceleration * Time.fixedDeltaTime;
        float forwardAcceleration = Mathf.Clamp(forwardSpeedDelta, -maxForwardAccelerationStep, maxForwardAccelerationStep) / Time.fixedDeltaTime;
        rb.AddForce(forward * forwardAcceleration, ForceMode.Acceleration);

        Vector3 lateralVelocity = planarVelocity - forward * currentForwardSpeed;
        if (lateralVelocity.sqrMagnitude > 0.0001f) {
            Vector3 lateralCorrection = Vector3.ClampMagnitude(-lateralVelocity / Time.fixedDeltaTime, lateralGrip);
            rb.AddForce(lateralCorrection, ForceMode.Acceleration);
            }
        }

    private void ApplySteering (float steer, bool isGrounded) {
        if (!isGrounded || Mathf.Abs(steer) < 0.0001f) {
            return;
            }

        float rotationStep = steer * turnSpeed * Time.fixedDeltaTime;
        Quaternion nextRotation = rb.rotation * Quaternion.Euler(0f, rotationStep, 0f);
        rb.MoveRotation(nextRotation);
        }

    private void ApplyStability (bool isGrounded) {
        rb.linearDamping = isGrounded ? dragOnGround : 0.4f;
        rb.angularDamping = isGrounded ? angularDragOnGround : 1f;

        if (isGrounded) {
            Vector3 angularVelocity = rb.angularVelocity;
            float stabilizedYaw = Mathf.MoveTowards(angularVelocity.y, 0f, yawSpinDamping * Time.fixedDeltaTime);
            rb.angularVelocity = new Vector3(0f, stabilizedYaw, 0f);
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
