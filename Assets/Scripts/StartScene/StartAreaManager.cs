using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// Timer is provided by TimersMadeEasyLite package in the project (global namespace)

public class StartAreaManager : MonoBehaviour
{
    public static StartAreaManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Tooltip("Seconds both areas must be occupied simultaneously to trigger start.")]
    public float requiredHoldTime = 3f;

    List<StartArea> _areas = new List<StartArea>();
    bool _started = false;
    Coroutine _countdownCoroutine = null;
    [Header("Timer")]
    [Tooltip("Optional: UI Timer to start when the start countdown finishes. If empty will try to find one in the scene.")]
    public Timer levelTimer;

    [Header("Fade & Transition")]
    [Tooltip("AudioMixer with exposed Music volume parameter (in dB)")]
    public AudioMixer audioMixer;
    [Tooltip("Name of the exposed volume parameter in the mixer controlling music")] 
    public string musicParameter = "Music";
    [Tooltip("UI Image used for fade-to-black. Alpha is expected 0..1.")]
    public Image fadeImage;
    [Tooltip("Seconds to fade the image to black")] public float imageFadeDuration = 2f;
    [Tooltip("Seconds to fade music to mute")] public float musicFadeDuration = 2f;
    [Tooltip("Minimum dB value to treat as silent (e.g. -80 dB)")]
    public float muteDb = -80f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // find existing areas
        var found = FindObjectsByType<StartArea>(FindObjectsSortMode.None);
        foreach (var a in found)
            RegisterArea(a);

        // Find a Timer in the scene if none assigned and disable its auto-start
        if (levelTimer == null)
        {
            var timers = FindObjectsByType<Timer>(FindObjectsSortMode.None);
            if (timers != null && timers.Length > 0)
                levelTimer = timers[0];
        }

        if (levelTimer != null)
        {
            // Prevent the timer from auto-starting at runtime; we'll start it when countdown completes
            levelTimer.startAtRuntime = false;
            // Ensure it's stopped at scene start
            levelTimer.StopTimer();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterArea(StartArea area)
    {
        if (!_areas.Contains(area))
            _areas.Add(area);
    }

    public void UnregisterArea(StartArea area)
    {
        if (_areas.Contains(area))
            _areas.Remove(area);
    }

    public void NotifyAreaChanged(StartArea area, bool occupied)
    {
        // We can react immediately in Update loop, nothing required here for now.
    }

    void Update()
    {
        if (_started) return;
        if (_areas.Count == 0) return;

        bool allOccupied = true;
        foreach (var a in _areas)
        {
            if (!a.IsOccupied)
            {
                allOccupied = false;
                break;
            }
        }

        if (allOccupied)
        {
            if (_countdownCoroutine == null)
            {
                // Show and start the level UI timer immediately when both players are present
                if (levelTimer != null)
                {
                    if (!levelTimer.gameObject.activeSelf)
                        levelTimer.gameObject.SetActive(true);
                    levelTimer.StartTimer();
                    Debug.Log("Players present: level timer shown and started.");
                }

                _countdownCoroutine = StartCoroutine(RunCountdown());
            }
        }
        else
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;

                // Occupancy broken during countdown: stop and hide timer, reset
                if (levelTimer != null)
                {
                    levelTimer.StopTimer();
                    if (levelTimer.gameObject.activeSelf)
                        levelTimer.gameObject.SetActive(false);
                    Debug.Log("Countdown interrupted: level timer stopped and hidden.");
                }
            }
        }
    }

    System.Collections.IEnumerator RunCountdown()
    {
        int seconds = Mathf.CeilToInt(requiredHoldTime);
        for (int s = seconds; s >= 1; s--)
        {
            Debug.Log(s);
            yield return new WaitForSeconds(1f);

            // if any area is no longer occupied, cancel countdown
            foreach (var a in _areas)
            {
                if (!a.IsOccupied)
                {
                    _countdownCoroutine = null;
                    yield break;
                }
            }
        }

        _started = true;
        _countdownCoroutine = null;
        Debug.Log($"Start areas occupied for {requiredHoldTime} seconds. Start confirmed.");

        if (levelTimer != null)
        {
            Debug.Log("Start confirmed. Stopping and hiding level timer, beginning transition.");
            // Stop and hide the timer UI immediately when countdown completes
            levelTimer.StopTimer();
            if (levelTimer.gameObject.activeSelf)
                levelTimer.gameObject.SetActive(false);
        }

        // Begin fading music and screen, then load next scene when complete
        StartCoroutine(FadeAndLoadFirstLevel());
    }

    System.Collections.IEnumerator FadeAndLoadFirstLevel()
    {
        bool musicDone = false;
        bool imageDone = false;

        if (audioMixer != null)
            StartCoroutine(FadeOutMusicCoroutine(musicFadeDuration, () => musicDone = true));
        else
            musicDone = true; // nothing to fade

        if (fadeImage != null)
            StartCoroutine(FadeImageCoroutine(imageFadeDuration, () => imageDone = true));
        else
            imageDone = true;

        // wait until both are finished
        yield return new WaitUntil(() => musicDone && imageDone);

        // safety small delay to ensure audio stops
        yield return null;

        Debug.Log("Fade complete — loading FirstLevel scene.");
        SceneManager.LoadScene("FirstLevel");
    }

    System.Collections.IEnumerator FadeOutMusicCoroutine(float duration, System.Action onComplete)
    {
        if (audioMixer == null || string.IsNullOrEmpty(musicParameter))
        {
            onComplete?.Invoke();
            yield break;
        }

        // read current dB value; if not present assume 0 dB
        if (!audioMixer.GetFloat(musicParameter, out float startDb))
            startDb = 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            float db = Mathf.Lerp(startDb, muteDb, f);
            audioMixer.SetFloat(musicParameter, db);
            yield return null;
        }

        audioMixer.SetFloat(musicParameter, muteDb);
        onComplete?.Invoke();
    }

    System.Collections.IEnumerator FadeImageCoroutine(float duration, System.Action onComplete)
    {
        if (fadeImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // ensure image is enabled and starts with alpha 0
        var c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
        if (!fadeImage.gameObject.activeSelf)
            fadeImage.gameObject.SetActive(true);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            c.a = Mathf.Lerp(0f, 1f, f);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
        onComplete?.Invoke();
    }
}
