using UnityEngine;

// Example placeholder action that would run when a tomato is applied
public class ApplyTomatoAction : BaseIngredientAction
{
    public override void Execute(UnityEngine.MonoBehaviour dishContext)
    {
        Debug.Log($"ApplyTomatoAction executed on {dishContext.gameObject.name}");
        // Placeholder: implement the small extra action (e.g., spread, press)
    }
}
