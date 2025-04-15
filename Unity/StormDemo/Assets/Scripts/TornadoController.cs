using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TornadoController : MonoBehaviour
{
    [Header("Debris Orbiting Motion")]
    public float baseRotationSpeed = 0.5f;
    public float verticalSpeed = 0.5f;
    public float bobHeight = 5f;
    public float minOrbitHeight = 25f;
    public float maxHeight = 80f;
    public float minRadius = 15f;
    public float maxRadius = 30f;
    public float chaosFactor = 0.2f;

    [Header("Dynamic Pickup Objects")]
    public List<Transform> pickUpCandidates = new();
    public float pickupRange = 5f;

    [Header("Tornado Waypoints")]
    public List<Transform> waypoints = new();
    public float distanceToWaypointThreshold = 5f;
    public float moveSpeed = 15f;
    public float curveStrength = 5f;
    public float curveFrequency = 0.5f;

    [Header("Spawn/Movement Timing")]
    public float movementDelay = 48f;

    [Header("Final Braking")]
    public float finalBrakeDistance = 5f;

    private List<Transform> orbitingObjects = new();
    private List<float> orbitSpeeds = new();
    private List<float> orbitOffsets = new();
    private List<float> heightOffsets = new();

    private int currentWaypointIndex = 0;
    private float timeOffset;

    void Start()
    {
        timeOffset = Random.Range(0f, 100f);
        if (waypoints != null && waypoints.Count > 0)
            transform.position = waypoints[0].position;
    }

    void Update()
    {
        UpdateOrbitingObjects();
        CheckDynamicPickups();
        MoveAlongWaypoints();
    }

    void UpdateOrbitingObjects()
    {
        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            Transform obj = orbitingObjects[i];
            float loopTime = (Time.time + heightOffsets[i]) * verticalSpeed;
            float height = minOrbitHeight + Mathf.PingPong(loopTime, maxHeight - minOrbitHeight);

            float radius = Mathf.Lerp(minRadius, maxRadius, Mathf.InverseLerp(minOrbitHeight, maxHeight, height));
            float angle = Time.time * orbitSpeeds[i] + orbitOffsets[i];

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float bobbingY = Mathf.Sin(Time.time * verticalSpeed + orbitOffsets[i]) * bobHeight;

            float jitterX = Mathf.PerlinNoise(Time.time + i, 0f) * chaosFactor - (chaosFactor / 2f);
            float jitterZ = Mathf.PerlinNoise(0f, Time.time + i) * chaosFactor - (chaosFactor / 2f);

            Vector3 newPos = Vector3.Lerp(obj.localPosition,
                new Vector3(x + jitterX, height + bobbingY, z + jitterZ),
                Time.deltaTime);
            obj.localPosition = newPos;
        }
    }

    void MoveAlongWaypoints()
    {
        if (Time.timeSinceLevelLoad < movementDelay)
            return;
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
            return;

        Transform targetPoint = waypoints[currentWaypointIndex];
        Vector3 toTarget = targetPoint.position - transform.position;
        float distance = toTarget.magnitude;

        // If within threshold, advance waypoint—and if it's #4, slow down.
        if (distance < distanceToWaypointThreshold)
        {
            currentWaypointIndex++;

            // *** NEW: once we hit waypoint index 4, reduce speed by 3 ***
            if (currentWaypointIndex == 4)
                moveSpeed -= 3f;

            return;
        }

        Vector3 direction = toTarget.normalized;
        Vector3 forwardMotion = direction * moveSpeed * Time.deltaTime;

        Vector3 right = Vector3.Cross(Vector3.up, direction);
        float lateralOffset = Mathf.Sin(Time.time * curveFrequency + timeOffset) * curveStrength;
        Vector3 curvedMotion = right * lateralOffset * Time.deltaTime;

        transform.position += forwardMotion + curvedMotion;

        Vector3 lookDir = forwardMotion + curvedMotion;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    void CheckDynamicPickups()
    {
        pickUpCandidates.RemoveAll(obj => obj == null);
        foreach (var obj in pickUpCandidates.ToArray())
        {
            if (orbitingObjects.Contains(obj)) continue;
            if (Vector3.Distance(transform.position, obj.position) <= pickupRange)
            {
                var rbObj = obj.GetComponent<Rigidbody>();
                if (rbObj) rbObj.isKinematic = true;
                obj.SetParent(transform);
                AddToOrbit(obj);
            }
        }
    }

    void AddToOrbit(Transform obj)
    {
        orbitingObjects.Add(obj);
        orbitSpeeds.Add(baseRotationSpeed * Random.Range(0.8f, 1.2f));
        orbitOffsets.Add(Random.Range(0f, 10f));
        heightOffsets.Add(Random.Range(0f, 100f));
    }

    [ContextMenu("Auto-Find Debris")]
    void AutoFindDebris()
    {
        pickUpCandidates.Clear();
        foreach (var d in GameObject.FindGameObjectsWithTag("PickUp"))
            pickUpCandidates.Add(d.transform);
        Debug.Log($"[TornadoController] Found {pickUpCandidates.Count} debris objects.");
    }
}
