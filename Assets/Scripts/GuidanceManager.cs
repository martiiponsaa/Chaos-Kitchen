using UnityEngine;
using TMPro;

public class GuidanceManager : MonoBehaviour
{
    [System.Serializable]
    public class RecipeStep
    {
        public GameObject ingredient;
        public GameObject slot;
        [TextArea(2, 4)] public string pickupInstructionText;
        [TextArea(2, 4)] public string placeInstructionText;
        [HideInInspector] public string instructionText;
    }

    [Header("Recipe Steps In Order")]
    public RecipeStep[] stepsPlayer1;
    public RecipeStep[] stepsPlayer2;

    [Header("Recipe UI")]
    [SerializeField] private TextMeshProUGUI player1StepText;
    [SerializeField] private TextMeshProUGUI player2StepText;

    private int currentStepP1 = 0;
    private int currentStepP2 = 0;
    private bool waitingForDropP1 = false;
    private bool waitingForDropP2 = false;

    [Header("Players")]
    public PlayerInteraction player1;
    public PlayerInteraction player2;

    [Header("Highlight Colors")]
    public Color player1Color = new Color(0xBF/255f, 0x95/255f, 0x11/255f);
    public Color player2Color = new Color(0x18/255f, 0x5F/255f, 0xFF/255f);

    void Start()
    {
        StartCoroutine(InitWithDelay());
    }

    private System.Collections.IEnumerator InitWithDelay()
    {
        yield return null;
        if (stepsPlayer1 != null && stepsPlayer1.Length > 0 && stepsPlayer1[0].ingredient != null)
        {
            Debug.Log($"GuidanceManager P1: highlighting {stepsPlayer1[0].ingredient.name}");
            SetHighlightForOwners(stepsPlayer1[0].ingredient, true);
        }

        if (stepsPlayer2 != null && stepsPlayer2.Length > 0 && stepsPlayer2[0].ingredient != null)
        {
            Debug.Log($"GuidanceManager P2: highlighting {stepsPlayer2[0].ingredient.name}");
            SetHighlightForOwners(stepsPlayer2[0].ingredient, true);
        }
        else
        {
            Debug.LogError("GuidanceManager: no steps are configured or ingredient is null.");
        }

        UpdateAllInstructionTexts();
    }

    public void OnIngredientPickedUp(GameObject pickedObject, PlayerInteraction player)
    {
        if (player == player1)
        {
            if (currentStepP1 >= (stepsPlayer1?.Length ?? 0)) return;
            if (pickedObject != stepsPlayer1[currentStepP1].ingredient) return;

            SetHighlightForOwners(stepsPlayer1[currentStepP1].ingredient, false);
            SetHighlightForOwners(stepsPlayer1[currentStepP1].slot, true);
            waitingForDropP1 = true;
            UpdatePlayerInstructionText(true);
            return;
        }

        if (player == player2)
        {
            if (currentStepP2 >= (stepsPlayer2?.Length ?? 0)) return;
            if (pickedObject != stepsPlayer2[currentStepP2].ingredient) return;

            SetHighlightForOwners(stepsPlayer2[currentStepP2].ingredient, false);
            SetHighlightForOwners(stepsPlayer2[currentStepP2].slot, true);
            waitingForDropP2 = true;
            UpdatePlayerInstructionText(false);
            return;
        }
    }

    public void OnIngredientPlaced(GameObject placedObject, PlayerInteraction player)
    {
        if (player == player1)
        {
            if (!waitingForDropP1) return;
            if (placedObject != stepsPlayer1[currentStepP1].ingredient) return;

            SetHighlightForOwners(stepsPlayer1[currentStepP1].slot, false);
            currentStepP1++;
            waitingForDropP1 = false;

            if (currentStepP1 < (stepsPlayer1?.Length ?? 0))
                SetHighlightForOwners(stepsPlayer1[currentStepP1].ingredient, true);
            else
                Debug.Log("Player1 recipe completed!");

            UpdatePlayerInstructionText(true);

            return;
        }

        if (player == player2)
        {
            if (!waitingForDropP2) return;
            if (placedObject != stepsPlayer2[currentStepP2].ingredient) return;

            SetHighlightForOwners(stepsPlayer2[currentStepP2].slot, false);
            currentStepP2++;
            waitingForDropP2 = false;

            if (currentStepP2 < (stepsPlayer2?.Length ?? 0))
                SetHighlightForOwners(stepsPlayer2[currentStepP2].ingredient, true);
            else
                Debug.Log("Player2 recipe completed!");

            UpdatePlayerInstructionText(false);

            return;
        }
    }

    public void OnIngredientRespawnedAfterBurn(GameObject respawnedObject, PlayerInteraction player = null)
    {
        if (respawnedObject == null) return;

        bool updatedP1 = TryRewindPlayerStep(true, respawnedObject, player);
        bool updatedP2 = TryRewindPlayerStep(false, respawnedObject, player);

        if (!updatedP1 && !updatedP2)
        {
            return;
        }

        UpdateAllInstructionTexts();
    }

