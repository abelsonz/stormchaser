using UnityEngine;
using System.Collections.Generic;

public class WaypointDriver : MonoBehaviour
{
    [Header("References")]
    public suspensionLogic suspension;
    public List<Transform> waypoints = new();

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

    public int CurrentWaypointIndex => currentWaypoint;

    private int currentWaypoint = 0;
    private Rigidbody rb;
    private bool hasStopped = false;

    void Start()
    {
        if (suspension != null)
            suspension.controlled = false;

        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // If there are no waypoints, exit.
        if (waypoints.Count == 0)
            return;

        // Check if the start delay hasn't elapsed yet.
        if (Time.timeSinceLevelLoad < startDelay)
        {
            // Optionally, fully brake (or just do nothing)
            ApplyDrive(0f, 0f, true, 1f);
            return;
        }

        if (hasStopped)
        {
            ApplyDrive(0f, 0f, true, 1f); // Full brake
            return;
        }

        // Calculate target point
        Vector3 target = waypoints[currentWaypoint].position;
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

        bool reached = Vector3.Distance(transform.position, target) < waypointThreshold;

        if (reached)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Count)
            {
                if (loopPath)
                    currentWaypoint = 0;
                else if (stopAtFinalWaypoint)
                    hasStopped = true;
            }
        }

        ApplyDrive(torque, steerAngle, brake, appliedBrakeTorque);
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
}
