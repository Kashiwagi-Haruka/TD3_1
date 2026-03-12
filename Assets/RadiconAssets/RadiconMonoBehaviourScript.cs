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

    [Header("Portal")]
    [SerializeField] private Transform topPortalTarget;
    [SerializeField] private Transform floatingPortalTarget;
    [SerializeField] private Vector3 topPortalLocalPosition = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector3 floatingPortalLocalPosition = new Vector3(0f, 1.4f, 0.25f);
    [SerializeField] private Vector3 portalLocalScale = new Vector3(0.45f, 0.25f, 1f);
    [SerializeField] private Vector3 portalForwardCameraLocalPosition = new Vector3(0f, 0.35f, 0.85f);
    [SerializeField] private float portalCameraFarClipPlane = 80f;
    [SerializeField] private int portalRenderTextureSize = 512;

    private Rigidbody rb;

    private void Awake () {
        rb = GetComponent<Rigidbody>();
        ResolvePortalTargets();
        EnsurePortalPair();

        if (groundCheck == null) {
            groundCheck = transform;
            }
        }

    private void ResolvePortalTargets () {
        if (!IsSceneTransform(topPortalTarget)) {
            topPortalTarget = transform;
            }

        if (!IsSceneTransform(floatingPortalTarget)) {
            floatingPortalTarget = transform;
            }
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

        ReattachPortal(topPortal.transform, topPortalTarget, topPortalLocalPosition);
        ReattachPortal(floatingPortal.transform, floatingPortalTarget, floatingPortalLocalPosition);

        RenderTexture portalRenderTexture = new RenderTexture(portalRenderTextureSize, portalRenderTextureSize, 16);
        portalRenderTexture.name = $"{name}_PortalRenderTexture";

        BindPortalTexture(topPortal, portalRenderTexture);
        BindPortalTexture(floatingPortal, portalRenderTexture);

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

    private GameObject CreatePortalSurface (string portalName, Transform parentTarget, Vector3 localPosition) {
        GameObject portalObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        portalObject.name = portalName;

        ReattachPortal(portalObject.transform, parentTarget, localPosition);

        Collider portalCollider = portalObject.GetComponent<Collider>();
        if (portalCollider != null) {
            Destroy(portalCollider);
            }

        return portalObject;
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
        portalRenderer.material = portalMaterial;
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
