using UnityEngine;

public class GuidanceManager : MonoBehaviour
{
    [System.Serializable]
    public class RecipeStep
    {
        public GameObject ingredient;   // cilindre a agafar
        public GameObject slot;         // zona on deixar-lo
    }

    [Header("Passos de la recepta en ordre")]
    public RecipeStep[] steps; 

    private int currentStep = 0;
    private bool waitingForDrop = false;

    void Start()
    {
        if (steps.Length > 0)
            SetHighlight(steps[0].ingredient, true);
    }

    public void OnIngredientPickedUp(GameObject pickedObject)
    {
        if (currentStep >= steps.Length) return;

        // Comprova que és l'ingredient correcte
        if (pickedObject != steps[currentStep].ingredient) return;

        // Apaga ingredient, il·lumina slot
        SetHighlight(steps[currentStep].ingredient, false);
        SetHighlight(steps[currentStep].slot, true);
        waitingForDrop = true;
    }

    public void OnIngredientPlaced(GameObject placedObject)
    {
        if (!waitingForDrop) return;
        if (placedObject != steps[currentStep].ingredient) return;

        // Apaga slot
        SetHighlight(steps[currentStep].slot, false);
        currentStep++;
        waitingForDrop = false;

        // Il·lumina el següent ingredient
        if (currentStep < steps.Length)
            SetHighlight(steps[currentStep].ingredient, true);
        else
            Debug.Log("Recepta completada!");
    }

    private void SetHighlight(GameObject obj, bool on)
    {
        if (obj == null) return;
        Highligh h = obj.GetComponent<Highligh>();
        if (h != null)
        {
            if (on) h.StartHighlight();
            else h.StopHighlight();
        }
    }
}