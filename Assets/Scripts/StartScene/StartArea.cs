using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StartArea : MonoBehaviour
{
    [Tooltip("Player ID expected to occupy this area. If a PlayerIdentifier component exists on the colliding object, its PlayerId will be compared.")]
    public int expectedPlayerId = 1;

    [Tooltip("Optional: fallback tag to match the player GameObject (e.g. Player1). Leave empty to ignore tag check.")]
    public string expectedPlayerTag = "";

    [Tooltip("Renderer to change alpha on for highlight. If empty, will try to get a Renderer on this GameObject.")]
    public Renderer targetRenderer;

    [Header("Material Swap")]
    [Tooltip("Optional material to apply when the expected player is in the area. If empty, alpha method is used.")]
    public Material highlightMaterial;

    Material _originalMaterial;

    [Tooltip("Alpha to set when highlighted (player is inside)")]
    [Range(0f,1f)]
    public float highlightAlpha = 0.8f;

    [Tooltip("Alpha to set when not highlighted")]
    [Range(0f,1f)]
    public float normalAlpha = 0.25f;

    [HideInInspector]
    public bool IsOccupied { get; private set; }

    [Header("Upward detection")]
    [Tooltip("Height above the area to check for players (meters)")]
    public float detectionHeight = 2f;

    [Tooltip("Radius around the area's center to detect players")]
    public float detectionRadius = 0.5f;

    [Tooltip("Layers to include when checking for players")]
    public LayerMask detectionMask = ~0;

    bool _previousOccupied = false;

    Material _instancedMaterial;

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            // cache original material (material creates an instance if needed)
            _originalMaterial = targetRenderer.material;
            _instancedMaterial = _originalMaterial;
            // if no highlightMaterial provided, use alpha on the instanced material
            if (highlightMaterial == null)
                SetAlpha(normalAlpha);
        }

        // Register with manager (manager will find areas automatically too)
        if (StartAreaManager.HasInstance)
            StartAreaManager.Instance.RegisterArea(this);
    }

    void OnDestroy()
    {
        if (StartAreaManager.HasInstance)
            StartAreaManager.Instance.UnregisterArea(this);
    }

    void SetAlpha(float a)
    {
        if (_instancedMaterial == null) return;
        if (_instancedMaterial.HasProperty("_Color"))
        {
            Color c = _instancedMaterial.color;
            c.a = a;
            _instancedMaterial.color = c;
        }
    }

    void Update()
    {
        // Perform an overlap capsule from this position upwards to detect anything above the area
        Vector3 bottom = transform.position;
        Vector3 top = transform.position + Vector3.up * detectionHeight;

        Collider[] hits = Physics.OverlapCapsule(bottom, top, detectionRadius, detectionMask, QueryTriggerInteraction.Collide);

        bool found = false;
        foreach (var c in hits)
        {
            if (IsExpectedPlayer(c))
            {
                found = true;
                break;
            }
        }

        if (found != _previousOccupied)
        {
            _previousOccupied = found;
            IsOccupied = found;
            // If a highlight material is provided, swap materials. Otherwise adjust alpha.
            if (targetRenderer != null && highlightMaterial != null)
            {
                if (found)
                    targetRenderer.material = highlightMaterial;
                else
                    targetRenderer.material = _originalMaterial;
            }
            else
            {
                SetAlpha(found ? highlightAlpha : normalAlpha);
            }
            if (StartAreaManager.HasInstance)
                StartAreaManager.Instance.NotifyAreaChanged(this, found);

            // Only log when the expected player enters the area (not on exit)
            if (found)
            {
                Debug.Log($"[{gameObject.name}] Expected player {expectedPlayerId} entered area.");
            }
        }
    }


    bool IsExpectedPlayer(Collider other)
    {
        if (other == null) return false;

        // Check for PlayerIdentifier component first
        var idComp = other.GetComponent<PlayerIdentifier>();
        if (idComp != null)
            return idComp.PlayerId == expectedPlayerId;

        // Fallback to tag check if provided
        if (!string.IsNullOrEmpty(expectedPlayerTag))
            return other.CompareTag(expectedPlayerTag);

        return false;
    }
}
