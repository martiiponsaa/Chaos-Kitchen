using System.Collections.Generic;
using UnityEngine;

public class StartAreaManager : MonoBehaviour
{
    public static StartAreaManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Tooltip("Seconds both areas must be occupied simultaneously to trigger start.")]
    public float requiredHoldTime = 3f;

    List<StartArea> _areas = new List<StartArea>();
    float _occupiedTimer = 0f;
    bool _started = false;

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
            _occupiedTimer += Time.deltaTime;
            if (_occupiedTimer >= requiredHoldTime)
            {
                _started = true;
                Debug.Log($"Start areas occupied for {requiredHoldTime} seconds. Start confirmed.");
            }
        }
        else
        {
            if (_occupiedTimer > 0f)
                _occupiedTimer = 0f;
        }
    }
}
