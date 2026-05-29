using System.Collections.Generic;
using UnityEngine;

public class PizzaOvenMaterialSwap : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Renderer root for the oven. If empty, uses this GameObject.")]
    [SerializeField] private GameObject ovenObject;
    [Tooltip("Semi-transparent material to show on the oven while pizza is cooking.")]
    [SerializeField] private Material ovenCookingMaterial;

    [Tooltip("Renderer root for the table. If empty, uses this GameObject.")]
    [SerializeField] private GameObject tableObject;
    [Tooltip("Semi-transparent material to show on the table while pizza is cooking.")]
    [SerializeField] private Material tableCookingMaterial;

    [Header("Behavior")]
    [Tooltip("If enabled, renderers are collected from child objects too.")]
    [SerializeField] private bool includeChildren = true;

    private readonly List<MaterialSnapshot> ovenSnapshots = new List<MaterialSnapshot>();
    private readonly List<MaterialSnapshot> tableSnapshots = new List<MaterialSnapshot>();
    private bool isCookingPizza;

    private struct MaterialSnapshot
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    private void Awake()
    {
        if (ovenObject == null)
        {
            ovenObject = gameObject;
        }

        CacheMaterials(ovenObject, ovenSnapshots);
        CacheMaterials(tableObject, tableSnapshots);
    }

    public void OnPizzaCookingStarted()
    {
        if (isCookingPizza)
        {
            return;
        }

        isCookingPizza = true;
        ApplyCookingMaterials();
    }

    public void OnPizzaCookingEnded()
    {
        if (!isCookingPizza)
        {
            return;
        }

        isCookingPizza = false;
        RestoreOriginalMaterials();
    }

    private void CacheMaterials(GameObject targetObject, List<MaterialSnapshot> snapshots)
    {
        snapshots.Clear();

        if (targetObject == null)
        {
            return;
        }

        Renderer[] renderers = includeChildren
            ? targetObject.GetComponentsInChildren<Renderer>(true)
            : targetObject.GetComponents<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            snapshots.Add(new MaterialSnapshot
            {
                renderer = renderer,
                originalMaterials = renderer.sharedMaterials
            });
        }
    }

    private void ApplyCookingMaterials()
    {
        ApplyMaterialToSnapshots(ovenSnapshots, ovenCookingMaterial);
        ApplyMaterialToSnapshots(tableSnapshots, tableCookingMaterial);
    }

    private void RestoreOriginalMaterials()
    {
        RestoreSnapshots(ovenSnapshots);
        RestoreSnapshots(tableSnapshots);
    }

    private void ApplyMaterialToSnapshots(List<MaterialSnapshot> snapshots, Material cookingMaterial)
    {
        if (cookingMaterial == null)
        {
            return;
        }

        foreach (MaterialSnapshot snapshot in snapshots)
        {
            if (snapshot.renderer == null)
            {
                continue;
            }

            Material[] replacementMaterials = BuildReplacementMaterials(snapshot.originalMaterials, cookingMaterial);
            snapshot.renderer.sharedMaterials = replacementMaterials;
        }
    }

    private void RestoreSnapshots(List<MaterialSnapshot> snapshots)
    {
        foreach (MaterialSnapshot snapshot in snapshots)
        {
            if (snapshot.renderer == null || snapshot.originalMaterials == null)
            {
                continue;
            }

            snapshot.renderer.sharedMaterials = snapshot.originalMaterials;
        }
    }

    private Material[] BuildReplacementMaterials(Material[] originals, Material replacement)
    {
        int count = originals != null && originals.Length > 0 ? originals.Length : 1;
        Material[] materials = new Material[count];

        for (int i = 0; i < count; i++)
        {
            materials[i] = replacement;
        }

        return materials;
    }
}