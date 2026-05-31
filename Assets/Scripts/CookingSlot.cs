using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

[RequireComponent(typeof(PlacementSlot))]
public class CookingSlot : MonoBehaviour
{
    [Header("Timings")]
    [Tooltip("Seconds the ingredient cooks before becoming ready to pick up.")]
    public float cookDuration = 10f;

    [Tooltip("Seconds the player has to pick up the cooked ingredient before it burns.")]
    public float pickupWindow = 10f;

    [Tooltip("Seconds the burned prefab stays visible before auto-destroying.")]
    public float burnedDisplayDuration = 5f;

    [Header("Visuals")]
    [Tooltip("Prefab shown while the ingredient is cooked and waiting to be picked up.")]
    public GameObject cookedPrefab;

    [Tooltip("Prefab spawned at the slot position when the ingredient burns.")]
    public GameObject burnedPrefab;

    [System.Serializable]
    private class IngredientVisualPair
    {
        public IngredientType ingredientType = IngredientType.None;
        public GameObject cookedPrefab;
        public GameObject burnedPrefab;
        public AudioClip cookedClip;
        public AudioClip burntClip;
    }

    [Tooltip("Optional ingredient-specific cooked and burned prefabs. Entries override the default prefabs for the matching raw ingredient type.")]
    [SerializeField] private IngredientVisualPair[] ingredientVisualPairs = new IngredientVisualPair[0];

    [Tooltip("Offset applied to the burned visual relative to the slot position.")]
    [SerializeField] private Vector3 burnedVisualLocalOffset = Vector3.zero;

    [Header("Audio")]
    [SerializeField] private AudioClip cookedClip;
    [SerializeField] private AudioClip burntClip;
    [SerializeField] private AudioClip cooking;                 // clip to play while cooking (loop)
    [SerializeField] private AudioMixerGroup sfxMixerGroup; 
    private AudioSource audioSource; //AUDIO MIXER

    // ── Internal state ────────────────────────────────────────────────
    private PlacementSlot slot;
    private InteractableObject objectBeingCooked;
    private Coroutine activeSequence;
    private Renderer[] originalRenderers;
    private GameObject cookedVisualInstance;
    private GameObject burnedVisualInstance;
    private IngredientType originalIngredientType = IngredientType.None;

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        slot = GetComponent<PlacementSlot>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    // ── Public API called by PlacementSlot.OnObjectPlaced ────────────

    /// <summary>
    /// Kick off the cooking sequence for <obj>.
    /// Called by PlacementSlot.OnObjectPlaced when this component is present.
    /// </summary>
    public bool StartCooking(InteractableObject obj)
    {
        if (obj == null) return false;

        // If already cooking, refuse to start a new one
        if (objectBeingCooked != null)
        {
            Debug.LogWarning($"[CookingSlot] Cannot start cooking {obj.name} on {name}: slot already busy with {objectBeingCooked.name}.");
            return false;
        }

        objectBeingCooked = obj;
        originalIngredientType = obj.GetIngredientType();
        NotifyPizzaMaterialSwapStart(obj);
        CacheOriginalRenderers(obj);
        DestroyCookedVisual();
        DestroyBurnedVisual();
        SetOriginalVisualVisible(true);
        obj.SetInteractionLocked(true);
        PlayCookingLoop();
        activeSequence = StartCoroutine(CookingSequence(obj));
        return true;
    }

    public bool IsBusy()
    {
        return objectBeingCooked != null;
    }

    /// <summary>
    /// Cancel the running sequence without burning — called when the player
    /// picks up the object before the pickup window expires.
    /// InteractableObject.PickUp already frees the slot; we just stop the coroutine.
    /// </summary>
    public void CancelCooking()
    {
        StopActiveSequence();
        NotifyPizzaMaterialSwapEnd();
        objectBeingCooked = null;
        Debug.Log($"[CookingSlot] Cooking cancelled on {name} (ingredient picked up).");
    }

    // ── Cooking sequence ─────────────────────────────────────────────

