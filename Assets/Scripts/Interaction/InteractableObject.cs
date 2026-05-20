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
    [SerializeField] private float placementYThreshold = 0.2f;
    
    [Tooltip("Use physics raycast to detect surface below for proper placement")]
    [SerializeField] private bool usePhysicsPlacement = false;
    
    [Tooltip("Layer mask for placement detection (surfaces/furniture)")]
    [SerializeField] private LayerMask placementLayerMask;

    [Header("Slot Placement")]
    [Tooltip("Maximum horizontal distance (meters) from player to a free slot to allow placement")]
    [SerializeField] private float slotSnapDistance = 1.0f;

    [Header("Pickup Settings")]
    [Tooltip("Y position threshold at or below which the player can pick this object up")]
    [SerializeField] private float pickupYThreshold = 0.2f;

    [Tooltip("Vertical tolerance (meters) within which player and object are considered at the same height for pickup")]
    [SerializeField] private float pickupHeightTolerance = 0.15f;

    private bool isHeld = false;
    private PlayerInteraction currentHolder;
    private Vector3 lastValidPosition;
    private float pickupYAtPickup;
    private PlacementSlot occupiedSlot;
    private PlacementSlot pendingPlacementSlot;

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
        if (isHeld) return;

        isHeld = true;
        currentHolder = player;
        // Record the Y at pickup so we can preserve it on drop
        pickupYAtPickup = transform.position.y;

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

        // Only allow placement into a pending slot (determined by ShouldBePlacedDown)
        if (pendingPlacementSlot != null)
        {
            Vector3 slotPos = pendingPlacementSlot.GetPosition();
            transform.position = new Vector3(slotPos.x, pickupYAtPickup, slotPos.z);
            pendingPlacementSlot.isOccupied = true;
            occupiedSlot = pendingPlacementSlot;
            Debug.Log($"{gameObject.name} placed into slot {pendingPlacementSlot.name} at {occupiedSlot.GetPosition()} (preserved Y={pickupYAtPickup})");
            pendingPlacementSlot = null;
        }
        else if (usePhysicsPlacement)
        {
            // Fallback behaviour if physics placement is enabled
            PlaceOnSurface();
        }
        else
        {
            // If placement isn't allowed (no nearby slot) keep object at last valid position
            transform.position = lastValidPosition;
            Debug.Log($"{gameObject.name} placement cancelled; no nearby slot found");
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
        // Follow player's X, Y and Z (with optional Y offset) so the object follows the player's movement
        Vector3 newPosition = new Vector3(playerPos.x, playerPos.y + heldYOffset, playerPos.z);
        transform.position = newPosition;
    }

    /// <summary>
    /// Use physics raycast to find surface below and place object on it
    /// </summary>
    private void PlaceOnSurface()
    {
        // Slot-based deterministic placement (preferred for designed gameplay)
        PlacementSlot[] slots = FindObjectsByType<PlacementSlot>(FindObjectsSortMode.None);

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
            Vector3 slotPos = closest.GetPosition();
            // Snap to slot center but preserve the original pickup height so dropping keeps same Y
            transform.position = new Vector3(slotPos.x, pickupYAtPickup, slotPos.z);
            closest.isOccupied = true;
            occupiedSlot = closest;
            Debug.Log($"{gameObject.name} snapped to slot {closest.name} at {closest.GetPosition()} (preserved Y={pickupYAtPickup})");
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
        // Pickup when player's height matches the object's height within a small tolerance
        return Mathf.Abs(playerPos.y - transform.position.y) <= pickupHeightTolerance;
    }

    /// <summary>
    /// Check if object should be placed down based on player's Y position
    /// Only allows placement when there is a nearby free PlacementSlot
    /// </summary>
    public bool ShouldBePlacedDown(Vector3 playerPos)
    {
        if (!isHeld) return false;
        if (playerPos.y > placementYThreshold) return false;

        // Find closest free placement slot to the player (XZ plane)
        PlacementSlot[] slots = FindObjectsByType<PlacementSlot>(FindObjectsSortMode.None);
        PlacementSlot closest = null;
        float minDist = Mathf.Infinity;

        Vector2 playerXZ = new Vector2(playerPos.x, playerPos.z);
        foreach (var slot in slots)
        {
            if (slot.isOccupied) continue;
            Vector3 sPos = slot.GetPosition();
            Vector2 slotXZ = new Vector2(sPos.x, sPos.z);
            float dist = Vector2.Distance(playerXZ, slotXZ);
            if (dist < minDist)
            {
                minDist = dist;
                closest = slot;
            }
        }

        if (closest != null && minDist <= slotSnapDistance)
        {
            // A nearby free slot exists — allow placement and remember which slot to use
            pendingPlacementSlot = closest;
            return true;
        }

        // No suitable slot nearby — placement not allowed
        pendingPlacementSlot = null;
        return false;
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
