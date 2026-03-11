using UnityEngine;

public class MirrorPair : MonoBehaviour {
    public Transform other;
    public Transform mirrorCenter;

    Vector3 lastPos;

    void Start () {
        lastPos = transform.position;
        }

    void Update () {
        if (transform.position != lastPos) {
            MirrorToOther();
            lastPos = transform.position;
            }
        }

    void MirrorToOther () {
        Vector3 dir = transform.position - mirrorCenter.position;
        dir.x = -dir.x;
        other.position = mirrorCenter.position + dir;
        }
    }