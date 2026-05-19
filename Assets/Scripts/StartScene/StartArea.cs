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
    List<Collider> _lastHits = new List<Collider>();

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            // instantiate material so we don't modify shared material
            _instancedMaterial = targetRenderer.material;
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
        var hitList = new List<Collider>(hits);
        var descriptions = new System.Text.StringBuilder();
        descriptions.Append($"[{gameObject.name}] Overlap hits count={hits.Length}: ");
        foreach (var c in hits)
        {
            descriptions.Append(DescribeCollider(c));
            descriptions.Append("; ");

            if (IsExpectedPlayer(c))
            {
                found = true;
                // keep checking others to provide full log
            }
        }

        // Log detailed hit info whenever hits change
        bool hitsChanged = !AreColliderListsEqual(_lastHits, hitList);
        if (hitsChanged)
        {
            Debug.Log(descriptions.ToString());
            _lastHits = hitList;
        }

        if (found != _previousOccupied)
        {
            _previousOccupied = found;
            IsOccupied = found;
            SetAlpha(found ? highlightAlpha : normalAlpha);
            if (StartAreaManager.HasInstance)
                StartAreaManager.Instance.NotifyAreaChanged(this, found);

            if (found)
            {
                // Identify which expected player(s) are present and whether others are present
                var expectedPresent = new System.Text.StringBuilder();
                var othersPresent = new System.Text.StringBuilder();
                foreach (var c in hits)
                {
                    var id = c.GetComponent<PlayerIdentifier>();
                    if (id != null)
                    {
                        if (id.PlayerId == expectedPlayerId)
                            expectedPresent.Append($"PlayerId={id.PlayerId} ");
                        else
                            othersPresent.Append($"PlayerId={id.PlayerId} ");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(expectedPlayerTag) && c.CompareTag(expectedPlayerTag))
                            expectedPresent.Append($"Tag={expectedPlayerTag} ");
                        else
                            othersPresent.Append($"Obj={DescribeCollider(c)} ");
                    }
                }

                Debug.Log($"[{gameObject.name}] Detected expected player? {expectedPresent.Length>0}. Expected={expectedPlayerId}. ExpectedPresent=[{expectedPresent}] Others=[{othersPresent}]");
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Area no longer occupied (player left or moved out of detection zone).");
            }
        }
    }

    static bool AreColliderListsEqual(List<Collider> a, List<Collider> b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    string DescribeCollider(Collider c)
    {
        if (c == null) return "<null>";
        var id = c.GetComponent<PlayerIdentifier>();
        if (id != null) return $"GameObject={c.gameObject.name}(PlayerId={id.PlayerId})";
        if (!string.IsNullOrEmpty(c.gameObject.tag)) return $"GameObject={c.gameObject.name}(Tag={c.gameObject.tag})";
        return $"GameObject={c.gameObject.name}";
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
