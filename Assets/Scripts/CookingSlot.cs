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
    public void StartCooking(InteractableObject obj)
    {
        if (obj == null) return;

        // Cancel any sequence that might still be running (safety)
        StopActiveSequence();

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
        IngredientType cookedType = slot.producesIngredient;
        if (cookedType != IngredientType.None)
        {
            obj.SetIngredientType(cookedType);
        }

        ApplyCookedVisual(obj);
        obj.SetInteractionLocked(false);

        Debug.Log($"[CookingSlot] {obj.name} is ready ({cookedType}). Pickup window: {pickupWindow}s.");
        if (cookedClip != null) audioSource.PlayOneShot(cookedClip);

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

        if (cookedPrefab == null || obj == null)
        {
            SetOriginalVisualVisible(true);
            return;
        }

        DestroyCookedVisual();
        SetOriginalVisualVisible(false);
        cookedVisualInstance = SpawnVisualInstance(cookedPrefab, obj.transform);
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
        if (burnedPrefab != null)
        {
            GameObject burned = SpawnVisualInstance(burnedPrefab, obj.transform);
            Destroy(burned, burnedDisplayDuration);
        }
        else
        {
            Debug.LogWarning($"[CookingSlot] No burnedPrefab assigned on {name}. Ingredient disappears silently.");
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
        if (obj == null || obj.GetIngredientType() != IngredientType.Pizza)
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

        if (burnedPrefab != null)
        {
            burnedVisualInstance = SpawnVisualInstance(burnedPrefab, obj.transform);
        }

        if (burntClip != null)
        {
            audioSource.PlayOneShot(burntClip);
        }

        yield return new WaitForSeconds(burnedDisplayDuration);

        if (obj == null) yield break;

        DestroyBurnedVisual();
        obj.transform.position = obj.GetInitialSpawnPosition();
        slot.isOccupied = false;
        NotifyPizzaMaterialSwapEnd();
        RestoreRawVisual(obj);
        FindObjectOfType<GuidanceManager>()?.OnIngredientRespawnedAfterBurn(obj.gameObject, obj.GetOwner());
        obj.SetInteractionLocked(false);
        objectBeingCooked = null;
        originalIngredientType = IngredientType.None;

        activeSequence = null;
        Debug.Log($"[CookingSlot] Burn finished on {name}. {obj.name} has been restored to raw state.");
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
        instance.transform.localScale = prefab.transform.localScale;

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
