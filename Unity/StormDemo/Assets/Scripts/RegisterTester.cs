using UnityEngine;

public class RegisterMultiple : MonoBehaviour
{
    public CamcorderFocusTracker tracker;
    public GameObject[] targets;

    void Start()
    {
        if (tracker != null)
        {
            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    tracker.WatchObject(target);
                    Debug.Log("Registered: " + target.name);
                }
            }
        }
    }
}
