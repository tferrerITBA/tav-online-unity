using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tests;
using UnityEngine;

public enum PacketType
{
    PLAYER_CONNECT = 24,
    PLAYER_JOINED = 25,
    PLAYER_DISCONNECT = 35,
    COMMANDS_ACK_MESSAGE = 0,
    UPDATE_MESSAGE = 1,
    PLAYER_SHOT = 88,
    SHOT_BROADCAST = 95,
    SHOT_ACK = 89,
    SHOT_BROADCAST_ACK = 99,
    COMMANDS = 77
}

public class Serializer
{

    public static void PlayerConnectSerialize(BitBuffer buffer, int userID)
    {
        buffer.PutByte((byte)PacketType.PLAYER_CONNECT);
        buffer.PutInt(userID);
    }

    public static int PlayerConnectDeserialize(BitBuffer buffer)
    {
        buffer.GetByte(); // PlayerConnect
        return buffer.GetInt();
    }

    public static void PlayerJoinedSerialize(BitBuffer buffer, PlayerJoined playerJoined)// , int sendPort, int recvPort)
    {
        buffer.PutByte((byte)PacketType.PLAYER_JOINED);
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
    
    public static void ServerWorldSerialize(Dictionary<int, ServerClientInfo> clients, BitBuffer buffer,
        int seq, float time, int cmdSeq) {
        
        buffer.PutByte((byte)PacketType.UPDATE_MESSAGE);
        buffer.PutInt(seq);
        buffer.PutFloat(time);
        buffer.PutInt(cmdSeq);
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

    public static PacketType ClientDeserialize(List<Snapshot> interpolationBuffer, PlayerJoined playerJoined, BitBuffer buffer,
        int displaySeq, CommandsList clientCommands, int cmdSeq, List<Shot> shots, int shotSeq,
        ShotBroadcast shotBroadcast) {
        var messageType = buffer.GetByte();

        if (messageType == (byte)PacketType.UPDATE_MESSAGE)
        {
            ClientDeserializeUpdate(interpolationBuffer, buffer, displaySeq, clientCommands);
            return PacketType.UPDATE_MESSAGE;
        }
        if (messageType == (byte)PacketType.PLAYER_JOINED)
        {
            PlayerJoinedDeserialize(playerJoined, buffer);
            return PacketType.PLAYER_JOINED;
        }
        if (messageType == (byte)PacketType.COMMANDS_ACK_MESSAGE)
        {
            int receivedAckSequence = ClientDeserializeAck(buffer);
            clientCommands.Ack(receivedAckSequence);
            return PacketType.COMMANDS_ACK_MESSAGE;
        }
        if (messageType == (byte) PacketType.SHOT_ACK)
        {
            int receivedShotAckSeq = ClientDeserializeShotAck(buffer);
            int count = 0;
            foreach (var shot in shots)
            {
                if (shot.Seq <= receivedShotAckSeq)
                    count++;
            }
            shots.RemoveRange(0, count);
            return PacketType.SHOT_ACK;
        }

        if (messageType == (byte) PacketType.SHOT_BROADCAST)
        {
            ClientDeserializeShotBroadcast(buffer, shotBroadcast);
            return PacketType.SHOT_BROADCAST;
        }

        return PacketType.PLAYER_DISCONNECT;
    }

    private static void ClientDeserializeUpdate(List<Snapshot> interpolationBuffer, BitBuffer buffer,
        int displaySeq, CommandsList clientCommands)
    {
        var seq = buffer.GetInt();
        var time = buffer.GetFloat();
        var cmdSeq = buffer.GetInt();
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

        if (seq < displaySeq) return;
        
        clientCommands.SnapshotAck(cmdSeq);

        Snapshot snapshot = new Snapshot(seq, time, cmdSeq, userStates);
        for (i = 0; i < interpolationBuffer.Count; i++)
        {
            if(interpolationBuffer[i].Seq > seq)
                break;
        }
        interpolationBuffer.Insert(i, snapshot);
    }

    public static void ClientSerializeInput(CommandsList clientCommands, BitBuffer buffer)
    {
        buffer.PutByte((byte)PacketType.COMMANDS);
        var unackedCommands = clientCommands.GetUnackedCommands();
        buffer.PutInt(unackedCommands.Count);
        foreach (Commands commands in unackedCommands)
        {
            buffer.PutInt(commands.Seq);
            buffer.PutInt(commands.UserID);
            buffer.PutInt(commands.Up ? 1 : 0);
            buffer.PutInt(commands.Down ? 1 : 0);
            buffer.PutInt(commands.Left ? 1 : 0);
            buffer.PutInt(commands.Right ? 1 : 0);
            buffer.PutInt(commands.Space ? 1 : 0);
            buffer.PutFloat(commands.Rotation);
        }
    }

    public static void ClientSerializeShot(List<Shot> shots, BitBuffer buffer)
    {
        buffer.PutByte((byte)PacketType.PLAYER_SHOT);
        buffer.PutInt(shots.Count);
        foreach (var shot in shots)
        {
            buffer.PutInt(shot.Seq);
            buffer.PutInt(shot.UserID);
            buffer.PutInt(shot.PlayerShotID);
        }
    }

    private static void DeserializeCommands(List<Commands> totalCommands, BitBuffer buffer)
    {
        var count = buffer.GetInt();
        while (count > 0)
        {
            var seq = buffer.GetInt();

            Commands commands = new Commands(
                seq,
                buffer.GetInt(),
                buffer.GetInt() > 0,
                buffer.GetInt() > 0,
                buffer.GetInt() > 0,
                buffer.GetInt() > 0,
                buffer.GetInt() > 0,
                buffer.GetFloat()
            );

            totalCommands.Add(commands);
            count--;
        }
    }

    public static void DeserializeShot(List<Shot> shots, BitBuffer buffer)
    {
        int count = buffer.GetInt();
        while (count > 0)
        {
            shots.Add(new Shot(
                    buffer.GetInt(),
                    buffer.GetInt(),
                    buffer.GetInt()
                )
            );
            count--;
        }
    }

    public static PacketType ServerDeserializeInput(BitBuffer buffer, List<Commands> commandsList,
        List<Shot> shotsList)
    {
        var messageType = buffer.GetByte();
        if (messageType == (byte) PacketType.COMMANDS)
        {
            DeserializeCommands(commandsList, buffer);
            return PacketType.COMMANDS;
        }
        if (messageType == (byte) PacketType.PLAYER_SHOT)
        {
            DeserializeShot(shotsList, buffer);
            return PacketType.PLAYER_SHOT;
        }

        return PacketType.PLAYER_DISCONNECT;
    }

    public static void ServerSerializeAck(BitBuffer buffer, int commandSequence)
    {
        buffer.PutByte((byte)PacketType.COMMANDS_ACK_MESSAGE);
        buffer.PutInt(commandSequence);
    }

    private static int ClientDeserializeAck(BitBuffer buffer)
    {
        return buffer.GetInt();
    }

    public static void ServerSerializeShotAck(BitBuffer buffer, int shotSequence)
    {
        buffer.PutByte((byte)PacketType.SHOT_ACK);
        buffer.PutInt(shotSequence);
    }
    
    private static int ClientDeserializeShotAck(BitBuffer buffer)
    {
        return buffer.GetInt();
    }
    
    private static void ClientDeserializeShotBroadcast(BitBuffer buffer, ShotBroadcast shotBroadcast)
    {
        shotBroadcast.Seq = buffer.GetInt();
        shotBroadcast.UserID = buffer.GetInt();
        shotBroadcast.PlayerShotID = buffer.GetInt();
        shotBroadcast.PlayerDied = buffer.GetInt() > 0;
    }
}
