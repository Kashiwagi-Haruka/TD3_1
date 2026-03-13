using System.Collections;
using TMPro;
using UnityEngine;

public class PlsyerRadiconMonoBehaviourScript : MonoBehaviour {
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -9.81f;

    [Header("Look")]
    public Transform viewPivot;
    public float mouseSensitivity = 2f;
    public bool limitPitch = false;
    public float minPitch = -70f;
    public float maxPitch = 75f;
    public bool lockCursor = true;

    [Header("Key & Door")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRange = 1.2f;
    [SerializeField] private TMP_Text stageClearText;
    [SerializeField] private TMP_Text keyDoorText;
    [SerializeField] private float keyDoorTextDuration = 2f;

    float pitch;
    float yaw;
    float verticalVelocity;
    CharacterController characterController;
    bool hasKey;
    Coroutine keyDoorTextCoroutine;

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

    void Update () {
        HandleLook();
        HandleMove();
        HandleKeyDoorInteraction();
        }

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

    void HandleMove () {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = ( transform.right * h + transform.forward * v ).normalized * moveSpeed;

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

    void HandleKeyDoorInteraction () {
        if (!Input.GetKeyDown(interactKey)) {
            return;
            }

        bool isTouchingKey = TryGetNearbyObject("Kagi", out GameObject keyObject);
        bool isTouchingDoor = IsTouchingDoor();

        if (isTouchingKey && !hasKey && keyObject != null) {
            hasKey = true;
            keyObject.SetActive(false);
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

    bool IsTouchingDoor () {
        return TryGetNearbyObject("Door", out _);
        }

    bool TryGetNearbyObject (string nameFragment, out GameObject foundObject) {
        Vector3 center = transform.position + Vector3.up * 0.5f;
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
        }

    void HideTexts () {
        SetStageClearVisible(false);
        SetKeyDoorVisible(false);
        }

    void ShowKeyDoorTemporarily () {
        if (keyDoorTextCoroutine != null) {
            StopCoroutine(keyDoorTextCoroutine);
            }

        keyDoorTextCoroutine = StartCoroutine(ShowKeyDoorCoroutine());
        }

    IEnumerator ShowKeyDoorCoroutine () {
        SetKeyDoorVisible(true);
        yield return new WaitForSeconds(keyDoorTextDuration);
        SetKeyDoorVisible(false);
        keyDoorTextCoroutine = null;
        }

    void SetStageClearVisible (bool isVisible) {
        if (stageClearText != null && stageClearText.gameObject.activeSelf != isVisible) {
            stageClearText.gameObject.SetActive(isVisible);
            }
        }

    void SetKeyDoorVisible (bool isVisible) {
        if (keyDoorText != null && keyDoorText.gameObject.activeSelf != isVisible) {
            keyDoorText.gameObject.SetActive(isVisible);
            }
        }
    }