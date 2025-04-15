using UnityEngine;

public class Calibrate : MonoBehaviour
{
    [Tooltip("Assign your PlayerFeetPosition GameObject here")]
    public GameObject playerFeetPosition;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            // grab the HMD’s current yaw
            float headYaw = Camera.main.transform.eulerAngles.y;
            // rotate your feet‑anchor by the negative of that yaw, in world space
            playerFeetPosition.transform.Rotate(0f, -headYaw, 0f, Space.World);
            Debug.Log("Manual recenter: aligned playerFeetPosition so camera faces forward");
        }
    }
}
