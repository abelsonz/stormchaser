using UnityEngine;

public class TruckToggleButton : MonoBehaviour
{
    public TruckController truckController;

    public void OnPress()
    {
        if (truckController != null)
        {
            truckController.ToggleMovement();
        }
    }
}
