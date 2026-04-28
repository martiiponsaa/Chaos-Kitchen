using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRotation : MonoBehaviour
{
    public GameObject player;
    public Vector3 rot;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rot = player.transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(new Vector3(0, rot.y, 0));
        transform.position = player.transform.position;
    }
}
