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

    [Header("Step Visuals")]
    [Tooltip("Prefabs to show for each recipe step. The list is trimmed to the recipe step count.")]
    [SerializeField] private List<GameObject> stepVisualPrefabs = new List<GameObject>();

    [Tooltip("Optional prefab shown when the dish is completed. If empty, the last step prefab is kept.")]
    [SerializeField] private GameObject completedVisualPrefab;

    [Tooltip("Ingredient type this dish becomes when the recipe is completed")]
    [SerializeField] private IngredientType completedIngredientType = IngredientType.None;

    private int currentStep = 0;
    private List<IngredientType> applied = new List<IngredientType>();
    private bool isCompleted = false;
    private GameObject currentVisualInstance;
    private int currentVisualIndex = -1;
    private bool currentVisualIsCompleted;
    // natalia was here
    public bool IsCompleted => isCompleted;

    private void OnValidate()
    {
        if (recipe == null || recipe.steps == null)
        {
            return;
        }

        int maxVisualCount = recipe.steps.Length;
        if (stepVisualPrefabs == null)
        {
            stepVisualPrefabs = new List<GameObject>();
        }

        while (stepVisualPrefabs.Count > maxVisualCount)
        {
            stepVisualPrefabs.RemoveAt(stepVisualPrefabs.Count - 1);
        }
    }

    private void Awake()
    {
        RefreshVisualForCurrentStep(true);
    }

    // Check whether this dish accepts the given ingredient right now
    public bool CanAccept(IngredientType type)
    {
        if (isCompleted || recipe == null || recipe.steps == null) return false;
        if (currentStep >= recipe.steps.Length) return false;
        return recipe.steps[currentStep] == type;
    }

    public PlayerInteraction GetAssignedPlayer() => GetOwner();

    // Called when an ingredient InteractableObject is placed onto the linked slot
    public bool ApplyIngredient(InteractableObject ingredientObj)
    {
        if (ingredientObj == null) return false;
        IngredientType type = IngredientType.None;
        // Prefer public API if object is InteractableObject
        if (ingredientObj is InteractableObject io)
        {
            type = io.GetIngredientType();
        }
        else
        {
            // Fallback: try to read private field via reflection
            var field = ingredientObj.GetType().GetField("ingredientType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                type = (IngredientType)field.GetValue(ingredientObj);
            }
        }

        if (!CanAccept(type))
        {
            Debug.LogWarning($"Dish cannot accept {type} now.");
            return false;
        }

        // Record the ingredient
        applied.Add(type);
        currentStep++;
        RefreshVisualForCurrentStep();

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
        ShowCompletedVisual();
        if (completedIngredientType != IngredientType.None)
        {
            SetIngredientType(completedIngredientType);
        }
        Debug.Log($"Dish completed: {gameObject.name}");
        // Optionally add visual feedback here
    }

    private void RefreshVisualForCurrentStep(bool forceRefresh = false)
    {
        if (isCompleted)
        {
            ShowCompletedVisual(forceRefresh);
            return;
        }

        if (stepVisualPrefabs == null || stepVisualPrefabs.Count == 0)
        {
            ClearCurrentVisual();
            return;
        }

        int targetIndex = currentStep - 1;
        if (targetIndex < 0)
        {
            ClearCurrentVisual();
            return;
        }

        if (targetIndex >= stepVisualPrefabs.Count)
        {
            targetIndex = stepVisualPrefabs.Count - 1;
        }

        if (!forceRefresh && !currentVisualIsCompleted && targetIndex == currentVisualIndex && currentVisualInstance != null)
        {
            return;
        }

        ClearCurrentVisual();

        GameObject visualPrefab = stepVisualPrefabs[targetIndex];
        if (visualPrefab == null)
        {
            currentVisualIndex = targetIndex;
            currentVisualIsCompleted = false;
            return;
        }

        currentVisualInstance = Instantiate(visualPrefab, transform, false);
        ResetVisualTransform(currentVisualInstance);
        currentVisualIndex = targetIndex;
        currentVisualIsCompleted = false;
    }

    private void ShowCompletedVisual(bool forceRefresh = false)
    {
        GameObject visualPrefab = completedVisualPrefab;
        if (visualPrefab == null)
        {
            if (stepVisualPrefabs != null && stepVisualPrefabs.Count > 0)
            {
                visualPrefab = stepVisualPrefabs[stepVisualPrefabs.Count - 1];
            }
            else
            {
                ClearCurrentVisual();
                return;
            }
        }

        if (!forceRefresh && currentVisualIsCompleted && currentVisualInstance != null)
        {
            return;
        }

        ClearCurrentVisual();

        currentVisualInstance = Instantiate(visualPrefab, transform, false);
        ResetVisualTransform(currentVisualInstance);
        currentVisualIndex = Mathf.Max(0, currentStep - 1);
        currentVisualIsCompleted = true;
    }

    private void ResetVisualTransform(GameObject visualInstance)
    {
        if (visualInstance == null)
        {
            return;
        }

        Transform visualTransform = visualInstance.transform;
        visualTransform.localPosition = Vector3.zero;
        visualTransform.localRotation = Quaternion.identity;
        visualTransform.localScale = GetScaleCompensation(transform, visualTransform.localScale);
    }

    private Vector3 GetScaleCompensation(Transform parentTransform, Vector3 prefabLocalScale)
    {
        Vector3 parentScale = parentTransform.lossyScale;

        float x = parentScale.x != 0f ? prefabLocalScale.x / parentScale.x : prefabLocalScale.x;
        float y = parentScale.y != 0f ? prefabLocalScale.y / parentScale.y : prefabLocalScale.y;
        float z = parentScale.z != 0f ? prefabLocalScale.z / parentScale.z : prefabLocalScale.z;

        return new Vector3(x, y, z);
    }

    private void ClearCurrentVisual()
    {
        if (currentVisualInstance != null)
        {
            Destroy(currentVisualInstance);
            currentVisualInstance = null;
        }

        currentVisualIndex = -1;
        currentVisualIsCompleted = false;
    }

    // Dishes are pickable only once completed
    public override bool CanBePickedUp(Vector3 playerPos, PlayerInteraction player = null)
    {
        if (!isCompleted) return false;

        bool heightOk = Mathf.Abs(playerPos.y - transform.position.y) <= GetPickupHeightTolerance();
        if (!heightOk) return false;

        if (GetOwner() != null && GetOwner() != player) return false;

        return true;
    }
}
