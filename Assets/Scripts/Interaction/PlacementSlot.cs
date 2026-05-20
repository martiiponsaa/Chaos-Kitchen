using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlacementSlot : MonoBehaviour
{
    // Public state used by InteractableObject
    public bool isOccupied = false;

    // Optional explicit center transform (if not set, uses this.transform)
    public Transform center;

    // Static registry for fast lookups
    public static readonly List<PlacementSlot> all = new List<PlacementSlot>();

    private void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    // Returns the world position objects should snap to
    public Vector3 GetPosition()
    {
        return (center != null) ? center.position : transform.position;
    }

    // Visual helper in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? new Color(1f, 0.4f, 0.4f, 0.6f) : new Color(0.4f, 1f, 0.4f, 0.4f);
        Vector3 pos = GetPosition();
        Gizmos.DrawSphere(pos, 0.05f);
#if UNITY_EDITOR
        var bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = isOccupied ? new Color(1f,0.4f,0.4f,0.15f) : new Color(0.4f,1f,0.4f,0.15f);
            Gizmos.DrawCube(bc.center, bc.size);
        }
#endif
    }
}
