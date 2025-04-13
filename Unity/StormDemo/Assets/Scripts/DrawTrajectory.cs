using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawTrajectory : MonoBehaviour
{
    // Start is called before the first frame update
    LineRenderer lr;
    float lastTime;
    Vector3 lastPosition;
    public float timeInterval = 0.5f; //seconds
    public float posInterval = 0.5f; //meters
    void Start()
    {
        lr = gameObject.AddComponent(typeof(LineRenderer)) as LineRenderer;
        lastTime = Time.time;
        lastPosition = this.transform.position;
        lr.SetPosition(0, lastPosition);
        lr.SetPosition(1, lastPosition);
    }

    // Update is called once per frame
    void Update()
    {

        float deltaPos = (this.transform.position - lastPosition).magnitude;
        if( ((Time.time - lastTime) > timeInterval) && (deltaPos > posInterval ))
        {
            lr.positionCount++;
            lr.SetPosition(lr.positionCount-1, this.transform.position); //starts at zero
            lastTime = Time.time;
            lastPosition = this.transform.position;
        }

        
    }
}
