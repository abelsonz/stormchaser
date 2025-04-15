using UnityEngine;
using System.Collections;
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

    // Reference to the camcorder script (set via the Inspector)
    public CamcorderScript camcorder;

    // Dedicated AudioSource for playing clips.
    public AudioSource audioSource;

    // Queue to hold detection event clips that couldn’t play immediately.
    private Queue<AudioClip> detectionQueue = new Queue<AudioClip>();

    private float timer = 0f;
    private HashSet<TimedAudioClip> playedTimedClips = new HashSet<TimedAudioClip>();

    // NEW: Lists for dialogue clips.
    public List<AudioClip> sufficientFootageClips = new List<AudioClip>();
    public List<AudioClip> insufficientFootageClips = new List<AudioClip>();

    // NEW: Public delays between dialogue clips.
    public float sufficientDialogueDelay = 2f;
    public float insufficientDialogueDelay = 2f;

    void Start()
    {
        if (camcorder != null)
        {
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
                    Debug.Log("[AudioManager] Playing timed clip: " + timedClip.clip.name);
                    PlayClip(timedClip.clip);
                    playedTimedClips.Add(timedClip);
                    break;
                }
            }
        }
    }

    private void HandleCamcorderDetection(GameObject detectedObject, AudioClip clipFromCamcorder)
    {
        if (clipFromCamcorder != null)
        {
            if (!audioSource.isPlaying && detectionQueue.Count == 0)
            {
                PlayClip(clipFromCamcorder);
            }
            else
            {
                detectionQueue.Enqueue(clipFromCamcorder);
            }
        }
    }

    // Internal method to play an AudioClip.
    private void PlayClip(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    // Public method to trigger a specific AudioClip.
    public void PlayAudioClip(AudioClip clip)
    {
        if (clip != null)
        {
            PlayClip(clip);
        }
    }

    // NEW: Coroutine to play a list of dialogue clips sequentially with a delay.
    public IEnumerator PlayDialogueList(List<AudioClip> dialogueList, float delayBetweenClips)
    {
        foreach (AudioClip clip in dialogueList)
        {
            PlayAudioClip(clip);
            yield return new WaitUntil(() => !audioSource.isPlaying);
            yield return new WaitForSeconds(delayBetweenClips);
        }
    }
}
