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

    float pitch;
    float yaw;
    float verticalVelocity;
    CharacterController characterController;

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
        }

    void Update () {
        HandleLook();
        HandleMove();
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
    }