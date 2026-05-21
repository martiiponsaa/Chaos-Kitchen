using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//using UnityEngine.SceneManagement;

public class FirstLevelTransition : MonoBehaviour
{
    [Header("Audio (preferred)")]
    public AudioMixer audioMixer;
    [Tooltip("Exposed parameter name in the mixer (case-sensitive)")]
    public string musicParameter = "Music";
    [Tooltip("Target dB to reach (e.g. 2 = +2 dB)")]
    public float targetDb = 2f;
    public float musicFadeDuration = 2f;

    [Header("Fallback AudioSource(s)")]
    [Tooltip("Optional: assign music AudioSource(s) if mixer param isn't available")]
    public AudioSource[] musicSources;

    [Header("Screen Fade")]
    public Image fadeImage;
    public float imageFadeDuration = 2f;

    void Start()
    {
        // Ensure there's only one active EventSystem in the loaded scene to avoid warnings
        var systems = FindObjectsOfType<EventSystem>();
        if (systems != null && systems.Length > 1)
        {
            // keep the first, destroy the rest
            for (int i = 1; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].gameObject != null)
                    Destroy(systems[i].gameObject);
            }
        }

        StartCoroutine(StartupRoutine());
    }

    IEnumerator StartupRoutine()
    {
        bool musicDone = false;
        bool imageDone = false;

        if (audioMixer != null && !string.IsNullOrEmpty(musicParameter))
            StartCoroutine(FadeMusicIn(musicFadeDuration, () => musicDone = true));
        else
            StartCoroutine(FadeAudioSourcesIn(musicFadeDuration, () => musicDone = true));

        if (fadeImage != null)
            StartCoroutine(FadeImageOut(imageFadeDuration, () => imageDone = true));
        else
            imageDone = true;

        yield return new WaitUntil(() => musicDone && imageDone);

        //SceneManager.LoadScene("FirstLevel");
    }

    IEnumerator FadeMusicIn(float duration, Action onComplete)
    {
        if (audioMixer == null || string.IsNullOrEmpty(musicParameter))
        {
            onComplete?.Invoke();
            yield break;
        }

        if (!audioMixer.GetFloat(musicParameter, out float startDb))
        {
            Debug.LogWarning($"Mixer parameter '{musicParameter}' not found — falling back to AudioSource fade.");
            StartCoroutine(FadeAudioSourcesIn(duration, onComplete));
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            float db = Mathf.Lerp(startDb, targetDb, f);
            audioMixer.SetFloat(musicParameter, db);
            yield return null;
        }

        audioMixer.SetFloat(musicParameter, targetDb);
        onComplete?.Invoke();
    }

    IEnumerator FadeAudioSourcesIn(float duration, Action onComplete)
    {
        List<AudioSource> sources = new List<AudioSource>();
        if (musicSources != null && musicSources.Length > 0)
            sources.AddRange(musicSources);
        else
        {
            foreach (var s in FindObjectsOfType<AudioSource>())
            {
                if (s.outputAudioMixerGroup != null &&
                    s.outputAudioMixerGroup.name.IndexOf("Music", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sources.Add(s);
                }
            }
        }

        if (sources.Count == 0)
        {
            Debug.LogWarning("No music AudioSource found for fade-in fallback. Skipping audio fade.");
            onComplete?.Invoke();
            yield break;
        }

        float t = 0f;
        float[] starts = new float[sources.Count];
        for (int i = 0; i < sources.Count; i++) starts[i] = sources[i].volume;

        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            for (int i = 0; i < sources.Count; i++)
                sources[i].volume = Mathf.Lerp(starts[i], 1f, f);
            yield return null;
        }

        for (int i = 0; i < sources.Count; i++) sources[i].volume = 1f;
        onComplete?.Invoke();
    }

    IEnumerator FadeImageOut(float duration, Action onComplete)
    {
        if (fadeImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;
        if (!fadeImage.gameObject.activeSelf) fadeImage.gameObject.SetActive(true);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            c.a = Mathf.Lerp(1f, 0f, f);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
        onComplete?.Invoke();
    }
}
