using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ViewableObject
{
    public GameObject targetObject;
    public AudioClip audioClip;
}

public class CamcorderScript : MonoBehaviour
{
    [Header("Viewfinder Camera")]
    public Camera cam;

    [Header("Viewfinder Box Bounds (Viewport space)")]
    public float boxMinX = 0.3f;
    public float boxMinY = 0.3f;
    public float boxMaxX = 0.7f;
    public float boxMaxY = 0.7f;

    [Header("Distance Filter (meters)")]
    public float maxDetectDistance = 50f;

    [Header("Viewable Objects")]
    public List<ViewableObject> viewableObjects = new();

    // Tracks which objects have been seen (and either played or dropped)
    private HashSet<GameObject> seenObjects = new HashSet<GameObject>();

    // Blocks new detections until ResetDetection() is called
    private bool hasDetectedThisCycle = false;

    // Fired when a new object is detected
    public event Action<GameObject, AudioClip> OnObjectDetected;

    // Let other scripts (like WaypointDriver) know how many you've recorded
    public int RecordedCount => seenObjects.Count;

    void Update()
    {
        if (hasDetectedThisCycle) return;

        var closest = GetClosestVisibleObject();
        if (closest != null)
        {
            seenObjects.Add(closest.targetObject);
            hasDetectedThisCycle = true;
            Debug.Log($"[Camcorder] Detected {closest.targetObject.name}");
            OnObjectDetected?.Invoke(closest.targetObject, closest.audioClip);
        }
    }

    private ViewableObject GetClosestVisibleObject()
    {
        ViewableObject best = null;
        float bestDist = Mathf.Infinity;

        foreach (var v in viewableObjects)
        {
            var obj = v.targetObject;
            if (obj == null || seenObjects.Contains(obj)) continue;

            Vector3 vp = cam.WorldToViewportPoint(obj.transform.position);
            if (!IsInViewfinder(vp) || vp.z > maxDetectDistance) continue;

            float d = Vector3.Distance(cam.transform.position, obj.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = v;
            }
        }

        return best;
    }

    private bool IsInViewfinder(Vector3 vp)
    {
        return vp.z > 0
            && vp.x > boxMinX && vp.x < boxMaxX
            && vp.y > boxMinY && vp.y < boxMaxY;
    }

    /// <summary>
    /// Call after your audio finishes to allow the next detection.
    /// </summary>
    public void ResetDetection()
    {
        hasDetectedThisCycle = false;
    }

    /// <summary>
    /// If you drop an object from the queue, call this so it can be re‑detected on re‑entry.
    /// </summary>
    public void ForgetObject(GameObject obj)
    {
        seenObjects.Remove(obj);
    }

    /// <summary>
    /// True if that object is currently within your viewfinder and range.
    /// </summary>
    public bool IsObjectVisible(GameObject obj)
    {
        if (obj == null) return false;
        Vector3 vp = cam.WorldToViewportPoint(obj.transform.position);
        return IsInViewfinder(vp) && vp.z <= maxDetectDistance;
    }
}
