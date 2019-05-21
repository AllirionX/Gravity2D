using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

    float jumpHeight = 4;
    float timeToJumpApex = 1f;
    float accelerationTimeAirborne = 0.2f;
    float accelerationTimeGrounded = 0.1f;
    float rotationSpeed = 540f;

    float gravity;
    float jumpVelocity;
    float velocityXSmoothing;

    Vector3 velocityRelative;
    float moveSpeed = 6;

    Controller2D controller;

	// Use this for initialization
	void Start () {
        controller = GetComponent<Controller2D>();

        gravity = - 2 * jumpHeight / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print("Gravity " + gravity + "Jump Velocity: " + jumpVelocity);
	}

    private void FixedUpdate()
    {
        if(controller.collisionInfo.above && velocityRelative.y >0 || controller.collisionInfo.below && velocityRelative.y < 0) {
            velocityRelative.y = 0;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        //jump
        if(Input.GetKeyDown (KeyCode.Space) && controller.collisionInfo.below)
        {
            velocityRelative.y = jumpVelocity;
        }

        //smooth x movement
        float targetVelocityX = input.x * moveSpeed;
        //velocityRelative.x=targetVelocityX;
        velocityRelative.x = Mathf.SmoothDamp(velocityRelative.x, targetVelocityX, ref velocityXSmoothing, (controller.collisionInfo.below ? accelerationTimeGrounded : accelerationTimeAirborne));
        
        
        velocityRelative.y += Gravity.GetInstance().gravity.y * Time.deltaTime;
        
        controller.Move(velocityRelative * Time.deltaTime);
    }


}
