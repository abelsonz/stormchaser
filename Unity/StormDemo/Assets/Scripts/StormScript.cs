using UnityEngine;

public class TornadoStorm : MonoBehaviour
{
    // Base speed at which the objects orbit around the tornado
    public float baseRotationSpeed = 50f;

    // Controls how fast the objects bob up and down vertically
    public float verticalSpeed = 2f;

    // Amplitude of the vertical bobbing motion
    public float bobHeight = 0.5f;

    // Maximum height the objects can reach in the tornado
    public float maxHeight = 5f;

    // Minimum orbit radius (near bottom of the tornado)
    public float minRadius = 0.5f;

    // Maximum orbit radius (near top of the tornado)
    public float maxRadius = 3f;

    // Degree of horizontal jitter to make motion feel chaotic
    public float chaosFactor = 0.2f;

    // Stores references to the child objects (debris)
    private Transform[] caughtObjects;

    // Stores each object's orbit speed
    private float[] orbitSpeeds;

    // Unique wave offset for bobbing motion
    private float[] orbitOffsets;

    // Time offset to stagger vertical motion per object
    private float[] heightOffsets;

    void Start()
    {
        int count = transform.childCount;

        caughtObjects = new Transform[count];
        orbitSpeeds = new float[count];
        orbitOffsets = new float[count];
        heightOffsets = new float[count];

        for (int i = 0; i < count; i++)
        {
            // Store the object
            caughtObjects[i] = transform.GetChild(i);

            // Assign a unique speed for spinning
            orbitSpeeds[i] = baseRotationSpeed * Random.Range(0.8f, 1.2f);

            // Assign a unique phase offset for bobbing
            orbitOffsets[i] = Random.Range(0f, 10f);

            // Assign a unique offset for height progression
            heightOffsets[i] = Random.Range(0f, 10f);
        }
    }

    void Update()
    {
        for (int i = 0; i < caughtObjects.Length; i++)
        {
            Transform obj = caughtObjects[i];

            // Calculate a height value using time (loops upward over time)
            float heightTime = (Time.time + heightOffsets[i]) * 0.2f;

            // Wrap around to keep height cycling smoothly
            float height = (heightTime % 1f) * maxHeight;

            // Based on height, linearly interpolate the radius
            float radius = Mathf.Lerp(minRadius, maxRadius, height / maxHeight);

            // Calculate the angle for circular motion (spins faster as it rises)
            float angle = Time.time * orbitSpeeds[i] + i;

            // Convert polar coordinates (angle, radius) into 3D XZ position
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            // Add a bobbing Y offset on top of the base height
            float bobbingY = Mathf.Sin(Time.time * verticalSpeed + orbitOffsets[i]) * bobHeight;

            // Add horizontal jitter using Perlin noise
            float jitterX = Mathf.PerlinNoise(Time.time + i, 0f) * chaosFactor - (chaosFactor / 2f);
            float jitterZ = Mathf.PerlinNoise(0f, Time.time + i) * chaosFactor - (chaosFactor / 2f);

            // Combine all parts into a new position relative to the tornado center
            Vector3 newPos = new Vector3(x + jitterX, height + bobbingY, z + jitterZ);

            // Update the object's local position
            obj.localPosition = newPos;
        }
    }
}



