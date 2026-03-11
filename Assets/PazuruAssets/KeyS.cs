using UnityEngine;
using System.Collections;

public class KeyS : MonoBehaviour {
    public float moveSpeed = 5f;
    public float gridSize = 1f;

    bool isMoving = false;
    Vector3 targetPos;

    public bool TryPush (Vector3 dir) {
        if (isMoving) return false;

        if (Physics.Raycast(transform.position, dir, gridSize))
            return false;

        targetPos = transform.position + dir * gridSize;
        StartCoroutine(Move());

        return true;
        }

    IEnumerator Move () {
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