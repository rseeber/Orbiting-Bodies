using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class OLD__playerMovement : MonoBehaviour
{
    //measured in units per second
    public float speed = 5.0f;

    public float rotationSpeed = 1.0f;

    float angle = 0f;

    // Update is called once per frame
    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        //get the direction we move in
        Vector3 moveDir = new Vector3(vertical, 0, 0).normalized * speed;
        //convert from local direction into global coordinates that can be used for actual translation
        moveDir = transform.TransformDirection(moveDir);

        //apply the motion
        transform.position += moveDir * Time.deltaTime;

        angle += horizontal * rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }
}
