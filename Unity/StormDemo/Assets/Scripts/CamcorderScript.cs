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
    // In Unity, one unit is typically equivalent to one meter.
    public float maxDetectDistance = 50f;

    [Header("Viewable Objects")]
    public List<ViewableObject> viewableObjects = new();

    // Prevents re-triggering the same object.
    private HashSet<GameObject> seenObjects = new();

    // Event fired when a new object is detected.
    public event Action<GameObject, AudioClip> OnObjectDetected;

    void Update()
    {
        // Removed the isHeld flag check so that detection runs continuously.
        ViewableObject closestInView = GetClosestVisibleObject();
        if (closestInView != null)
        {
            GameObject obj = closestInView.targetObject;
            seenObjects.Add(obj);
            float distance = Vector3.Distance(cam.transform.position, obj.transform.position);
            Debug.Log($"[Camcorder] {obj.name} entered viewfinder at {distance:F1}m");

            // Fire the event to notify listeners (such as an audio manager).
            OnObjectDetected?.Invoke(obj, closestInView.audioClip);
        }
    }

    // Determines if a viewport position is within the viewfinder bounds.
    private bool IsInViewfinder(Vector3 viewportPos)
    {
        return viewportPos.z > 0 &&
               viewportPos.x > boxMinX && viewportPos.x < boxMaxX &&
               viewportPos.y > boxMinY && viewportPos.y < boxMaxY;
    }

    // Finds the closest object that is visible in the viewfinder and within range.
    private ViewableObject GetClosestVisibleObject()
    {
        ViewableObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (var vObj in viewableObjects)
        {
            GameObject obj = vObj.targetObject;
            if (obj == null || seenObjects.Contains(obj))
                continue;

            Vector3 viewportPos = cam.WorldToViewportPoint(obj.transform.position);
            if (IsInViewfinder(viewportPos) && viewportPos.z <= maxDetectDistance)
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
    public int RecordedCount
    {
        get { return seenObjects.Count; }
    }
}
