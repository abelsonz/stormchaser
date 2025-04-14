using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calibrate : MonoBehaviour
{
    public GameObject sceneStartPosition;
    public GameObject playerStartPosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            this.transform.position += sceneStartPosition.transform.position - playerStartPosition.transform.position;
            Debug.LogError("Calibrating!");
        }

    }
}
