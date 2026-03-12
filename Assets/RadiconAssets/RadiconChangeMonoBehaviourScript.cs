using UnityEngine;

public class RadiconChangeMonoBehaviourScript : MonoBehaviour {
    [Header("Targets")]
    [SerializeField] private PlsyerRadiconMonoBehaviourScript playerController;
    [SerializeField] private RadiconMonoBehaviourScript radiconController;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform radiconTransform;

    [Header("Switch")]
    [SerializeField] private KeyCode switchKey = KeyCode.E;
    [SerializeField] private float interactDistance = 4f;

    [Header("UI")]
    [SerializeField] private GameObject handSprite;

    [Header("Camera")]
    [SerializeField] private Vector3 playerCameraOffset = new Vector3(0f, 1.8f, -3f);
    [SerializeField] private Vector3 radiconCameraOffset = new Vector3(0f, 1.6f, 1.5f);
    [SerializeField] private float cameraFollowSpeed = 9f;

    private bool controlRadicon;
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

        bool isInCameraCenter = IsInCameraCenter();
        SetHandSpriteVisible(isInCameraCenter);

        if (Input.GetKeyDown(switchKey) && isInCameraCenter) {
            controlRadicon = !controlRadicon;
            ApplyControlState();
            }
        }

    private void LateUpdate () {
        if (mainCamera == null || playerTransform == null) {
            return;
            }

        Vector3 targetPosition = controlRadicon
            ? playerTransform.TransformPoint(radiconCameraOffset)
            : playerTransform.TransformPoint(playerCameraOffset);

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            cameraFollowSpeed * Time.deltaTime
        );

        if (controlRadicon && radiconTransform != null) {
            mainCamera.transform.LookAt(radiconTransform.position + Vector3.up * 0.35f);
            } else {
            mainCamera.transform.LookAt(playerTransform.position + Vector3.up * 1f);
            }
        }

    private void ApplyControlState () {
        if (playerController != null) {
            playerController.enabled = !controlRadicon;
            }

        if (radiconController != null) {
            radiconController.enabled = controlRadicon;
            }
        }

    private bool IsInCameraCenter () {
        if (mainCamera == null) {
            return false;
            }

        Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(centerRay, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Collide)) {
            return false;
            }

        return hit.collider != null && hit.collider.gameObject == gameObject;
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