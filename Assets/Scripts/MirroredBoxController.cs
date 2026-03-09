using UnityEngine;

public class MirroredBoxController : MonoBehaviour
{
    [SerializeField] private int boxPairCount = 3;
    [SerializeField] private float xSpacing = 2.5f;
    [SerializeField] private float yHeight = 0.5f;
    [SerializeField] private float amplitude = 2.0f;
    [SerializeField] private float speed = 1.5f;

    private Transform[] leaders;
    private Transform[] mirrors;

    private void Start()
    {
        leaders = new Transform[boxPairCount];
        mirrors = new Transform[boxPairCount];

        for (int i = 0; i < boxPairCount; i++)
        {
            float x = (i - (boxPairCount - 1) * 0.5f) * xSpacing;

            GameObject leader = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leader.name = $"LeaderBox_{i + 1}";
            leader.transform.position = new Vector3(x, yHeight, 1.5f + i * 0.75f);

            GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mirror.name = $"MirrorBox_{i + 1}";
            mirror.transform.position = new Vector3(x, yHeight, -leader.transform.position.z);

            leaders[i] = leader.transform;
            mirrors[i] = mirror.transform;
        }
    }

    private void Update()
    {
        for (int i = 0; i < leaders.Length; i++)
        {
            Vector3 pos = leaders[i].position;
            pos.z = Mathf.Sin(Time.time * speed + i) * amplitude;
            leaders[i].position = pos;

            Vector3 mirrorPos = mirrors[i].position;
            mirrorPos.x = pos.x;
            mirrorPos.y = pos.y;
            mirrorPos.z = -pos.z;
            mirrors[i].position = mirrorPos;
        }
    }
}
