using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelFlowManager : MonoBehaviour
{
    public static LevelFlowManager Instance { get; private set; }

    private class TimerWarningState
    {
        public Graphic graphic;
        public RectTransform rectTransform;
        public Vector2 baseAnchoredPosition;
        public Vector3 baseScale;
        public Color baseColor;
        public bool captured;

        public void Capture(Graphic targetGraphic)
        {
            if (targetGraphic == null)
            {
                return;
            }

            graphic = targetGraphic;
            rectTransform = targetGraphic.rectTransform;
            baseColor = targetGraphic.color;

            if (rectTransform != null)
            {
                baseAnchoredPosition = rectTransform.anchoredPosition;
                baseScale = rectTransform.localScale;
            }

            captured = true;
        }

        public void Reset()
        {
            if (!captured)
            {
                return;
            }

            if (graphic != null)
            {
                graphic.color = baseColor;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = baseAnchoredPosition;
                rectTransform.localScale = baseScale;
            }
        }
    }

    [System.Serializable]
    public class DishObjective
    {
        public string label;
        public PlayerInteraction player;
        public Dish dish;
        [HideInInspector] public bool completed;
    }

    [Header("Phase Timers")]
    [SerializeField] private float prepCountdownSeconds = 10f;
    [SerializeField] private float levelDurationSeconds = 180f;
    [SerializeField] private Timer prepTimer;
    [SerializeField] private Timer levelTimer;

    [Header("Players")]
    [SerializeField] private List<PlayerInteraction> players = new List<PlayerInteraction>();

    [Header("Objectives")]
    [SerializeField] private List<DishObjective> objectives = new List<DishObjective>();

    [Header("Failure")]
    [SerializeField] private float reloadDelaySeconds = 7f;

    [Header("Warning FX")]
    [SerializeField] private float prepWarningThresholdSeconds = 3f;
    [SerializeField] private Color prepWarningColor = new Color(0.35f, 0.95f, 0.35f, 1f);
    [SerializeField] private float prepWarningScaleBoost = 0.18f;
    [SerializeField] private float prepWarningShake = 4f;
    [SerializeField] private float prepWarningPulseSpeed = 4f;
    [SerializeField] private float failWarningThresholdSeconds = 3f;
    [SerializeField] private Color failWarningColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float failWarningScaleBoost = 0.3f;
    [SerializeField] private float failWarningShake = 8f;
    [SerializeField] private float failWarningPulseSpeed = 8f;

    [Header("Events")]
    [SerializeField] private UnityEvent onLevelStarted;
    [SerializeField] private UnityEvent onLevelCompleted;
    [SerializeField] private UnityEvent onLevelFailed;

    [Header("Sounds")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioMixerGroup sfxOutputGroup;
    private AudioSource audioSource;

    [Header("UI Text")]
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject loseText;

    private bool prepFinished;
    private bool levelActive;
    private bool levelEnded;
    private Coroutine reloadCoroutine;
    private readonly TimerWarningState prepTimerWarning = new TimerWarningState();
    private readonly TimerWarningState levelTimerWarning = new TimerWarningState();

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = sfxOutputGroup;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
    }

    private void Start()
    {
        ResolveReferences();
        PrepareInteractionState(true);
        ConfigureTimers();
        StartPrepPhase();
    }

    private void Update()
    {
        if (levelEnded)
        {
            ResetTimerWarning(prepTimerWarning);
            ResetTimerWarning(levelTimerWarning);
            return;
        }

        UpdateTimerWarning(prepTimer, prepWarningThresholdSeconds, prepWarningColor, prepWarningScaleBoost, prepWarningShake, prepWarningPulseSpeed, prepTimerWarning);
        UpdateTimerWarning(levelTimer, failWarningThresholdSeconds, failWarningColor, failWarningScaleBoost, failWarningShake, failWarningPulseSpeed, levelTimerWarning);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (prepTimer != null)
        {
            prepTimer.onTimerEnd.RemoveListener(HandlePrepTimerEnded);
        }

        if (levelTimer != null)
        {
            levelTimer.onTimerEnd.RemoveListener(HandleLevelTimerEnded);
        }
    }

    private void ResolveReferences()
    {
        if (players == null || players.Count == 0)
        {
            players = new List<PlayerInteraction>(FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None));
        }

        if (prepTimer == null || levelTimer == null)
        {
            Timer[] timers = FindObjectsByType<Timer>(FindObjectsSortMode.None);

            if (prepTimer == null && timers.Length > 0)
            {
                prepTimer = timers[0];
            }

            if (levelTimer == null && timers.Length > 1)
            {
                levelTimer = timers[1];
            }
        }

        if (prepTimer == null)
        {
            Debug.LogWarning("LevelFlowManager: prep timer not assigned.");
        }

        if (levelTimer == null)
        {
            Debug.LogWarning("LevelFlowManager: level timer not assigned.");
        }
    }

    private void ConfigureTimers()
    {
        ConfigureTimer(prepTimer, prepCountdownSeconds, false);
        ConfigureTimer(levelTimer, levelDurationSeconds, false);

        if (prepTimer != null)
        {
            prepTimer.onTimerEnd.RemoveListener(HandlePrepTimerEnded);
            prepTimer.onTimerEnd.AddListener(HandlePrepTimerEnded);
        }

        if (levelTimer != null)
        {
            levelTimer.onTimerEnd.RemoveListener(HandleLevelTimerEnded);
            levelTimer.onTimerEnd.AddListener(HandleLevelTimerEnded);
        }
    }

    private void ConfigureTimer(Timer timer, float totalSeconds, bool visible)
    {
        if (timer == null) return;

        timer.StopTimer();
        timer.startAtRuntime = false;
        timer.countMethod = Timer.CountMethod.CountDown;

        int seconds = Mathf.Max(1, Mathf.RoundToInt(totalSeconds));
        timer.hours = seconds / 3600;
        timer.minutes = (seconds % 3600) / 60;
        timer.seconds = seconds % 60;

        if (timer.gameObject.activeSelf != visible)
        {
            timer.gameObject.SetActive(visible);
        }
    }

    private void StartPrepPhase()
    {
        if (prepTimer == null)
        {
            HandlePrepTimerEnded();
            return;
        }

        prepTimer.gameObject.SetActive(true);
        prepTimer.StartTimer();
    }

    private void HandlePrepTimerEnded()
    {
        if (prepFinished) return;

        prepFinished = true;

        if (prepTimer != null)
        {
            prepTimer.StopTimer();
            prepTimer.gameObject.SetActive(false);
        }

        ResetTimerWarning(prepTimerWarning);

        PrepareInteractionState(false);

        if (levelTimer != null)
        {
            levelTimer.gameObject.SetActive(true);
            levelTimer.StartTimer();
        }

        levelActive = true;
        onLevelStarted?.Invoke();
    }

    private void HandleLevelTimerEnded()
    {
        if (levelEnded) return;

        if (!AreAllObjectivesCompleted())
        {
            FailLevel();
        }
        else
        {
            CompleteLevel();
        }
    }

    public void NotifyDishDelivered(Dish deliveredDish, PlayerInteraction deliveringPlayer)
    {
        if (!prepFinished || levelEnded || deliveredDish == null)
        {
            return;
        }

        bool changed = false;

        for (int i = 0; i < objectives.Count; i++)
        {
            DishObjective objective = objectives[i];

            if (objective == null || objective.completed)
            {
                continue;
            }

            if (objective.dish != deliveredDish)
            {
                continue;
            }

            if (objective.player != null && objective.player != deliveringPlayer)
            {
                Debug.LogWarning($"LevelFlowManager: {deliveredDish.name} was delivered by the wrong player.");
                return;
            }

            objective.completed = true;
            changed = true;
            Debug.Log($"LevelFlowManager: objective completed for {deliveredDish.name}");
            break;
        }

        if (changed && AreAllObjectivesCompleted())
        {
            CompleteLevel();
        }
    }

    private bool AreAllObjectivesCompleted()
    {
        if (objectives == null || objectives.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < objectives.Count; i++)
        {
            DishObjective objective = objectives[i];
            if (objective == null || objective.dish == null || !objective.completed)
            {
                return false;
            }
        }

        return true;
    }

    private void CompleteLevel()
    {
        if (levelEnded) return;

        levelEnded = true;
        levelActive = false;

        StopTimersAndHide();
        PrepareInteractionState(true);

        if (winSound != null) audioSource.PlayOneShot(winSound);
        if (winText != null) winText.SetActive(true);

        Debug.Log("Level complete: all objectives delivered.");
        onLevelCompleted?.Invoke();

        //Per carregar els nivells nous 
        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(LoadNextSceneAfterDelay());

    }

    private void FailLevel()
    {
        if (levelEnded) return;

        levelEnded = true;
        levelActive = false;

        StopTimersAndHide();
        PrepareInteractionState(true);

        Debug.Log("Level failed: time ran out before all objectives were delivered.");
        onLevelFailed?.Invoke();

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }

        if (loseSound != null) audioSource.PlayOneShot(loseSound);
        if (loseText != null) loseText.SetActive(true);

        reloadCoroutine = StartCoroutine(ReloadSceneAfterDelay()); //com que aix� es crida si perds ja est� b� que el nivell es reinici. 
    }

    private void StopTimersAndHide()
    {
        if (prepTimer != null)
        {
            prepTimer.StopTimer();
            prepTimer.gameObject.SetActive(false);
        }

        ResetTimerWarning(prepTimerWarning);

        if (levelTimer != null)
        {
            levelTimer.StopTimer();
            levelTimer.gameObject.SetActive(false);
        }

        ResetTimerWarning(levelTimerWarning);
    }

    private void UpdateTimerWarning(Timer timer, float thresholdSeconds, Color warningColor, float scaleBoost, float shakeAmount, float pulseSpeed, TimerWarningState warningState)
    {
        if (timer == null || !timer.gameObject.activeInHierarchy || timer.countMethod != Timer.CountMethod.CountDown)
        {
            ResetTimerWarning(warningState);
            return;
        }

        Graphic targetGraphic = timer.textMeshProText != null ? timer.textMeshProText : timer.standardText;
        if (targetGraphic == null)
        {
            ResetTimerWarning(warningState);
            return;
        }

        float remainingSeconds = (float)timer.GetRemainingSeconds();
        if (remainingSeconds <= 0f || remainingSeconds > thresholdSeconds)
        {
            ResetTimerWarning(warningState);
            return;
        }

        if (!warningState.captured || warningState.graphic != targetGraphic)
        {
            warningState.Capture(targetGraphic);
        }

        if (warningState.rectTransform == null)
        {
            return;
        }

        float intensity = 1f - Mathf.Clamp01(remainingSeconds / thresholdSeconds);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
        float scale = 1f + (scaleBoost * intensity * pulse);
        Vector2 shakeOffset = Random.insideUnitCircle * (shakeAmount * intensity * pulse);

        warningState.graphic.color = Color.Lerp(warningState.baseColor, warningColor, intensity);
        warningState.rectTransform.anchoredPosition = warningState.baseAnchoredPosition + shakeOffset;
        warningState.rectTransform.localScale = warningState.baseScale * scale;
    }

    private void ResetTimerWarning(TimerWarningState warningState)
    {
        if (warningState == null || !warningState.captured)
        {
            return;
        }

        warningState.Reset();
    }

    private void PrepareInteractionState(bool locked)
    {
        if (players == null || players.Count == 0)
        {
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            PlayerInteraction player = players[i];
            if (player == null) continue;
            player.enabled = !locked;
        }
    }

    private IEnumerator ReloadSceneAfterDelay()
    {
        yield return new WaitForSeconds(reloadDelaySeconds);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    //funci� per cridar la seguent escena en comptes del mateix per si es guanya. 
    private IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(reloadDelaySeconds);

        Scene currentScene = SceneManager.GetActiveScene();
        int nextSceneIndex = currentScene.buildIndex + 1;

        // Comprovem si hi ha un seg�ent nivell a la llista de Build Settings
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("LevelFlowManager: No hi ha m�s nivells introdu�ts al Build Settings! Tornant al men� o primer nivell.");
            // Opcional: Aqu� pots carregar l'escena 0 (men� principal) si s'ha acabat el joc:
            // SceneManager.LoadScene(0);
        }
    }
}