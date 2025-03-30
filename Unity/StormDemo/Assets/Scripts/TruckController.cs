// TruckController.cs
using UnityEngine;

public class TruckController : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float accelerationTime = 2f;
    public float decelerationTime = 2f;

    private float currentSpeed = 0f;
    private bool isMoving = false;

    void Update()
    {
        float targetSpeed = isMoving ? maxSpeed : 0f;
        float speedStep = (isMoving ? maxSpeed / accelerationTime : -maxSpeed / decelerationTime) * Time.deltaTime;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Mathf.Abs(speedStep));

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    public void ToggleMovement()
    {
        isMoving = !isMoving;
    }
}
