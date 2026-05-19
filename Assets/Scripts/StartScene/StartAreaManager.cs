using System.Collections.Generic;
using UnityEngine;

public class StartAreaManager : MonoBehaviour
{
    public static StartAreaManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Tooltip("Seconds both areas must be occupied simultaneously to trigger start.")]
    public float requiredHoldTime = 3f;

    List<StartArea> _areas = new List<StartArea>();
    bool _started = false;
    Coroutine _countdownCoroutine = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // find existing areas
        var found = FindObjectsOfType<StartArea>();
        foreach (var a in found)
            RegisterArea(a);
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
                _countdownCoroutine = StartCoroutine(RunCountdown());
        }
        else
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
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
    }
}
