using UnityEngine;

public class RousokuMonoBehaviourScript : MonoBehaviour {
    [SerializeField] private Vector3 holdLocalPosition = new Vector3(0.35f, -0.25f, 0.8f);
    [SerializeField] private Vector3 holdLocalEulerAngles = Vector3.zero;

    private Collider[] cachedColliders = System.Array.Empty<Collider>();
    private Rigidbody cachedRigidbody;

    public bool IsHeld { get; private set; }

    private void Awake () {
        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedRigidbody = GetComponent<Rigidbody>();
        }

    public void AttachTo (Transform holdParent) {
        if (holdParent == null) {
            return;
            }

        IsHeld = true;
        transform.SetParent(holdParent, false);
        transform.localPosition = holdLocalPosition;
        transform.localRotation = Quaternion.Euler(holdLocalEulerAngles);

        if (cachedRigidbody != null) {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
            }

        foreach (Collider currentCollider in cachedColliders) {
            if (currentCollider == null) {
                continue;
                }

            currentCollider.enabled = false;
            }
        }
    }