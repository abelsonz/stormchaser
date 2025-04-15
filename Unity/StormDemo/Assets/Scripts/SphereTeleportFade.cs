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
    [Tooltip("User camera rig reference (defaults to Camera.main if left empty)." )]
    public Transform userCameraRig;
    
    [Header("Additional Objects")]
    [Tooltip("Reference to the pickup truck object that should be teleported away on sufficient ending.")]
    public Transform pickupTruck;
    [Tooltip("Offset vector to use when teleporting the pickup truck away from the user.")]
    public Vector3 truckTeleportOffset = new Vector3(50f, 0f, 0f);

    // Cached material for the sphere.
    private Material sphereMaterial;
    
    // Ending flag.
    private bool isSufficientEnding = true;
    
    // Flag to indicate that the sphere should follow the camera rig every frame.
    private bool followCamera = false;
    
    void Start()
    {
        // Use Camera.main if no camera rig is assigned.
        if (userCameraRig == null && Camera.main != null)
        {
            userCameraRig = Camera.main.transform;
        }
    
        // Get the Renderer and set the material to fully transparent.
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
    
    void Update()
    {
        // Once followCamera is enabled, update the sphere's position every frame.
        if (followCamera)
        {
            Vector3 cameraPos = (userCameraRig != null ? userCameraRig.position : Camera.main.transform.position);
            transform.position = cameraPos;
        }
    }
    
    /// <summary>
    /// Trigger the teleport sequence. Once called, after the delay the sphere
    /// will continuously follow the camera rig and execute the fade effect.
    /// Additionally, if the sufficient ending is triggered, the pickup truck is teleported away.
    /// </summary>
    /// <param name="sufficientEnding">True for sufficient ending; false for insufficient.</param>
    public void StartTeleportSequence(bool sufficientEnding)
    {
        Debug.Log("StartTeleportSequence called. Sufficient: " + sufficientEnding);
        isSufficientEnding = sufficientEnding;
        float delayTime = isSufficientEnding ? sufficientDelay : insufficientDelay;
        Debug.Log("Waiting for delay: " + delayTime + " seconds before teleporting.");
        StartCoroutine(TeleportAndFadeCoroutine(delayTime));
    }
    
    private IEnumerator TeleportAndFadeCoroutine(float delayTime)
    {
        // Wait for the appropriate delay after dialogue completes.
        yield return new WaitForSeconds(delayTime);
        
        // Enable continuous following of the camera rig.
        followCamera = true;
        
        // Determine camera position.
        Vector3 cameraPos = (userCameraRig != null ? userCameraRig.position : Camera.main.transform.position);

        // Immediately update sphere position.
        transform.position = cameraPos;
        Debug.Log("Teleporting sphere to userCameraRig position: " + cameraPos);
        
        // If this is a sufficient ending, teleport the pickup truck away.
        if (isSufficientEnding && pickupTruck != null)
        {
            // Detach pickup truck from any parent to avoid moving with camera rig.
            pickupTruck.SetParent(null);
            Vector3 truckPos = cameraPos + truckTeleportOffset;
            pickupTruck.position = truckPos;
            Debug.Log("Teleporting pickup truck away to position: " + truckPos);
        }
    
        // Start the fade effect (from transparent to opaque).
        yield return StartCoroutine(FadeAudioAndSphere());
    }
    
    private IEnumerator FadeAudioAndSphere()
    {
        // Store the original volumes.
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
    
            // Fade audio sources.
            foreach (AudioSource src in audioSourcesToFade)
            {
                if (src != null && originalVolumes.ContainsKey(src))
                {
                    src.volume = Mathf.Lerp(originalVolumes[src], 0f, t);
                }
            }
    
            // Fade sphere material from transparent to opaque.
            if (sphereMaterial != null)
            {
                Color c = sphereMaterial.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                sphereMaterial.color = c;
            }
    
            yield return null;
        }
    
        // Ensure final volumes and alpha are set.
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
