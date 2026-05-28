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

    [Header("Ingredient")]
    [Tooltip("Type of ingredient this object represents (None for non-ingredients)")]
    [SerializeField] private IngredientType ingredientType = IngredientType.None;
    [Tooltip("Assign this object to a specific player (optional). If set, only that player may pick/place it.")]
    [SerializeField] private PlayerInteraction ownerPlayer = null;

    // Public getter and setter to allow processing slots to transform ingredients (e.g., cook meat -> cooked meat)
    public IngredientType GetIngredientType() => ingredientType;
    public void SetIngredientType(IngredientType t) => ingredientType = t;

    public PlayerInteraction GetOwner() => ownerPlayer;
    public void SetOwner(PlayerInteraction p) => ownerPlayer = p;

    protected float GetPickupHeightTolerance() => pickupHeightTolerance;

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
        lastValidPosition = transform.position;
        // Record the Y at pickup so we can preserve it on drop
        pickupYAtPickup = transform.position.y;

        // Stop highlight feedback and notify guidance system
        Highligh highlight = GetComponentInChildren<Highligh>();
        if (highlight != null) highlight.StopHighlight();
        FindObjectOfType<GuidanceManager>()?.OnIngredientPickedUp(gameObject, player);

        // If this object was occupying a placement slot, free it when picked up
        if (occupiedSlot != null)
        {
            //Cuinat d'hamburguesa
            var cookingSlot = occupiedSlot.GetComponent<CookingSlot>();
            if (cookingSlot != null) cookingSlot.CancelCooking();

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
    public bool PlaceDown()
    {
        if (!isHeld) return false;
        // capture the player who is placing before we clear the holder
        var placingPlayer = currentHolder;

        isHeld = false;
        currentHolder = null;
        // Only allow placement into a pending slot (determined by ShouldBePlacedDown)
        var slot = pendingPlacementSlot;
        // Clear pendingPlacementSlot early to avoid reuse during placement
        pendingPlacementSlot = null;

        if (slot != null)
        {
            Vector3 slotPos = slot.GetPosition();
            // If this slot accepts only dishes, handle delivery/consumption logic
            if (slot.acceptOnlyDishes)
            {
                // Only Dish objects are acceptable here
                if (this is Dish dishObj)
                {
                    if (!dishObj.IsCompleted)
                    {
                        // Can't deliver an incomplete dish
                        Debug.LogWarning($"Cannot deliver incomplete dish {gameObject.name} to {slot.name}");
                        return false;
                    }

                    if (!slot.CanAcceptDish(dishObj))
                    {
                        Debug.LogWarning($"Slot {slot.name} does not accept completed dish type {dishObj.GetIngredientType()}");
                        return false;
                    }

                    if (slot.consumeOnPlace)
                    {
                        // Consume/deliver the dish
                        Debug.Log($"Dish {gameObject.name} delivered at {slot.name}");
                        LevelFlowManager.Instance?.NotifyDishDelivered(dishObj, placingPlayer);
                        FindObjectOfType<GuidanceManager>()?.OnIngredientPlaced(gameObject, placingPlayer);
                        Destroy(gameObject);
                        return true;
                    }
                    else
                    {
                        // Snap the dish to the slot but mark occupied
                        transform.position = new Vector3(slotPos.x, pickupYAtPickup, slotPos.z);
                        slot.isOccupied = true;
                        occupiedSlot = slot;
                        Debug.Log($"Dish {gameObject.name} placed into delivery slot {slot.name}");
                        LevelFlowManager.Instance?.NotifyDishDelivered(dishObj, placingPlayer);
                        FindObjectOfType<GuidanceManager>()?.OnIngredientPlaced(gameObject, placingPlayer);
                        return true;
                    }
                }
                else
                {
                    // This slot only accepts dishes
                    Debug.LogWarning($"Slot {slot.name} accepts only dishes");
                    return false;
                }
            }
            else
            {
                transform.position = new Vector3(slotPos.x, pickupYAtPickup, slotPos.z);
                slot.isOccupied = true;
                occupiedSlot = slot;
                Debug.Log($"{gameObject.name} placed into slot {slot.name} at {occupiedSlot.GetPosition()} (preserved Y={pickupYAtPickup})");

                // Let slot process the placed object (e.g., cook meat in a pan)
                slot.OnObjectPlaced(this);

                // If this slot is linked to a Dish, notify it that an ingredient was placed here
                if (slot.linkedDish != null)
                {
                    bool applied = slot.linkedDish.ApplyIngredient(this);
                    if (applied)
                    {
                        // Ingredient was consumed by the dish, destroy this object
                        FindObjectOfType<GuidanceManager>()?.OnIngredientPlaced(gameObject, placingPlayer);
                        Destroy(gameObject);
                        return true;
                    }
                }
                FindObjectOfType<GuidanceManager>()?.OnIngredientPlaced(gameObject, placingPlayer);
                return true;
            }
        }
        else if (usePhysicsPlacement)
        {
            // Fallback behaviour if physics placement is enabled
            PlaceOnSurface();

            // Re-enable physics if using rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            Debug.Log($"{gameObject.name} placed down at position {transform.position}");
            FindObjectOfType<GuidanceManager>()?.OnIngredientPlaced(gameObject, placingPlayer);
            return true;
        }
        else
        {
            // If placement isn't allowed (no nearby slot) keep object at last valid position
            transform.position = lastValidPosition;
            Debug.Log($"{gameObject.name} placement cancelled; no nearby slot found");

            // Re-enable physics if using rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            return false;
        }
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
            // Ensure the closest slot actually accepts this ingredient (processing or dish rules)
            if (closest.isProcessingSlot && closest.acceptsIngredient != IngredientType.None && closest.acceptsIngredient != ingredientType)
            {
                Debug.LogWarning($"Slot {closest.name} does not accept {ingredientType}; placement cancelled.");
                transform.position = lastValidPosition;
                return;
            }

            // Snap to slot center but preserve the original pickup height so dropping keeps same Y
            transform.position = new Vector3(slotPos.x, pickupYAtPickup, slotPos.z);
            closest.isOccupied = true;
            occupiedSlot = closest;
            Debug.Log($"{gameObject.name} snapped to slot {closest.name} at {closest.GetPosition()} (preserved Y={pickupYAtPickup})");
            // let slot handle processing
            closest.OnObjectPlaced(this);
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
    public virtual bool CanBePickedUp(Vector3 playerPos, PlayerInteraction player = null)
    {
        // Pickup when player's height matches the object's height within a small tolerance
        bool heightOk = Mathf.Abs(playerPos.y - transform.position.y) <= pickupHeightTolerance;
        if (!heightOk) return false;

        // If this object has an assigned owner, only that player may pick it up
        if (GetOwner() != null && GetOwner() != player) return false;

        // If this object is an ingredient, only allow pickup if there is at least one Dish
        // that can accept this ingredient now. If no dishes exist yet, allow pickup anyway.
        if (ingredientType != IngredientType.None)
        {
            Dish[] dishes = FindObjectsByType<Dish>(FindObjectsSortMode.None);

            // If no dish exists, allow pickup (developer choice)
            if (dishes.Length == 0) return true;

            // If any dish currently accepts this ingredient and it's assigned to this player or its slot is assigned, allow pickup
            foreach (var d in dishes)
            {
                if (d == null) continue;
                // Consider dish only if it's assigned to the player or linked to a slot assigned to the player
                bool dishForPlayer = false;
                if (d.GetAssignedPlayer() != null)
                {
                    dishForPlayer = (d.GetAssignedPlayer() == player);
                }
                else
                {
                    // check if any slot links to this dish and is assigned to the player
                    foreach (var s in PlacementSlot.all)
                    {
                        if (s == null) continue;
                        if (s.linkedDish == d && s.assignedPlayer == player)
                        {
                            dishForPlayer = true;
                            break;
                        }
                    }
                }

                if (!dishForPlayer) continue;

                if (d.CanAccept(ingredientType)) return true;
            }

            // No dish currently accepts this raw ingredient. Allow pickup if there is
            // a processing slot (e.g., pan) that can transform this ingredient into
            // a type that is currently accepted by any dish. This lets players pick
            // up raw `Meat` when a dish expects `CookedMeat` and there is a pan.
            var slots = FindObjectsByType<PlacementSlot>(FindObjectsSortMode.None);
            foreach (var s in slots)
            {
                if (s == null) continue;
                if (!s.isProcessingSlot) continue;
                if (s.acceptsIngredient != ingredientType) continue;
                // If this processing slot produces a type that some dish accepts now,
                // allow pickup so player can place it into the processor.
                if (s.producesIngredient == IngredientType.None) continue;
                // Only consider processing slots assigned to this player (or unassigned)
                if (s.assignedPlayer != null && s.assignedPlayer != player) continue;

                foreach (var d2 in dishes)
                {
                    if (d2 == null) continue;
                    // filter dishes to those relevant to this player
                    bool dishForPlayer = false;
                    if (d2.GetAssignedPlayer() != null)
                    {
                        dishForPlayer = (d2.GetAssignedPlayer() == player);
                    }
                    else
                    {
                        foreach (var ss in PlacementSlot.all)
                        {
                            if (ss == null) continue;
                            if (ss.linkedDish == d2 && ss.assignedPlayer == player)
                            {
                                dishForPlayer = true;
                                break;
                            }
                        }
                    }

                    if (!dishForPlayer) continue;

                    if (d2.CanAccept(s.producesIngredient)) return true;
                }
            }

            // Not acceptable and no processing path to required type
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if object should be placed down based on player's Y position
    /// Only allows placement when there is a nearby free PlacementSlot
    /// </summary>
    public bool ShouldBePlacedDown(Vector3 playerPos, PlayerInteraction player = null)
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
            // Skip dish-only slots when this object is not a Dish
            if (slot.acceptOnlyDishes && !(this is Dish)) continue;
            if (slot.acceptOnlyDishes && this is Dish dishObj && !slot.CanAcceptDish(dishObj)) continue;
            Vector3 sPos = slot.GetPosition();
            Vector2 slotXZ = new Vector2(sPos.x, sPos.z);
            float dist = Vector2.Distance(playerXZ, slotXZ);
            if (dist >= minDist) continue;

            // Determine if this slot can accept placement:
            // - empty slots (not occupied) are acceptable
            // - or slots that are occupied by a Dish which can accept this ingredient now
            bool slotAccepts = false;
                // If slot specifies an accepted ingredient, enforce it
                if (slot.isProcessingSlot && slot.acceptsIngredient != IngredientType.None)
                {
                    if (slot.acceptsIngredient != ingredientType)
                    {
                        // doesn't accept this ingredient type
                        slotAccepts = false;
                        goto CHECK_CONTINUE;
                    }
                }

                // If a dish is linked to this slot, require the dish to accept this ingredient
                if (slot.linkedDish != null)
                {
                    slotAccepts = slot.linkedDish.CanAccept(ingredientType);
                }
                else if (!slot.isOccupied)
                {
                    // otherwise empty free slots are acceptable
                    slotAccepts = true;
                }

            CHECK_CONTINUE:;
            if (!slotAccepts) continue;

            minDist = dist;
            closest = slot;
        }

        if (closest != null && minDist <= slotSnapDistance)
        {
            // A nearby suitable slot exists — allow placement and remember which slot to use
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