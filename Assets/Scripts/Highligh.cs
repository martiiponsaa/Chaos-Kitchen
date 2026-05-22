using UnityEngine;

public class Highligh : MonoBehaviour
{
    [Header("Materials (fallback)")]
    public Material highlightMaterial;
    public Material defaultMaterial;

    [Header("Pulse Settings")]
    public bool usePulse = true;
    public Color pulseColor = new Color(0xBF/255f, 0x95/255f, 0x11/255f);
    [Tooltip("Pulse cycles per second")]
    public float pulseSpeed = 0.5f;
    [Tooltip("Maximum emission intensity multiplier")]
    public float pulseIntensity = 1.5f;

    [Tooltip("Legacy blink speed (fallback)")]
    public float blinkSpeed = 1.5f;

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private int emissionID;
    private bool isBlinking = false;
    private float timer = 0f;
    private bool showingHighlight = false;
    private Color baseEmissionColor = Color.black;

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        mpb = new MaterialPropertyBlock();
        emissionID = Shader.PropertyToID("_EmissionColor");

        // Read base emission color if available
        var mat = rend.sharedMaterial;
        if (mat != null && mat.HasProperty(emissionID))
        {
            baseEmissionColor = mat.GetColor(emissionID);
        }

        // If a highlight material defines an emission color, use it as default pulse color
        if (highlightMaterial != null && highlightMaterial.HasProperty(emissionID))
        {
            pulseColor = highlightMaterial.GetColor(emissionID);
        }

        // Initial visual state
        if (defaultMaterial != null)
            rend.sharedMaterial = defaultMaterial;
        ApplyBaseEmission();
    }

    void Update()
    {
        if (rend == null) return;
        if (!isBlinking) return;

        if (usePulse && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(emissionID))
        {
            timer += Time.deltaTime * pulseSpeed;
            float intensity = (Mathf.Sin(timer * Mathf.PI * 2f) * 0.5f + 0.5f) * pulseIntensity;
            Color c = pulseColor * intensity;
            mpb.SetColor(emissionID, c);
            rend.SetPropertyBlock(mpb);
        }
        else
        {
            // Legacy discrete blink fallback
            timer += Time.deltaTime * blinkSpeed;
            if (timer >= 1f)
            {
                timer = 0f;
                showingHighlight = !showingHighlight;
                rend.material = showingHighlight ? (highlightMaterial ?? rend.material) : (defaultMaterial ?? rend.material);
            }
        }
    }

    public void StartHighlight(Color? overrideColor = null)
    {
        if (rend == null) return;
        if (overrideColor.HasValue) pulseColor = overrideColor.Value;
        isBlinking = true;
        timer = 0f;
        showingHighlight = true;

        if (usePulse && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(emissionID))
        {
            // Ensure emission is enabled on an instance material so the property shows
            var inst = rend.material;
            if (inst != null)
                inst.EnableKeyword("_EMISSION");
        }
        else
        {
            if (highlightMaterial != null)
                rend.material = highlightMaterial;
        }
    }

    public void StopHighlight()
    {
        if (rend == null) return;
        isBlinking = false;
        timer = 0f;
        showingHighlight = false;

        if (usePulse && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(emissionID))
        {
            ApplyBaseEmission();
            // disable emission keyword on instance if present
            if (rend.material != null)
                rend.material.DisableKeyword("_EMISSION");
        }
        else
        {
            if (defaultMaterial != null)
                rend.material = defaultMaterial;
        }
    }

    private void ApplyBaseEmission()
    {
        mpb.Clear();
        mpb.SetColor(emissionID, baseEmissionColor);
        rend.SetPropertyBlock(mpb);
    }
}