using UnityEngine;

public class CameraMonoBehaviourScript : MonoBehaviour {
    [Header("Follow (Player)")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.8f, -3f);
    [SerializeField] private float followSpeed = 9f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    [Header("Fixed (Radicon)")]
    [SerializeField] private Transform fixedAnchor;
    [SerializeField] private Vector3 fixedOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 fixedEulerAngles = new Vector3(90f, 0f, 0f);
    [SerializeField] private float fixedZRotationOffset = 180f;

    [SerializeField] private bool startInFollowMode = true;


    private bool isFollowMode;

    private void Awake () {
        ResolveFollowTargetIfNeeded();
        isFollowMode = startInFollowMode;
        }

    private void LateUpdate () {
        if (isFollowMode) {
            UpdateFollowCamera();
            return;
            }

        UpdateFixedCamera();
        }

    public void SetupTargets (Transform playerTarget, Transform radiconChangeAnchor) {
        if (playerTarget != null) {
            followTarget = playerTarget;
            }

        if (radiconChangeAnchor != null) {
            fixedAnchor = radiconChangeAnchor;
            }
        }

    public void SetFollowMode (bool enableFollow) {
        isFollowMode = enableFollow;
        }

    public void ConfigureFollow (Vector3 offset, float speed) {
        followOffset = offset;
        followSpeed = Mathf.Max(0f, speed);
        }

    public void ConfigureFixed (Vector3 offset, Vector3 eulerAngles) {
        fixedOffset = offset;
        fixedEulerAngles = eulerAngles;
        }

    private void ResolveFollowTargetIfNeeded () {
        if (IsSceneTransform(followTarget)) {
            return;
            }

        followTarget = null;

        PlsyerRadiconMonoBehaviourScript player = FindAnyObjectByType<PlsyerRadiconMonoBehaviourScript>();
        if (player != null) {
            followTarget = player.transform;
            }
        }

    private bool IsSceneTransform (Transform target) {
        return target != null && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded;
        }

    private void UpdateFollowCamera () {
        ResolveFollowTargetIfNeeded();
        if (followTarget == null) {
            return;
            }

        Vector3 targetPosition = followTarget.TransformPoint(followOffset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        transform.LookAt(followTarget.position + lookAtOffset);
        }

    private void UpdateFixedCamera () {
        if (fixedAnchor == null) {
            return;
            }

        transform.position = fixedAnchor.TransformPoint(fixedOffset);
        Quaternion baseRotation = Quaternion.Euler(fixedEulerAngles);
        Quaternion zOffsetRotation = Quaternion.Euler(0f, 0f, fixedZRotationOffset);
        transform.rotation = baseRotation * zOffsetRotation;
        }
    }