using System.Collections.Generic;
using UnityEngine;

public class AutoDrive : MonoBehaviour
{
    public suspensionLogic suspension;
    public float maxSpeed = 3f;

    [Header("Drive Path Steps")]
    public List<DriveStep> driveSteps = new List<DriveStep>();

    private int currentStepIndex = 0;
    private float timer = 0f;
    private float currentTorque = 0f;
    private float lastSpeed = 0f;
    private float adaptiveBrakeThreshold = 0.2f;
    private bool applyAdaptiveBrake = false;

    void Start()
    {
        suspension.controlled = false;
    }

    void FixedUpdate()
    {
        if (driveSteps.Count == 0 || currentStepIndex >= driveSteps.Count)
        {
            ApplyDrive(0f, 0f, true);
            return;
        }

        DriveStep step = driveSteps[currentStepIndex];
        timer += Time.fixedDeltaTime;

        float desiredTorque = step.wait ? 0f : step.torque;
        float currentSteer = step.steeringAngle;
        bool applyBrake = step.wait;

        float currentSpeed = GetComponent<Rigidbody>().velocity.magnitude;

        // Adaptive Braking if coasting downhill
        if (desiredTorque == 0f && currentSpeed > lastSpeed + adaptiveBrakeThreshold)
            applyAdaptiveBrake = true;
        else if (desiredTorque > 0f || currentSpeed <= maxSpeed)
            applyAdaptiveBrake = false;

        if (currentSpeed >= maxSpeed)
            desiredTorque = 0f;

        if (applyAdaptiveBrake)
            applyBrake = true;

        if (desiredTorque > 0f)
            currentTorque = Mathf.Lerp(currentTorque, desiredTorque, Time.fixedDeltaTime * 1.5f);
        else
            currentTorque = Mathf.Lerp(currentTorque, 0f, Time.fixedDeltaTime * 5f);

        ApplyDrive(currentTorque, currentSteer, applyBrake);

        if (timer >= step.duration)
        {
            currentStepIndex++;
            timer = 0f;
        }

        lastSpeed = currentSpeed;
    }

    void ApplyDrive(float torqueValue, float steerAngleValue, bool brake)
    {
        foreach (var axle in suspension.axleInfos)
        {
            axle.RotateWheels(steerAngleValue);
            axle.SetTorque(torqueValue, suspension.maxBrakeTorque, brake);
        }
    }
}

[System.Serializable]
public class DriveStep
{
    [Tooltip("How long this step lasts (in seconds)")]
    public float duration = 5f;

    [Tooltip("Set to true if this is a wait (idle) step")]
    public bool wait = false;

    [Tooltip("Torque value to apply if this is a driving step")]
    public float torque = 1500f;

    [Tooltip("Steering angle for this step (positive = right, negative = left)")]
    public float steeringAngle = 0f;
}
