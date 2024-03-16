using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompassControl : MonoBehaviour
{

    public Transform destination;

    // Update is called once per frame
    void Update()
    {
        //always point towards the destination
        transform.LookAt(destination.position);
    }
}
