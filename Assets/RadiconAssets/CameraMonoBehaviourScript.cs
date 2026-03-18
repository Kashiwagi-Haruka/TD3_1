using UnityEngine;

public class CameraMonoBehaviourScript : MonoBehaviour {
    [Header("Follow (Player)")]
    // 追従カメラが見る対象の Transform
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.1f, -3f);
    // 追従カメラの移動速度
    [SerializeField] private float followSpeed = 9f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    [Header("Fixed (Radicon)")]
    // 固定カメラの基準 Transform
    [SerializeField] private Transform fixedAnchor;
    [SerializeField] private Vector3 fixedOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 fixedEulerAngles = new Vector3(90f, 0f, 0f);
    // 固定カメラのZ軸補正角
    [SerializeField] private float fixedZRotationOffset = 180f;

    // 開始時に追従モードで始めるかどうか
    [SerializeField] private bool startInFollowMode = true;


    // 現在が追従モードかどうか
    private bool isFollowMode;

    // 参照の取得や初期設定
    private void Awake () {
        ResolveFollowTargetIfNeeded();
        isFollowMode = startInFollowMode;
        }

    // 通常更新のあとで見た目を反映
    private void LateUpdate () {
        if (isFollowMode) {
            UpdateFollowCamera();
            return;
            }

        UpdateFixedCamera();
        }

    // カメラが使う追従対象と固定基準を設定
    public void SetupTargets (Transform playerTarget, Transform radiconChangeAnchor) {
        if (playerTarget != null) {
            followTarget = playerTarget;
            }

        if (radiconChangeAnchor != null) {
            fixedAnchor = radiconChangeAnchor;
            }
        }

    // カメラの追従モードを切り替え
    public void SetFollowMode (bool enableFollow) {
        isFollowMode = enableFollow;
        }

    // 追従カメラの位置と速度を設定
    public void ConfigureFollow (Vector3 offset, float speed) {
        followOffset = offset;
        followSpeed = Mathf.Max(0f, speed);
        }

    // 固定カメラの位置と角度を設定
    public void ConfigureFixed (Vector3 offset, Vector3 eulerAngles) {
        fixedOffset = offset;
        fixedEulerAngles = eulerAngles;
        }

    // 必要ならプレイヤーを追従対象として再取得
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

    // Transform が有効なシーン上のオブジェクトか判定
    private bool IsSceneTransform (Transform target) {
        return target != null && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded;
        }

    // 追従カメラの位置と向きを更新
    private void UpdateFollowCamera () {
        ResolveFollowTargetIfNeeded();
        if (followTarget == null) {
            return;
            }

        Vector3 targetPosition = followTarget.TransformPoint(followOffset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        transform.LookAt(followTarget.position + lookAtOffset);
        }

    // 固定カメラの位置と向きを更新
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