using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeEntity
{
    public static void Serialize(Rigidbody rigidBody, BitBuffer buffer) {
        var transform = rigidBody.transform;
        var position = transform.position;
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
    }

    public static void Deserialize(Transform transform, BitBuffer buffer) {
        var position = new Vector3();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        transform.position = position;
    }
}
