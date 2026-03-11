using UnityEngine;

public class PlsyerRadiconMonoBehaviourScript : MonoBehaviour {
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -9.81f;

    [Header("Look")]
    public Transform viewPivot;
    public float mouseSensitivity = 2f;
    public float minPitch = -70f;
    public float maxPitch = 75f;
    public bool lockCursor = true;

    float pitch;
    float verticalVelocity;
    CharacterController characterController;

    void Start () {
        characterController = GetComponent<CharacterController>();

        if (viewPivot == null && UnityEngine.Camera.main != null) {
            viewPivot = UnityEngine.Camera.main.transform;
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

        transform.Rotate(0f, mouseX, 0f);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (viewPivot != null) {
            viewPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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