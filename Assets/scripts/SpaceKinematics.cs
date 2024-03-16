using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class SpaceKinematics : MonoBehaviour
{
    //in kg
    public float mass = 10.0f;

    [SerializeField]
    private GameObject targetPlanet;
    private List<GameObject> otherPlanets;

    public Transform destination;
    [HideInInspector] public CompassControl compass;

    public float timeMultiplier = 1.0f;
    //in m/s^2
    public float gravityThreshold = 0.5f;

    //in m/s^2
    public Vector3 velocity = Vector3.zero;
    public Vector3 acceleration = Vector3.zero;
    private Rigidbody rb;

    private float escapeVel;
    private float orbitalVel;

    public bool isSatellite = true;
    public bool doStartingVelocity = true;
    public bool isRocket = false;

    [SerializeField]
    //stored as x1000 Newton minutes
    private float fuelCapacity = 12.0f;
    private float fuel;

    //in Newtons
    public float thrustForce = 50.0f;
    //in m/s^2
    private Vector3 thrustAccel = Vector3.zero;
    private bool grounded = false;

    //a vector pointing from this to the acting gravitational force
    private Vector3 r = Vector3.zero;

    private float partialLittleG;
    private float partialOrbitalVelocity;
    private float partialEscapeVelocity;
    private float radius;

    [SerializeField] private GameObject gameManager;

    [SerializeField] private Transform modelCenter;
    [SerializeField] private TextMeshProUGUI velocityText;

    void cacheNewPlanet() {
        //cache the values so you don't need to access it every frame
        Planet p = targetPlanet.GetComponent<Planet>();
        partialLittleG = p.partialLittleG;
        partialOrbitalVelocity = p.partialOrbitalVelocity;
        partialEscapeVelocity = p.partialEscapeVelocity;
        radius = p.radius;
        otherPlanets = gameManager.GetComponent<GameManagerScript>().planets;
    }

    // Start is called before the first frame update
    void Start() {
        //assign the destination onto the compass, satellites don't have compasses
        if (!isSatellite) {
            compass = transform.Find("Compass").gameObject.GetComponent<CompassControl>();
            compass.destination = destination;
        }

        rb = GetComponent<Rigidbody>();

        fuel = fuelCapacity;

        //initialize all the values related to this planet
        cacheNewPlanet();

        //used for if you want to create an orbiting satellite, giving it the proper velocity based on it's distance to the planet
        if (doStartingVelocity) {
            //get a vector pointing 90deg from r, set velocity at some speed in that direction
            r = targetPlanet.transform.position - transform.position;

            //we need a random vector (v2) on a different axis than r
            Vector3 v2 = Vector3.forward;
            //check if it's different axis
            if (v2 == r.normalized || v2 == -r.normalized) {
                //this will definetly be a different axis
                v2 = Vector3.right;
            }

            //do a dot product to get a vector orthogonal to the plane formed by both vectors
            // See: https://stackoverflow.com/a/33454100/18014565
            velocity = Vector3.Cross(r, v2).normalized;

            //get the speed required
            float orbitalVelocity = (partialOrbitalVelocity / Mathf.Sqrt(r.magnitude));
            velocity *= orbitalVelocity;

            Debug.Log("final velocity: " + velocity + "\nr = " + r + "partial orbital velocity: " + partialOrbitalVelocity);
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
        //ACCELERATION
        acceleration = Vector3.zero;
        thrustAccel = Vector3.zero;

        //do thrust
        if (isRocket) {
            if (fuel > 0.0f) {
                //get unit vector
                Vector3 dir = transform.TransformDirection(Vector3.up);

                float thrustInput = Input.GetAxis("Thrust");
                dir *= thrustInput;

                //remember to remove fuel (in x1,000 Newton Minutes)
                if (Mathf.Abs(thrustInput) > 0.01) {
                    fuel -= (thrustForce / 1000) * (Time.deltaTime / 60);
                }

                //get acceleration, multiply it by the unit vector dir
                thrustAccel = (thrustForce / mass) * dir;
            }

            //rotation
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            float roll = Input.GetAxis("Roll");

            transform.Rotate(vertical, roll, -horizontal, Space.Self);
        }


        //do gravity
        Vector3 gravity = Vector3.zero;
        string test = "";
        foreach (GameObject p in otherPlanets) {
            test += p.name + ", ";
        }
        //iterate through each planet, check gravity, apply gravity if it exceeds threshold
        foreach (GameObject p in otherPlanets) {
            Vector3 otherR = Vector3.zero;
            Vector3 pGravity = getGravity(p, ref otherR);
            //Debug.Log("Gravity for " + p.name + " = " + pGravity.magnitude);
            //switch targetPlanets if the new planet has a bigger gravitational pull
            if (pGravity.magnitude > gravity.magnitude && pGravity.magnitude > gravityThreshold) {
                targetPlanet = p;
                cacheNewPlanet();
            }
            //if we're doing the targetPlanet, we need to actually
            //know what that r vector is to calculate escape velocity, orbit velocity, etc
            if (p == targetPlanet) {
                r = otherR;
                if (pGravity.magnitude < gravityThreshold) {
                    targetPlanet = null;
                }
            }

            //only add this gravity up to the total of 'otherGravity' if it passes a certain threshold of acceleration
            if (pGravity.magnitude > gravityThreshold) {
                gravity += pGravity;
            }
        }

        //add it all up
        //(no need to do math if you're grounded with no power for takeoff)
        if (!grounded || thrustAccel.magnitude > gravity.magnitude) {
            acceleration = gravity + thrustAccel;
        }
        else {  //eventually look into checking angles or something like that? maybe just set the r direction to be zero?? perpindicular thrust is real.
            acceleration = Vector3.zero;
        }

        //VELOCITY
        velocity += acceleration * Time.deltaTime * timeMultiplier;
        //apply velocity using rigidbody instead of adjusting position
        rb.velocity = velocity * timeMultiplier;
        
        if (isRocket) {
            float rootR = Mathf.Sqrt(r.magnitude);
            escapeVel = partialEscapeVelocity / rootR;
            orbitalVel = partialOrbitalVelocity / rootR;

            string targetName;
            if (targetPlanet == null) {
                targetName = "none";
            }
            else {
                targetName = targetPlanet.name;
            }
            //float distUntilEscapeVelocity = 0.0f;
            //float timeUntilEscapeVelocity = 0.0f;
            //float timeUntilThrust = tMinusToThrust(ref distUntilEscapeVelocity, ref timeUntilEscapeVelocity);

            velocityText.text = "Current Velocity = " + velocity.magnitude.ToString("0.0") + " m/s\n" +
                                "Orbital Velocity = " + orbitalVel.ToString("0.0") + " m/s\n" +
                                "Escape Velocity  = " + escapeVel.ToString("0.0") + " m/s\n\n" +
                                
                                "Target Planet: " + targetName + "\n" +
                                "Destination Planet: " + destination.name + "\n" +
           //                     "Time Until Thrust: " + timeUntilThrust.ToString("0.0") + " seconds\n" +
           //                     "Time to Escape Velocity: " + timeUntilEscapeVelocity + " seconds\n\n" +

           //                     "Distance to Escape Velocity: " + distUntilEscapeVelocity + "meters\n\n" +

                                "Current Height = " + (r.magnitude - radius).ToString("0.0") + " meters\n" +
                                "Thrust Acceleration = " + thrustAccel.magnitude.ToString("0.0") + " m/s/s\n" +
                                "Gravitational Acceleration = " + gravity.magnitude.ToString("0.0") + " m/s/s\n" +
                                "Fuel: " + fuel.ToString("0.0") + " / " + fuelCapacity.ToString("0.0")
                                ;
        }

        //POSITION
        //transform.position += velocity * Time.deltaTime * timeMultiplier;

        //rotate them if they're a satellite
        //TODO: add satellite bool
        if (isSatellite) {
            transform.rotation = Quaternion.LookRotation(r, velocity);
        }
    }

    //this function calculates when you need to begin thrusting (deltaT_1_2), how long until you can
    //stop thrusting (deltaT_1_3), and the distance to travel until then (orbitalDist).
    //It is used for planning an approach to escape Velocity while pointing in the correct direction to arrive at your destination planet.

    //this math is very difficult to process, please do all the workings out on a piece of paper using kinematics before trying to mess with this.
    float tMinusToThrust(ref float orbitalDist, ref float deltaT_1_3) {

        //get angularDistance by comparing velocity to displacement
        Vector3 displacement = (destination.position - targetPlanet.transform.position); //.normalized() ???
        float angularDist = Vector3.Angle(displacement, velocity);

        //get the orbitalDistance (dist across the curved line of the orbit, assuming a perfect circle) 2*pi*r*(deg/360)
        //orbitalDist is deltaX between current moment and time of escapeVelocity (ie deltaX_1_3)
        orbitalDist = 2 * Mathf.PI * (angularDist / 360.0f);

        //find our max possible acceleration
        float maxAccel = thrustForce / mass;

        //find time between thrust and escapeVelocity
        float deltaT_2_3 = (escapeVel - velocity.magnitude) / maxAccel;
        //find distance between those two points across the orbit
        float deltaX_2_3 = (velocity.magnitude * deltaT_2_3) + (0.5f * maxAccel * Mathf.Pow(deltaT_2_3, 2));

        //now get the distance between current position and the point of thrust engage
        float deltaX_1_2 = orbitalDist - deltaX_2_3;
        float deltaT_1_2 = velocity.magnitude / deltaX_1_2;

        //add up the two time intervals to get the overall time between now and when you will achieve escape velocity
        deltaT_1_3 = deltaT_1_2 + deltaT_2_3;
        return deltaT_1_2;
    }
    //overloaded taking only 1 argument
    float tMinusToThrust (ref float orbitalDist) {
        float arg2 = 0.0f;
        return tMinusToThrust(ref orbitalDist, ref arg2);
    }
    //overloaded taking no arguments
    float tMinusToThrust() {
        float arg1 = 0.0f;
        float arg2 = 0.0f;
        return tMinusToThrust(ref arg1, ref arg2);
    }

    //DEPRECATED
    void solveEscapeVelocity() {
        //we need to find the orbital distance, angular distance (degrees), 
        //time required to accelerate, and optionally -- the acceleration power required (else just use the constant value of thrust)

        //Focus on: angular distance and delta-t

        //get angularDistance by comparing velocity to required displacement
        Vector3 displacement = (destination.position - targetPlanet.transform.position); //.normalized() ???
        float angularDist = Vector3.Angle(displacement, velocity);

        //get the orbitalDistance (dist across the curved line of the orbit, assuming a perfect circle) 2*pi*r*(deg/360)
        float orbitalDist = 2 * Mathf.PI * (angularDist / 360.0f);

        //find our max possible acceleration
        float maxAccel = thrustForce / mass;

        //solve for deltaT using everything we know
        //storing both ends of the fraction in different variables to improve readability
        //very difficult equation here, we might need to reduce the number of times we run this func, not every frame if possible
        float deltaT_topHalf = (escapeVel - velocity.magnitude) * 4 * Mathf.PI * r.magnitude * angularDist;
        float deltaT_bottomHalf = (Mathf.Pow(escapeVel, 2) - Mathf.Pow(velocity.magnitude, 2)) * 360;
        //divide the fraction
        float deltaT = deltaT_topHalf / deltaT_bottomHalf;

        //actually this is all we need I think:
        deltaT = (escapeVel - velocity.magnitude) / acceleration.magnitude;
    }

    //planet is the planet you are being pulled towards
    //radial is the r vector you want to be overwritten with a vector pointing towards the planet
    //usually this is something you only do in C, but like procedural programming so I'm doing it anyways
    private Vector3 getGravity(GameObject planet, ref Vector3 radial) {
        //get the distance between objects (the 'r-axis' from circular motion in physics)
        radial = planet.transform.position - transform.position;
        //Debug.Log("Distance for " + planet.name + "= " + radial.magnitude);
        //get the gravitational acceleration towards that planet
        Vector3 gravity = (partialLittleG / (radial.magnitude * radial.magnitude)) * radial.normalized;
        return gravity;
    }

    private void OnCollisionStay(Collision other) {
        //set acceleration and velocity to only be effected by thrust and the acceleration of the parent body.
        //acceleration = thrust;
        //velocity += acceleration * Time.deltaTime * timeMultiplier;
        if (velocity.magnitude > 0.05) {
            foreach (ContactPoint item in other.contacts) {
                Vector3 contact = item.point - modelCenter.position;      //IMPORTANT: if transform.position is not located in the center of the model, this code will not work!
                float angle = Vector3.Angle(contact, velocity);

                Debug.DrawRay(modelCenter.position, contact, Color.red);
                Debug.DrawRay(modelCenter.position, velocity, Color.blue);

                if (Mathf.Abs(angle) >= 90) {
                    continue;
                }
                else {
                    velocity = Vector3.zero;
                }

                ////"bounce" off of walls, assuming big enough impact speed. Need to make vector mirrored eventually....
                //if (velocity.magnitude > 1.0) {
                //    velocity = -velocity;
                //}
                //else {
                //}
            }
        }
    }

    private void OnCollisionEnter(Collision other) {
        fuel = fuelCapacity;

        Debug.Log("Impact Velocity of " + velocity.magnitude.ToString("0.0") + "m/s");
        grounded = true;

        //iterate through each contact point
        //check angle diff between contact point and velocity vector
        //if diff >= 90, don't zero out the velocity
        //else if diff < 90, velocity = -velocity (not a permanent solution, need to do proper mirroring later)
        foreach (ContactPoint item in other.contacts) {
            Vector3 contact = item.point - transform.position;      //IMPORTANT: if transform.position is not located in the center of the model, this code will not work!
            float angle = Vector3.Angle(contact, velocity);

            if (angle >= 90) {
                continue;
            }

            ////"bounce" off of walls, assuming big enough impact speed. Need to make vector mirrored eventually....
            //if (velocity.magnitude > 1.0) {
            //    velocity = -velocity;
            //}
            //else {
            velocity = Vector3.zero;
            //}
        }

    }

    private void OnCollisionExit(Collision collision) {
        grounded = false;
    }
}
