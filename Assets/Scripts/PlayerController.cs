using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // in m/s
    [SerializeField] private float speed = 5f;
    // degrees per second
    [SerializeField] private float lookSpeed = 5.0f;
    //radians per second
    [SerializeField] private float gravityRotationSpeed = 3.14f;

    public bool isPilot = false;

    private Transform cam;
    private Rigidbody rb;
    private SpaceKinematicsRefactor kinematics;

    // Start is called before the first frame update
    void Start() {
        kinematics = GetComponent<SpaceKinematicsRefactor>();

        cam = transform.Find("CamParent");

        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate() {

        //MOVEMENT
        if (!isPilot) {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            //convert local direction into global coordinates
            Vector3 movement = transform.TransformDirection(new Vector3(horizontal, 0, vertical).normalized);

            //TODO: add a steady increase up to velocity for 0.1 seconds??
            transform.position += movement * speed * Time.deltaTime;

            //GRAVITY ROTATION
            rotatePlayer();
        }

        //CAMERA
        float lookX = Input.GetAxis("CamX") * lookSpeed * Time.deltaTime;
        float lookY = Input.GetAxis("CamY") * lookSpeed * Time.deltaTime;

        transform.Rotate(0, lookX, 0, Space.Self);
        cam.transform.Rotate(-lookY, 0, 0, Space.Self);
    }

    public void becomePilot() {
        isPilot = true;
        //freeze the player relative to the rocket, they shouldn't move for any reason
        rb.constraints = RigidbodyConstraints.FreezePosition;
        //disable the capsule collider
        GetComponent<CapsuleCollider>().enabled = false;
        rb.isKinematic = true;
    }

    //will rotate a player to become parallel with gravity
    void rotatePlayer() {
        /*
        //keep player parallel with gravity
        //if the angle between the local "down" for the player is the same as the pull of gravity
        float angle = Vector3.SignedAngle(transform.TransformDirection(Vector3.down), kinematics.gravityForce, transform.TransformDirection(Vector3.right));
        if (Mathf.Abs(angle) > 0.01) {
            //find the degrees we need to rotate this time
            float rotation = angle * gravityRotationSpeed * Time.deltaTime;
            //apply the rotation I DON'T CARE IF IT'S DEPRECATED THE OTHER FUNCTION WON'T WORK
            transform.RotateAround(transform.TransformDirection(Vector3.right), rotation);
        }
        */

        //couldn't get this to work^^
        return;
    }
}
