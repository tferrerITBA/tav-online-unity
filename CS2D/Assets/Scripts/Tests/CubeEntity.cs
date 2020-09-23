using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;

public class CubeEntity
{
    private const int PlayerConnect = 24;
    private const int PlayerJoined = 25;
    private const int PlayerDisconnect = 35;
    private const int CommandsAckMessage = 0;
    private const int UpdateMessage = 1;

    public static void PlayerConnectSerialize(BitBuffer buffer, int userID)
    {
        buffer.PutInt(PlayerConnect);
        buffer.PutInt(userID);
    }

    public static int PlayerConnectDeserialize(BitBuffer buffer)
    {
        buffer.GetInt(); // PlayerConnect
        return buffer.GetInt();
    }

    public static void PlayerJoinedSerialize(BitBuffer buffer, int userID, int playerCount)// , int sendPort, int recvPort)
    {
        buffer.PutInt(PlayerJoined);
        buffer.PutInt(userID);
        buffer.PutInt(playerCount);
    }

    public static int[] PlayerJoinedDeserialize(BitBuffer buffer)
    {
        buffer.GetInt(); // PlayerJoined
        int userID = buffer.GetInt();
        int playerCount = buffer.GetInt();
        // int sendPort = buffer.GetInt();
        // int recvPort = buffer.GetInt();
        return new int[] { userID, playerCount };// ), sendPort, recvPort }; // userID, playerCount
    }
    
    public static void ServerWorldSerialize(Dictionary<int, Rigidbody> rigidBodies, BitBuffer buffer, int seq, float time) {
        foreach (var userRigidBodyPair in rigidBodies)
        {
            var rigidBody = userRigidBodyPair.Value;
            var transform = rigidBody.transform;
            var position = transform.position;
            var rotation = transform.rotation;
            buffer.PutByte(UpdateMessage);
            buffer.PutInt(seq);
            buffer.PutInt(userRigidBodyPair.Key);
            buffer.PutFloat(time);
            buffer.PutFloat(position.x);
            buffer.PutFloat(position.y);
            buffer.PutFloat(position.z);
            buffer.PutFloat(rotation.w);
            buffer.PutFloat(rotation.x);
            buffer.PutFloat(rotation.y);
            buffer.PutFloat(rotation.z);
        }
    }

    public static void ClientDeserialize(List<Snapshot> interpolationBuffer, BitBuffer buffer, int seqCli, List<Commands> clientCommands) {
        var messageType = buffer.GetByte();
        
        if (messageType == UpdateMessage)
        {
            ClientDeserializeUpdate(interpolationBuffer, buffer, seqCli);
        }
        else if (messageType == CommandsAckMessage)
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
        var userID = buffer.GetInt();
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
            buffer.PutInt(commands.UserID);
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
                buffer.GetInt(),
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
        buffer.PutByte(CommandsAckMessage);
        buffer.PutInt(commandSequence);
    }
    
    private static int ClientDeserializeAck(BitBuffer buffer)
    {
        return buffer.GetInt();
    }
}
