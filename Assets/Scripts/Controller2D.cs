using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class Controller2D : MonoBehaviour {

    public LayerMask collisionMask;

    const float skinWidth = 0.1f;
    const float maximumAngle = 80f;
    public int horizontalRayCount = 2;
    public int verticalRayCount = 2;

    
    float rotationSpeed = 540f;

    float horizontalRaySpacing;
    float verticalRaySpacing;

    RaycastOrigins raycastOrigins;
    BoxCollider2D collider2D;
    SpriteRenderer spriteRenderer;
    public CollisionInfo collisionInfo;

	// Use this for initialization
	void Start () {
        collider2D = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        collisionInfo.rotating = false;
        CalculateRaySpacing();
    }

    public void Move(Vector3 velocityRelative)
    {

        //If the player rotation animation is not over, the player cannot move
        if(collisionInfo.rotating) {
            velocityRelative.y=0;
        }

        UpdateRaycastOrigins();
        collisionInfo.Reset();
        
        //TODO explain this
        if(velocityRelative.y > 0) {
            collisionInfo.climbing=false;
        }    

        if(velocityRelative.x !=0)
        {
            HorizontalCollisions(ref velocityRelative);
        }

        if (velocityRelative.y != 0)
        {
            VerticalCollisions(ref velocityRelative);
        }
       
        transform.Translate(velocityRelative,Space.Self);

        //if the player is on the ground or if he started a rotation, then rotate him accordingly to the fround angle
        if(collisionInfo.below || collisionInfo.rotating) {
            UpdateRaycastOrigins();
            Rotate(ref velocityRelative);
        }
        
    }

    void Rotate(ref Vector3 velocityRelative) {
        
        Quaternion targetRotation = transform.rotation;

        float directionX =Mathf.Sign(velocityRelative.x);

        RaycastHit2D forwardHit= Physics2D.Raycast((directionX == 1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft, -transform.up, collisionMask);
        RaycastHit2D backwardHit= Physics2D.Raycast((directionX == 1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight, -transform.up, collisionMask);
        RaycastHit2D nearestHit = forwardHit.collider == null || forwardHit.distance > backwardHit.distance ? backwardHit : forwardHit;
        RaycastHit2D farestHit = nearestHit == backwardHit ? forwardHit : backwardHit;

        if(forwardHit.collider == null || backwardHit.collider == null){
            //DO NOTHING 
        } else if(Vector2.Dot(backwardHit.normal.normalized, forwardHit.normal.normalized) == 1f){
            //no slope change
            if(Vector2.Dot(nearestHit.normal.normalized, transform.up) != 1f) {
                //adjust rotation
                targetRotation = Quaternion.LookRotation(transform.forward, backwardHit.normal.normalized);
            }
        } else {
            //slope change
            Vector2 normal = directionX * Vector2.Perpendicular(forwardHit.point - backwardHit.point);
            if(Vector2.Dot(normal, transform.up) != 1f) {
                //adjust rotation
                targetRotation = Quaternion.LookRotation(transform.forward, normal);
            }
        }

        //If the trget rotation is different from the current rotation, rotate. Else end rotation
        if(targetRotation != transform.rotation){
            Quaternion quat =
            transform.rotation =  Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime);
            Physics2D.gravity=-Physics2D.gravity.magnitude * transform.up.normalized; 

            collisionInfo.rotating = true;                
        } else {
            collisionInfo.rotating = false;
        }
    }


/*
Check for backward or forward collision. 
 */   
 void HorizontalCollisions(ref Vector3 velocityRelative)
    {
        float directionX = Mathf.Sign(velocityRelative.x);
        float rayLength = Mathf.Abs(velocityRelative.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += (Vector2)transform.up * (horizontalRaySpacing * i + velocityRelative.y);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionX * transform.right, rayLength, collisionMask);
            
            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal,transform.up);
                //Collision actions : climbslope or stop
                if(i !=0 || slopeAngle > maximumAngle) {
                    velocityRelative.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;
                    collisionInfo.left = directionX == -1;
                    collisionInfo.right = directionX == 1;
                } else {
                    float distanceToSlopeStart = hit.distance-skinWidth;
                    velocityRelative.x -= distanceToSlopeStart * directionX;
                    ClimbSlope(ref velocityRelative, slopeAngle);
                    velocityRelative.x += distanceToSlopeStart * directionX;
                } 
            }
        }
    }

    void VerticalCollisions(ref Vector3 velocityRelative)
    {
        float directionY = Mathf.Sign(velocityRelative.y);
        float rayLength = Mathf.Abs(velocityRelative.y) + skinWidth;

        RaycastHit2D nearestHit=new RaycastHit2D();
        Vector2 rotationOrigin = new Vector2(0,0);

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += (Vector2)transform.right * (verticalRaySpacing * i +  velocityRelative.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, transform.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, transform.up * directionY* rayLength, Color.red);
            if(hit)
            {
                if(nearestHit.collider==null || nearestHit.distance > hit.distance) {
                    nearestHit = hit;
                } 
            }
        }

        if(nearestHit.collider!=null && !collisionInfo.rotating) {
            velocityRelative.y = (nearestHit.distance - skinWidth) * directionY;
            

            collisionInfo.above = directionY == 1;
            collisionInfo.below = directionY == -1;
        }
    }
    
    void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
        float directionX = Mathf.Sign(velocity.x);
        float moveDistance = Mathf.Abs(velocity.x);
        float climbingVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if(velocity.y <= climbingVelocityY) { 
            velocity.y = climbingVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
        }
        //Update collision info to allow jump actions
        collisionInfo.below=true;
    }

    void UpdateRaycastOrigins()
    {
        Vector2 center = transform.position;
        float width = collider2D.size.x;
        float height = collider2D.size.y;
        raycastOrigins.bottomLeft = (Vector2)center - (Vector2)transform.up * height / 2 - (Vector2)transform.right * width / 2;
        raycastOrigins.bottomRight = (Vector2)center - (Vector2)transform.up * height / 2 + (Vector2)transform.right * width / 2;
        raycastOrigins.topLeft = (Vector2)center + (Vector2)transform.up * height / 2 - (Vector2)transform.right * width / 2;
        raycastOrigins.topRight = (Vector2)center + (Vector2)transform.up * height / 2 + (Vector2)transform.right * width / 2;
    }


    void CalculateRaySpacing()
    {
        Bounds bounds = collider2D.bounds;
        //bounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

	struct RaycastOrigins
    {
        public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below, left, right, climbing, rotating;

        public void Reset()
        {
            above = below = left = right = false;
        }
    }

    public void DrawPoint(Vector2 v, Color color) {
        Debug.DrawRay(v,0.1f * transform.up.normalized,color,10);
        Debug.DrawRay(v,-transform.up.normalized * 0.1f,color,10);
        Debug.DrawRay(v,transform.right.normalized * 0.1f,color,10);
        Debug.DrawRay(v,-transform.right.normalized * 0.1f,color,10);
    }
}
