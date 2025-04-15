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

    // Reference to FollowCar script (if any)
    private FollowCar followCarScript;

    void Start()
    {
        if (userCameraRig == null && Camera.main != null)
            userCameraRig = Camera.main.transform;

        followCarScript = userCameraRig?.GetComponent<FollowCar>();

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Clone material to avoid modifying shared asset
            sphereMaterial = rend.material;

            // Switch to Transparent mode so alpha works
            SetupMaterialForTransparency(sphereMaterial);

            // Start fully transparent
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
        if (followCamera)
        {
            Vector3 cameraPos = userCameraRig != null ? userCameraRig.position : Camera.main.transform.position;
            transform.position = cameraPos;
        }
    }

    public void StartTeleportSequence(bool sufficientEnding)
    {
        isSufficientEnding = sufficientEnding;
        float delayTime = isSufficientEnding ? sufficientDelay : insufficientDelay;
        StartCoroutine(TeleportAndFadeCoroutine(delayTime));
    }

    private IEnumerator TeleportAndFadeCoroutine(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        if (isSufficientEnding && followCarScript != null)
            followCarScript.enabled = false;

        followCamera = true;

        Vector3 cameraPos = userCameraRig != null ? userCameraRig.position : Camera.main.transform.position;

        if (isSufficientEnding && userCameraRig != null)
        {
            userCameraRig.position += new Vector3(0f, 3f, 0f);
            cameraPos = userCameraRig.position;
        }

        transform.position = cameraPos;

        if (isSufficientEnding && pickupTruck != null)
        {
            pickupTruck.SetParent(null);
            pickupTruck.position = cameraPos + truckTeleportOffset;
        }

        yield return StartCoroutine(FadeAudioAndSphere());
    }

    private IEnumerator FadeAudioAndSphere()
    {
        Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();
        foreach (AudioSource src in audioSourcesToFade)
        {
            if (src != null)
                originalVolumes[src] = src.volume;
        }

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            foreach (AudioSource src in audioSourcesToFade)
            {
                if (src != null && originalVolumes.ContainsKey(src))
                    src.volume = Mathf.Lerp(originalVolumes[src], 0f, t);
            }

            if (sphereMaterial != null)
            {
                Color c = sphereMaterial.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                sphereMaterial.color = c;
            }

            yield return null;
        }

        foreach (AudioSource src in audioSourcesToFade)
        {
            if (src != null)
                src.volume = 0f;
        }

        if (sphereMaterial != null)
        {
            Color c = sphereMaterial.color;
            c.a = 1f;
            sphereMaterial.color = c;
        }
    }

    // Helper to configure a Standard shader material for transparency
    private void SetupMaterialForTransparency(Material mat)
    {
        mat.SetFloat("_Mode", 2);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }
}
