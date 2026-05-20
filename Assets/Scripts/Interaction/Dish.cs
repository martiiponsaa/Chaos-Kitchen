using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a dish being prepared. Tracks recipe progress and applies ingredient actions.
/// Inherit from InteractableObject so it becomes pickable only when completed.
/// </summary>
public class Dish : InteractableObject
{
    [Tooltip("Recipe to prepare on this dish")]
    public RecipeSO recipe;

    private int currentStep = 0;
    private List<IngredientType> applied = new List<IngredientType>();
    private bool isCompleted = false;

    public bool IsCompleted => isCompleted;

    // Check whether this dish accepts the given ingredient right now
    public bool CanAccept(IngredientType type)
    {
        if (isCompleted || recipe == null || recipe.steps == null) return false;
        if (currentStep >= recipe.steps.Length) return false;
        return recipe.steps[currentStep] == type;
    }

    // Called when an ingredient InteractableObject is placed onto the linked slot
    public bool ApplyIngredient(InteractableObject ingredientObj)
    {
        if (ingredientObj == null) return false;
        IngredientType type = IngredientType.None;
        // Attempt to read ingredient type from the object if available
        var field = ingredientObj.GetType().GetField("ingredientType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            type = (IngredientType)field.GetValue(ingredientObj);
        }

        if (!CanAccept(type))
        {
            Debug.LogWarning($"Dish cannot accept {type} now.");
            return false;
        }

        // Record the ingredient
        applied.Add(type);
        currentStep++;

        // Execute any action components on the ingredient (placeholders)
        var actions = ingredientObj.GetComponents<MonoBehaviour>();
        foreach (var a in actions)
        {
            if (a is IIngredientAction ia)
            {
                ia.Execute(this);
            }
        }

        Debug.Log($"Applied {type} to dish ({currentStep}/{recipe.steps.Length})");

        if (currentStep >= recipe.steps.Length)
        {
            MarkCompleted();
        }

        return true;
    }

    private void MarkCompleted()
    {
        isCompleted = true;
        Debug.Log($"Dish completed: {gameObject.name}");
        // Optionally add visual feedback here
    }

    // Dishes are pickable only once completed
    public override bool CanBePickedUp(Vector3 playerPos)
    {
        if (!isCompleted) return false;
        return base.CanBePickedUp(playerPos);
    }
}
