using UnityEngine;

/// <summary>
/// Script for objects that can be picked up and placed in the game.
/// These are cooking ingredients, utensils, and other props in the restaurant game.
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("Hold Settings")]
    [Tooltip("Height offset when held by player (prevents collision with other objects)")]
    [SerializeField] private float heldYOffset = 1.0f;

    [Header("Placement Settings")]
    [Tooltip("Y position threshold below which the object can be placed")]
    [SerializeField] private float placementYThreshold = 0.1f;
    
    [Tooltip("Use physics raycast to detect surface below for proper placement")]
    [SerializeField] private bool usePhysicsPlacement = false;
    
    [Tooltip("Layer mask for placement detection (surfaces/furniture)")]
    [SerializeField] private LayerMask placementLayerMask;

    private bool isHeld = false;
    private PlayerInteraction currentHolder;
    private Vector3 lastValidPosition;
    private PlacementSlot occupiedSlot;

    private void Start()
    {
        lastValidPosition = transform.position;
    }

    private void Update()
    {
        // If held, update position to follow player
        if (isHeld && currentHolder != null)
        {
            FollowPlayer();
        }
    }

    /// <summary>
    /// Called when player picks up this object
    /// </summary>
    public void PickUp(PlayerInteraction player)
    {
        isHeld = true;
        currentHolder = player;
        lastValidPosition = transform.position;
        // If this object was occupying a placement slot, free it when picked up
        if (occupiedSlot != null)
        {
            occupiedSlot.isOccupied = false;
            occupiedSlot = null;
        }
        
        // Disable physics if using rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Debug.Log($"{gameObject.name} picked up by player");
    }

    /// <summary>
    /// Called when player places this object down
    /// </summary>
    public void PlaceDown()
    {
        if (!isHeld) return;

        isHeld = false;
        currentHolder = null;

        // Try to place on surface using physics
        if (usePhysicsPlacement)
        {
            PlaceOnSurface();
        }

        // Re-enable physics if using rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        Debug.Log($"{gameObject.name} placed down at position {transform.position}");
    }

    /// <summary>
    /// Update object position to follow player while held
    /// Object follows player's X and Z coordinates, with fixed Y offset
    /// </summary>
    private void FollowPlayer()
    {
        Vector3 playerPos = currentHolder.GetPlayerPosition();
        Vector3 newPosition = new Vector3(playerPos.x, playerPos.y + heldYOffset, playerPos.z);
        transform.position = newPosition;
        lastValidPosition = newPosition;
    }

    /// <summary>
    /// Use physics raycast to find surface below and place object on it
    /// </summary>
    private void PlaceOnSurface()
    {
        // Slot-based deterministic placement (preferred for designed gameplay)
        PlacementSlot[] slots = FindObjectsOfType<PlacementSlot>();

        PlacementSlot closest = null;
        float minDist = Mathf.Infinity;

        foreach (var slot in slots)
        {
            if (slot.isOccupied) continue;

            float dist = Vector3.Distance(transform.position, slot.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = slot;
            }
        }

        if (closest != null)
        {
            transform.position = closest.GetPosition();
            closest.isOccupied = true;
            occupiedSlot = closest;
            Debug.Log($"{gameObject.name} snapped to slot {closest.name} at {closest.GetPosition()}");
        }
        else
        {
            // No free slot found: fall back to previous valid position
            transform.position = lastValidPosition;
            Debug.Log($"{gameObject.name} could not find free slot; reverted to last valid position");
        }
    }

    /// <summary>
    /// Check if this object can be picked up based on player's Y position
    /// Player must be at or below the object's Y position to pick it up
    /// </summary>
    public bool CanBePickedUp(Vector3 playerPos)
    {
        return playerPos.y <= transform.position.y;
    }

    /// <summary>
    /// Check if object should be placed down based on player's Y position
    /// </summary>
    public bool ShouldBePlacedDown(Vector3 playerPos)
    {
        return playerPos.y <= placementYThreshold && isHeld;
    }

    public bool IsHeld()
    {
        return isHeld;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }
}
