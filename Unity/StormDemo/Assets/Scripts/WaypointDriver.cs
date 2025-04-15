using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaypointData {
    public Transform waypoint;
    public bool pause;
    public float pauseDuration;
}

public class WaypointDriver : MonoBehaviour {
    public suspensionLogic suspension;
    public CamcorderScript camcorder;
    public AudioManager audioManager;
    public List<WaypointData> waypoints;

    [Header("Speeds & Torques")]
    public float maxSpeed = 10f;
    public float decisionSpeed = 12f;
    public float accelerationTorque = 1500f;
    public float decelerationTorque = 0f;
    public float brakeTorque = 3000f;
    public float overSpeedBrakeSensitivity = 2f;

    [Header("Steering")]
    public float steeringSensitivity = 5f;
    public float maxSteeringAngle = 45f;

    [Header("Waypoint Behavior")]
    public float waypointThreshold = 3f;
    public bool loopPath = false;
    public bool stopAtFinalWaypoint = true;

    [Header("Start Delay")]
    public float startDelay = 0f;

    [Header("Decision Braking")]
    public float finalBrakeDistance = 5f;

    private Rigidbody rb;
    private int currentWaypoint = 0;
    private enum State { Driving, Pausing, DecisionBrake, DrivingToFinal, FinalBrake, Ended }
    private State state = State.Driving;
    private bool playedFirstInsufficient = false;
    private bool playedSecondInsufficient = false;
    private bool playedSufficient = false;

    public bool forceSufficientEnding = false;

    private bool pauseCoroutineStarted = false;

    void Start() {
        rb = GetComponent<Rigidbody>();
        if (suspension != null) suspension.controlled = false;
    }

    void FixedUpdate() {
        if (waypoints == null || waypoints.Count == 0 || state == State.Ended) return;
        if (Time.timeSinceLevelLoad < startDelay) {
            ApplyDrive(0f, 0f, true, brakeTorque);
            return;
        }

        int finalIndex = waypoints.Count - 1;
        int decisionIndex = Mathf.Max(0, waypoints.Count - 2);

        if (currentWaypoint > finalIndex) {
            state = State.Ended;
            return;
        }
        if (state == State.DecisionBrake || state == State.FinalBrake) {
            // let those states handle braking
        }

        // pick target based on state
        int targetIndex = (state == State.DrivingToFinal || state == State.FinalBrake)
            ? finalIndex
            : currentWaypoint;

        WaypointData wp = waypoints[targetIndex];
        Vector3 tgt = wp.waypoint.position;
        float dist = Vector3.Distance(transform.position, tgt);
        bool reached = dist < waypointThreshold;

        // steering
        Vector3 local = transform.InverseTransformPoint(tgt);
        float steer = Mathf.Clamp(local.x * steeringSensitivity, -maxSteeringAngle, maxSteeringAngle);

        // speed limit
        float usedMax = (state == State.DrivingToFinal) ? decisionSpeed : maxSpeed;
        float speed = rb.velocity.magnitude;
        float torque = speed < usedMax ? accelerationTorque : decelerationTorque;
        bool braking = false;
        float appliedBrake = 0f;
        if (speed > usedMax) {
            braking = true;
            float over = speed - usedMax;
            appliedBrake = brakeTorque * Mathf.Clamp01(over * overSpeedBrakeSensitivity);
        }

        switch (state) {
            case State.Driving:
                if (!reached) {
                    ApplyDrive(torque, steer, braking, appliedBrake);
                } else if (forceSufficientEnding || currentWaypoint == decisionIndex) {
                    if (camcorder.RecordedCount >= 7) {
                        state = State.DecisionBrake;
                    } else {
                        state = State.DrivingToFinal;
                        if (!playedFirstInsufficient) {
                            playedFirstInsufficient = true;
                            audioManager.PlayAudioClip(audioManager.insufficientFootageClips[0]);
                        }
                        currentWaypoint = finalIndex;
                    }
                } else if (wp.pause) {
                    state = State.Pausing;
                    StartCoroutine(PauseAtWaypoint(wp.pauseDuration));
                } else {
                    currentWaypoint++;
                }
                break;

            case State.DecisionBrake:
                if (dist < finalBrakeDistance) {
                    if (!playedSufficient) {
                        playedSufficient = true;
                        StartCoroutine(PlayDialogueList(audioManager.sufficientFootageClips, audioManager.sufficientDialogueDelay));
                    }
                    ApplyDrive(0f, steer, true, brakeTorque);
                    state = State.Ended;
                } else {
                    ApplyDrive(torque, steer, braking, appliedBrake);
                }
                break;

            case State.Pausing:
                // pause coroutine drives the transition
                break;

            case State.DrivingToFinal:
                if (!reached) {
                    ApplyDrive(torque, steer, braking, appliedBrake);
                } else {
                    state = State.FinalBrake;
                }
                break;

            case State.FinalBrake:
                if (!playedSecondInsufficient) {
                    playedSecondInsufficient = true;
                    audioManager.PlayAudioClip(audioManager.insufficientFootageClips[1]);
                }
                ApplyDrive(0f, steer, true, brakeTorque);
                state = State.Ended;
                break;
        }
    }

    IEnumerator PauseAtWaypoint(float t) {
        float timer = 0f;
        while (timer < t) {
            ApplyDrive(0f, 0f, true, brakeTorque);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        pauseCoroutineStarted = false;
        state = State.Driving;
        currentWaypoint++;
    }

    IEnumerator PlayDialogueList(List<AudioClip> clips, float delay) {
        foreach (var c in clips) {
            audioManager.PlayAudioClip(c);
            yield return new WaitUntil(() => !audioManager.audioSource.isPlaying);
            yield return new WaitForSeconds(delay);
        }
    }

    void ApplyDrive(float torque, float steer, bool brake, float brakeStrength) {
        foreach (var axle in suspension.axleInfos) {
            axle.RotateWheels(steer);
            float b = brake ? brakeStrength : 0f;
            axle.SetTorque(torque, b, brake);
        }
    }

    // ---- Draw Gizmos ----
    void OnDrawGizmos() {
        if (waypoints == null) return;
        for (int i = 0; i < waypoints.Count; i++) {
            Gizmos.color = waypoints[i].pause ? Color.green : Color.blue;
            if (waypoints[i].waypoint != null)
                Gizmos.DrawSphere(waypoints[i].waypoint.position, 1f);
        }
        Gizmos.color = Color.blue;
        for (int i = 0; i < waypoints.Count - 1; i++) {
            if (waypoints[i].waypoint != null && waypoints[i+1].waypoint != null)
                Gizmos.DrawLine(waypoints[i].waypoint.position, waypoints[i+1].waypoint.position);
        }
    }
}
