using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Tests;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

/*
 * En el Update recibir los Shots. Aplicar todos los shots que lleguen
 * Para cada uno de los disparos foreach de los clientes y mandar broadcast con tal disparo a tal
 * y lo mato o no. Mandar el ack. Mandar un ack de ese broadcast.
 */

public class ServerEntity : MonoBehaviour
{
    private Dictionary<int, ServerClientInfo> clients = new Dictionary<int, ServerClientInfo>();
    
    public const int PlayerJoinPort = 8999;
    private Channel playerJoinChannel;
    public int serverToClientPort = 9000;
    
    private int clientCount;
    
    private int pps = 10;
    private float sendRate;
    
    private float accum;
    private float serverTime;
    private int seq; // Next snapshot to send
    private bool serverConnected = true;

    public CharacterController cubePrefab;
    public CubeClient clientPrefab;

    public ClientManager clientManager;

    public float gravity = -9.81f;
    private const int DamagePerShot = 10;
    private int shotCount;

    public float ShotAckTimeout = 1f;
    public float ShotAckTime;
    public float PlayerJoinedAckTimeout = 1f;
    public float PlayerJoinedAckTime;
    
    private Dictionary<PlayerJoined, List<int>> pendingPlayerJoined = new Dictionary<PlayerJoined, List<int>>();

