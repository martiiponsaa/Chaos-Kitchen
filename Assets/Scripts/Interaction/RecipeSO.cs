using UnityEngine;

[CreateAssetMenu(menuName = "ChaosKitchen/Recipe", fileName = "NewRecipe")]
public class RecipeSO : ScriptableObject
{
    public IngredientType[] steps;
}
