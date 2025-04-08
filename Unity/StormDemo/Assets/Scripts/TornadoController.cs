using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TornadoController : MonoBehaviour
{
    [Header("Debris Orbiting Motion")]
    public float baseRotationSpeed = 50f;
    public float verticalSpeed = 2f;
    public float bobHeight = 0.5f;
    public float maxHeight = 5f;
    public float minRadius = 0.5f;
    public float maxRadius = 3f;
    public float chaosFactor = 0.2f;

    [Header("Truck Pickup")]
    public Transform truck;
    public float truckCatchDistance = 10f;
    public float truckPullRadius = 2f;
    public float truckLiftHeight = 5f;
    public float truckLiftSpeed = 2f;
    public float truckSpinSpeed = 100f;
    public int pickupTruckAtWaypoint = 7;

    [Header("Dynamic Pickup Objects")]
    public List<Transform> pickUpCandidates = new();
    public float pickupRange = 5f;

    [Header("Tornado Waypoints")]
    public List<Transform> waypoints = new();
    public float distanceToWaypointThreshold = 3f;
    public float moveSpeed = 5f;
    public float curveStrength = 5f;
    public float curveFrequency = 0.5f;
    public float startDelay = 3f;

    private List<Transform> orbitingObjects = new();
    private List<float> orbitSpeeds = new();
    private List<float> orbitOffsets = new();
    private List<float> heightOffsets = new();

    private bool truckCaught = false;
    private Rigidbody truckRb;
    private Transform truckOriginalParent;

    private int currentWaypointIndex = 0;
    private float timer = 0f;
    private float timeOffset;
    private bool isMoving = false;

    void Start()
    {
        // Cache tornado children (if any already orbiting)
        for (int i = 0; i < transform.childCount; i++)
        {
            AddToOrbit(transform.GetChild(i));
        }

        // Cache truck
        if (truck == null)
            truck = GameObject.FindWithTag("PlayerTruck")?.transform;

        if (truck != null)
        {
            truckRb = truck.GetComponent<Rigidbody>();
            truckOriginalParent = truck.parent;
        }

        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        UpdateOrbitingObjects();
        CheckDynamicPickups();

        if (!isMoving)
        {
            timer += Time.deltaTime;
            if (timer >= startDelay)
                isMoving = true;
            else
                return;
        }

        MoveAlongWaypoints();
    }

    void UpdateOrbitingObjects()
    {
        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            Transform obj = orbitingObjects[i];

            float heightTime = (Time.time + heightOffsets[i]) * 0.2f;
            float height = (heightTime % 1f) * maxHeight;
            float radius = Mathf.Lerp(minRadius, maxRadius, height / maxHeight);

            float angle = Time.time * orbitSpeeds[i] + i;
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
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count) return;

        Transform targetPoint = waypoints[currentWaypointIndex];
        Vector3 toTarget = targetPoint.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance < distanceToWaypointThreshold)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                // End of path
                isMoving = false;
                return;
            }

            // Trigger truck pickup if this is the right waypoint
            if (currentWaypointIndex == pickupTruckAtWaypoint)
            {
                CheckTruckCatch();
            }

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

            truck.SetParent(transform);
            StartCoroutine(SpiralTruckUp());
        }
    }

    IEnumerator SpiralTruckUp()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.5f;

            float angle = Time.time * truckSpinSpeed;
            float radius = Mathf.Lerp(10f, truckPullRadius, t);
            float height = Mathf.Lerp(0f, truckLiftHeight, t);

            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            truck.localPosition = new Vector3(x, height, z);
            truck.localRotation = Quaternion.Euler(0, angle, 0);

            yield return null;
        }

        AddToOrbit(truck);
    }

    void CheckDynamicPickups()
    {
        foreach (var obj in pickUpCandidates.ToArray())
        {
            if (obj == null || orbitingObjects.Contains(obj)) continue;

            float dist = Vector3.Distance(transform.position, obj.position);
            if (dist <= pickupRange)
            {
                var rb = obj.GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = true;

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
        heightOffsets.Add(Random.Range(0f, 10f));
    }

    [ContextMenu("Auto-Find Debris")]
    void AutoFindDebris()
    {
        pickUpCandidates.Clear();
        GameObject[] debris = GameObject.FindGameObjectsWithTag("PickUp");
        foreach (var d in debris)
        {
            if (!pickUpCandidates.Contains(d.transform))
                pickUpCandidates.Add(d.transform);
        }

        Debug.Log($"[TornadoController] Found {pickUpCandidates.Count} debris objects with tag 'PickUp'.");
    }
}
