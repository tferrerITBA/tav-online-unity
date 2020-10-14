using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tests;
using UnityEngine;

public class Serializer
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

    public static void PlayerJoinedSerialize(BitBuffer buffer, PlayerJoined playerJoined)// , int sendPort, int recvPort)
    {
        buffer.PutByte(PlayerJoined);
        buffer.PutInt(playerJoined.UserID);
        buffer.PutInt(playerJoined.PlayerCount);
        buffer.PutInt(playerJoined.Seq);
        buffer.PutFloat(playerJoined.Time);
    }

    public static void PlayerJoinedDeserialize(PlayerJoined playerJoined, BitBuffer buffer)
    {
        playerJoined.UserID = buffer.GetInt();
        playerJoined.PlayerCount = buffer.GetInt();
        playerJoined.Seq = buffer.GetInt();
        playerJoined.Time = buffer.GetFloat();
        playerJoined.InstantiateCubesPending = true;
    }
    
    public static void ServerWorldSerialize(Dictionary<int, ServerClientInfo> clients, BitBuffer buffer, int seq, float time) {
        
        buffer.PutByte(UpdateMessage);
        buffer.PutInt(seq);
        buffer.PutFloat(time);
        buffer.PutInt(clients.Count);
        
        foreach (var userRigidBodyPair in clients)
        {
            var transform = userRigidBodyPair.Value.characterController.transform;
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

    public static void ClientDeserialize(List<Snapshot> interpolationBuffer, PlayerJoined playerJoined, BitBuffer buffer,
        int seqCli, List<Commands> clientCommands, int cmdSeq) {
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
            // Debug.Log($"ANTES {clientCommands.Count}");
            foreach (var commands in clientCommands)
            {
                if (cmdSeq - 1 > receivedAckSequence)
                {
                    break;
                }
                lastAckedCommandsIndex++;
            }
            // Debug.Log($"DSPS {clientCommands.Count} cmdSeq {cmdSeq} {receivedAckSequence}");
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
            buffer.PutFloat(commands.Vertical);
            buffer.PutFloat(commands.Horizontal);
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
                buffer.GetFloat(),
                buffer.GetFloat()
            );

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
