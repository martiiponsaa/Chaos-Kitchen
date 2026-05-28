using System.Collections.Generic;
using UnityEngine;

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
    [Tooltip("Resulting ingredient type after processing (set to same type for no change)")]
    public IngredientType producesIngredient = IngredientType.None;

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

        if (isProcessingSlot && acceptsIngredient != IngredientType.None)
        {
            var cur = obj.GetIngredientType();
            if (cur == acceptsIngredient)
            {
                // Transform immediately (simple cooking placeholder)
                //if (producesIngredient != IngredientType.None)
                //{
                //    obj.SetIngredientType(producesIngredient);
                //    Debug.Log($"Processed {cur} -> {producesIngredient} on slot {name}");
                //}
                var cookingSlot = GetComponent<CookingSlot>();
                if (cookingSlot != null)
                {
                    cookingSlot.StartCooking(obj);
                }
                else if (producesIngredient != IngredientType.None)
                {
                    obj.SetIngredientType(producesIngredient);
                    Debug.Log($"Processed {cur} -> {producesIngredient} on slot {name}");
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
}
