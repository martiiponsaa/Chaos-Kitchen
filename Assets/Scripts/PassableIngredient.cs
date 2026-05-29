using UnityEngine;

[DisallowMultipleComponent]
public class PassableIngredient : MonoBehaviour
{
    private void Awake()
    {
        InteractableObject interactableObject = GetComponent<InteractableObject>();
        if (interactableObject != null)
        {
            interactableObject.SetPassableIngredient(true);
        }
    }
}