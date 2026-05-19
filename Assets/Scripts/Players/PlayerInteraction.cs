using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles player interaction with objects in the restaurant game.
/// Manages picking up ingredients/utensils and placing them in cooking areas.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance to detect interactable objects")]
    [SerializeField] private float detectionRadius = 1.0f;

    [Header("Held Object Settings")]
    [Tooltip("Maximum number of objects player can hold at once (for future expansion)")]
    [SerializeField] private int maxHeldObjects = 1;

    private List<InteractableObject> heldObjects = new List<InteractableObject>();
    private List<InteractableObject> nearbyObjects = new List<InteractableObject>();
    
    private PlayerMovement playerMovement;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerInteraction: PlayerMovement component not found!");
        }
    }

    private void Update()
    {
        UpdateNearbyObjects();
        CheckForPickup();
        CheckForPlacement();
    }

    /// <summary>
    /// Update list of nearby interactable objects
    /// </summary>
    private void UpdateNearbyObjects()
    {
        nearbyObjects.Clear();
        
        // Find all interactable objects in the scene
        InteractableObject[] allObjects = FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
        
        foreach (InteractableObject obj in allObjects)
        {
            if (obj.IsHeld()) continue; // Skip already held objects
            
            float distance = Vector3.Distance(transform.position, obj.GetPosition());
            if (distance <= detectionRadius)
            {
                nearbyObjects.Add(obj);
            }
        }
    }

    /// <summary>
    /// Check if player should pick up a nearby object
    /// Picks up object if player's Y position is at or below the object's Y position
    /// </summary>
    private void CheckForPickup()
    {
        if (heldObjects.Count >= maxHeldObjects) return;

        Vector3 playerPos = transform.position;

        foreach (InteractableObject obj in nearbyObjects)
        {
            if (obj.IsHeld()) continue;

            // Player must be at or below object's Y position to pick it up
            if (obj.CanBePickedUp(playerPos))
            {
                PickUpObject(obj);
                break; // Only pick up one object at a time
            }
        }
    }

    /// <summary>
    /// Check if held object should be placed down
    /// Places object when player's Y position goes below placement threshold
    /// </summary>
    private void CheckForPlacement()
    {
        Vector3 playerPos = transform.position;

        // Check held objects in reverse to safely remove while iterating
        for (int i = heldObjects.Count - 1; i >= 0; i--)
        {
            InteractableObject obj = heldObjects[i];
            
            if (obj.ShouldBePlacedDown(playerPos))
            {
                PlaceObjectDown(obj);
            }
        }
    }

    /// <summary>
    /// Pick up an interactable object
    /// </summary>
    private void PickUpObject(InteractableObject obj)
    {
        if (obj == null || heldObjects.Contains(obj)) return;

        obj.PickUp(this);
        heldObjects.Add(obj);
        
        Debug.Log($"Player picked up {obj.gameObject.name}. Holding {heldObjects.Count} object(s)");
    }

    /// <summary>
    /// Place down a held object
    /// </summary>
    private void PlaceObjectDown(InteractableObject obj)
    {
        if (obj == null || !heldObjects.Contains(obj)) return;

        obj.PlaceDown();
        heldObjects.Remove(obj);
        
        Debug.Log($"Player placed down {obj.gameObject.name}. Holding {heldObjects.Count} object(s)");
    }

    /// <summary>
    /// Get player's current position (for held objects to follow)
    /// </summary>
    public Vector3 GetPlayerPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Get number of objects currently held
    /// </summary>
    public int GetHeldObjectCount()
    {
        return heldObjects.Count;
    }

    /// <summary>
    /// Get list of held objects
    /// </summary>
    public List<InteractableObject> GetHeldObjects()
    {
        return new List<InteractableObject>(heldObjects);
    }
}
