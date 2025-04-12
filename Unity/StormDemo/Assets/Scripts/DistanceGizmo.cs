using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DistanceGizmo : MonoBehaviour
{
    [Tooltip("Assign the GameObjects (or their Transforms) you want to measure the distances between.")]
    public Transform[] objectsToMeasure;

    void OnDrawGizmos()
    {
        if (objectsToMeasure == null || objectsToMeasure.Length < 2)
            return;

        for (int i = 0; i < objectsToMeasure.Length; i++)
        {
            for (int j = i + 1; j < objectsToMeasure.Length; j++)
            {
                if (objectsToMeasure[i] != null && objectsToMeasure[j] != null)
                {
                    Vector3 posA = objectsToMeasure[i].position;
                    Vector3 posB = objectsToMeasure[j].position;

                    // Draw a line between the two objects.
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(posA, posB);

                    // Calculate the midpoint for placing the label.
                    Vector3 midPoint = (posA + posB) / 2f;
                    float distance = Vector3.Distance(posA, posB);

#if UNITY_EDITOR
                    // Draw the distance label at the midpoint.
                    Handles.Label(midPoint, distance.ToString("F2") + " m");
#endif
                }
            }
        }
    }
}
