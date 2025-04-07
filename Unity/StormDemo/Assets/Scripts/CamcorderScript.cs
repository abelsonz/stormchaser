using System.Collections.Generic;
using UnityEngine;

public class CamcorderScript : MonoBehaviour
{
    public Camera cam;

    [Header("Viewfinder Box Bounds (Viewport space)")]
    public float boxMinX = 0.3f;
    public float boxMinY = 0.3f;
    public float boxMaxX = 0.7f;
    public float boxMaxY = 0.7f;

    [Header("Distance Filter")]
    public float maxDetectDistance = 30f;

    private HashSet<GameObject> seenObjects = new();
    private List<GameObject> trackedObjects = new();

    public void WatchObject(GameObject obj)
    {
        if (!trackedObjects.Contains(obj))
            trackedObjects.Add(obj);
    }

    void Start()
    {
        WatchObject(GameObject.Find("TargetSphere"));
        WatchObject(GameObject.Find("FlyingTrashCan"));
        WatchObject(GameObject.Find("FlyingPiano"));
    }

    void Update()
    {
        foreach (GameObject obj in trackedObjects)
        {
            if (!obj || seenObjects.Contains(obj)) continue;

            Vector3 pos = cam.WorldToViewportPoint(obj.transform.position);

            bool inViewfinder = pos.z > 0 &&
                                pos.x > boxMinX && pos.x < boxMaxX &&
                                pos.y > boxMinY && pos.y < boxMaxY;

            bool withinRange = pos.z <= maxDetectDistance;

            if (inViewfinder && withinRange)
            {
                seenObjects.Add(obj);
                Debug.Log($"[Camcorder] {obj.name} entered viewfinder at {pos.z:F1}m");

                if (obj.name == "FlyingTrashCan") Debug.Log("NPC: You got the trash can! That thing was flying!");
                else if (obj.name == "FlyingPiano") Debug.Log("NPC: Is that a piano? That’s insane!");
                else Debug.Log("NPC: Nice shot!");
            }
        }
    }

    public bool HasSeen(GameObject obj) => seenObjects.Contains(obj);
}
