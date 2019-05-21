using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour {

    public GameObject player;
    public Vector2 gravity = new Vector2(0, -9.82f);
    public Matrix4x4 originToCurrentBase = Matrix4x4.identity;
    public Vector2 right = Vector2.right;
    public Vector2 up = Vector2.up;


    private static Gravity instance = new Gravity();
    public static object _lock = new object();
    public static Gravity GetInstance()
    {
        return instance;
    }

    //private constructor to avoid client applications to use constructor
    private Gravity() { }




    public Vector2 FromRelativetoAbsoluteBase(Vector2 vector)
    {
        return originToCurrentBase.inverse.MultiplyVector(vector);
    }

    public void UpdateBase(Player player, Vector2 normalOrigin)
    {
        //Debug.Log(normalOrigin);
        //Update Origin to Next
        Matrix4x4 originToNextBase = originToCurrentBase;
        originToNextBase.SetColumn(0, new Vector4(normalOrigin.y, normalOrigin.x,0,0));
        originToNextBase.SetColumn(1, new Vector4(-normalOrigin.x, normalOrigin.y, 0, 0));
        //Debug.Log(originToNextBase.ToString());

        //Get Current to Next base
        Matrix4x4 currentToNextBase = Matrix4x4.identity;
        currentToNextBase = originToNextBase * originToCurrentBase.inverse;

        //Update all vectors
        up = originToNextBase.inverse.MultiplyVector(Vector2.up).normalized;
        //Debug.Log("up " + up);
        //Debug.Log("normal " + normalOrigin.normalized);
        right = originToNextBase.inverse.MultiplyVector(Vector2.right).normalized;
        //gravity, raycast, stay the same in current base
        //velocity and acceleration stay the same in current base

        //Set Current = Next
        originToCurrentBase = originToNextBase;
    }
}
