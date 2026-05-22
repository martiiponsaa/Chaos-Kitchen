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
    public RecipeStep[] stepsPlayer1;
    public RecipeStep[] stepsPlayer2;

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
            Debug.LogError("GuidanceManager: no hi ha steps o ingredient és null!");
        }
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
            return;
        }

        if (player == player2)
        {
            if (currentStepP2 >= (stepsPlayer2?.Length ?? 0)) return;
            if (pickedObject != stepsPlayer2[currentStepP2].ingredient) return;

            SetHighlightForOwners(stepsPlayer2[currentStepP2].ingredient, false);
            SetHighlightForOwners(stepsPlayer2[currentStepP2].slot, true);
            waitingForDropP2 = true;
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

            return;
        }
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