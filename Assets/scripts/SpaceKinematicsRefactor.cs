using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class SpaceKinematicsRefactor : MonoBehaviour
{
    [SerializeField] private bool active = false;
    private List<Camera> cams = new List<Camera>();

    [SerializeField] private float timeMultiplier = 1;


    [SerializeField] private bool isRocket = false;
    [SerializeField] private bool isSatellite = false;
    //in Newtons
    [SerializeField] private float thrustPower = 500;
    [SerializeField] private float turnPower = 5;

    private float turningAngularDrag = 0f;
    private float regularAngularDrag = 1f;

    //in x1,000 Newton Minutes
    [SerializeField] private float fuelCapacity = 100.0f;
    private float fuel;

    [SerializeField] private string forwardDir = "up";
    private Vector3 thrustDir;

    private List<Planet> planetList = new List<Planet>();
    private GameManagerScript gameManager;
    private Rigidbody rb;
    private Transform seat;

    public Vector3 gravityForce { get; private set; } = Vector3.zero;

    void Start() {
        rb = GetComponent<Rigidbody>();
        cams = gameObject.GetComponentsInChildren<Camera>().ToList();
        if (!active) {
            deactivate();
        }

        //the position of the seat location, used to place the player there during flight
        seat = transform.Find("seat");

        //this is weird, just look up Reflection or PropertyInfo for more details on what this means.
        //essentially it just gets the Vector3.[forward] static property. Ideally, you're putting in stuff like forwardDir = "up" or "down"
        thrustDir = (Vector3)thrustDir.GetType().GetProperty("up").GetValue(null, null);

        fuel = fuelCapacity;
        //first find the gameManager gameObject (it needs the "GameController" tag!), then get the script attached to it
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManagerScript>();

        planetList = gameManager.planets;
    }

    void FixedUpdate() {
        //let the player get inside
        if (!active && isRocket && Input.GetButtonDown("Interact")) {
            activate();
        }

        //ROTATION
        if (active) {
            //get inputs
            float yaw = Input.GetAxis("Horizontal") * turnPower;
            float pitch = Input.GetAxis("Vertical") * turnPower;
            float roll = Input.GetAxis("Roll") * turnPower;
            //intentionally not normalizing this vector, since rotating on two axis would use more thrusters (ie be faster)
            Vector3 rotation = new Vector3(pitch, roll, yaw) * timeMultiplier;

            rb.AddRelativeTorque(rotation);

            if (rotation.magnitude / turnPower > 0.05) {
                rb.angularDrag = turningAngularDrag;
            }
            else {
                rb.angularDrag = regularAngularDrag;
            }
        }

        //FORCE
        //thrust
        Vector3 netThrustForce = Vector3.zero;
        if (active) {
            //TODO: make the bool based on if they have thrust powers, also make a global variable for what that axis is,
            //also probably make a check for if they're occupied (astronaught can't thrust if he's controlling his rocket)
            if (isRocket && fuel > 0) {
                netThrustForce = findThrust("Thrust", thrustDir);

                //remember to remove fuel (in x1,000 Newton Minutes)
                if (netThrustForce.magnitude > 5) {
                    //divide by 1,000 to remove in Newtons (not KiloNewtons), divide deltaTime by 60 to remove in seconds (not minutes)
                    fuel -= (netThrustForce.magnitude / 1000) * (Time.deltaTime / 60);
                }
            }
        }

        //gravity
        Vector3 gravityForce = getNetGrav(planetList);

        //APPLY ALL FORCES
        //get the net off all forces acting on you
        Vector3 netForce = gravityForce + netThrustForce;
        rb.AddForce(netForce * timeMultiplier);
    }

    private Vector3 getNetGrav(List<Planet> planetList) {
        Vector3 netGravity = Vector3.zero;
        foreach (Planet p in planetList) {
            Vector3 radial = Vector3.zero;
            netGravity += getGravity(p.gameObject, ref radial, p.partialLittleG);

            //should probably do something to have a "currentPlanet", so we can calculate escapeVelocity, etc
        }
        //note: using this method is redundant, since rigidbody divides by mass when calculating accel, but it improves readability to do it this way
        //check out the 'mode' parameter of Rigidbody.AddForce for more info (the 'acceleration' mode)
        // F = ma
        gravityForce = netGravity * rb.mass;

        return gravityForce;
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

    private Vector3 findThrust(string inputAxis, Vector3 forward) {
        //unit vector for direction of Forward-Thrust
        Vector3 dir = transform.TransformDirection(forward.normalized);

        float thrustInput = Input.GetAxis("Thrust");
        //direction (dir) * quantity of force (thrustPower) * sign (+/-) (thrustInput)
        Vector3 netThrustForce = dir * thrustPower * thrustInput;

        return netThrustForce;
    }

    public void activate() {
        Transform player;
        if ((player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>()) == null) {
            Debug.Log("it's null");
        }

        active = true;
        Debug.Log(player.position);
        Debug.Log(seat.position);
        //put the player into the seat and freeze movement
        player.position = seat.position;
        player.rotation = seat.rotation;

        //the PlayerController also has some stuff to do when they become a pilot instead of walking around
        player.GetComponent<PlayerController>().becomePilot();

        //make the player a child object of the rocket
        player.SetParent(seat);

        //re-enable all the cameras
        foreach (Camera cam in cams) {
            cam.gameObject.SetActive(true);
        }
    }

    public void deactivate() {
        active = false;
        if (isRocket || isSatellite) {
            foreach (Camera cam in cams) {
                cam.gameObject.SetActive(false);
            }
        }
    }
}