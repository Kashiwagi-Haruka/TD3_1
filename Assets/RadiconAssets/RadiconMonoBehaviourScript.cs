using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RadiconMonoBehaviourScript : MonoBehaviour {
    [Header("Driving")]
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float turnSpeed = 1f;
    [SerializeField] private bool useCameraRelativeInput = true;

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

    private Rigidbody rb;
    private Transform mainCameraTransform;

    private void Awake () {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.maxAngularVelocity = maxYawAngularSpeed;
        mainCameraTransform = UnityEngine.Camera.main == null ? null : UnityEngine.Camera.main.transform;

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
    private void FixedUpdate () {
        float throttle = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");
        bool isGrounded = IsGrounded();

        ApplyPlanarMovement(steer, throttle, isGrounded);
        ApplyStability(isGrounded);
        }

    private void ApplyPlanarMovement (float horizontal, float vertical, bool isGrounded) {
        if (!isGrounded) {
            return;
            }

        Vector3 inputDirection = GetInputDirection(horizontal, vertical);
        Vector3 planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 targetVelocity = inputDirection * maxSpeed;
        Vector3 velocityDelta = targetVelocity - planarVelocity;

        if (velocityDelta.sqrMagnitude > 0.0001f) {
            Vector3 accelerationStep = Vector3.ClampMagnitude(velocityDelta / Time.fixedDeltaTime, acceleration);
            rb.AddForce(accelerationStep, ForceMode.Acceleration);
            }

        RotateTowardsMoveDirection(inputDirection);
        }

    private Vector3 GetInputDirection (float horizontal, float vertical) {
        Vector3 rawInput = new Vector3(horizontal, 0f, vertical);
        if (rawInput.sqrMagnitude < 0.0025f) {
            return Vector3.zero;
            }

        if (useCameraRelativeInput) {
            if (mainCameraTransform == null && UnityEngine.Camera.main != null) {
                mainCameraTransform = UnityEngine.Camera.main.transform;
                }

            if (mainCameraTransform != null) {
                Vector3 cameraForward = Vector3.ProjectOnPlane(mainCameraTransform.forward, Vector3.up);
                if (cameraForward.sqrMagnitude < 0.0001f) {
                    cameraForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                    }

                if (cameraForward.sqrMagnitude < 0.0001f) {
                    cameraForward = Vector3.forward;
                    }

                cameraForward.Normalize();
                Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
                return ( cameraRight * horizontal + cameraForward * vertical ).normalized;
                }
            }

        return rawInput.normalized;
        }

    private void RotateTowardsMoveDirection (Vector3 inputDirection) {
        if (inputDirection.sqrMagnitude < 0.0001f) {
            return;
            }

        Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
        Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothedRotation);
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
