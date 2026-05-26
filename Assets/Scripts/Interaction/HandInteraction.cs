using UnityEngine;

public class HandInteraction : MonoBehaviour
{
    [SerializeField] private GameObject openHand;
    [SerializeField] private GameObject closedHand;

    private void Awake()
    {
        ResolveHandParts();

        OpenHand();
    }

    private void ResolveHandParts()
    {
        if (openHand == null)
        {
            openHand = FindChildByName("Open");
        }

        if (closedHand == null)
        {
            closedHand = FindChildByName("Closed");
        }

        if (openHand == null || closedHand == null)
        {
            Debug.LogWarning($"HandInteraction on {gameObject.name} could not find Open/Closed children. Open found: {openHand != null}, Closed found: {closedHand != null}");
        }
    }

    private GameObject FindChildByName(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != transform && child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    public void OpenHand()
    {
        if (openHand != null)
        {
            openHand.SetActive(true);
        }

        if (closedHand != null)
        {
            closedHand.SetActive(false);
        }
    }

    public void CloseHand()
    {
        if (openHand != null)
        {
            openHand.SetActive(false);
        }

        if (closedHand != null)
        {
            closedHand.SetActive(true);
        }
    }
}
