using UnityEngine;

/// <summary>
/// Simple placement slot used for deterministic snap-to placement.
/// Place empty GameObjects where you want items to snap to (tables, stoves, prep spots, etc.).
/// </summary>
public class PlacementSlot : MonoBehaviour
{
    public bool isOccupied = false;

    public Vector3 GetPosition()
    {
        return transform.position;
    }
}
