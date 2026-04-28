using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public GameObject playerToFollow;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!playerToFollow.GetComponent<DetectCollisions>())
        {
            transform.localScale = Vector3.one * (1 + playerToFollow.transform.position.y) * -2;
            transform.position = playerToFollow.transform.position;
        }

    }
}
