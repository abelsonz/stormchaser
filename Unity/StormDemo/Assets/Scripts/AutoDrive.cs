using System.Collections.Generic;
using UnityEngine;

public class AutoDrive : MonoBehaviour
{
    public suspensionLogic suspension;
    public float maxSpeed = 3f;

    [Header("Drive Path Steps")]
    public List<DriveStep> driveSteps = new List<DriveStep>();

    [Header("Adaptive Braking Settings")]
    public float brakeTorque = 3000f;
    public float adaptiveBrakeSensitivity = 1.5f;

    private int currentStepIndex = 0;
    private float timer = 0f;
    private float currentTorque = 0f;

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

        float currentSpeed = GetComponent<Rigidbody>().velocity.magnitude;

        bool applyBrake = false;
        float brakeStrength = 0f;

        if (step.wait)
        {
            applyBrake = true;
            brakeStrength = 1f; // full brake during wait steps
        }
        else if (currentSpeed > maxSpeed)
        {
            float overspeed = currentSpeed - maxSpeed;
            brakeStrength = Mathf.Clamp01(overspeed * adaptiveBrakeSensitivity);
            applyBrake = true;
        }

        // Smooth torque ramping
        if (desiredTorque > 0f)
            currentTorque = Mathf.Lerp(currentTorque, desiredTorque, Time.fixedDeltaTime * 1.5f);
        else
            currentTorque = Mathf.Lerp(currentTorque, 0f, Time.fixedDeltaTime * 5f);

        ApplyDrive(currentTorque, currentSteer, applyBrake, brakeStrength);

        if (timer >= step.duration)
        {
            currentStepIndex++;
            timer = 0f;
        }
    }

    void ApplyDrive(float torqueValue, float steerAngleValue, bool brake, float brakeStrength = 1f)
    {
        foreach (var axle in suspension.axleInfos)
        {
            axle.RotateWheels(steerAngleValue);
            float appliedBrake = brake ? brakeTorque * brakeStrength : 0f;
            axle.SetTorque(torqueValue, appliedBrake, brake);
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
