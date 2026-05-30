using UnityEngine;
using UnityEngine.Audio;
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

    [Header("Hand Reference")]
    [Tooltip("Optional: assign the real hand hitbox transform. Transfer checks use this position.")]
    [SerializeField] private Transform handHitboxTransform;
    [Tooltip("Optional: assign the collider used as the hand hitbox. Pass transfer uses overlap between both players' hand hitboxes.")]
    [SerializeField] private Collider handHitboxCollider;

    [Header("Sons d'Interacci�")]
    [Tooltip("Arrossega el so que far� el personatge quan agafi un objecte")]
    [SerializeField] private AudioClip grabSound;
    [Tooltip("Arrossega el so que far� el personatge quan deixi anar un objecte")]
    [SerializeField] private AudioClip dropSound;

    [Tooltip("Audio mixer group used for SFX playback. Assign the SFX group from MainAudioMixer.")]
    [SerializeField] private AudioMixerGroup sfxOutputGroup;

    private List<InteractableObject> heldObjects = new List<InteractableObject>();
    private List<InteractableObject> nearbyObjects = new List<InteractableObject>();
    
    private PlayerMovement playerMovement;
    private HandInteraction handInteraction;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerInteraction: PlayerMovement component not found!");
        }

        GameObject handSearchRoot = playerMovement != null ? playerMovement.gameObject : transform.root.gameObject;
        handInteraction = handSearchRoot.GetComponentInChildren<HandInteraction>(true);
        if (handInteraction == null)
        {
            Debug.LogWarning($"PlayerInteraction: HandInteraction component not found under {handSearchRoot.name}.");
        }
        else
        {
            handInteraction.OpenHand();
        }

        if (handHitboxCollider == null && handHitboxTransform != null)
        {
            handHitboxCollider = handHitboxTransform.GetComponent<Collider>();
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
            if (obj.CanBePickedUp(playerPos, this))
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
            
            if (obj.ShouldBePlacedDown(playerPos, this))
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

        if (!obj.PickUp(this))
        {
            return;
        }

        // For passable ingredients, keep the pre-assigned final owner until transfer.
        if (!obj.IsPassableIngredient())
        {
            obj.SetOwner(this);
        }
        heldObjects.Add(obj);

        if (handInteraction != null)
        {
            handInteraction.CloseHand();
        }
        
        Debug.Log($"Player picked up {obj.gameObject.name}. Holding {heldObjects.Count} object(s)");
        if (grabSound != null)
        {
            Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            PlaySfxAtPoint(grabSound, cameraPos);
        }
    }

    public bool CanReceiveTransferredObject(InteractableObject obj)
    {
        return obj != null && heldObjects.Count < maxHeldObjects;
    }

    public bool ReceiveTransferredObject(InteractableObject obj)
    {
        if (!CanReceiveTransferredObject(obj) || heldObjects.Contains(obj)) return false;

        heldObjects.Add(obj);
        FindObjectOfType<GuidanceManager>()?.OnIngredientPickedUp(obj.gameObject, this);

        if (handInteraction != null)
        {
            handInteraction.CloseHand();
        }

        Debug.Log($"Player received {obj.gameObject.name}. Holding {heldObjects.Count} object(s)");
        return true;
    }

    public bool ReleaseHeldObject(InteractableObject obj)
    {
        if (obj == null) return false;

        bool removed = heldObjects.Remove(obj);
        if (removed && heldObjects.Count == 0 && handInteraction != null)
        {
            handInteraction.OpenHand();
        }

        return removed;
    }

    /// <summary>
    /// Place down a held object
    /// </summary>
    private void PlaceObjectDown(InteractableObject obj)
    {
        if (obj == null || !heldObjects.Contains(obj)) return;

        bool placed = obj.PlaceDown();
        if (placed)
        {
            heldObjects.Remove(obj);

            if (heldObjects.Count == 0 && handInteraction != null)
            {
                handInteraction.OpenHand();
            }

            Debug.Log($"Player placed down {obj.gameObject.name}. Holding {heldObjects.Count} object(s)");
            if (dropSound != null)
            {
                Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : transform.position;
                PlaySfxAtPoint(dropSound, cameraPos);
            }
        }
        else
        {
            // Placement failed (slot rejected); keep holding the object
            Debug.Log($"Placement cancelled for {obj.gameObject.name}; still holding {heldObjects.Count} object(s)");
        }
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

    public Vector3 GetHandPosition()
    {
        if (handHitboxTransform != null)
        {
            return handHitboxTransform.position;
        }

        if (handInteraction != null)
        {
            return handInteraction.transform.position;
        }

        return transform.position;
    }

    public bool IsHandHitboxInContactWith(PlayerInteraction other)
    {
        if (other == null || handHitboxCollider == null) return false;

        Collider otherHandCollider = other.GetHandHitboxCollider();
        if (otherHandCollider == null) return false;

        return handHitboxCollider.bounds.Intersects(otherHandCollider.bounds);
    }

    public Collider GetHandHitboxCollider()
    {
        return handHitboxCollider;
    }

    private void PlaySfxAtPoint(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        GameObject temp = new GameObject($"SFX_{clip.name}");
        temp.transform.position = position;

        AudioSource source = temp.AddComponent<AudioSource>();
        source.clip = clip;
        source.outputAudioMixerGroup = sfxOutputGroup;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.Play();

        float lifetime = clip.length / Mathf.Max(source.pitch, 0.01f);
        Destroy(temp, lifetime + 0.1f);
    }
}
