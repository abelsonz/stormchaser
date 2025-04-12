using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ViewableObject
{
    public GameObject targetObject;
    public AudioClip audioClip;
}

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

    [Header("Interaction State")]
    public bool isHeld = false;

    [Header("Viewable Objects")]
    public List<ViewableObject> viewableObjects = new();

    // Legacy support for tracking objects
    private List<GameObject> trackedObjects = new();

    // Keeps track of objects already detected so they don't trigger multiple times
    private HashSet<GameObject> seenObjects = new();

    // Event triggered when a new object enters the viewfinder.
    // Passes the object's GameObject and its associated AudioClip.
    public event Action<GameObject, AudioClip> OnObjectDetected;

    void Start()
    {
        // Legacy support: add common objects to track
        WatchObject(GameObject.Find("TargetSphere"));
        WatchObject(GameObject.Find("FlyingTrashCan"));
        WatchObject(GameObject.Find("FlyingPiano"));
    }

    void Update()
    {
        if (!isHeld) return;

        // Use helper method to get the closest visible object that hasn't been seen yet.
        ViewableObject closestInView = FindClosestVisibleObject();

        if (closestInView != null)
        {
            GameObject obj = closestInView.targetObject;
            seenObjects.Add(obj);
            float distance = Vector3.Distance(cam.transform.position, obj.transform.position);
            Debug.Log($"[Camcorder] {obj.name} entered viewfinder at {distance:F1}m");

            // Notify subscribers (e.g., AudioManager) via event
            OnObjectDetected?.Invoke(obj, closestInView.audioClip);

            // Optionally display NPC dialogue based on object name
            if (obj.name == "FlyingTrashCan")
                Debug.Log("NPC: You got the trash can! That thing was flying!");
            else if (obj.name == "FlyingPiano")
                Debug.Log("NPC: Is that a piano? That’s insane!");
            else
                Debug.Log("NPC: Nice shot!");
        }
    }

    // Determines if the given viewport position is within our viewfinder bounds.
    private bool IsInViewfinder(Vector3 viewportPos)
    {
        return viewportPos.z > 0 &&
               viewportPos.x > boxMinX && viewportPos.x < boxMaxX &&
               viewportPos.y > boxMinY && viewportPos.y < boxMaxY;
    }

    // Loops through viewable objects to find the closest one that's within the viewfinder and range.
    private ViewableObject FindClosestVisibleObject()
    {
        ViewableObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (var vObj in viewableObjects)
        {
            GameObject obj = vObj.targetObject;
            if (obj == null || seenObjects.Contains(obj))
                continue;

            Vector3 viewportPos = cam.WorldToViewportPoint(obj.transform.position);
            bool inViewfinder = IsInViewfinder(viewportPos);
            bool withinRange = viewportPos.z <= maxDetectDistance;

            if (inViewfinder && withinRange)
            {
                float distance = Vector3.Distance(cam.transform.position, obj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = vObj;
                }
            }
        }
        return closest;
    }

    // Legacy support — track objects manually.
    public void WatchObject(GameObject obj)
    {
        if (obj != null && !trackedObjects.Contains(obj))
        {
            trackedObjects.Add(obj);
            if (!viewableObjects.Exists(v => v.targetObject == obj))
            {
                viewableObjects.Add(new ViewableObject { targetObject = obj });
            }
        }
    }

    // Provides external access in case event-driven handling isn't used.
    public GameObject GetClosestVisibleObject()
    {
        ViewableObject closestInView = FindClosestVisibleObject();
        return closestInView?.targetObject;
    }

    public bool HasSeen(GameObject obj) => seenObjects.Contains(obj);

    public void SetIsHeld(bool held)
    {
        isHeld = held;
        Debug.Log($"Camcorder isHeld = {isHeld}");
    }
}
