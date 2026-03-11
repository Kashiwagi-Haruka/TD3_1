using UnityEngine;

public class Camera : MonoBehaviour {
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
    public float distance = 6f;
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public bool lockCursor = true;

    float yaw;
    float pitch = 20f;

    void Start () {
        Vector3 currentEuler = transform.eulerAngles;
        yaw = currentEuler.y;
        pitch = currentEuler.x;

        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            }
        }

    void Update () {
        if (target == null) {
            return;
            }

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 center = target.position + targetOffset;

        transform.position = center + rotation * new Vector3(0f, 0f, -distance);
        transform.LookAt(center);
        }
    }