using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowScript : MonoBehaviour
{
    [SerializeField]
    private GameObject followObject;

    void Update()
    {
        this.transform.position 
            = followObject.transform.position;
    }
}
