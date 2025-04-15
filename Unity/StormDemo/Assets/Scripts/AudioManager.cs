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

    [Header("Timed Clips")]
    public List<TimedAudioClip> timedAudioClips;

    [Header("Camcorder Reference")]
    public CamcorderScript camcorder;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Dialogue Clips")]
    public List<AudioClip> sufficientFootageClips = new List<AudioClip>();
    public List<AudioClip> insufficientFootageClips = new List<AudioClip>();

    [Header("Dialogue Delays")]
    public float sufficientDialogueDelay = 2f;
    public float insufficientDialogueDelay = 2f;

    // Internal queue of (object, clip) pairs
    private Queue<(GameObject obj, AudioClip clip)> detectionQueue = new Queue<(GameObject, AudioClip)>();

    // Helpers to prevent double‑enqueue or replay
    private HashSet<GameObject> queuedObjects = new HashSet<GameObject>();
    private HashSet<GameObject> playedObjects = new HashSet<GameObject>();

    private float timer = 0f;
    private HashSet<TimedAudioClip> playedTimedClips = new HashSet<TimedAudioClip>();

    void Start()
    {
        if (camcorder != null)
            camcorder.OnObjectDetected += HandleCamcorderDetection;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1) If nothing is playing and queue is empty, run your timed clips
        if (!audioSource.isPlaying && detectionQueue.Count == 0)
        {
            foreach (var t in timedAudioClips)
            {
                if (!playedTimedClips.Contains(t) && timer >= t.triggerTime)
                {
                    PlayClip(t.clip);
                    playedTimedClips.Add(t);
                    break;
                }
            }
        }

        // 2) If nothing is playing but we have detections queued, process the next valid one
        if (!audioSource.isPlaying && detectionQueue.Count > 0)
        {
            while (detectionQueue.Count > 0)
            {
                var (obj, clip) = detectionQueue.Peek();

                // Only play if it's still visible right now
                if (camcorder.IsObjectVisible(obj))
                {
                    PlayClip(clip);
                    playedObjects.Add(obj);
                    queuedObjects.Remove(obj);
                    detectionQueue.Dequeue();
                    StartCoroutine(ResetDetectionAfterPlayback());
                    break;
                }
                else
                {
                    // Drop it and forget so it can re‑enqueue on true re‑entry
                    detectionQueue.Dequeue();
                    queuedObjects.Remove(obj);
                    camcorder.ForgetObject(obj);
                }
            }
        }
    }

    private void HandleCamcorderDetection(GameObject obj, AudioClip clip)
    {
        // Enqueue only if never queued before and never played
        if (clip != null
         && !queuedObjects.Contains(obj)
         && !playedObjects.Contains(obj))
        {
            detectionQueue.Enqueue((obj, clip));
            queuedObjects.Add(obj);
        }
    }

    private IEnumerator ResetDetectionAfterPlayback()
    {
        // Wait until the clip is done
        yield return new WaitUntil(() => !audioSource.isPlaying);
        // Allow the camcorder to fire again
        camcorder.ResetDetection();
    }

    private void PlayClip(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    // Public helper for other scripts
    public void PlayAudioClip(AudioClip clip)
    {
        if (clip != null)
            PlayClip(clip);
    }

    // Public coroutine for dialogue lists
    public IEnumerator PlayDialogueList(List<AudioClip> dialogueList, float delayBetweenClips)
    {
        foreach (AudioClip c in dialogueList)
        {
            PlayAudioClip(c);
            yield return new WaitUntil(() => !audioSource.isPlaying);
            yield return new WaitForSeconds(delayBetweenClips);
        }
    }
}
