using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeEntity
{
    public static void Serialize(Rigidbody rigidBody, BitBuffer buffer, int seq) {
        var transform = rigidBody.transform;
        var position = transform.position;
        var rotation = transform.rotation;
        buffer.PutInt(seq);
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(rotation.w);
        buffer.PutFloat(rotation.x);
        buffer.PutFloat(rotation.y);
        buffer.PutFloat(rotation.z);
    }

    public static void Deserialize(SortedList<int, Snapshot> interpolationBuffer, BitBuffer buffer, int seqCli) {
        var position = new Vector3();
        var rotation = new Quaternion();

        var seq = buffer.GetInt();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        rotation.w = buffer.GetFloat();
        rotation.x = buffer.GetFloat();
        rotation.y = buffer.GetFloat();
        rotation.z = buffer.GetFloat();
        //Debug.Log("SEQ LEIDO: " + seq + " " + seqCli);
        if (seq < seqCli) return;
        
        Snapshot snapshot = new Snapshot(seq, position, rotation);
        //Debug.Log(snapshot);
        interpolationBuffer.Add(seq, snapshot);
    }
}