    // Start is called before the first frame update
    void Start() {
        sendRate = 1f / pps;
        playerJoinChannel = new Channel(PlayerJoinPort);
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.J))
        {
            serverConnected = !serverConnected;
        }

        accum += Time.deltaTime;

        var packet = playerJoinChannel.GetPacket();
        if (packet != null)
        {
            int userID = Serializer.PlayerConnectDeserialize(packet.buffer);
            var clientInfo = new IPEndPoint(packet.fromEndPoint.Address, packet.fromEndPoint.Port);
            RegisterClient(userID, clientInfo);
            BroadcastPlayerJoined(userID);
        }

        if (serverConnected)
        {
            UpdateServer();
        }
    }

    private void RegisterClient(int userID, IPEndPoint clientInfo)
    {
        var origPort = serverToClientPort;
        CharacterController newCube = Instantiate(cubePrefab, transform); // instantiate server cube (gray)
        clients.Add(userID, new ServerClientInfo(userID, origPort, clientInfo, newCube));
            
        clientCount++;
        serverToClientPort += 2;

        var playerConnectResponse = Packet.Obtain();
        Serializer.PlayerConnectResponse(playerConnectResponse.buffer, userID, origPort);
        playerConnectResponse.buffer.Flush();
            
        playerJoinChannel.Send(playerConnectResponse, clientInfo);
        playerConnectResponse.Free();
    }

    private void BroadcastPlayerJoined(int userID)
    {
        PlayerJoined playerJoined = new PlayerJoined(userID, clientCount, seq, serverTime);
        pendingPlayerJoined[playerJoined] = new List<int>(clients.Count);
        
        foreach (var clientPair in clients)
        {
            var playerJoinedPacket = Packet.Obtain();
            Serializer.PlayerJoinedSerialize(playerJoinedPacket.buffer, playerJoined);
            playerJoinedPacket.buffer.Flush();
                
            var remoteEp = clientPair.Value.dest;
            clientPair.Value.channel.Send(playerJoinedPacket, remoteEp);

            playerJoinedPacket.Free();
            pendingPlayerJoined[playerJoined].Add(clientPair.Key);
        }
    }

    private void AckPlayerJoinedBroadcast(int userID, PlayerJoined playerJoined)
    {
        var list = pendingPlayerJoined[playerJoined];
        list.RemoveAll(x => x == userID);
        if (list.Count == 0)
        {
            pendingPlayerJoined.Remove(playerJoined);
        }
    }

    private void FixedUpdate()
    {
        foreach (var client in clients)
        {
            var cube = client.Value.characterController;
            if (!cube.isGrounded)
            {
                // Vector3 vel = new Vector3(0, gravity * Time.deltaTime, 0);
                // cube.Move(vel * Time.deltaTime);
                cube.SimpleMove(Vector3.zero);
            }
        }
        foreach (var clientPair in clients)
        {
            var cli = clientPair.Value;
            foreach (var commands in cli.pendingCommands)
            {
                ExecuteClientInput(commands);
            }

            foreach (var shot in cli.pendingShots)
            {
                ExecuteShot(shot);
            }
            cli.pendingCommands.Clear();
            cli.pendingShots.Clear();
        }
    }

    private void UpdateServer()
    {
        serverTime += Time.deltaTime;
        
        foreach (var cubeClientPair in clients)
        {
            int userID = cubeClientPair.Key;
            var cubeClient = cubeClientPair.Value;
            var packet = cubeClient.channel.GetPacket();
            
            while (packet != null) {
                var buffer = packet.buffer;

                List<Commands> commandsList = new List<Commands>();
                List<Shot> shotsList = new List<Shot>();
                ShotBroadcast s = new ShotBroadcast();
                PlayerJoined p = new PlayerJoined();
                var packetType = Serializer.ServerDeserializeInput(buffer, commandsList, shotsList, s, p);
                // TODO: use generics to avoid code repetition,
                // as shots and commands are handled the same way
                switch (packetType)
                {
                    case PacketType.PLAYER_JOINED_ACK:
                        AckPlayerJoinedBroadcast(userID, p);
                        break;
                    case PacketType.COMMANDS:
                        StoreCommands(userID, cubeClient, commandsList);
                        break;
                    case PacketType.PLAYER_SHOT:
                        StoreShots(userID, cubeClient, shotsList);
                        break;
                    case PacketType.SHOT_BROADCAST_ACK:
                        AckBroadcastShot(userID, s);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                packet = cubeClient.channel.GetPacket();
            }
        }
        if (accum >= sendRate)
        {
            foreach (var cubeClientPair in clients)
            {
                int userID = cubeClientPair.Key;
                var cubeClient = cubeClientPair.Value;
                int lastCommandsReceived = clients[userID].cmdSeqReceived;
                // Serialize snapshot
                var packet = Packet.Obtain();
                Serializer.ServerWorldSerialize(clients, packet.buffer, seq,
                    serverTime, lastCommandsReceived);
                packet.buffer.Flush();
                
                var remoteEp = cubeClientPair.Value.dest;
                cubeClient.channel.Send(packet, remoteEp);

                packet.Free();
            }

            accum -= sendRate;
            seq++;
        }
    }

    private void ExecuteClientInput(Commands commands)
    {
        CharacterController cubeCharacterCtrl = clients[commands.UserID].characterController;
        
        Vector3 move = new Vector3();
        //Debug.Log(commands);
        move.x += commands.GetXDirection() * Time.fixedDeltaTime;
        move.z += commands.GetZDirection() * Time.fixedDeltaTime;

        cubeCharacterCtrl.transform.rotation = Quaternion.Euler(0, commands.Rotation, 0);
        move = cubeCharacterCtrl.transform.TransformDirection(move);
        cubeCharacterCtrl.Move(move);
    }

    private void ExecuteShot(Shot shot)
    {
        shotCount++;
        clients[shot.PlayerShotID].health -= DamagePerShot;
        Debug.Log(clients[shot.PlayerShotID].health);
        bool playerDied = clients[shot.PlayerShotID].health <= 0;
        // var clientPorts = clientManager.cubeClients.Values.Select(x => x.channel).ToList();
        BroadcastShot(shot, playerDied);
    }

    private void BroadcastShot(Shot shot, bool playerDied)
    {
        ShotBroadcast s = new ShotBroadcast
        {
            ShotId = shotCount, UserID = shot.UserID, PlayerShotID = shot.PlayerShotID, PlayerDied = playerDied
        };
        clients[shot.UserID].unackedShotBroadcasts[s] = new List<int>(); 
        foreach (var client in clients)
        {
            Channel channel = client.Value.channel;
            var packet = Packet.Obtain();
            Serializer.ShotBroadcastMessage(packet.buffer, shot, playerDied);
            packet.buffer.Flush();
            
            var remoteEp = client.Value.dest;
            channel.Send(packet, remoteEp);

            packet.Free();
            clients[shot.UserID].unackedShotBroadcasts[s].Add(client.Key);
        }
    }

    private void AckBroadcastShot(int userID, ShotBroadcast shotBroadcast)
    {
        var pendingAcks = clients[shotBroadcast.UserID].unackedShotBroadcasts[shotBroadcast];
        pendingAcks.RemoveAll(
            x => x == userID);
        if (pendingAcks.Count == 0)
            clients[shotBroadcast.UserID].unackedShotBroadcasts.Remove(shotBroadcast);
    }

    private void StoreCommands(int userID, ServerClientInfo client, List<Commands> commandsList)
    {
        var packet = Packet.Obtain();
                
        foreach (Commands commands in commandsList)
        {
            int receivedCommandSequence = commands.Seq;
            // Debug.Log($"rcvd {receivedCommandSequence} vs {clients[userID].cmdSeqReceived}");
            if (receivedCommandSequence > clients[userID].cmdSeqReceived)
            {
                clients[userID].pendingCommands.Add(commands);
                clients[userID].cmdSeqReceived = receivedCommandSequence;
            }
        }
        Serializer.ServerSerializeAck(packet.buffer, clients[userID].cmdSeqReceived);
        packet.buffer.Flush();
        
        var remoteEp = client.dest;
        client.channel.Send(packet, remoteEp);

        packet.Free();
    }

    private void StoreShots(int userID, ServerClientInfo client, List<Shot> shotsList)
    {
        var packet = Packet.Obtain();
        
        foreach (Shot shot in shotsList)
        {
            int receivedShotSequence = shot.Seq;
            if (receivedShotSequence > clients[userID].shotSeqReceived)
            {
                clients[userID].pendingShots.Add(shot);
                clients[userID].shotSeqReceived = receivedShotSequence;
            }
        }
        Serializer.ServerSerializeShotAck(packet.buffer, clients[userID].shotSeqReceived);
        packet.buffer.Flush();
        
        var remoteEp = client.dest;
        client.channel.Send(packet, remoteEp);

        packet.Free();
    }

    public void OnDestroy()
    {
        playerJoinChannel.Disconnect();
        foreach (var cli in clients)
        {
            cli.Value.channel.Disconnect();
        }
    }
}
