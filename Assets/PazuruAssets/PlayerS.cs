using UnityEngine;

public class PlayerS : MonoBehaviour
{
    public float moveSpeed = 5.0f;   // 移動速度
    public float gridSize = 1.0f;    // マスサイズ

    bool isMoving = false;
    Vector3 targetPos;

    void Start () {
        targetPos = transform.position;
    }

    void Update () {
        if (!isMoving) {
            Vector3 dir = Vector3.zero;

            if (Input.GetKeyDown(KeyCode.W))
                dir = Vector3.forward;

            if (Input.GetKeyDown(KeyCode.S))
                dir = Vector3.back;

            if (Input.GetKeyDown(KeyCode.A))
                dir = Vector3.left;

            if (Input.GetKeyDown(KeyCode.D))
                dir = Vector3.right;

            if (dir != Vector3.zero) {
                RaycastHit hit;

                if (Physics.Raycast(transform.position, dir, out hit, gridSize)) {
                    PushBlock block = hit.collider.GetComponent<PushBlock>();

                    if (block != null) {
                        if (block.TryPush(dir)) {
                            targetPos = transform.position + dir * gridSize;
                            StartCoroutine(Move());
                        }
                    }
                } else {
                   targetPos = transform.position + dir * gridSize;
                   StartCoroutine(Move());
                }
            }
        }
    }

    System.Collections.IEnumerator Move () {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f) {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
        }
    }
