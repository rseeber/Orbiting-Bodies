using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
//public
    //move
    public float moveSpeed = 5f;
    public float moveAccel = 2f;
    //look
    public float lookSpeed = 100f;
    public Camera cam;
    
//private
    //move
    private Rigidbody rb;
    private bool grounded = true;
    //look
    private float angleX, angleY = 0;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //movement
        //you cant walk unless you're grounded
        if (grounded)
        {
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(vertical, 0, 0).normalized * moveSpeed;

            rb.velocity = movement;
        }

        //looking
        //get mouse/look input
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;

        //rotate the whole player around based on mouseX
        angleX += mouseX;
        rb.rotation = Quaternion.Euler(0, angleX, 0);

        //rotate just the camera up and down based on mouseY
        angleY += mouseY;
        cam.transform.rotation = Quaternion.Euler(rb.rotation.x - angleY, rb.rotation.y, rb.rotation.z);

    }
}
