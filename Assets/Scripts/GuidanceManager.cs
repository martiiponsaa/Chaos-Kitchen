using UnityEngine;

public class GuidanceManager : MonoBehaviour
{
    [System.Serializable]
    public class RecipeStep
    {
        public GameObject ingredient;
        public GameObject slot;
    }

    [Header("Passos de la recepta en ordre")]
    public RecipeStep[] steps;

    private int currentStep = 0;
    private bool waitingForDrop = false;

    void Start()
    {
        StartCoroutine(InitWithDelay());
    }

    private System.Collections.IEnumerator InitWithDelay()
    {
        yield return null;
        if (steps.Length > 0 && steps[0].ingredient != null)
        {
            Debug.Log($"GuidanceManager: il·luminant {steps[0].ingredient.name}");
            SetHighlight(steps[0].ingredient, true);
        }
        else
        {
            Debug.LogError("GuidanceManager: no hi ha steps o ingredient és null!");
        }
    }

    public void OnIngredientPickedUp(GameObject pickedObject)
    {
        if (currentStep >= steps.Length) return;
        if (pickedObject != steps[currentStep].ingredient) return;

        SetHighlight(steps[currentStep].ingredient, false);
        SetHighlight(steps[currentStep].slot, true);
        waitingForDrop = true;
    }

    public void OnIngredientPlaced(GameObject placedObject)
    {
        if (!waitingForDrop) return;
        if (placedObject != steps[currentStep].ingredient) return;

        SetHighlight(steps[currentStep].slot, false);
        currentStep++;
        waitingForDrop = false;

        if (currentStep < steps.Length)
            SetHighlight(steps[currentStep].ingredient, true);
        else
            Debug.Log("Recepta completada!");
    }

    private void SetHighlight(GameObject obj, bool on)
    {
        if (obj == null) return;
        Highligh h = obj.GetComponent<Highligh>();
        Debug.Log($"SetHighlight: {obj.name} → {on}, Highligh trobat: {h != null}");
        if (h != null)
        {
            if (on) h.StartHighlight();
            else h.StopHighlight();
        }
    }
}