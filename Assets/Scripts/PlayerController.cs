using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // in m/s
    [SerializeField] private float speed = 5f;
    // degrees per second
    [SerializeField] private float lookSpeed = 5.0f;
    private Transform cam;
    private Rigidbody rb;
    private SpaceKinematicsRefactor kinematics;

    // Start is called before the first frame update
    void Start()
    {
        cam = transform.Find("CamParent");

        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float lookX = Input.GetAxis("CamX") * lookSpeed * Time.deltaTime;
        float lookY = Input.GetAxis("CamY") * lookSpeed * Time.deltaTime;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        //convert local direction into global coordinates
        Vector3 movement = transform.TransformDirection(new Vector3(horizontal, 0, vertical).normalized);

        //TODO: add a steady increase up to velocity for 0.1 seconds??
        transform.position += movement * speed * Time.deltaTime;

        //camera looking
        transform.Rotate(0, lookX, 0, Space.Self);
        cam.transform.Rotate(-lookY, 0, 0, Space.Self);

        //keep player parallel with gravity
        //get vector for player down
        //do vector3.rotateTowards() to make it line up with direction of gravity
        //


        Vector3 targetRotation = Vector3.Cross(transform.TransformDirection(Vector3.forward), kinematics.gravityForce);

        Vector3 step = Vector3.

        ;
    }
}
