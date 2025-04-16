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

    public GameObject worldParent;
    public GameObject introEnvironment;
    public GameObject endEnvironment;

    showEnding ending;

   /// Cached material for the sphere.
    private Material sphereMaterial;

    // Ending flag.
    private bool isSufficientEnding = true;

    // Flag to indicate that the sphere should follow the camera rig every frame.
    private bool followCamera = false;

    // Reference to FollowCar script (if any)
    private FollowCar followCarScript;

    MeshRenderer mr;

    Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        mr.enabled = true;
        endEnvironment.SetActive(false);
        introEnvironment.SetActive(true);

        foreach (AudioSource src in audioSourcesToFade)
        {
            if (src != null)
                originalVolumes[src] = src.volume;
            src.volume = 0;
        }

        ending = endEnvironment.GetComponent<showEnding>();
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Clone material to avoid modifying shared asset
            sphereMaterial = rend.material;

            // Switch to Transparent mode so alpha works
            SetupMaterialForTransparency(sphereMaterial);

            // Start fully transparent
            Color c = sphereMaterial.color;
            c.a = 1f;
            sphereMaterial.color = c;
        }
        else
        {
            Debug.LogError("SphereTeleportFade: Renderer not found!");
        }

       //StartIntroSequence();
        
    }

    void Update()
    {
       

    }

    public void StartEndingSequence(bool sufficientEnding)
    {
        worldParent.SetActive(false);
        mr.enabled = true;
        if (sphereMaterial != null)
        {
            Color c = sphereMaterial.color;
            c.a = 0f;
            sphereMaterial.color = c;
        }       
        StartCoroutine(TeleportAndFadeOutCoroutine(sufficientEnding));
    }

    private IEnumerator TeleportAndFadeOutCoroutine(bool sufficientEnding)
    {
        float delayTime = isSufficientEnding ? sufficientDelay : insufficientDelay;
        yield return new WaitForSeconds(delayTime);
        yield return StartCoroutine(FadeOutAudioAndSphere(sufficientEnding));
    }

    public void StartIntroSequence()
    {
         StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        yield return StartCoroutine(FadeInAudioAndSphere());
    }

    private IEnumerator FadeInAudioAndSphere()
    {
        //turn off intro env.
        introEnvironment.SetActive(false);

        //fade in
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            foreach (AudioSource src in audioSourcesToFade)
            {
                if (src != null && originalVolumes.ContainsKey(src))
                    src.volume = Mathf.Lerp(0f,originalVolumes[src], t*t);
            }

            if (sphereMaterial != null)
            {
                Color c = sphereMaterial.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sphereMaterial.color = c;
            }

            yield return null;
        }

        //disable fade plane
        mr.enabled = false;

    }

    private IEnumerator FadeOutAudioAndSphere(bool type)
    {
        
        //start fading out
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

        //activate end environment
        endEnvironment.SetActive(true);
        ending.ShowPoster(type);
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
        mat.renderQueue = 50000;
    }
}
