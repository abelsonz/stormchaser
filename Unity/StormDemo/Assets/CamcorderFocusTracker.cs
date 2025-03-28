// CamcorderFocusTracker.cs
using System.Collections.Generic;
using UnityEngine;

public class CamcorderFocusTracker : MonoBehaviour
{
    public Camera cam;
    public float timeToRecord = 1.5f;

    [Header("Focus Box Settings")]
    public float boxMinX = 0.4f;
    public float boxMinY = 0.4f;
    public float boxMaxX = 0.6f;
    public float boxMaxY = 0.6f;

    Dictionary<GameObject, float> timers = new();
    List<GameObject> recorded = new();

    public void WatchObject(GameObject obj)
    {
        if (!timers.ContainsKey(obj)) timers[obj] = 0f;
    }

    void Update()
    {
        foreach (GameObject obj in new List<GameObject>(timers.Keys))
        {
            if (!obj) continue;

            Vector3 pos = cam.WorldToViewportPoint(obj.transform.position);
            bool inBox = pos.z > 0 && pos.x > boxMinX && pos.x < boxMaxX && pos.y > boxMinY && pos.y < boxMaxY;

            Debug.Log($"[{obj.name}] inBox = {inBox}, time = {timers[obj]:F2}");

            if (inBox)
            {
                timers[obj] += Time.deltaTime;
                if (timers[obj] >= timeToRecord && !recorded.Contains(obj))
                {
                    recorded.Add(obj);
                    Debug.Log("Captured: " + obj.name);

                    if (obj.name == "FlyingTrashCan") Debug.Log("NPC: You got the trash can! That thing was flying!");
                    else if (obj.name == "FlyingPiano") Debug.Log("NPC: Is that a piano? That’s insane!");
                    else Debug.Log("NPC: Nice shot!");
                }
            }
            else
            {
                timers[obj] = Mathf.Max(0f, timers[obj] - Time.deltaTime * 2f);
            }
        }
    }

    public bool HasCaptured(GameObject obj) => recorded.Contains(obj);
}
