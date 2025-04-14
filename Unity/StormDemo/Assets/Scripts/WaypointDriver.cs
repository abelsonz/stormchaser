using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaypointData
{
    [Tooltip("Reference to the waypoint transform.")]
    public Transform waypoint;

    [Tooltip("If checked, the truck will pause here.")]
    public bool pause;

    [Tooltip("If checked, the story decision and pause here.")]
    public bool decision;

    [Tooltip("Time (in seconds) to pause at this waypoint. Must be > 0 if Pause is checked.")]
    public float pauseDuration;
}

public class WaypointDriver : MonoBehaviour
{
    [Header("References")]
    public suspensionLogic suspension;
    // The waypoints list is now filled via the Inspector.
    public List<WaypointData> waypoints = new List<WaypointData>();

    [Header("Speed Settings")]
    public float maxSpeed = 10f;
    public float accelerationTorque = 1500f;
    public float decelerationTorque = 0f;

    [Header("Steering Settings")]
    public float steeringSensitivity = 5f;
    public float maxSteeringAngle = 45f;

    [Header("Braking Settings")]
    public float brakeTorque = 3000f;
    public float overSpeedBrakeSensitivity = 2f;

    [Header("Waypoint Behavior")]
    public float waypointThreshold = 3f;
    public bool loopPath = false;
    public bool stopAtFinalWaypoint = true;

    [Header("Start Delay")]
    [Tooltip("Time in seconds to wait before the truck starts driving to the first waypoint.")]
    public float startDelay = 0f;

    [Header("Final Braking")]
    [Tooltip("Distance from the waypoint at which the truck should start braking if pause is enabled or if it's a final stop.")]
    public float finalBrakeDistance = 5f;

    public int CurrentWaypointIndex => currentWaypoint;
    public bool isStoryDecisionPoint;

    private int currentWaypoint = 0;
    private Rigidbody rb;
    private bool hasStopped = false;
    private bool isPaused = false;

    void Start()
    {
        if (suspension != null)
            suspension.controlled = false;

        rb = GetComponent<Rigidbody>();
        isStoryDecisionPoint = false;

        // The auto-population code has been removed so that the inspector-assigned list is preserved.
        // You can manually assign or modify the waypoint list (including pause settings) in the Inspector.
    }

    public bool isThisDecisionPoint()
    {
        return waypoints[currentWaypoint].decision;
    }

    // Extract the number from a waypoint name formatted as "TruckWaypoint (x)"
    float ExtractWaypointNumber(string waypointName)
    {
        int startIndex = waypointName.IndexOf('(');
        int endIndex = waypointName.IndexOf(')');
        if (startIndex >= 0 && endIndex > startIndex)
        {
            string numStr = waypointName.Substring(startIndex + 1, endIndex - startIndex - 1);
            if (float.TryParse(numStr, out float num))
                return num;
        }
        return 0f;
    }

    void FixedUpdate()
    {
        if (waypoints.Count == 0)
            return;

        if (Time.timeSinceLevelLoad < startDelay)
        {
            ApplyDrive(0f, 0f, true, 1f);
            return;
        }

        if (isPaused)
        {
            ApplyDrive(0f, 0f, true, brakeTorque);
            return;
        }

        if (hasStopped)
        {
            ApplyDrive(0f, 0f, true, brakeTorque);
            return;
        }

        // Get the current waypoint data from the inspector-assigned list.
        WaypointData currentData = waypoints[currentWaypoint];
        Vector3 target = currentData.waypoint.position;
        Vector3 localTarget = transform.InverseTransformPoint(target);
        float steerAngle = Mathf.Clamp(localTarget.x * steeringSensitivity, -maxSteeringAngle, maxSteeringAngle);
        float speed = rb.velocity.magnitude;

        float torque = speed < maxSpeed ? accelerationTorque : decelerationTorque;
        bool brake = false;
        float appliedBrakeTorque = 0f;

        if (speed > maxSpeed)
        {
            brake = true;
            float overspeed = speed - maxSpeed;
            appliedBrakeTorque = brakeTorque * Mathf.Clamp01(overspeed * overSpeedBrakeSensitivity);
        }

        float distanceToTarget = Vector3.Distance(transform.position, target);
        bool reached = distanceToTarget < waypointThreshold;
        bool pauseWaypoint = currentData.pause && currentData.pauseDuration > 0f;

        // If this waypoint requires pausing, then if within the final braking distance, apply full braking.
        if (pauseWaypoint)
        {
            if (distanceToTarget < finalBrakeDistance)
            {
                ApplyDrive(0f, steerAngle, true, brakeTorque);
                if (reached)
                {
                    isPaused = true;
                    StartCoroutine(PauseAtWaypoint(currentData.pauseDuration));
                }
                return;
            }
        }
        else
        {
            // Standard final brake for the final waypoint.
            bool isFinalWaypoint = !loopPath && stopAtFinalWaypoint && (currentWaypoint == waypoints.Count - 1);
            if (isFinalWaypoint && distanceToTarget < finalBrakeDistance)
            {
                ApplyDrive(0f, steerAngle, true, brakeTorque);
                return;
            }
            if (reached)
            {
                if (!loopPath && stopAtFinalWaypoint && currentWaypoint == waypoints.Count - 1)
                {
                    hasStopped = true;
                    ApplyDrive(0f, steerAngle, true, brakeTorque);
                    return;
                }
                else
                {
                    currentWaypoint++;
                }
            }
        }

        ApplyDrive(torque, steerAngle, brake, appliedBrakeTorque);
    }

    IEnumerator PauseAtWaypoint(float waitTime)
    {
        float timer = 0f;
        while (timer < waitTime)
        {
            ApplyDrive(0f, 0f, true, brakeTorque);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        isPaused = false;
        currentWaypoint++;
    }

    void ApplyDrive(float torque, float steer, bool brake, float brakeStrength)
    {
        foreach (var axle in suspension.axleInfos)
        {
            axle.RotateWheels(steer);
            float appliedBrake = brake ? brakeStrength : 0f;
            axle.SetTorque(torque, appliedBrake, brake);
        }
    }

    void OnDrawGizmos()
    {

        for (int i = 0; i < waypoints.Count; i++)
        {
            if(waypoints[i].pause)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.blue;
            Gizmos.DrawSphere(waypoints[i].waypoint.transform.position, 1+waypoints[i].pauseDuration/10f);
        }

        Gizmos.color = Color.blue;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Gizmos.DrawLine(waypoints[i].waypoint.transform.position, waypoints[i + 1].waypoint.transform.position);
        }
    }
}
