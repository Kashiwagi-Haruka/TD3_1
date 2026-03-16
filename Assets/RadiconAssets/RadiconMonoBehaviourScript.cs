using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Vector3 topPortalLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 floatingPortalLocalEulerAngles = new Vector3(-90f, 0f, 0f);
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

    [Header("Block Movie")]
    [SerializeField] private EnemyMonoBehaviourScript enemyForMovie;
    [SerializeField] private float movieFadeDuration = 0.35f;
    [SerializeField] private float movieBlackHoldDuration = 0.2f;
    [SerializeField] private float movieLiftDelay = 1f;
    [SerializeField] private float movieLiftDuration = 0.55f;
    [SerializeField] private float movieLiftHeight = 1.5f;
    [SerializeField] private float movieRotateDuration = 0.65f;
    [SerializeField] private float movieNoiseDelayAfterRotate = 0.5f;
    [SerializeField] private float movieScreenNoiseDuration = 1f;
    [SerializeField] private float movieFinalHoldDuration = 0.2f;
    [SerializeField] private float movieBlockForwardOffset = 1.45f;
    [SerializeField] private float movieEnemyBehindOffset = 2.2f;
    [SerializeField] private float movieCameraDistance = 4.2f;
    [SerializeField] private float movieCameraHeight = 2.1f;
    [SerializeField] private Vector3 movieCameraLookOffset = new Vector3(0f, 0.8f, 0f);

    [Header("Movie Noise Overlay")]
    [SerializeField] private float screenNoiseAlpha = 0.9f;

    private RousokuMonoBehaviourScript heldCandle;
    private Rigidbody rb;
    private MeshRenderer[] portalRenderers = System.Array.Empty<MeshRenderer>();
    private Material[] portalBaseMaterials = System.Array.Empty<Material>();
    private bool isPortalNoiseActive;
    private Vector2 portalNoiseOffset;
    private bool isPlayingMovie;
    private Texture2D activePortalNoiseTexture;
    private Canvas movieOverlayCanvas;
    private RawImage movieNoiseImage;
    private Texture2D activeScreenNoiseTexture;

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

        if (enemyForMovie == null) {
            enemyForMovie = FindAnyObjectByType<EnemyMonoBehaviourScript>();
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
            ? CreatePortalSurface("TopPortal", topPortalTarget, topPortalLocalPosition, topPortalLocalEulerAngles)
            : topPortalTransform.gameObject;

        GameObject floatingPortal = floatingPortalTransform == null
            ? CreatePortalSurface("FloatingPortal", floatingPortalTarget, floatingPortalLocalPosition, floatingPortalLocalEulerAngles)
            : floatingPortalTransform.gameObject;

        RemovePortalCollider(topPortal);
        RemovePortalCollider(floatingPortal);

        ReattachPortal(topPortal.transform, topPortalTarget, topPortalLocalPosition, topPortalLocalEulerAngles);
        ReattachPortal(floatingPortal.transform, floatingPortalTarget, floatingPortalLocalPosition, floatingPortalLocalEulerAngles);

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
        portalBaseMaterials = new Material[portalRenderers.Length];

        for (int i = 0; i < portalRenderers.Length; i++) {
            MeshRenderer renderer = portalRenderers[i];
            if (renderer == null) {
                continue;
                }

            portalBaseMaterials[i] = renderer.material;
            }
        }


    public void FillPortalsWithBlackAndWhiteNoise () {
        SetPortalNoiseActive(true);
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
    private GameObject CreatePortalSurface (string portalName, Transform parentTarget, Vector3 localPosition, Vector3 localEulerAngles) {
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

        ReattachPortal(portalObject.transform, parentTarget, localPosition, localEulerAngles);
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

    private void ReattachPortal (Transform portalTransform, Transform parentTarget, Vector3 localPosition, Vector3 localEulerAngles) {
        if (portalTransform == null) {
            return;
            }

        if (!IsSceneTransform(parentTarget)) {
            parentTarget = transform;
            }

        portalTransform.SetParent(parentTarget, false);
        portalTransform.localPosition = localPosition;
        portalTransform.localRotation = Quaternion.Euler(localEulerAngles);
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
        if (isPlayingMovie) {
            return;
            }

        if (!Input.GetKeyDown(interactKey)) {
            return;
            }

        if (heldCandle == null) {
            TryPickupCandle();
            return;
            }

        if (BlockBehaviourScript.TryGetTouchingBlock(transform, interactionHeightOffset, blockCheckRange, out Collider blockCollider, out Vector3 blockPoint)) {
            StartCoroutine(PlayBlockMovie(blockCollider, blockPoint));
            }
        }

    private IEnumerator PlayBlockMovie (Collider blockCollider, Vector3 blockPoint) {
        if (blockCollider == null) {
            yield break;
            }

        isPlayingMovie = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
        CameraMonoBehaviourScript cameraController = mainCamera == null ? null : mainCamera.GetComponent<CameraMonoBehaviourScript>();

        bool hadMainCamera = mainCamera != null;
        bool wasCameraControllerEnabled = cameraController != null && cameraController.enabled;

        if (cameraController != null) {
            cameraController.enabled = false;
            }

        EnsureMovieOverlay(mainCamera);

        if (enemyForMovie != null) {
            enemyForMovie.SetMovementPaused(true);
            }

        Transform blockTransform = blockCollider.transform;
        Transform enemyTransform = enemyForMovie == null ? null : enemyForMovie.transform;

        Quaternion blockFacing = Quaternion.Euler(0f, blockTransform.eulerAngles.y, 0f);
        Vector3 blockForward = blockFacing * Vector3.forward;
        Vector3 blockGroundPoint = blockTransform.position;
        blockGroundPoint.y = transform.position.y;

        Vector3 radiconStartPosition = blockGroundPoint - blockForward * movieBlockForwardOffset;
        Vector3 enemyStartPosition = radiconStartPosition - blockForward * movieEnemyBehindOffset;

        if (rb != null) {
            RigidbodyConstraints originalConstraints = rb.constraints;
            bool originalUseGravity = rb.useGravity;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;

            transform.SetPositionAndRotation(radiconStartPosition, blockFacing);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (enemyTransform != null) {
                enemyTransform.SetPositionAndRotation(enemyStartPosition, blockFacing);
                Rigidbody enemyRb = enemyTransform.GetComponent<Rigidbody>();
                if (enemyRb != null) {
                    enemyRb.linearVelocity = Vector3.zero;
                    enemyRb.angularVelocity = Vector3.zero;
                    }
                }

            if (hadMainCamera) {
                yield return StartCoroutine(FadeScreen(0f, 1f, movieFadeDuration));
                PlaceCameraInFrontOfRadicon(mainCamera.transform, blockTransform);
                yield return new WaitForSeconds(movieBlackHoldDuration);
                yield return StartCoroutine(FadeScreen(1f, 0f, movieFadeDuration));
                }

            yield return new WaitForSeconds(movieLiftDelay);
            yield return StartCoroutine(LiftRadiconToHeight(radiconStartPosition.y + movieLiftHeight, movieLiftDuration));

            if (hadMainCamera) {
                Quaternion startRotation = mainCamera.transform.rotation;
                Quaternion endRotation = Quaternion.LookRotation(( enemyTransform == null ? transform.position : enemyTransform.position ) - mainCamera.transform.position, Vector3.up);
                yield return StartCoroutine(RotateTransform(mainCamera.transform, startRotation, endRotation, movieRotateDuration));

                Quaternion radiconStartRotation = transform.rotation;
                Quaternion radiconEndRotation = enemyTransform == null
                    ? radiconStartRotation
                    : Quaternion.LookRotation(enemyTransform.position - transform.position, Vector3.up);
                yield return StartCoroutine(RotateTransform(transform, radiconStartRotation, radiconEndRotation, movieRotateDuration));
                }

            yield return new WaitForSeconds(movieNoiseDelayAfterRotate);

            SetPortalNoiseActive(true);
            SetScreenNoiseActive(true);

            RadiconChangeMonoBehaviourScript changeSwitch = FindAnyObjectByType<RadiconChangeMonoBehaviourScript>();
            if (changeSwitch != null) {
                changeSwitch.ForceReturnToPlayerControl();
                }

            if (cameraController != null) {
                cameraController.enabled = wasCameraControllerEnabled;
                }

            yield return new WaitForSeconds(movieScreenNoiseDuration);
            SetScreenNoiseActive(false);
            yield return new WaitForSeconds(movieFinalHoldDuration);

            rb.constraints = originalConstraints;
            rb.useGravity = originalUseGravity;
            }
        if (enemyForMovie != null) {
            enemyForMovie.SetMovementPaused(false);
            }
        BlockBehaviourScript.ConsumeBlock(blockCollider, blockPoint, particleLifetime, particleSpeed, particleBurstCount);

        isPlayingMovie = false;
        }

    private IEnumerator FadeScreen (float fromAlpha, float toAlpha, float duration) {
        if (movieNoiseImage == null) {
            yield break;
            }

        Color startColor = Color.black;
        startColor.a = fromAlpha;
        Color endColor = Color.black;
        endColor.a = toAlpha;
        movieNoiseImage.texture = null;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration) {
            float t = elapsed / safeDuration;
            movieNoiseImage.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
            }

        movieNoiseImage.color = endColor;
        }

    private void PlaceCameraInFrontOfRadicon (Transform cameraTransform, Transform radiconTransform) {
        if (cameraTransform == null || radiconTransform == null) {
            return;
            }

        Vector3 radiconForward = radiconTransform.forward;
        radiconForward.y = 0f;
        if (radiconForward.sqrMagnitude < 0.0001f) {
            radiconForward = Vector3.forward;
            }

        radiconForward.Normalize();
        Vector3 focusPoint = radiconTransform.position + movieCameraLookOffset;
        cameraTransform.position = focusPoint + radiconForward * movieCameraDistance + Vector3.up * movieCameraHeight;
        cameraTransform.LookAt(focusPoint);
        }

    private IEnumerator LiftRadiconToHeight (float targetY, float duration) {
        float startY = transform.position.y;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration) {
            float t = elapsed / safeDuration;
            Vector3 nextPosition = transform.position;
            nextPosition.y = Mathf.Lerp(startY, targetY, t);
            transform.position = nextPosition;
            elapsed += Time.deltaTime;
            yield return null;
            }

        Vector3 finalPosition = transform.position;
        finalPosition.y = targetY;
        transform.position = finalPosition;
        }

    private IEnumerator RotateTransform (Transform target, Quaternion startRotation, Quaternion endRotation, float duration) {
        if (target == null) {
            yield break;
            }

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration) {
            float t = elapsed / safeDuration;
            target.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
            }

        target.rotation = endRotation;
        }

    private void SetPortalNoiseActive (bool isActive) {
        isPortalNoiseActive = isActive;

        if (!isActive) {
            RestorePortalBaseTexture();
            return;
            }

        if (activePortalNoiseTexture == null) {
            activePortalNoiseTexture = BuildBlackAndWhiteNoiseTexture();
            }

        portalNoiseOffset = Vector2.zero;

        for (int i = 0; i < portalRenderers.Length; i++) {
            MeshRenderer portalRenderer = portalRenderers[i];
            if (portalRenderer == null || portalRenderer.material == null) {
                continue;
                }

            Material portalMaterial = portalRenderer.material;
            portalMaterial.mainTexture = activePortalNoiseTexture;
            portalMaterial.mainTextureOffset = Vector2.zero;
            if (portalMaterial.HasProperty("_BaseMap")) {
                portalMaterial.SetTexture("_BaseMap", activePortalNoiseTexture);
                portalMaterial.SetTextureOffset("_BaseMap", Vector2.zero);
                }
            }
        }

    private void RestorePortalBaseTexture () {
        for (int i = 0; i < portalRenderers.Length; i++) {
            MeshRenderer renderer = portalRenderers[i];
            Material baseMaterial = ( i < portalBaseMaterials.Length ) ? portalBaseMaterials[i] : null;
            if (renderer == null || baseMaterial == null) {
                continue;
                }

            renderer.material = baseMaterial;
            }
        }

    private void EnsureMovieOverlay (UnityEngine.Camera mainCamera) {
        if (movieOverlayCanvas != null && movieNoiseImage != null) {
            return;
            }

        GameObject canvasObject = new GameObject("MovieNoiseCanvas");
        movieOverlayCanvas = canvasObject.AddComponent<Canvas>();
        movieOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        movieOverlayCanvas.sortingOrder = 500;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject noiseImageObject = new GameObject("MovieNoiseImage");
        noiseImageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rectTransform = noiseImageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        movieNoiseImage = noiseImageObject.AddComponent<RawImage>();
        movieNoiseImage.color = new Color(0f, 0f, 0f, 0f);

        if (mainCamera != null) {
            canvasObject.layer = mainCamera.gameObject.layer;
            }
        }

    private void SetScreenNoiseActive (bool isActive) {
        if (movieNoiseImage == null) {
            return;
            }

        if (!isActive) {
            movieNoiseImage.texture = null;
            movieNoiseImage.color = new Color(0f, 0f, 0f, 0f);
            return;
            }

        if (activeScreenNoiseTexture == null) {
            activeScreenNoiseTexture = BuildBlackAndWhiteNoiseTexture();
            }

        movieNoiseImage.texture = activeScreenNoiseTexture;
        movieNoiseImage.color = new Color(1f, 1f, 1f, screenNoiseAlpha);
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

 

    private void FixedUpdate () {
        if (isPlayingMovie) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
            }

        EnforceYawOnlyRotation();

        float throttle = Input.GetAxisRaw("Vertical");
        float steer = Input.GetAxisRaw("Horizontal");
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
