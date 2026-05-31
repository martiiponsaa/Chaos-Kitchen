using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

[DisallowMultipleComponent]
public class PlacementSlot : MonoBehaviour
{
    // Public state used by InteractableObject
    public bool isOccupied = false;

    // Optional explicit center transform (if not set, uses this.transform)
    public Transform center;
    // If true, this slot accepts only completed Dish objects.
    public bool acceptOnlyDishes = false;
    // If true and a dish is placed here, the dish will be consumed (delivered) instead of being snapped.
    public bool consumeOnPlace = false;
    [Tooltip("Final dish ingredient types this slot accepts. Leave empty to accept any completed dish.")]
    public IngredientType[] acceptedDishTypes = new IngredientType[0];
    // Optional linked Dish that occupies this slot (assign in inspector)
    public Dish linkedDish;
    [Tooltip("Assign this slot to a specific player (optional). If set, only that player may use this slot.")]
    public PlayerInteraction assignedPlayer;

    [Header("Processing Slot")]
    [Tooltip("If true, this slot will process/transform placed ingredients immediately (e.g., cook meat in a pan)")]
    public bool isProcessingSlot = false;
    [Tooltip("Ingredient type this slot accepts for processing (Ignored if None)")]
    public IngredientType acceptsIngredient = IngredientType.None;
    [Tooltip("Optional list of ingredient types this slot accepts for processing. Leave empty to use the single acceptedIngredient field.")]
    public IngredientType[] acceptedIngredients = new IngredientType[0];
    [Tooltip("Resulting ingredient type after processing (set to same type for no change)")]
    public IngredientType producesIngredient = IngredientType.None;
    [Tooltip("Optional list of resulting ingredient types after processing. If provided, each entry matches the ingredient type at the same index in acceptedIngredients.")]
    public IngredientType[] producedIngredients = new IngredientType[0];

    // Static registry for fast lookups
    public static readonly List<PlacementSlot> all = new List<PlacementSlot>();

    private void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    // Returns the world position objects should snap to
    public Vector3 GetPosition()
    {
        return (center != null) ? center.position : transform.position;
    }

    // Visual helper in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? new Color(1f, 0.4f, 0.4f, 0.6f) : new Color(0.4f, 1f, 0.4f, 0.4f);
        Vector3 pos = GetPosition();
        Gizmos.DrawSphere(pos, 0.05f);
#if UNITY_EDITOR
        var bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = isOccupied ? new Color(1f,0.4f,0.4f,0.15f) : new Color(0.4f,1f,0.4f,0.15f);
            Gizmos.DrawCube(bc.center, bc.size);
        }
#endif
    }

    /// <summary>
    /// Called by InteractableObject when an object is placed into this slot.
    /// Allows processing slots to transform ingredient objects immediately.
    /// </summary>
    public void OnObjectPlaced(InteractableObject obj)
    {
        if (obj == null) return;

        if (CanProcessIngredient(obj.GetIngredientType()))
        {
            var cur = obj.GetIngredientType();
                var cookingSlot = GetComponent<CookingSlot>();
                if (cookingSlot != null)
                {
                    // If the cooking slot is already busy, revert placement and restore the object
                    if (!cookingSlot.StartCooking(obj))
                    {
                        Debug.LogWarning($"[PlacementSlot] Cooking slot {name} busy; reverting placement of {obj.name}.");
                        // Revert the newly placed object but keep the slot occupied by the cooking item.
                        obj.transform.position = obj.GetLastValidPosition();
                        obj.SetInteractionLocked(false);
                        // Clear the object's occupiedSlot field so it no longer points to this slot
                        var field = obj.GetType().GetField("occupiedSlot", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(obj, null);
                        }
                        return;
                    }
                }
            else
            {
                IngredientType producedType = GetProducedIngredient(cur);
                if (producedType != IngredientType.None && producedType != cur)
                {
                    obj.SetIngredientType(producedType);
                    Debug.Log($"Processed {cur} -> {producedType} on slot {name}");
                }
            }
        }
        // Execute any ingredient action components attached to the placed object
        var actions = obj.GetComponents<MonoBehaviour>();
        foreach (var a in actions)
        {
            if (a is IIngredientAction ia)
            {
                ia.Execute(this);
            }
        }
    }

    public bool CanAcceptDish(Dish dish)
    {
        if (dish == null || !dish.IsCompleted) return false;

        if (acceptedDishTypes == null || acceptedDishTypes.Length == 0)
        {
            return true;
        }

        var dishType = dish.GetIngredientType();
        foreach (var acceptedType in acceptedDishTypes)
        {
            if (acceptedType == dishType)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanAcceptIngredient(IngredientType ingredientType)
    {
        if (ingredientType == IngredientType.None)
        {
            return false;
        }

        if (acceptOnlyDishes)
        {
            return false;
        }

        if (isProcessingSlot)
        {
            return !isOccupied && CanProcessIngredient(ingredientType);
        }

        if (linkedDish != null)
        {
            return linkedDish.CanAccept(ingredientType);
        }

        return !isOccupied;
    }

    public bool CanProcessIngredient(IngredientType ingredientType)
    {
        if (!isProcessingSlot || ingredientType == IngredientType.None)
        {
            return false;
        }

        if (acceptedIngredients != null && acceptedIngredients.Length > 0)
        {
            for (int i = 0; i < acceptedIngredients.Length; i++)
            {
                if (acceptedIngredients[i] == ingredientType)
                {
                    return true;
                }
            }

            return false;
        }

        return acceptsIngredient == IngredientType.None || acceptsIngredient == ingredientType;
    }

    public IngredientType GetProducedIngredient(IngredientType ingredientType)
    {
        if (!isProcessingSlot)
        {
            return IngredientType.None;
        }

        if (producedIngredients != null && producedIngredients.Length > 0)
        {
            int matchedIndex = -1;

            if (acceptedIngredients != null && acceptedIngredients.Length > 0)
            {
                for (int i = 0; i < acceptedIngredients.Length; i++)
                {
                    if (acceptedIngredients[i] == ingredientType)
                    {
                        matchedIndex = i;
                        break;
                    }
                }
            }
            else if (acceptsIngredient == ingredientType)
            {
                matchedIndex = 0;
            }

            if (matchedIndex >= 0 && matchedIndex < producedIngredients.Length)
            {
                IngredientType producedType = producedIngredients[matchedIndex];
                if (producedType != IngredientType.None)
                {
                    return producedType;
                }
            }
        }

        return producesIngredient;
    }

    /// <summary>
    /// Returns true if this slot can produce the given ingredient type (either via the single producesIngredient
    /// field or via the producedIngredients array).
    /// </summary>
    public bool ProducesIngredient(IngredientType producedType)
    {
        if (producedType == IngredientType.None) return false;

        if (producedIngredients != null && producedIngredients.Length > 0)
        {
            foreach (var p in producedIngredients)
            {
                if (p == producedType) return true;
            }
        }

        return producesIngredient == producedType;
    }
}