    private void UpdateAllInstructionTexts()
    {
        UpdatePlayerInstructionText(true);
        UpdatePlayerInstructionText(false);
    }

    private void UpdatePlayerInstructionText(bool isPlayer1)
    {
        TextMeshProUGUI targetText = isPlayer1 ? player1StepText : player2StepText;
        if (targetText == null) return;

        RecipeStep[] steps = isPlayer1 ? stepsPlayer1 : stepsPlayer2;
        int currentStep = isPlayer1 ? currentStepP1 : currentStepP2;
        bool waitingForDrop = isPlayer1 ? waitingForDropP1 : waitingForDropP2;

        targetText.text = BuildInstructionText(steps, currentStep, waitingForDrop);
    }

    private string BuildInstructionText(RecipeStep[] steps, int currentStep, bool waitingForDrop)
    {
        if (steps == null || steps.Length == 0)
        {
            return "no recipe steps configured.";
        }

        if (currentStep >= steps.Length)
        {
            return "recipe completed!";
        }

        RecipeStep step = steps[currentStep];
        string customText = waitingForDrop
            ? step != null && !string.IsNullOrWhiteSpace(step.placeInstructionText)
                ? step.placeInstructionText
                : null
            : step != null && !string.IsNullOrWhiteSpace(step.pickupInstructionText)
                ? step.pickupInstructionText
                : null;

        if (!string.IsNullOrWhiteSpace(customText))
        {
            return $"({currentStep + 1}/{steps.Length}): {customText}";
        }

        string ingredientName = step != null && step.ingredient != null ? step.ingredient.name : "ingredient";
        string slotName = step != null && step.slot != null ? step.slot.name : "target slot";
        string action = waitingForDrop
            ? $"Place {ingredientName} on {slotName}."
            : $"Pick up {ingredientName}.";

        return $"({currentStep + 1}/{steps.Length}): {action}";
    }

    private bool TryRewindPlayerStep(bool isPlayer1, GameObject respawnedObject, PlayerInteraction player)
    {
        RecipeStep[] steps = isPlayer1 ? stepsPlayer1 : stepsPlayer2;
        int currentStep = isPlayer1 ? currentStepP1 : currentStepP2;
        bool waitingForDrop = isPlayer1 ? waitingForDropP1 : waitingForDropP2;

        if (steps == null || currentStep <= 0 || currentStep > steps.Length)
        {
            return false;
        }

        if (player != null)
        {
            if ((isPlayer1 && player != player1) || (!isPlayer1 && player != player2))
            {
                return false;
            }
        }

        RecipeStep previousStep = steps[currentStep - 1];
        if (previousStep == null || previousStep.ingredient != respawnedObject)
        {
            return false;
        }

        if (waitingForDrop)
        {
            SetHighlightForOwners(steps[currentStep].slot, false);
        }

        SetHighlightForOwners(steps[currentStep].ingredient, false);

        if (isPlayer1)
        {
            currentStepP1 = currentStep - 1;
            waitingForDropP1 = false;
            if (currentStepP1 < stepsPlayer1.Length)
            {
                SetHighlightForOwners(stepsPlayer1[currentStepP1].ingredient, true);
            }
        }
        else
        {
            currentStepP2 = currentStep - 1;
            waitingForDropP2 = false;
            if (currentStepP2 < stepsPlayer2.Length)
            {
                SetHighlightForOwners(stepsPlayer2[currentStepP2].ingredient, true);
            }
        }

        return true;
    }

    private void SetHighlight(GameObject obj, bool on)
    {
        if (obj == null) return;
        Highligh h = obj.GetComponentInChildren<Highligh>();
        Debug.Log($"SetHighlight: {obj.name} → {on}, Highligh trobat: {h != null}");
        if (h != null)
        {
            if (on) h.StartHighlight();
            else h.StopHighlight();
        }
    }

    // Highlight object depending on its owner (player1/player2)
    private void SetHighlightForOwners(GameObject obj, bool on)
    {
        if (obj == null) return;

        // Try to determine owner from InteractableObject or PlacementSlot
        var io = obj.GetComponent<InteractableObject>();
        PlayerInteraction owner = null;
        if (io != null) owner = io.GetOwner();
        else
        {
            var slot = obj.GetComponent<PlacementSlot>();
            if (slot != null) owner = slot.assignedPlayer;
        }

        var h = obj.GetComponentInChildren<Highligh>();
        if (h == null) return;

        if (owner == null)
        {
            // Unassigned -> white
            if (on) h.StartHighlight(Color.white);
            else h.StopHighlight();
            return;
        }

        if (owner == player1)
        {
            if (on) h.StartHighlight(player1Color);
            else h.StopHighlight();
            return;
        }

        if (owner == player2)
        {
            if (on) h.StartHighlight(player2Color);
            else h.StopHighlight();
            return;
        }
    }
}