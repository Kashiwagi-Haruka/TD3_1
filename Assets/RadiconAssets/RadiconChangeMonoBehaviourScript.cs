using UnityEngine;

public class RadiconChangeMonoBehaviourScript : MonoBehaviour {
    [Header("Targets")]
    // playerController
    [SerializeField] private PlsyerRadiconMonoBehaviourScript playerController;
    // radiconController
    [SerializeField] private RadiconMonoBehaviourScript radiconController;
    // playerTransform
    [SerializeField] private Transform playerTransform;
    // radiconTransform
    [SerializeField] private Transform radiconTransform;

    [Header("Switch")]
    // activateKey
    [SerializeField] private KeyCode activateKey = KeyCode.E;
    // returnKey
    [SerializeField] private KeyCode returnKey = KeyCode.R;
    // touchDistance
    [SerializeField] private float touchDistance = 1.35f;

    [Header("UI")]
    // handSprite
    [SerializeField] private GameObject handSprite;

    [Header("Camera")]
    [SerializeField] private Vector3 playerCameraOffset = new Vector3(0f, 1.8f, -3f);
    // playerCameraFollowSpeed
    [SerializeField] private float playerCameraFollowSpeed = 9f;
    [SerializeField] private Vector3 fixedCameraOffset = new Vector3(0f, 1.8f, -3f);
    [SerializeField] private Vector3 fixedCameraEulerAngles = new Vector3(0f, 90f, 0f);
    // playerRenderers
    private Renderer[] playerRenderers = System.Array.Empty<Renderer>();

    // controlRadicon
    private bool controlRadicon;
    // isPlayerTouching
    private bool isPlayerTouching;
    // cameraController
    private CameraMonoBehaviourScript cameraController;

    // 参照の取得や初期設定
    private void Awake () {
        ConfigureSwitchCollision();
        ResolveTargetsIfNeeded();
        EnsureCameraController();
        ConfigureCameraController();
        ApplyControlState();
        SetHandSpriteVisible(false);
        }

    // ConfigureSwitchCollision の処理
    private void ConfigureSwitchCollision () {
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider currentCollider in colliders) {
            currentCollider.isTrigger = true;
            }

        Rigidbody attachedRigidbody = GetComponent<Rigidbody>();
        if (attachedRigidbody == null) {
            return;
            }

        attachedRigidbody.linearVelocity = Vector3.zero;
        attachedRigidbody.angularVelocity = Vector3.zero;
        attachedRigidbody.isKinematic = true;
        attachedRigidbody.useGravity = false;
        }

    // 毎フレームの入力処理や状態更新
    // 毎フレームの処理
    private void Update () {
        ResolveTargetsIfNeeded();

        if (cameraController == null) {
            EnsureCameraController();
            ConfigureCameraController();
            ApplyControlState();
            }

        isPlayerTouching = IsPlayerTouchingRadiconChange();
        SetHandSpriteVisible(isPlayerTouching && !controlRadicon);

        if (!controlRadicon && isPlayerTouching && Input.GetKeyDown(activateKey)) {
            controlRadicon = true;
            ApplyControlState();
            }

        if (controlRadicon && Input.GetKeyDown(returnKey)) {
            controlRadicon = false;
            ApplyControlState();
            }
        }

    // ResolveTargetsIfNeeded の処理
    private void ResolveTargetsIfNeeded () {
        if (!IsSceneComponent(playerController)) {
            playerController = FindAnyObjectByType<PlsyerRadiconMonoBehaviourScript>();
            }

        if (!IsSceneComponent(radiconController)) {
            radiconController = FindAnyObjectByType<RadiconMonoBehaviourScript>();
            }

        if (!IsSceneTransform(playerTransform) && playerController != null) {
            playerTransform = playerController.transform;
            CachePlayerRenderers();
            }

        if (!IsSceneTransform(radiconTransform) && radiconController != null) {
            radiconTransform = radiconController.transform;
            }
        }

    // CachePlayerRenderers の処理
    private void CachePlayerRenderers () {
        if (playerTransform == null) {
            playerRenderers = System.Array.Empty<Renderer>();
            return;
            }

        playerRenderers = playerTransform.GetComponentsInChildren<Renderer>(true);
        }

    // IsSceneComponent の処理
    private bool IsSceneComponent (MonoBehaviour component) {
        return component != null && component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;
        }

    // Transform が有効なシーン上のオブジェクトか判定
    private bool IsSceneTransform (Transform target) {
        return target != null && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded;
        }

    // EnsureCameraController の処理
    private void EnsureCameraController () {
        UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
        if (mainCamera == null) {
            return;
            }

        cameraController = mainCamera.GetComponent<CameraMonoBehaviourScript>();
        if (cameraController == null) {
            cameraController = mainCamera.gameObject.AddComponent<CameraMonoBehaviourScript>();
            }
        }

    // ConfigureCameraController の処理
    private void ConfigureCameraController () {
        if (cameraController == null) {
            return;
            }

        cameraController.SetupTargets(playerTransform, transform);
        cameraController.ConfigureFollow(playerCameraOffset, playerCameraFollowSpeed);
        cameraController.ConfigureFixed(fixedCameraOffset, fixedCameraEulerAngles);
        }
    // 強制戻るtoプレイヤーcontrol
    // ForceReturnToPlayerControl の処理
    public void ForceReturnToPlayerControl () {
        if (!controlRadicon) {
            return;
            }

        controlRadicon = false;
        ApplyControlState();
        }
    // ApplyControlState の処理
    private void ApplyControlState () {
        if (playerController != null) {
            playerController.enabled = !controlRadicon;
            }

        SetPlayerVisualVisible(!controlRadicon);

        if (radiconController != null) {
            radiconController.enabled = controlRadicon;
            }

        if (cameraController != null) {
            cameraController.SetFollowMode(!controlRadicon);
            }
        }

    // SetPlayerVisualVisible の処理
    private void SetPlayerVisualVisible (bool isVisible) {
        if (playerRenderers.Length == 0) {
            CachePlayerRenderers();
            }

        foreach (Renderer playerRenderer in playerRenderers) {
            if (playerRenderer == null) {
                continue;
                }

            playerRenderer.enabled = isVisible;
            }
        }

    // IsPlayerTouchingRadiconChange の処理
    private bool IsPlayerTouchingRadiconChange () {
        if (playerTransform == null) {
            return false;
            }

        Vector3 horizontalDelta = playerTransform.position - transform.position;
        horizontalDelta.y = 0f;
        return horizontalDelta.sqrMagnitude <= touchDistance * touchDistance;
        }

    // SetHandSpriteVisible の処理
    private void SetHandSpriteVisible (bool isVisible) {
        if (handSprite == null) {
            return;
            }

        if (handSprite.activeSelf != isVisible) {
            handSprite.SetActive(isVisible);
            }
        }
    }