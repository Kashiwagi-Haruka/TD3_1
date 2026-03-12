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
    private UnityEngine.Camera mainCamera;

    private void Awake () {
        if (playerController == null) {
            playerController = FindAnyObjectByType<PlsyerRadiconMonoBehaviourScript>();
            }

        if (radiconController == null) {
            radiconController = FindAnyObjectByType<RadiconMonoBehaviourScript>();
            }

        if (playerTransform == null && playerController != null) {
            playerTransform = playerController.transform;
            }

        if (radiconTransform == null && radiconController != null) {
            radiconTransform = radiconController.transform;
            }

        mainCamera = UnityEngine.Camera.main;
        ApplyControlState();
        SetHandSpriteVisible(false);
        }

    private void Update () {
        if (mainCamera == null) {
            mainCamera = UnityEngine.Camera.main;
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

    private void LateUpdate () {
        if (mainCamera == null || playerTransform == null) {
            return;
            }

        if (controlRadicon) {
            mainCamera.transform.position = transform.TransformPoint(fixedCameraOffset);
            mainCamera.transform.rotation = Quaternion.Euler(fixedCameraEulerAngles);
            return;
            }

        Vector3 targetPosition = playerTransform.TransformPoint(playerCameraOffset);

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            playerCameraFollowSpeed * Time.deltaTime
        );

        mainCamera.transform.LookAt(playerTransform.position + Vector3.up * 1f);
        }

    private void ApplyControlState () {
        if (playerController != null) {
            playerController.enabled = !controlRadicon;
            }

        if (radiconController != null) {
            radiconController.enabled = controlRadicon;
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
