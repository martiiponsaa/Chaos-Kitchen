using UnityEngine;

// Simple base class for ingredient actions — override Execute to implement behavior
public abstract class BaseIngredientAction : MonoBehaviour, IIngredientAction
{
    public abstract void Execute(UnityEngine.MonoBehaviour dishContext);
}
