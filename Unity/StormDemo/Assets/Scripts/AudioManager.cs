using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class TimedAudioClip
    {
        public AudioClip clip;
        public float triggerTime;
    }

    public List<TimedAudioClip> timedAudioClips;

    // Reference to the camcorder script (set this via the Inspector)
    public CamcorderScript camcorder;

    // Dedicated AudioSource for playing clips.
    public AudioSource audioSource;

    // Queue to hold detection event clips that couldn't play immediately.
    private Queue<AudioClip> detectionQueue = new Queue<AudioClip>();

    private float timer = 0f;
    private HashSet<TimedAudioClip> playedTimedClips = new HashSet<TimedAudioClip>();

    void Start()
    {
        if (camcorder != null)
        {
            // Subscribe to the camcorder detection event.
            camcorder.OnObjectDetected += HandleCamcorderDetection;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Always prioritize any pending detection events.
        if (!audioSource.isPlaying && detectionQueue.Count > 0)
        {
            PlayClip(detectionQueue.Dequeue());
        }
        // Otherwise, if nothing is playing, check for timed audio.
        else if (!audioSource.isPlaying && detectionQueue.Count == 0)
        {
            foreach (var timedClip in timedAudioClips)
            {
                if (!playedTimedClips.Contains(timedClip) && timer >= timedClip.triggerTime)
                {
                    PlayClip(timedClip.clip);
                    playedTimedClips.Add(timedClip);
                    // Only play one timed clip at a time.
                    break;
                }
            }
        }
    }

    // Handles detection events from the camcorder.
    private void HandleCamcorderDetection(GameObject detectedObject, AudioClip clipFromCamcorder)
    {
        // Only process if we have a valid clip.
        if (clipFromCamcorder != null)
        {
            // If no audio is currently playing and there's no pending detection, play immediately.
            if (!audioSource.isPlaying && detectionQueue.Count == 0)
            {
                PlayClip(clipFromCamcorder);
            }
            else
            {
                // Queue the clip to be played later.
                detectionQueue.Enqueue(clipFromCamcorder);
            }
        }
    }

    // Plays an AudioClip using the designated AudioSource.
    private void PlayClip(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