    private IEnumerator CookingSequence(InteractableObject obj)
    {
        // ── Phase 1: cooking ─────────────────────────────────────────
        Debug.Log($"[CookingSlot] {obj.name} started cooking on {name}. Ready in {cookDuration}s.");

        yield return new WaitForSeconds(cookDuration);

        // If the object was picked up during cooking the reference will be cleared
        if (!IsStillOnSlot(obj))
        {
            activeSequence = null;
            objectBeingCooked = null;
            StopCookingLoop();
            NotifyPizzaMaterialSwapEnd();
            yield break;
        }

        // Transform ingredient to the cooked type
        IngredientType cookedType = slot.GetProducedIngredient(obj.GetIngredientType());
        if (cookedType != IngredientType.None)
        {
            obj.SetIngredientType(cookedType);
        }

        ApplyCookedVisual(obj);
        obj.SetInteractionLocked(false);

        Debug.Log($"[CookingSlot] {obj.name} is ready ({cookedType}). Pickup window: {pickupWindow}s.");
        AudioClip cookedAudio = GetCookedClipForIngredient(originalIngredientType);
        if (cookedAudio != null) audioSource.PlayOneShot(cookedAudio);

        // ── Phase 2: pickup window ────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < pickupWindow)
        {
            // slot.isOccupied is set to false by InteractableObject.PickUp
            if (!IsStillOnSlot(obj))
            {
                Debug.Log($"[CookingSlot] Ingredient picked up in time on {name}.");
                activeSequence = null;
                objectBeingCooked = null;
                originalIngredientType = IngredientType.None;
                StopCookingLoop();
                NotifyPizzaMaterialSwapEnd();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ── Phase 3: burn ─────────────────────────────────────────────
        if (!IsStillOnSlot(obj))
        {
            // Picked up on the very last frame — still counts as success
            activeSequence = null;
            objectBeingCooked = null;
            originalIngredientType = IngredientType.None;
            StopCookingLoop();
            NotifyPizzaMaterialSwapEnd();
            yield break;
        }

        obj.SetInteractionLocked(true);
        yield return StartCoroutine(AutomaticBurnAndResetSequence(obj));
        //if (burntClip != null) audioSource.PlayOneShot(burntClip);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns true while the object is still sitting on this slot
    /// (i.e. has not been picked up and is the one we started cooking).
    /// </summary>
    private bool IsStillOnSlot(InteractableObject obj)
    {
        // obj destroyed (shouldn't happen mid-cook but be safe)
        if (obj == null) return false;
        // Reference was cleared by CancelCooking
        if (objectBeingCooked != obj) return false;
        // Slot freed by PickUp
        if (!slot.isOccupied) return false;

        return true;
    }

    private void CacheOriginalRenderers(InteractableObject obj)
    {
        if (obj == null) return;
        originalRenderers = obj.GetComponentsInChildren<Renderer>(true);
    }

    private void SetOriginalVisualVisible(bool visible)
    {
        if (originalRenderers == null) return;

        for (int i = 0; i < originalRenderers.Length; i++)
        {
            Renderer renderer = originalRenderers[i];
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void DestroyCookedVisual()
    {
        if (cookedVisualInstance != null)
        {
            Destroy(cookedVisualInstance);
            cookedVisualInstance = null;
        }
    }

    private void DestroyBurnedVisual()
    {
        if (burnedVisualInstance != null)
        {
            Destroy(burnedVisualInstance);
            burnedVisualInstance = null;
        }
    }

    private void ApplyCookedVisual(InteractableObject obj)
    {
        DestroyBurnedVisual();

        if (obj == null)
        {
            SetOriginalVisualVisible(true);
            return;
        }

        GameObject prefabToSpawn = GetCookedPrefabForIngredient(originalIngredientType);
        if (prefabToSpawn == null)
        {
            SetOriginalVisualVisible(true);
            return;
        }

        DestroyCookedVisual();
        SetOriginalVisualVisible(false);
        cookedVisualInstance = SpawnVisualInstance(prefabToSpawn, obj.transform);
    }

    private void RestoreRawVisual(InteractableObject obj)
    {
        DestroyCookedVisual();
        SetOriginalVisualVisible(true);

        if (obj != null && originalIngredientType != IngredientType.None)
        {
            obj.SetIngredientType(originalIngredientType);
        }
    }

    private void BurnIngredient(InteractableObject obj)
    {
        Debug.Log($"[CookingSlot] {obj.name} burned on {name}!");

        // Spawn burned visual at the slot position
        GameObject prefabToSpawn = GetBurnedPrefabForIngredient(originalIngredientType);
        if (prefabToSpawn != null)
        {
            GameObject burned = SpawnVisualInstance(prefabToSpawn, obj.transform);
            Destroy(burned, burnedDisplayDuration);
        }
        else
        {
            Debug.LogWarning($"[CookingSlot] No burned prefab assigned on {name} for {originalIngredientType}. Ingredient disappears silently.");
        }

        // Free the slot and destroy the original ingredient
        slot.isOccupied = false;
        objectBeingCooked = null;
        activeSequence = null;
        NotifyPizzaMaterialSwapEnd();

        Destroy(obj.gameObject);
    }

    private void StopActiveSequence()
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }
    }

    private void NotifyPizzaMaterialSwapStart(InteractableObject obj)
    {
        if (obj == null)
        {
            return;
        }

        // Support multiple pizza-like ingredient types (e.g., Pizza, Pizza1)
        string name = obj.GetIngredientType().ToString();
        if (!name.Contains("Pizza"))
        {
            return;
        }

        PizzaOvenMaterialSwap swap = GetComponent<PizzaOvenMaterialSwap>();
        if (swap != null)
        {
            swap.OnPizzaCookingStarted();
        }
    }

    private void NotifyPizzaMaterialSwapEnd()
    {
        PizzaOvenMaterialSwap swap = GetComponent<PizzaOvenMaterialSwap>();
        if (swap != null)
        {
            swap.OnPizzaCookingEnded();
        }
    }

    private IEnumerator AutomaticBurnAndResetSequence(InteractableObject obj)
    {
        Debug.Log($"[CookingSlot] {obj.name} burned on {name}! Showing burn visual for {burnedDisplayDuration}s.");

        DestroyCookedVisual();
        SetOriginalVisualVisible(false);
        StopCookingLoop();

        GameObject prefabToSpawn = GetBurnedPrefabForIngredient(originalIngredientType);
        if (prefabToSpawn != null)
        {
            burnedVisualInstance = SpawnVisualInstance(prefabToSpawn, obj.transform);
        }

        AudioClip burntAudio = GetBurntClipForIngredient(originalIngredientType);
        if (burntAudio != null)
        {
            audioSource.PlayOneShot(burntAudio);
        }

        yield return new WaitForSeconds(burnedDisplayDuration);

        if (obj == null) yield break;

        DestroyBurnedVisual();
        obj.transform.position = obj.GetInitialSpawnPosition();
        slot.isOccupied = false;
        NotifyPizzaMaterialSwapEnd();
        RestoreRawVisual(obj);
        obj.RestoreInitialPassableIngredientState();
        FindObjectOfType<GuidanceManager>()?.OnIngredientRespawnedAfterBurn(obj.gameObject, obj.GetOwner());
        obj.SetInteractionLocked(false);
        objectBeingCooked = null;
        originalIngredientType = IngredientType.None;

        activeSequence = null;
        Debug.Log($"[CookingSlot] Burn finished on {name}. {obj.name} has been restored to raw state.");
    }

    private GameObject GetCookedPrefabForIngredient(IngredientType ingredientType)
    {
        IngredientVisualPair pair = GetVisualPair(ingredientType);
        if (pair != null && pair.cookedPrefab != null)
        {
            return pair.cookedPrefab;
        }

        return cookedPrefab;
    }

    private GameObject GetBurnedPrefabForIngredient(IngredientType ingredientType)
    {
        IngredientVisualPair pair = GetVisualPair(ingredientType);
        if (pair != null && pair.burnedPrefab != null)
        {
            return pair.burnedPrefab;
        }

        return burnedPrefab;
    }

    private IngredientVisualPair GetVisualPair(IngredientType ingredientType)
    {
        if (ingredientVisualPairs == null)
        {
            return null;
        }

        for (int i = 0; i < ingredientVisualPairs.Length; i++)
        {
            IngredientVisualPair pair = ingredientVisualPairs[i];
            if (pair != null && pair.ingredientType == ingredientType)
            {
                return pair;
            }
        }

        return null;
    }

    private AudioClip GetCookedClipForIngredient(IngredientType ingredientType)
    {
        IngredientVisualPair pair = GetVisualPair(ingredientType);
        if (pair != null && pair.cookedClip != null)
        {
            return pair.cookedClip;
        }

        return cookedClip;
    }

    private AudioClip GetBurntClipForIngredient(IngredientType ingredientType)
    {
        IngredientVisualPair pair = GetVisualPair(ingredientType);
        if (pair != null && pair.burntClip != null)
        {
            return pair.burntClip;
        }

        return burntClip;
    }

    private GameObject SpawnVisualInstance(GameObject prefab, Transform parent)
    {
        if (prefab == null || parent == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        Vector3 targetWorldScale = prefab.transform.localScale;
        Vector3 parentScale = parent.lossyScale;
        instance.transform.localScale = new Vector3(
            parentScale.x != 0f ? targetWorldScale.x / parentScale.x : targetWorldScale.x,
            parentScale.y != 0f ? targetWorldScale.y / parentScale.y : targetWorldScale.y,
            parentScale.z != 0f ? targetWorldScale.z / parentScale.z : targetWorldScale.z);

        MakeVisualOnly(instance);
        return instance;
    }

    private void MakeVisualOnly(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        foreach (InteractableObject interactable in instance.GetComponentsInChildren<InteractableObject>(true))
        {
            Destroy(interactable);
        }

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            Destroy(collider);
        }

        foreach (Rigidbody rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            Destroy(rigidbody);
        }
    }

    private void PlayCookingLoop()
    {
        if (audioSource == null || cooking == null)
        {
            return;
        }

        audioSource.clip = cooking;
        audioSource.loop = true;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void StopCookingLoop()
    {
        if (audioSource == null)
        {
            return;
        }

        if (audioSource.isPlaying && audioSource.clip == cooking)
        {
            audioSource.Stop();
        }

        audioSource.loop = false;
        if (audioSource.clip == cooking)
        {
            audioSource.clip = null;
        }
    }
}
