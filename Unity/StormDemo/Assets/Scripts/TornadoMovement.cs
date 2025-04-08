using UnityEngine;
using System.Collections.Generic;

public class TornadoMovement : MonoBehaviour
{
    [Header("Waypoint Path")]
    public List<Transform> waypoints = new List<Transform>();
    public float waypointThreshold = 3f;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float curveStrength = 5f;
    public float curveFrequency = 0.5f;

    [Header("Movement Delay")]
    public float startDelay = 3f;

    private int currentWaypointIndex = 0;
    private float timer = 0f;
    private float timeOffset;
    private bool isMoving = false;

    void Start()
    {
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!isMoving)
        {
            timer += Time.deltaTime;
            if (timer >= startDelay)
                isMoving = true;
            else
                return;
        }

        if (waypoints == null || waypoints.Count == 0) return;

        Transform targetPoint = waypoints[currentWaypointIndex];
        Vector3 toTarget = targetPoint.position - transform.position;
        float distance = toTarget.magnitude;

        // Move to next waypoint if close enough
        if (distance < waypointThreshold)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                // You can change this to loop, stop, or drift
                enabled = false;
                return;
            }
            targetPoint = waypoints[currentWaypointIndex];
            toTarget = targetPoint.position - transform.position;
        }

        Vector3 direction = toTarget.normalized;

        // Forward movement
        Vector3 forwardMotion = direction * moveSpeed * Time.deltaTime;

        // Side-to-side drift
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        float lateralOffset = Mathf.Sin(Time.time * curveFrequency + timeOffset) * curveStrength;
        Vector3 curvedMotion = right * lateralOffset * Time.deltaTime;

        // Move the tornado
        transform.position += forwardMotion + curvedMotion;

        // Rotate to face movement direction
        Vector3 lookDir = forwardMotion + curvedMotion;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }
}
