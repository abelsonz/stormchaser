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

    [System.Serializable]
    public class CamcorderAudioClip
    {
        public GameObject targetObject;
        public AudioClip clip;
    }

    public List<TimedAudioClip> timedAudioClips;
    public List<CamcorderAudioClip> camcorderAudioClips;

    // Reference to the camcorder script (ensure this is set via the Inspector)
    public CamcorderScript camcorder;

    // Dedicated AudioSource for playing clips.
    public AudioSource audioSource;

    private float timer = 0f;
    private HashSet<TimedAudioClip> playedTimedClips = new HashSet<TimedAudioClip>();

    // Tracks the last played camcorder audio clip to avoid repetition.
    private CamcorderAudioClip lastPlayedCamcorderClip = null;

    void Start()
    {
        if (camcorder != null)
        {
            // Subscribe to the camcorder detection event so we get notified when an object enters the viewfinder.
            camcorder.OnObjectDetected += HandleCamcorderDetection;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Process timed audio clips if nothing is already playing.
        if (!audioSource.isPlaying)
        {
            foreach (var timedClip in timedAudioClips)
            {
                if (!playedTimedClips.Contains(timedClip) && timer >= timedClip.triggerTime)
                {
                    PlayClip(timedClip.clip);
                    playedTimedClips.Add(timedClip);
                    // Break out to let this clip play before scheduling additional ones.
                    break;
                }
            }
        }
    }

    // Handles the event from the camcorder when a new object is detected.
    private void HandleCamcorderDetection(GameObject detectedObject, AudioClip clipFromCamcorder)
    {
        // Look up an associated clip from our custom list, allowing you to override the camcorder's clip if desired.
        CamcorderAudioClip matchingClip = camcorderAudioClips.Find(c => c.targetObject == detectedObject);

        // If there's no matching clip or it's the same as the one we just played, do nothing.
        if (matchingClip == null || matchingClip == lastPlayedCamcorderClip)
            return;

        // Only play the triggered clip if no other audio is playing.
        if (!audioSource.isPlaying)
        {
            PlayClip(matchingClip.clip);
            lastPlayedCamcorderClip = matchingClip;
        }
    }

    // Plays a given AudioClip using the designated AudioSource.
    private void PlayClip(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
