using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class RadiconMonoBehaviourScript : MonoBehaviour {
    [Header("Driving")]
    // acceleration
    [SerializeField] private float acceleration = 20f;
    // maxSpeed
    [SerializeField] private float maxSpeed = 8f;
    // turnSpeed
    [SerializeField] private float turnSpeed = 120f;
    // lateralGrip
    [SerializeField] private float lateralGrip = 12f;

    [Header("Stability")]
    // dragOnGround
    [SerializeField] private float dragOnGround = 3.2f;
    // angularDragOnGround
    [SerializeField] private float angularDragOnGround = 8f;
    // maxYawAngularSpeed
    [SerializeField] private float maxYawAngularSpeed = 2.5f;
    // yawSpinDamping
    [SerializeField] private float yawSpinDamping = 18f;
    // keepOnGroundPlane
    [SerializeField] private bool keepOnGroundPlane = true;

    [Header("Ground Check")]
    // groundCheck
    [SerializeField] private Transform groundCheck;
    // groundCheckRadius
    [SerializeField] private float groundCheckRadius = 0.35f;
    // groundLayers
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Portal")]
    // topPortalTarget
    [SerializeField] private Transform topPortalTarget;
    // floatingPortalTarget
    [SerializeField] private Transform floatingPortalTarget;
    [SerializeField] private Vector3 topPortalLocalPosition = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector3 floatingPortalLocalPosition = new Vector3(0f, 1.4f, 0.25f);
    // topPortalLocalEulerAngles
    [SerializeField] private Vector3 topPortalLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 floatingPortalLocalEulerAngles = new Vector3(-90f, 0f, 0f);
    [SerializeField] private Vector3 portalLocalScale = new Vector3(0.45f, 0.25f, 1f);
    [SerializeField] private Vector3 portalForwardCameraLocalPosition = new Vector3(0f, 0.35f, 0.85f);
    // portalCameraFarClipPlane
    [SerializeField] private float portalCameraFarClipPlane = 80f;
    // portalRenderTextureSize
    [SerializeField] private int portalRenderTextureSize = 512;
    // 色
    // Color の処理
    [SerializeField] private Color portalVisibleColor = new Color(0.2f, 0.9f, 1f, 1f);
    // portalNoiseTextureSize
    [SerializeField] private int portalNoiseTextureSize = 128;
    // portalNoiseScrollSpeed
    [SerializeField] private float portalNoiseScrollSpeed = 1.2f;
    [Header("Interaction")]
    // 調べる操作に使うキー
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // candlePickupRange
    [SerializeField] private float candlePickupRange = 1.5f;
    // blockCheckRange
    [SerializeField] private float blockCheckRange = 1.3f;
    // interactionHeightOffset
    [SerializeField] private float interactionHeightOffset = 0.35f;
    // particleBurstCount
    [SerializeField] private int particleBurstCount = 25;
    // particleLifetime
    [SerializeField] private float particleLifetime = 0.6f;
    // particleSpeed
    [SerializeField] private float particleSpeed = 2.5f;

    [Header("Block Movie")]
    // enemyForMovie
    [SerializeField] private EnemyMonoBehaviourScript enemyForMovie;
    // movieFadeDuration
    [SerializeField] private float movieFadeDuration = 0.35f;
    // movieBlackHoldDuration
    [SerializeField] private float movieBlackHoldDuration = 0.2f;
    // movieLiftDelay
    [SerializeField] private float movieLiftDelay = 1f;
    // movieLiftDuration
    [SerializeField] private float movieLiftDuration = 0.55f;
    // movieLiftHeight
    [SerializeField] private float movieLiftHeight = 1.5f;
    // movieRotateDuration
    [SerializeField] private float movieRotateDuration = 0.65f;
    // movieNoiseDelayAfterRotate
    [SerializeField] private float movieNoiseDelayAfterRotate = 0.5f;
    // movieScreenNoiseDuration
    [SerializeField] private float movieScreenNoiseDuration = 1f;
    // movieFinalHoldDuration
    [SerializeField] private float movieFinalHoldDuration = 0.2f;
    // movieBlockForwardOffset
    [SerializeField] private float movieBlockForwardOffset = 1.45f;
    // movieEnemyBehindOffset
    [SerializeField] private float movieEnemyBehindOffset = 2.2f;
    // movieCameraDistance
    [SerializeField] private float movieCameraDistance = 4.2f;
    // movieCameraHeight
    [SerializeField] private float movieCameraHeight = 2.1f;
    [SerializeField] private Vector3 movieCameraLookOffset = new Vector3(0f, 0.8f, 0f);

    [Header("Movie Noise Overlay")]
    // screenNoiseAlpha
    [SerializeField] private float screenNoiseAlpha = 0.9f;

    // heldCandle
    private RousokuMonoBehaviourScript heldCandle;
    // rb
    private Rigidbody rb;
    // portalRenderers
    private MeshRenderer[] portalRenderers = System.Array.Empty<MeshRenderer>();
    // portalBaseMaterials
    private Material[] portalBaseMaterials = System.Array.Empty<Material>();
    // isPortalNoiseActive
    private bool isPortalNoiseActive;
    // portalNoiseOffset
    private Vector2 portalNoiseOffset;
    // isPlayingMovie
    private bool isPlayingMovie;
    // activePortalNoiseTexture
    private Texture2D activePortalNoiseTexture;
    // movieOverlayCanvas
    private Canvas movieOverlayCanvas;
    // movieNoiseImage
    private RawImage movieNoiseImage;
    // activeScreenNoiseTexture
    private Texture2D activeScreenNoiseTexture;

    // 参照の取得や初期設定
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

    // ResolvePortalTargets の処理
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

    // ResolveSceneTransformReference の処理
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

    // Transform が有効なシーン上のオブジェクトか判定
    private bool IsSceneTransform (Transform target) {
        return target != null && target.gameObject.scene.IsValid();
        }

    // EnsurePortalPair の処理
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

    // CachePortalRenderers の処理
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


    // 満たすportalswith暗転andwhiteノイズ
    // FillPortalsWithBlackAndWhiteNoise の処理
    public void FillPortalsWithBlackAndWhiteNoise () {
        SetPortalNoiseActive(true);
        }

    // UpdatePortalNoise の処理
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

    // 生成暗転andwhiteノイズテクスチャ
    // BuildBlackAndWhiteNoiseTexture の処理
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
    // CreatePortalSurface の処理
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

    // RemovePortalCollider の処理
    private void RemovePortalCollider (GameObject portalObject) {
        if (portalObject == null) {
            return;
            }

        Collider portalCollider = portalObject.GetComponent<Collider>();
        if (portalCollider != null) {
            Destroy(portalCollider);
            }
        }

    // reattachポータル
    // ReattachPortal の処理
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


    // 関連付けポータルテクスチャ
    // BindPortalTexture の処理
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
    // 毎フレームの入力処理や状態更新
    // 毎フレームの処理
    private void Update () {
        UpdatePortalNoise();
        HandleInteract();
        }

    // handle操作
    // HandleInteract の処理
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

    // playブロック演出
    // PlayBlockMovie の処理
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
        HideBlockForMovie(blockCollider);

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
    // hideブロックfor演出
    // HideBlockForMovie の処理
    private void HideBlockForMovie (Collider blockCollider) {
        if (blockCollider == null) {
            return;
            }

        Renderer[] blockRenderers = blockCollider.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer hiddenRenderer in blockRenderers) {
            if (hiddenRenderer == null) {
                continue;
                }

            hiddenRenderer.enabled = false;
            }

        blockCollider.enabled = false;
        }
    // フェード画面
    // FadeScreen の処理
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

    // placeカメラin前側ofラジコン
    // PlaceCameraInFrontOfRadicon の処理
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

    // 持ち上げラジコンto高さ
    // LiftRadiconToHeight の処理
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

    // 回転transform
    // RotateTransform の処理
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

    // SetPortalNoiseActive の処理
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

    // restoreポータルbaseテクスチャ
    // RestorePortalBaseTexture の処理
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

    // EnsureMovieOverlay の処理
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

    // SetScreenNoiseActive の処理
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



    // 試行取得ろうそくを試行
    // TryPickupCandle の処理
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



    // 物理更新
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
    // enforceヨーonlyrotation
    // EnforceYawOnlyRotation の処理
    private void EnforceYawOnlyRotation () {
        Vector3 currentEulerAngles = rb.rotation.eulerAngles;
        rb.MoveRotation(Quaternion.Euler(0f, currentEulerAngles.y, 0f));

        Vector3 angularVelocity = rb.angularVelocity;
        rb.angularVelocity = new Vector3(0f, angularVelocity.y, 0f);
        }

    // ApplyPlanarMovement の処理
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

    // ApplySteering の処理
    private void ApplySteering (float steer, bool isGrounded) {
        if (!isGrounded || Mathf.Abs(steer) < 0.0001f) {
            return;
            }

        float rotationStep = steer * turnSpeed * Time.fixedDeltaTime;
        Quaternion nextRotation = rb.rotation * Quaternion.Euler(0f, rotationStep, 0f);
        rb.MoveRotation(nextRotation);
        }

    // ApplyStability の処理
    private void ApplyStability (bool isGrounded) {
        rb.linearDamping = isGrounded ? dragOnGround : 0.4f;
        rb.angularDamping = isGrounded ? angularDragOnGround : 1f;

        if (isGrounded) {
            Vector3 angularVelocity = rb.angularVelocity;
            float stabilizedYaw = Mathf.MoveTowards(angularVelocity.y, 0f, yawSpinDamping * Time.fixedDeltaTime);
            rb.angularVelocity = new Vector3(0f, stabilizedYaw, 0f);
            }

        }

    // IsGrounded の処理
    private bool IsGrounded () {
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
        }

#if UNITY_EDITOR
    // ondrawgizmosselected
    // OnDrawGizmosSelected の処理
    private void OnDrawGizmosSelected () {
        Transform checkTarget = groundCheck == null ? transform : groundCheck;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkTarget.position, groundCheckRadius);
        }
#endif
    }