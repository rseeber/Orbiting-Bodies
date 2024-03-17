using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceKinematicsRefactor : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private bool isRocket = false;

    //in Newtons
    [SerializeField] private float thrustForce = 500;
    //in x1,000 Newton Minutes
    [SerializeField] private float fuelCapacity = 100.0f;
    private float fuel;

    //this is the final Force vector for thrust
    private Vector3 finalThrust = Vector3.zero;

    private List<Planet> planetList = new List<Planet>();
    [SerializeField] private GameManagerScript gameManager;

    void Start() {
        rb = GetComponent<Rigidbody>();

        fuel = fuelCapacity;

        //first find the gameManager gameObject (it needs the "GameController" tag!), then get the script attached to it
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManagerScript>();

        planetList = gameManager.planets;
    }

    void FixedUpdate() {
        //FORCE
        //thrust
        if (isRocket) {
            //unit vector for direction of Forward-Thrust
            Vector3 dir = transform.TransformDirection(Vector3.up);

            float thrustInput = Input.GetAxis("Thrust");
            //direction (dir) * quantity of force (thrustForce) * sign (+/-) (thrustInput)
            finalThrust = dir * thrustForce * thrustInput;

            //remember to remove fuel (in x1,000 Newton Minutes)
            if (Mathf.Abs(thrustInput) > 0.01) {
                fuel -= (thrustForce / 1000) * (Time.deltaTime / 60);
            }
        }

        //gravity
        Vector3 netGravity = Vector3.zero;
        foreach(Planet p in planetList) {
            Vector3 radial = Vector3.zero;
            netGravity += getGravity(p.gameObject, ref radial, p.partialLittleG);

            //should probably do something to have a "currentPlanet", so we can calculate escapeVelocity, etc
        }

    }

    //planet is the planet you are being pulled towards
    //radial is the r vector you want to be overwritten with a vector pointing towards the planet
    //usually this is something you only do in C, but like procedural programming so I'm doing it anyways
    private Vector3 getGravity(GameObject planet, ref Vector3 radial, float partialLittleG) {
        //get the distance between objects (the 'r-axis' from circular motion in physics)
        radial = planet.transform.position - transform.position;
        //Debug.Log("Distance for " + planet.name + "= " + radial.magnitude);
        //get the gravitational acceleration towards that planet
        Vector3 gravity = (partialLittleG / (radial.magnitude * radial.magnitude)) * radial.normalized;
        return gravity;
    }
}
