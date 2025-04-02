using UnityEngine;

public class TruckController : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float accelerationTime = 2f;
    public float decelerationTime = 2f;
    public float stormLiftSpeed = 10f;
    public float stormSpinSpeed = 120f;
    public float stormOrbitRadius = 5f;
    public float stormOrbitSpeed = 2f;

    private float currentSpeed = 0f;
    private bool isMoving = false;
    private float timer = 0f;
    private int phase = 0; // 0 = waiting, 1 = driving, 2 = stopping, 3 = storm
    private int repetitions = 0;
    private float stormAngle = 0f;
    private Vector3 stormCenter;
    private bool stormStarted = false;

    void Update()
    {
        Debug.Log($"[TruckController] Update running - Phase: {phase}, Timer: {timer:F2}, Speed: {currentSpeed:F2}");

        timer += Time.deltaTime;

        if (phase == 0 && timer >= 1f)
        {
            timer = 0f;
            isMoving = true;
            phase = 1;
        }
        else if (phase == 1 && timer >= 5f)
        {
            timer = 0f;
            isMoving = false;
            phase = 2;
        }
        else if (phase == 2 && timer >= 5f)
        {
            timer = 0f;
            repetitions++;

            if (repetitions >= 3)
            {
                phase = 3; // storm
                stormCenter = transform.position + new Vector3(0, 0, -stormOrbitRadius);
                stormStarted = true;
            }
            else
            {
                isMoving = true;
                phase = 1;
            }
        }

        if (phase < 3)
        {
            float targetSpeed = isMoving ? maxSpeed : 0f;
            float speedStep = (isMoving ? maxSpeed / accelerationTime : -maxSpeed / decelerationTime) * Time.deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Mathf.Abs(speedStep));

            transform.Translate(transform.forward * currentSpeed * Time.deltaTime);
        }
        else if (phase == 3 && stormStarted)
        {
            // Orbit and rise like storm debris
            stormAngle += stormOrbitSpeed * Time.deltaTime;
            float radians = stormAngle * Mathf.Deg2Rad;
            Vector3 orbitPos = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)) * stormOrbitRadius;
            Vector3 stormTarget = stormCenter + orbitPos;

            transform.position = Vector3.MoveTowards(transform.position, stormTarget + Vector3.up * stormLiftSpeed * Time.timeSinceLevelLoad * 0.1f, stormLiftSpeed * Time.deltaTime);
            transform.Rotate(Vector3.up, stormSpinSpeed * Time.deltaTime);
        }
    }
    void OnDrawGizmos()
{
    Gizmos.color = Color.red;
    Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
}

}
