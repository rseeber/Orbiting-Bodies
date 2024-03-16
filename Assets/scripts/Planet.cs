using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 This is a script used entirely to store data to be retrieved by the spaceKinematics script
 */

public class Planet : MonoBehaviour
{
    //TODO: move to a game manager object
    public float bigG { get; private set; }

    //in kg
    [Tooltip("in kgs x1,000")]
    [SerializeField] private float mass = 100.0f;

    //in meters
    [Tooltip("in meters (just copy the number from transform.scale)")]
    public float radius = 100.0f;

    private GameObject gameManager;

    [HideInInspector] public float partialLittleG { get; private set; } //divide by r^2 to get full value
    [HideInInspector] public float partialEscapeVelocity { get; private set; }     //just need to divide by sqrt(r)

    //velocity required to maintain orbit at the given distance r
    [HideInInspector] public float partialOrbitalVelocity { get; private set; }    //just need to divide by sqrt(r)

    // Start is called before the first frame update
    void Awake()
    {
        //convert from kgs x1000 back to regular kg
        mass *= 1000;

        gameManager = GameObject.FindGameObjectWithTag("GameController");
        bigG = gameManager.GetComponent<GameManagerScript>().bigG;

        //get all the partial calculations
        partialLittleG = bigG * mass;
        partialOrbitalVelocity = Mathf.Sqrt(bigG * mass);
        partialEscapeVelocity = Mathf.Sqrt(2 * bigG * mass);

        Debug.Log("Partial orbital velocity: " + partialOrbitalVelocity);
    }
}
