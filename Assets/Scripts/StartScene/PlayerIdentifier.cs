using UnityEngine;

/// <summary>
/// Small helper to mark a GameObject as a player with an integer id.
/// Attach this to the player cube used by your HTC sensor template if needed.
/// </summary>
public class PlayerIdentifier : MonoBehaviour
{
    [Tooltip("Numeric player id (e.g. 1 or 2)")]
    public int PlayerId = 1;
}
