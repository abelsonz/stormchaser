using System.Collections.Generic;
using UnityEngine;

public class AudioTriggerScript : MonoBehaviour
{
    [Header("Radio Audio Source")]
    public GameObject radio; // The radio GameObject inside the truck
    private AudioSource audioSource;

    [System.Serializable]
    public class WatchedObjectAudio
    {
        public GameObject targetObject;
        public AudioClip audioClip;
    }

    [System.Serializable]
    public class TimedAudioClip
    {
        public AudioClip audioClip;
        public float triggerTime; // time in seconds from game start
    }

    [Header("Watched Objects & Their Audio Clips")]
    public List<WatchedObjectAudio> watchList = new();

    [Header("Timed Audio Clips")]
    public List<TimedAudioClip> timedClips = new();

    private CamcorderScript camcorder;
    private HashSet<GameObject> alreadyTriggered = new();
    private HashSet<TimedAudioClip> alreadyPlayedTimedClips = new();

    private float timeSinceStart = 0f;

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

        camcorder = GetComponent<CamcorderScript>();
        if (camcorder == null)
        {
            Debug.LogError("CamcorderScript not found on this GameObject.");
            return;
        }

        // Register watched objects with camcorder
        foreach (var entry in watchList)
            camcorder.WatchObject(entry.targetObject);
    }

    void Update()
    {
        timeSinceStart += Time.deltaTime;

        if (audioSource == null || audioSource.isPlaying)
            return;

        // Check timed audio clips first
        foreach (var timedClip in timedClips)
        {
            if (alreadyPlayedTimedClips.Contains(timedClip))
                continue;

            if (timeSinceStart >= timedClip.triggerTime)
            {
                audioSource.clip = timedClip.audioClip;
                audioSource.Play();
                alreadyPlayedTimedClips.Add(timedClip);
                Debug.Log($"[AudioManager] Playing timed clip at {timedClip.triggerTime}s");
                return;
            }
        }

        // Then check watched objects
        if (!camcorder.isHeld)
            return;

        foreach (var entry in watchList)
        {
            if (entry.targetObject == null || alreadyTriggered.Contains(entry.targetObject))
                continue;

            if (camcorder.HasSeen(entry.targetObject))
            {
                audioSource.clip = entry.audioClip;
                audioSource.Play();
                alreadyTriggered.Add(entry.targetObject);
                Debug.Log($"[AudioManager] Playing clip for {entry.targetObject.name}");
                break; // Do not allow multiple clips to queue up
            }
        }
    }
}
