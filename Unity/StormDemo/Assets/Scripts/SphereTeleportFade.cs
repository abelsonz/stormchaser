using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SphereTeleportFade : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Delay in seconds for sufficient ending.")]
    public float sufficientDelay = 3f;
    [Tooltip("Delay in seconds for insufficient ending.")]
    public float insufficientDelay = 15f;
    
    [Header("Fade Settings")]
    [Tooltip("Duration over which the sphere and audio fade.")]
    public float fadeDuration = 2.0f;
    [Tooltip("List of audio sources to fade out.")]
    public List<AudioSource> audioSourcesToFade;

    [Header("Camera Rig Reference")]
    [Tooltip("User camera rig reference (defaults to Camera.main if left empty).")]
    public Transform userCameraRig;

    // Cached material for the sphere
    private Material sphereMaterial;

    // Ending flag set through method parameter or externally.
    private bool isSufficientEnding = true;

    void Start()
    {
        // Use main camera if no camera rig is assigned.
        if (userCameraRig == null && Camera.main != null)
        {
            userCameraRig = Camera.main.transform;
        }

        // Set up the sphere material and make it fully transparent initially.
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            sphereMaterial = rend.material;
            Color c = sphereMaterial.color;
            c.a = 0f;
            sphereMaterial.color = c;
        }
        else
        {
            Debug.LogError("SphereTeleportFade: Renderer not found!");
        }
    }

    /// <summary>
    /// Call this method (or trigger via an event) when your dialogues complete.
    /// </summary>
    /// <param name="sufficientEnding">True for sufficient footage ending; false for insufficient.</param>
    public void StartTeleportSequence(bool sufficientEnding)
    {
        isSufficientEnding = sufficientEnding;
        float delayTime = isSufficientEnding ? sufficientDelay : insufficientDelay;
        StartCoroutine(TeleportAndFadeCoroutine(delayTime));
    }

    private IEnumerator TeleportAndFadeCoroutine(float delayTime)
    {
        // Wait for the appropriate delay after dialogue finishes.
        yield return new WaitForSeconds(delayTime);

        // Teleport sphere to the user's camera rig.
        if (userCameraRig != null)
        {
            transform.position = userCameraRig.position;
        }
        else
        {
            Debug.LogError("SphereTeleportFade: User camera rig not assigned!");
        }

        // Start the fading effect.
        yield return StartCoroutine(FadeAudioAndSphere());
    }

    private IEnumerator FadeAudioAndSphere()
    {
        // Capture the original volumes of each audio source.
        Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();
        foreach (AudioSource src in audioSourcesToFade)
        {
            if (src != null)
            {
                originalVolumes[src] = src.volume;
            }
        }

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            // Fade each audio source to zero.
            foreach (AudioSource src in audioSourcesToFade)
            {
                if (src != null && originalVolumes.ContainsKey(src))
                {
                    src.volume = Mathf.Lerp(originalVolumes[src], 0f, t);
                }
            }

            // Fade sphere from transparent to opaque.
            if (sphereMaterial != null)
            {
                Color c = sphereMaterial.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                sphereMaterial.color = c;
            }

            yield return null;
        }

        // Ensure final values are set.
        foreach (AudioSource src in audioSourcesToFade)
        {
            if (src != null)
            {
                src.volume = 0f;
            }
        }
        if (sphereMaterial != null)
        {
            Color c = sphereMaterial.color;
            c.a = 1f;
            sphereMaterial.color = c;
        }
    }
}
