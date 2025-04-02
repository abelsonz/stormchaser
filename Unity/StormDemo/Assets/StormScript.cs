using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TornadoStorm : MonoBehaviour
{
    public float baseRotationSpeed = 50f;
    public float verticalSpeed = 2f;
    public float bobHeight = 0.5f;
    public float orbitRadius = 3f;
    public float chaosFactor = 0.5f;  // Controls jitter/randomness

    private Transform[] caughtObjects;
    private float[] orbitSpeeds;
    private float[] orbitOffsets;
    private Vector3[] baseLocalPositions;

    void Start()
    {
        int count = transform.childCount;
        caughtObjects = new Transform[count];
        orbitSpeeds = new float[count];
        orbitOffsets = new float[count];
        baseLocalPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            caughtObjects[i] = transform.GetChild(i);

            // Spread objects in a ring with small variation
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radiusOffset = Random.Range(-0.3f, 0.3f);  // Smaller variation
            float finalRadius = orbitRadius + radiusOffset;

            float x = Mathf.Cos(angle) * finalRadius;
            float z = Mathf.Sin(angle) * finalRadius;
            float y = Random.Range(-0.5f, 0.5f);  // Tighter vertical start too

            Vector3 localOffset = new Vector3(x, y, z);
            caughtObjects[i].localPosition = localOffset;
            baseLocalPositions[i] = localOffset;

            orbitSpeeds[i] = baseRotationSpeed * Random.Range(0.7f, 1.3f);
            orbitOffsets[i] = Random.Range(0f, 10f);
        }

    }

    void Update()
    {
        for (int i = 0; i < caughtObjects.Length; i++)
        {
            Transform obj = caughtObjects[i];

            // Orbit with unique speed
            obj.RotateAround(transform.position, Vector3.up, orbitSpeeds[i] * Time.deltaTime);

            // Add vertical bobbing
            Vector3 localPos = obj.localPosition;
            localPos.y = baseLocalPositions[i].y + Mathf.Sin(Time.time * verticalSpeed + orbitOffsets[i]) * bobHeight;

            // Add random jitter (like wind gusts)
            localPos.x += Mathf.PerlinNoise(Time.time + i, 0f) * chaosFactor - (chaosFactor / 2f);
            localPos.z += Mathf.PerlinNoise(0f, Time.time + i) * chaosFactor - (chaosFactor / 2f);

            obj.localPosition = localPos;
        }
    }
}


