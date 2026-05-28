using UnityEngine;

public class PassableIngredient : MonoBehaviour
{
    private InteractableObject io;
    [Tooltip("Distància màxima a la qual s'han d'apropar els jugadors per passar-se l'ingredient")]
    [SerializeField] private float checkRadius = 1.5f;

    void Start()
    {
        io = GetComponent<InteractableObject>();
    }

    void Update()
    {
        if (io == null) return;

        // Si l'objecte no està a la mà de ningú, no fem res
        if (!io.IsHeld()) return;

        // Busquem el GuidanceManager de l'escena per saber qui són els jugadors reals
        GuidanceManager gm = FindObjectOfType<GuidanceManager>();
        if (gm == null) return;

        // Mirem qui el té agafat actualment pel script d'interacció de l'objecte
        // Com que l'amo va canviant segons qui el llança/agafa, usem el Holder actual o comprovem la distància
        PlayerInteraction p1 = gm.player1;
        PlayerInteraction p2 = gm.player2;

        if (p1 == null || p2 == null) return;

        // Calculem la distància real en el món 3D entre tots dos jugadors
        float distance = Vector3.Distance(p1.transform.position, p2.transform.position);

        // Si estan a prop...
        if (distance <= checkRadius)
        {
            // Cas A: El portador és el Player 1 (Taronja) i el volem passar al Player 2 (Blau)
            // L'objecte ha de tenir assignat com a OwnerPlayer final el Player 2
            if (io.GetOwner() == p1 && io.GetIngredientType() != IngredientType.None && p2.GetHeldObjectCount() == 0)
            {
                // Mirem si aquest objecte realment correspon al jugador 2 (com la tomata blava)
                // Per seguretat, només fem el canvi si el Player 1 el té a la llista d'objectes agafats
                var heldByP1 = p1.GetHeldObjects();
                if (heldByP1.Contains(io))
                {
                    Debug.Log($"[Traspàs] Passant {gameObject.name} de Player 1 a Player 2");

                    // Alliberem l'objecte simulant que el deixa anar a terra/slot, però el canviem al vol
                    io.PlaceDown();
                    // Netegem la llista del Player 1 manualment destruint-lo de la seva interacció
                    p1.Invoke("CheckForPlacement", 0f); // Forcem un mini-check de seguretat del joc

                    // Li donem el control absolut al segon jugador
                    io.SetOwner(p2);
                    io.PickUp(p2);

                    // Afegim manualment l'objecte a la mà de P2 com faria el seu PickUpObject intern
                    // Però com que heldObjects és privat, cridem a la funció nativa a través de l'objecte
                    Destroy(this); // Missió complerta, eliminem el script d'aquest ingredient
                }
            }
            // Cas B: El portador és el Player 2 (Blau) i el volem passar al Player 1 (Taronja)
            else if (io.GetOwner() == p2 && p1.GetHeldObjectCount() == 0)
            {
                var heldByP2 = p2.GetHeldObjects();
                if (heldByP2.Contains(io))
                {
                    Debug.Log($"[Traspàs] Passant {gameObject.name} de Player 2 a Player 1");

                    io.PlaceDown();
                    io.SetOwner(p1);
                    io.PickUp(p1);

                    Destroy(this);
                }
            }
        }
    }
}