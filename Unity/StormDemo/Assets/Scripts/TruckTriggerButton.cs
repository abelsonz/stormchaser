using UnityEngine;

public class TruckTriggerButton : MonoBehaviour
{
    public TruckController truckController;

    private void OnTriggerEnter(Collider other)
    {
        if (truckController != null)
        {
            truckController.ToggleMovement();
            Debug.Log("Truck triggered by: " + other.name);
        }
    }
}
