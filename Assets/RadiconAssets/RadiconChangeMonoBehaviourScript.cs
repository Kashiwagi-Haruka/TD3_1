using UnityEngine;

public class RadiconChangeMonoBehaviourScript : MonoBehaviour {
    [Header("Targets")]
    [SerializeField] private PlsyerRadiconMonoBehaviourScript playerController;
    [SerializeField] private RadiconMonoBehaviourScript radiconController;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform radiconTransform;

    [Header("Switch")]
    [SerializeField] private KeyCode activateKey = KeyCode.E;
    [SerializeField] private KeyCode returnKey = KeyCode.R;
    [SerializeField] private float touchDistance = 1.35f;

    [Header("UI")]
    [SerializeField] private GameObject handSprite;

    [Header("Camera")]
    [SerializeField] private Vector3 playerCameraOffset = new Vector3(0f, 1.8f, -3f);
    [SerializeField] private float playerCameraFollowSpeed = 9f;
    [SerializeField] private Vector3 fixedCameraOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 fixedCameraEulerAngles = new Vector3(90f, 0f, 0f);

    private bool controlRadicon;
    private bool isPlayerTouching;
    private CameraMonoBehaviourScript cameraController;

    private void Awake () {
        ConfigureSwitchCollision();
        ResolveTargetsIfNeeded();
        EnsureCameraController();
        ConfigureCameraController();
        ApplyControlState();
        SetHandSpriteVisible(false);
        }

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

    private void ResolveTargetsIfNeeded () {
        if (!IsSceneComponent(playerController)) {
            playerController = FindAnyObjectByType<PlsyerRadiconMonoBehaviourScript>();
            }

        if (!IsSceneComponent(radiconController)) {
            radiconController = FindAnyObjectByType<RadiconMonoBehaviourScript>();
            }

        if (!IsSceneTransform(playerTransform) && playerController != null) {
            playerTransform = playerController.transform;
            }

        if (!IsSceneTransform(radiconTransform) && radiconController != null) {
            radiconTransform = radiconController.transform;
            }
        }

    private bool IsSceneComponent (MonoBehaviour component) {
        return component != null && component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;
        }

    private bool IsSceneTransform (Transform target) {
        return target != null && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded;
        }

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

    private void ConfigureCameraController () {
        if (cameraController == null) {
            return;
            }

        cameraController.SetupTargets(playerTransform, transform);
        cameraController.ConfigureFollow(playerCameraOffset, playerCameraFollowSpeed);
        cameraController.ConfigureFixed(fixedCameraOffset, fixedCameraEulerAngles);
        }

    private void ApplyControlState () {
        if (playerController != null) {
            playerController.enabled = !controlRadicon;
            }

        if (radiconController != null) {
            radiconController.enabled = controlRadicon;
            }

        if (cameraController != null) {
            cameraController.SetFollowMode(!controlRadicon);
            }
        }

    private bool IsPlayerTouchingRadiconChange () {
        if (playerTransform == null) {
            return false;
            }

        Vector3 horizontalDelta = playerTransform.position - transform.position;
        horizontalDelta.y = 0f;
        return horizontalDelta.sqrMagnitude <= touchDistance * touchDistance;
        }

    private void SetHandSpriteVisible (bool isVisible) {
        if (handSprite == null) {
            return;
            }

        if (handSprite.activeSelf != isVisible) {
            handSprite.SetActive(isVisible);
            }
        }
    }
