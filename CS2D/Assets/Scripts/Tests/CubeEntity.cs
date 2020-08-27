using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;

public class CubeEntity
{
    private const int ACK_MESSAGE = 0;
    private const int UPDATE_MESSAGE = 1;
    
    public static void ServerWorldSerialize(Rigidbody rigidBody, BitBuffer buffer, int seq, float time) {
        var transform = rigidBody.transform;
        var position = transform.position;
        var rotation = transform.rotation;
        buffer.PutByte(UPDATE_MESSAGE);
        buffer.PutInt(seq);
        buffer.PutFloat(time);
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(rotation.w);
        buffer.PutFloat(rotation.x);
        buffer.PutFloat(rotation.y);
        buffer.PutFloat(rotation.z);
    }

    public static void ClientDeserialize(List<Snapshot> interpolationBuffer, BitBuffer buffer, int seqCli, List<Commands> clientCommands) {
        var messageType = buffer.GetByte();
        
        if (messageType == UPDATE_MESSAGE)
        {
            ClientDeserializeUpdate(interpolationBuffer, buffer, seqCli);
        }
        else if (messageType == ACK_MESSAGE)
        {
            int receivedAckSequence = ClientDeserializeAck(buffer);
            int lastAckedCommandsIndex = 0;
            foreach (var commands in clientCommands)
            {
                if (commands.Seq > receivedAckSequence)
                {
                    break;
                }
                lastAckedCommandsIndex++;
            }
            clientCommands.RemoveRange(0, lastAckedCommandsIndex);
        }
    }

    private static void ClientDeserializeUpdate(List<Snapshot> interpolationBuffer, BitBuffer buffer, int seqCli)
    {
        var position = new Vector3();
        var rotation = new Quaternion();
        
        var seq = buffer.GetInt();
        var time = buffer.GetFloat();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        rotation.w = buffer.GetFloat();
        rotation.x = buffer.GetFloat();
        rotation.y = buffer.GetFloat();
        rotation.z = buffer.GetFloat();
        
        if (seq < seqCli) return;
        
        Snapshot snapshot = new Snapshot(seq, time, position, rotation);
        int i;
        for (i = 0; i < interpolationBuffer.Count; i++)
        {
            if(interpolationBuffer[i].Seq > seq)
                break;
        }
        interpolationBuffer.Insert(i, snapshot);
    }

    public static void ClientSerializeInput(List<Commands> clientCommands, BitBuffer buffer)
    {
        foreach (Commands commands in clientCommands)
        {
            buffer.PutInt(commands.Seq);
            buffer.PutInt(commands.Up ? 1 : 0);
            buffer.PutInt(commands.Down ? 1 : 0);
            buffer.PutInt(commands.Right ? 1 : 0);
            buffer.PutInt(commands.Left ? 1 : 0);
            buffer.PutInt(commands.Space ? 1 : 0);
        }
    }

    public static List<Commands> ServerDeserializeInput(BitBuffer buffer)
    {
        List<Commands> totalCommands = new List<Commands>();
        
        while (buffer.HasRemaining())
        {
            int seq = buffer.GetInt();

            Commands commands = new Commands(
                seq,
                buffer.GetInt() > 0,
                buffer.GetInt() > 0,
                buffer.GetInt() > 0,
                buffer.GetInt() > 0,
                buffer.GetInt() > 0);

            totalCommands.Add(commands);
        }

        return totalCommands;
    }

    public static void ServerSerializeAck(BitBuffer buffer, int commandSequence)
    {
        buffer.PutByte(ACK_MESSAGE);
        buffer.PutInt(commandSequence);
    }
    
    private static int ClientDeserializeAck(BitBuffer buffer)
    {
        return buffer.GetInt();
    }
}
