﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        buffer.PutByte(PlayerConnect);
        buffer.PutInt(userID);
    }

    public static int PlayerConnectDeserialize(BitBuffer buffer)
    {
        buffer.GetByte(); // PlayerConnect
        return buffer.GetInt();
    }

    public static void PlayerJoinedSerialize(BitBuffer buffer, int userID, int playerCount)// , int sendPort, int recvPort)
    {
        Debug.Log($"Mandando PlayerJoined {userID} {playerCount}");
        buffer.PutByte(PlayerJoined);
        buffer.PutInt(userID);
        buffer.PutInt(playerCount);
    }

    public static void PlayerJoinedDeserialize(int[] playerJoined, BitBuffer buffer)
    {
        playerJoined[0] = buffer.GetInt(); // userID
        playerJoined[1] = buffer.GetInt(); // playerCount
    }
    
    public static void ServerWorldSerialize(Dictionary<int, Rigidbody> rigidBodies, BitBuffer buffer, int seq, float time) {
        
        buffer.PutByte(UpdateMessage);
        buffer.PutInt(seq);
        buffer.PutFloat(time);
        buffer.PutInt(rigidBodies.Count);
        
        foreach (var userRigidBodyPair in rigidBodies)
        {
            var transform = userRigidBodyPair.Value.transform;
            var position = transform.position;
            var rotation = transform.rotation;
            
            buffer.PutInt(userRigidBodyPair.Key);
            buffer.PutFloat(position.x);
            buffer.PutFloat(position.y);
            buffer.PutFloat(position.z);
            buffer.PutFloat(rotation.w);
            buffer.PutFloat(rotation.x);
            buffer.PutFloat(rotation.y);
            buffer.PutFloat(rotation.z);
        }
    }

    public static void ClientDeserialize(List<Snapshot> interpolationBuffer, int[] playerJoined, BitBuffer buffer, int seqCli, List<Commands> clientCommands) {
        var messageType = buffer.GetByte();

        if (messageType == UpdateMessage)
        {
            ClientDeserializeUpdate(interpolationBuffer, buffer, seqCli);
        }
        else if (messageType == PlayerJoined)
        {
            PlayerJoinedDeserialize(playerJoined, buffer);
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
        var seq = buffer.GetInt();
        var time = buffer.GetFloat();
        var playerCount = buffer.GetInt();

        Dictionary<int, UserState> userStates = new Dictionary<int, UserState>();
        int i;
        for (i = 0; i < playerCount; i++)
        {
            var position = new Vector3();
            var rotation = new Quaternion();

            var userID = buffer.GetInt();
            position.x = buffer.GetFloat();
            position.y = buffer.GetFloat();
            position.z = buffer.GetFloat();
            rotation.w = buffer.GetFloat();
            rotation.x = buffer.GetFloat();
            rotation.y = buffer.GetFloat();
            rotation.z = buffer.GetFloat();
            
            userStates.Add(userID, new UserState(position, rotation));
        }
        
        // Debug.Log($"seq {seq} seqCli {seqCli} {userStates.Keys.First()}");
        
        if (seq < seqCli) return;

        Snapshot snapshot = new Snapshot(seq, time, userStates);
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
