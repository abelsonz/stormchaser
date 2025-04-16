using UnityEngine;

public class Calibrate : MonoBehaviour
{
    [Tooltip("Assign your PlayerFeetPosition GameObject here")]
    public GameObject playerFeetPosition;
    public GameObject worldParent;
    public GameObject target;
    public GameObject blackSphere;

    FollowCar follow;
    SphereTeleportFade fade;

    private void Start()
    {
        follow = this.GetComponent<FollowCar>();
        fade = blackSphere.GetComponent<SphereTeleportFade>();
        follow.enabled = false;
        blackSphere.SetActive(true);
        worldParent.SetActive(false);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        //if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            // grab the HMD’s current yaw
        
            //adjust the position so they match
            Vector3 worldPosition = playerFeetPosition.transform.position;
            Vector3 targetPosition = target.transform.position;
            targetPosition.y = 0;
            worldParent.transform.position += (targetPosition - worldPosition);

            //adjust the rotation so they match
            Vector3 worldDirection = playerFeetPosition.transform.forward;
            worldDirection.y = 0; //makes the world vector flat on the xz axis
            Vector3 targetDirection = target.transform.forward;
            targetDirection.y = 0; //makes the camera vector flat on the xz axis

          //  float yawDegrees = Vector3.Angle(worldDirection, targetDirection);

            // rotate your feet‑anchor by the negative of that yaw, in world space
          //  worldParent.transform.RotateAround(targetPosition,Vector3.up,-yawDegrees);
            Debug.LogError("Manual recenter: aligned playerFeetPosition so camera faces forward");

            follow.enabled = true;
            // blackSphere.SetActive(false);
            worldParent.SetActive(true);
            fade.StartIntroSequence();

        }
    }
}
