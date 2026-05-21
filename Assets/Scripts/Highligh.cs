using UnityEngine;

public class Highligh : MonoBehaviour
{
    public Material highlightMaterial;
    public Material defaultMaterial;

    [Tooltip("Velocitat del parpelleig")]
    public float blinkSpeed = 0.8f;

    private Renderer rend;
    private bool isBlinking = false;
    private float timer = 0f;
    private bool showingHighlight = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null) return;
        isBlinking = false;
        showingHighlight = false;
        rend.material = defaultMaterial;
    }

    void Update()
    {
        if (rend == null) return;
        if (!isBlinking) return;

        timer += Time.deltaTime * blinkSpeed;
        if (timer >= 1f)
        {
            timer = 0f;
            showingHighlight = !showingHighlight;
            rend.material = showingHighlight ? highlightMaterial : defaultMaterial;
        }
    }

    public void StartHighlight()
    {
        if (rend == null) return;
        isBlinking = true;
        timer = 0f;
        showingHighlight = true;
        rend.material = highlightMaterial;
    }

    public void StopHighlight()
    {
        if (rend == null) return;
        isBlinking = false;
        timer = 0f;
        showingHighlight = false;
        rend.material = defaultMaterial;
    }
}