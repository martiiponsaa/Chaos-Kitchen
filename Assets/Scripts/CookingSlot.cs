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
    [Tooltip("Prefab spawned at the slot position when the ingredient burns.")]
    public GameObject burnedPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip cookedClip;
    [SerializeField] private AudioClip burntClip;
    [SerializeField] private AudioMixerGroup sfxMixerGroup; 
    private AudioSource audioSource; //AUDIO MIXER

    // ── Internal state ────────────────────────────────────────────────
    private PlacementSlot slot;
    private InteractableObject objectBeingCooked;
    private Coroutine activeSequence;

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
            yield break;
        }

        // Transform ingredient to the cooked type
        IngredientType cookedType = slot.producesIngredient;
        if (cookedType != IngredientType.None)
        {
            obj.SetIngredientType(cookedType);
        }

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
            yield break;
        }

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

    private void BurnIngredient(InteractableObject obj)
    {
        Debug.Log($"[CookingSlot] {obj.name} burned on {name}!");

        // Spawn burned visual at the slot position
        if (burnedPrefab != null)
        {
            GameObject burned = Instantiate(burnedPrefab, slot.GetPosition(), Quaternion.identity);
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
    private IEnumerator AutomaticBurnAndResetSequence(InteractableObject obj)
    {
        Debug.Log($"[CookingSlot] {obj.name} es crema! Amagant original i mostrant visual de cremat durant {burnedDisplayDuration}s.");

        // 1. TROBEM I AMAGUEM EL VISUAL DE L'HAMBURGUESA ORIGINAL
        // Busquem el MeshRenderer en el mateix objecte o en els seus fills (models 3D)
        MeshRenderer originalMesh = obj.GetComponentInChildren<MeshRenderer>();
        if (originalMesh != null)
        {
            originalMesh.enabled = false; // La fem invisible temporalment
        }

        // 2. INSTANCIEM EL PREFAB DE L'HAMBURGUESA NEGRA / FUM
        GameObject burnedVisual = null;
        if (burnedPrefab != null)
        {
            Vector3 spawnPos = slot.GetPosition(); // Pots posar un petit offset si cal: + new Vector3(0, 0.01f, 0);
            burnedVisual = Instantiate(burnedPrefab, spawnPos, Quaternion.identity);
            Destroy(burnedVisual, burnedDisplayDuration); // Es destrueix sol als 5s
            if (burntClip != null) audioSource.PlayOneShot(burntClip);
        }

        // 3. BLOQUEJEM EL SLOT (Evita que el jugador interactuï mentre està "invisible/cremada")
        slot.isOccupied = false;

        // 4. ESPEREM ELS 5 SEGONS DE PENALITZACIÓ
        yield return new WaitForSeconds(burnedDisplayDuration);

        // Seguretat: Si el jugador ha tret l'objecte o reiniciat el nivell mentrestant, sortim
        if (obj == null) yield break;

        // 5. TORNEM A MOSTRAR L'HAMBURGUESA ORIGINAL (Ara ja es veurà la bona)
        if (originalMesh != null)
        {
            originalMesh.enabled = true; // Torna a ser visible!
        }

        // 6. REINICI DE LÒGICA I RE-ACTIVACIÓ DEL SLOT
        slot.isOccupied = true;
        objectBeingCooked = obj;

        // Forcem que l'hamburguesa sigui del tipus Cuita (CookedMeat) perquè el plat l'accepti a l'instant
        IngredientType cookedType = slot.producesIngredient;
        if (cookedType != IngredientType.None)
        {
            obj.SetIngredientType(cookedType);
        }

        // Deixem la seqüència activa en null per finalitzar el procés
        activeSequence = null;
        Debug.Log($"[CookingSlot] El fum s'ha apagat. {obj.name} torna a ser visible i llista per recollir!");
    }
}
