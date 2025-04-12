using System.Collections.Generic;
using UnityEngine;

public class AudioTriggerScript : MonoBehaviour
{
    [Header("Radio Audio Source")]
    public GameObject radio;
    private AudioSource audioSource;

    [Header("Waypoint Driver")]
    public WaypointDriver driver;

    [System.Serializable]
    public class WaypointAudioClip
    {
        public int waypointIndex;
        public AudioClip audioClip;
    }

    [Header("Waypoint-Triggered Audio Clips")]
    public List<WaypointAudioClip> waypointClips = new();

    private HashSet<int> alreadyPlayed = new();

    void Start()
    {
        if (radio == null)
        {
            Debug.LogError("Radio GameObject not assigned.");
            return;
        }

        audioSource = radio.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = radio.AddComponent<AudioSource>();

        if (driver == null)
        {
            Debug.LogError("WaypointDriver script not assigned.");
            return;
        }
    }

    void Update()
    {
        if (audioSource == null || audioSource.isPlaying || driver == null)
            return;

        int currentWaypoint = driver.CurrentWaypointIndex;

        foreach (var clip in waypointClips)
        {
            if (clip.waypointIndex == currentWaypoint && !alreadyPlayed.Contains(currentWaypoint))
            {
                if (clip.audioClip != null)
                {
                    audioSource.clip = clip.audioClip;
                    audioSource.Play();
                    alreadyPlayed.Add(currentWaypoint);
                    Debug.Log($"[AudioTrigger] Played clip at waypoint {currentWaypoint}");
                }
                break;
            }
        }
    }
}
