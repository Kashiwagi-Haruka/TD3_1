using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlsyerRadiconMonoBehaviourScript : MonoBehaviour {
    [Header("Movement")]
    // 移動速度
    public float moveSpeed = 6f;
    // 重力値
    public float gravity = -9.81f;
    // 入力を無視するデッドゾーン
    [SerializeField] private float inputDeadzone = 0.1f;
    // 壁への張り付き補正を打ち消すしきい値
    [SerializeField] private float wallStickCancelThreshold = 0.6f;

    [Header("Look")]
    // 視点上下回転の基準 Transform
    public Transform viewPivot;
    // マウス感度
    public float mouseSensitivity = 2f;
    // ピッチ角を制限するかどうか
    public bool limitPitch = false;
    // ピッチ角の最小値
    public float minPitch = -70f;
    // ピッチ角の最大値
    public float maxPitch = 75f;
    // カーソルを固定するかどうか
    public bool lockCursor = true;

    [Header("Key & Door")]
    // 調べる操作に使うキー
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // インタラクト可能距離
    [SerializeField] private float interactRange = 1.2f;
    // ステージクリア表示用テキスト
    [SerializeField] private TMP_Text stageClearText;
    // 鍵が必要な扉の案内テキスト
    [SerializeField] private TMP_Text keyDoorText;
    // 閉じた扉の案内テキスト
    [SerializeField] private TMP_Text closeDoorText;
    // ゲームオーバー表示用テキスト
    [SerializeField] private TMP_Text gameOverText;
    // 鍵メッセージの表示時間
    [SerializeField] private float keyDoorTextDuration = 2f;
    // 閉じた扉メッセージの表示時間
    [SerializeField] private float closeDoorTextDuration = 2f;
    // リスタート用キー
    [SerializeField] private KeyCode restartKey = KeyCode.Space;

    // 現在のピッチ角
    float pitch;
    // 現在のヨー角
    float yaw;
    // 上下方向の速度
    float verticalVelocity;
    // プレイヤー移動に使う CharacterController
    CharacterController characterController;
    // 鍵を持っているかどうか
    bool hasKey;
    // ゲームオーバー状態かどうか
    bool isGameOver;
    // 鍵メッセージ表示用コルーチン
    Coroutine keyDoorTextCoroutine;
    // 閉じた扉メッセージ表示用コルーチン
    Coroutine closeDoorTextCoroutine;
    // 最後に当たった壁の法線
    Vector3 lastWallNormal;
    // 最後に壁へ当たったフレーム番号
    int lastWallHitFrame = -1;

    // シーン開始時の初期化
    void Start () {
        characterController = GetComponent<CharacterController>();

        if (viewPivot == null && UnityEngine.Camera.main != null) {
            viewPivot = UnityEngine.Camera.main.transform;
            }

        yaw = transform.eulerAngles.y;

        if (viewPivot == transform) {
            pitch = transform.eulerAngles.x;
            } else if (viewPivot != null) {
            pitch = viewPivot.localEulerAngles.x;
            if (pitch > 180f) {
                pitch -= 360f;
                }
            }

        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            }

        ResolveUIReferences();
        HideTexts();
        }

    // 毎フレームの入力処理や状態更新
    // 毎フレームの処理
    void Update () {
        if (isGameOver) {
            if (Input.GetKeyDown(restartKey)) {
                RestartScene();
                }

            return;
            }

        HandleLook();
        HandleMove();
        HandleKeyDoorInteraction();
        }

    // CharacterController が当たった壁情報を記録
    void OnControllerColliderHit (ControllerColliderHit hit) {
        if (hit == null) {
            return;
            }

        if (Mathf.Abs(hit.normal.y) > 0.25f) {
            return;
            }

        lastWallNormal = hit.normal.normalized;
        lastWallHitFrame = Time.frameCount;
        }
    // 衝突した相手に応じた処理
    // 衝突した相手に応じた処理
    void OnCollisionEnter (Collision collision) {
        TryTriggerGameOver(collision.collider);
        }

    // トリガーに触れた相手に応じた処理
    // トリガー接触時の処理
    void OnTriggerEnter (Collider other) {
        TryTriggerGameOver(other);
        }

    // 試行triggerゲームゲームオーバーを試行
    // TryTriggerGameOver の処理
    void TryTriggerGameOver (Collider hitCollider) {
        if (isGameOver || hitCollider == null) {
            return;
            }

        EnemyMonoBehaviourScript enemy = hitCollider.GetComponentInParent<EnemyMonoBehaviourScript>();
        if (enemy == null) {
            return;
            }

        isGameOver = true;
        StopTextCoroutines();
        HideTexts();
        SetGameOverVisible(true);
        Time.timeScale = 0f;
        }

    // リスタートシーン
    // 現在のシーンを再読み込み
    void RestartScene () {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

    // handle視点
    // 視点回転入力を処理
    void HandleLook () {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        yaw += mouseX;
        pitch -= mouseY;

        if (limitPitch) {
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

        if (viewPivot == transform) {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            return;
            }

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (viewPivot != null) {
            if (viewPivot.IsChildOf(transform)) {
                viewPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                } else {
                viewPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
                }
            }
        }

    // handle移動
    // 移動入力と重力を処理
    void HandleMove () {
        float h = ApplyInputDeadzone(Input.GetAxisRaw("Horizontal"));
        float v = ApplyInputDeadzone(Input.GetAxisRaw("Vertical"));

        Vector3 moveDirection = ( transform.right * h + transform.forward * v ).normalized;
        Vector3 move = moveDirection * moveSpeed;

        if (lastWallHitFrame == Time.frameCount && lastWallNormal != Vector3.zero && moveDirection != Vector3.zero) {
            float moveIntoWall = Vector3.Dot(moveDirection, -lastWallNormal);
            bool noStrafeInput = Mathf.Abs(h) < 0.001f;

            if (moveIntoWall >= wallStickCancelThreshold && noStrafeInput) {
                move = Vector3.zero;
                } else if (moveIntoWall > 0f) {
                move = Vector3.ProjectOnPlane(move, lastWallNormal);
                }
            }

        if (characterController != null) {
            if (characterController.isGrounded && verticalVelocity < 0f) {
                verticalVelocity = -2f;
                }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = move + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
            } else {
            transform.position += move * Time.deltaTime;
            }
        }

    // 小さな入力を無効化
    float ApplyInputDeadzone (float axisValue) {
        return Mathf.Abs(axisValue) < inputDeadzone ? 0f : axisValue;
        }

    // handle鍵扉interaction
    // 鍵と扉のインタラクト処理
    void HandleKeyDoorInteraction () {
        if (!Input.GetKeyDown(interactKey)) {
            return;
            }

        bool isTouchingKey = TryGetNearbyKey(out KagiBehaviourScript nearbyKey);
        bool isTouchingDoor = IsTouchingDoor();
        bool isTouchingCloseDoor = IsTouchingCloseDoor();

        if (isTouchingCloseDoor) {
            SetStageClearVisible(false);
            SetKeyDoorVisible(false);

            if (HasActiveBlock()) {
                ShowCloseDoorTemporarily();
                } else {
                SetCloseDoorVisible(false);
                }

            return;
            }

        if (isTouchingKey && !hasKey && nearbyKey != null) {
            hasKey = true;
            nearbyKey.gameObject.SetActive(false);
            HideTexts();
            return;
            }

        if (isTouchingDoor && hasKey) {
            SetStageClearVisible(true);
            SetKeyDoorVisible(false);
            return;
            }

        if (isTouchingDoor && !hasKey) {
            SetStageClearVisible(false);
            ShowKeyDoorTemporarily();
            return;
            }

        HideTexts();
        }

    // 通常の扉に触れているか確認
    bool IsTouchingDoor () {
        return TryGetNearbyObject("Door", out _);
        }

    // 閉じた扉に触れているか確認
    bool IsTouchingCloseDoor () {
        return TryGetNearbyObject("CloseDoor", out _);
        }

    // 試行getnearby鍵を試行
    // 近くにある鍵を取得
    bool TryGetNearbyKey (out KagiBehaviourScript foundKey) {
        Vector3 center = GetInteractionCenter();
        Collider[] nearby = Physics.OverlapSphere(center, interactRange, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider current in nearby) {
            if (current == null) {
                continue;
                }

            KagiBehaviourScript key = current.GetComponentInParent<KagiBehaviourScript>();
            if (key == null) {
                key = current.GetComponent<KagiBehaviourScript>();
                }

            if (key == null || !key.gameObject.activeInHierarchy) {
                continue;
                }

            foundKey = key;
            return true;
            }

        foundKey = null;
        return false;
        }


    // シーン内に有効なブロックが残っているか確認
    bool HasActiveBlock () {
        return false;
        }

    // 試行getnearbyobjectを試行
    // 名前に一致する近くのオブジェクトを探
    bool TryGetNearbyObject (string nameFragment, out GameObject foundObject) {
        Vector3 center = GetInteractionCenter();
        Collider[] nearby = Physics.OverlapSphere(center, interactRange, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider current in nearby) {
            if (current == null) {
                continue;
                }

            GameObject target = current.gameObject;
            if (target.name.Contains(nameFragment)) {
                foundObject = target;
                return true;
                }
            }

        foundObject = null;
        return false;
        }

    // インタラクト判定の中心位置を返
    Vector3 GetInteractionCenter () {
        if (characterController != null) {
            return characterController.bounds.center;
            }

        return transform.position;
        }

    // 未設定の UI 参照をシーンから補完
    void ResolveUIReferences () {
        if (stageClearText == null) {
            GameObject stageClearObject = GameObject.Find("StageClearText");
            if (stageClearObject != null) {
                stageClearText = stageClearObject.GetComponent<TMP_Text>();
                }
            }

        if (keyDoorText == null) {
            GameObject keyDoorObject = GameObject.Find("KeyDoorText");
            if (keyDoorObject != null) {
                keyDoorText = keyDoorObject.GetComponent<TMP_Text>();
                }
            }

        if (closeDoorText == null) {
            GameObject closeDoorObject = GameObject.Find("CloseDoorText");
            if (closeDoorObject != null) {
                closeDoorText = closeDoorObject.GetComponent<TMP_Text>();
                }
            }

        if (gameOverText == null) {
            GameObject gameOverObject = GameObject.Find("GameOverText");
            if (gameOverObject != null) {
                gameOverText = gameOverObject.GetComponent<TMP_Text>();
                }
            }
        }

    // hidetexts
    // 各種メッセージをすべて非表示に
    void HideTexts () {
        SetStageClearVisible(false);
        SetKeyDoorVisible(false);
        SetCloseDoorVisible(false);
        SetGameOverVisible(false);
        }

    // 停止テキストcoroutines
    // メッセージ表示用コルーチンを停止
    void StopTextCoroutines () {
        if (keyDoorTextCoroutine != null) {
            StopCoroutine(keyDoorTextCoroutine);
            keyDoorTextCoroutine = null;
            }

        if (closeDoorTextCoroutine != null) {
            StopCoroutine(closeDoorTextCoroutine);
            closeDoorTextCoroutine = null;
            }
        }

    // show鍵扉temporarily
    // 鍵メッセージを一定時間だけ表示
    void ShowKeyDoorTemporarily () {
        if (keyDoorTextCoroutine != null) {
            StopCoroutine(keyDoorTextCoroutine);
            }

        keyDoorTextCoroutine = StartCoroutine(ShowKeyDoorCoroutine());
        }

    // show鍵扉コルーチン
    // ShowKeyDoorCoroutine の処理
    IEnumerator ShowKeyDoorCoroutine () {
        SetKeyDoorVisible(true);
        yield return new WaitForSeconds(keyDoorTextDuration);
        SetKeyDoorVisible(false);
        keyDoorTextCoroutine = null;
        }

    // show閉鎖扉temporarily
    // 閉じた扉メッセージを一定時間だけ表示
    void ShowCloseDoorTemporarily () {
        if (closeDoorTextCoroutine != null) {
            StopCoroutine(closeDoorTextCoroutine);
            }

        closeDoorTextCoroutine = StartCoroutine(ShowCloseDoorCoroutine());
        }

    // show閉鎖扉コルーチン
    // ShowCloseDoorCoroutine の処理
    IEnumerator ShowCloseDoorCoroutine () {
        SetCloseDoorVisible(true);
        yield return new WaitForSeconds(closeDoorTextDuration);
        SetCloseDoorVisible(false);
        closeDoorTextCoroutine = null;
        }

    // ステージクリア表示の表示状態を切り替え
    void SetStageClearVisible (bool isVisible) {
        if (stageClearText != null && stageClearText.gameObject.activeSelf != isVisible) {
            stageClearText.gameObject.SetActive(isVisible);
            }
        }

    // 鍵メッセージの表示状態を切り替え
    void SetKeyDoorVisible (bool isVisible) {
        if (keyDoorText != null && keyDoorText.gameObject.activeSelf != isVisible) {
            keyDoorText.gameObject.SetActive(isVisible);
            }
        }

    // 閉じた扉メッセージの表示状態を切り替え
    void SetCloseDoorVisible (bool isVisible) {
        if (closeDoorText != null && closeDoorText.gameObject.activeSelf != isVisible) {
            closeDoorText.gameObject.SetActive(isVisible);
            }
        }

    // ゲームオーバー表示の表示状態を切り替え
    void SetGameOverVisible (bool isVisible) {
        if (gameOverText != null && gameOverText.gameObject.activeSelf != isVisible) {
            gameOverText.gameObject.SetActive(isVisible);
            }
        }
    }
