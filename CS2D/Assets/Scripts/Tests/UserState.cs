using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserState
{
    private Vector3 position;
    private Quaternion rotation;
    
    public UserState(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    public Vector3 Position => position;

    public Quaternion Rotation => rotation;
}
