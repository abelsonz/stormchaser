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

    [Header("Truck Pickup")]
    public Transform truck;
    public float truckCatchDistance = 15f;
    public float truckLiftHeight = 25f;
    public float minTruckLiftHeight = 1f;
    public float truckLiftSpeed = 2f;
    public float truckSpinSpeed = 1f;
    public int pickupTruckAtWaypoint = 1;

    [Header("Truck Orbit Radius")]
    public float minTruckOrbitRadius = 10f;
    public float maxTruckOrbitRadius = 15f;

    [Header("Dynamic Pickup Objects")]
    public List<Transform> pickUpCandidates = new();
    public float pickupRange = 5f;

    [Header("Tornado Waypoints")]
    public List<Transform> waypoints = new();
    public float distanceToWaypointThreshold = 5f;
    public float moveSpeed = 15f;
    public float curveStrength = 5f;
    public float curveFrequency = 0.5f;

    private List<Transform> orbitingObjects = new();
    private List<float> orbitSpeeds = new();
    private List<float> orbitOffsets = new();
    private List<float> heightOffsets = new();

    private bool truckCaught = false;
    private Rigidbody truckRb;
    private Transform truckOriginalParent;

    private int currentWaypointIndex = 0;
    private float timeOffset;
    private float truckOrbitRadius;

    void Start()
    {
        if (truck == null)
            truck = GameObject.FindWithTag("PlayerTruck")?.transform;

        if (truck != null)
        {
            truckRb = truck.GetComponent<Rigidbody>();
            truckOriginalParent = truck.parent;

            // ✅ Force remove truck from pickup list
            pickUpCandidates.RemoveAll(obj => obj == truck || obj == null);
        }

        timeOffset = Random.Range(0f, 100f);
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

            Vector3 newPos = new Vector3(x + jitterX, height + bobbingY, z + jitterZ);
            obj.localPosition = newPos;
        }
    }

    void MoveAlongWaypoints()
    {
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
            return;

        Transform targetPoint = waypoints[currentWaypointIndex];
        Vector3 toTarget = targetPoint.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance < distanceToWaypointThreshold)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Count)
                return;

            if (currentWaypointIndex == pickupTruckAtWaypoint)
                CheckTruckCatch();

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

    void CheckTruckCatch()
    {
        if (truckCaught || truck == null) return;

        float distance = Vector3.Distance(transform.position, truck.position);
        if (distance <= truckCatchDistance)
        {
            truckCaught = true;

            if (truckRb != null)
                truckRb.isKinematic = true;

            var drive = truck.GetComponent<AutoDrive>();
            if (drive) drive.enabled = false;

            truckOrbitRadius = Random.Range(minTruckOrbitRadius, maxTruckOrbitRadius);
            StartCoroutine(SpiralTruckUp());
        }
    }

    IEnumerator SpiralTruckUp()
    {
        float duration = 4f;
        float elapsed = 0f;

        Vector3 startPos = truck.position;
        Vector3 tornadoPos = transform.position;
        float startY = truck.position.y;
        float targetY = Mathf.Min(truckLiftHeight, maxHeight);

        float angle = Mathf.Atan2(startPos.z - tornadoPos.z, startPos.x - tornadoPos.x);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            float height = Mathf.Lerp(startY, targetY, t);
            angle += Time.deltaTime * truckSpinSpeed;

            float x = Mathf.Cos(angle) * truckOrbitRadius + tornadoPos.x;
            float z = Mathf.Sin(angle) * truckOrbitRadius + tornadoPos.z;

            truck.position = new Vector3(x, height, z);

            Vector3 flatDir = (new Vector3(x, 0, z) - new Vector3(tornadoPos.x, 0, tornadoPos.z)).normalized;
            truck.rotation = Quaternion.LookRotation(flatDir, Vector3.up);

            yield return null;
        }

        StartCoroutine(HoverTruck(targetY, angle));
    }

    IEnumerator HoverTruck(float hoverY, float angle)
    {
        while (true)
        {
            angle += Time.deltaTime * truckSpinSpeed;

            float x = Mathf.Cos(angle) * truckOrbitRadius + transform.position.x;
            float z = Mathf.Sin(angle) * truckOrbitRadius + transform.position.z;

            truck.position = new Vector3(x, hoverY, z);

            Vector3 flatDir = (new Vector3(x, 0, z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            truck.rotation = Quaternion.LookRotation(flatDir, Vector3.up);

            yield return null;
        }
    }

    void CheckDynamicPickups()
    {
        // ✅ Final protection to prevent teleportation
        pickUpCandidates.RemoveAll(obj => obj == null || obj == truck);

        foreach (var obj in pickUpCandidates.ToArray())
        {
            if (orbitingObjects.Contains(obj)) continue;

            float dist = Vector3.Distance(transform.position, obj.position);
            if (dist <= pickupRange)
            {
                var rb = obj.GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = true;

                obj.SetParent(transform);
                obj.localPosition += Vector3.up * 2f;
                AddToOrbit(obj);
            }
        }
    }

    void AddToOrbit(Transform obj)
    {
        if (obj == truck) return;

        orbitingObjects.Add(obj);
        orbitSpeeds.Add(baseRotationSpeed * Random.Range(0.8f, 1.2f));
        orbitOffsets.Add(Random.Range(0f, 10f));
        heightOffsets.Add(Random.Range(0f, 100f));
    }

    [ContextMenu("Auto-Find Debris")]
    void AutoFindDebris()
    {
        pickUpCandidates.Clear();
        GameObject[] debris = GameObject.FindGameObjectsWithTag("PickUp");
        foreach (var d in debris)
        {
            if (d.transform != truck && !pickUpCandidates.Contains(d.transform))
                pickUpCandidates.Add(d.transform);
        }

        Debug.Log($"[TornadoController] Found {pickUpCandidates.Count} debris objects with tag 'PickUp'.");
    }
}
