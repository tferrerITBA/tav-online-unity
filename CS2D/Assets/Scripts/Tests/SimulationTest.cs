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

public class SimulationTest : MonoBehaviour
{
    private Dictionary<int, ServerClientInfo> clients = new Dictionary<int, ServerClientInfo>();

    private const int PortsPerClient = 2;
    public const int PlayerJoinPort = 8999;
    private Channel playerJoinChannel;
    public int sendBasePort = 9000;
    public int recvBasePort = 9001;
    public int clientCount;
    
    public int pps = 10;
    private float sendRate;
    
    private float accum;
    private float serverTime;
    public int seq; // Next snapshot to send
    private bool serverConnected = true;

    public CharacterController cubePrefab;
    public CubeClient clientPrefab;

    public ClientManager clientManager;

    public float gravity = -9.81f;
    private const int DamagePerShot = 10;
    private int shotCount = 0;

    public float ShotAckTimeout = 1f;
    public float ShotAckTime;
    public float PlayerJoinedAckTimeout = 1f;
    public float PlayerJoinedAckTime;

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
            
            CharacterController newCube = Instantiate(cubePrefab, transform); // instantiate server cube (gray)
            clients.Add(userID, new ServerClientInfo(userID, newCube));

            InstantiateClient(userID, sendBasePort + clientCount * PortsPerClient,
                recvBasePort + clientCount * PortsPerClient);
            clientCount++;

            PlayerJoined playerJoined = new PlayerJoined(userID, clientCount, seq, serverTime);
            foreach (var clientPair in clientManager.cubeClients)
            {
                var playerJoinedPacket = Packet.Obtain();
                Serializer.PlayerJoinedSerialize(playerJoinedPacket.buffer, playerJoined);
                playerJoinedPacket.buffer.Flush();
                
                string serverIP = "127.0.0.1";
                var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), clientPair.Value.recvPort);
                clientPair.Value.recvChannel.Send(playerJoinedPacket, remoteEp);

                packet.Free();
            }
        }

        if (serverConnected)
        {
            UpdateServer();
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

        /*string str = "";
        foreach (var cli in clientManager.cubeClients)
        {
            str += $"{cli.Value.userID} ";
        }
        Debug.Log(str);*/
        foreach (var cubeClientPair in clientManager.cubeClients)
        {
            int userID = cubeClientPair.Key;
            CubeClient cubeClient = cubeClientPair.Value;
            var packet = cubeClient.sendChannel.GetPacket();
            
            while (packet != null) {
                var buffer = packet.buffer;

                List<Commands> commandsList = new List<Commands>();
                List<Shot> shotsList = new List<Shot>();
                ShotBroadcast s = new ShotBroadcast();
                var packetType = Serializer.ServerDeserializeInput(buffer, commandsList, shotsList, s);
                // TODO: use generics to avoid code repetition,
                // as shots and commands are handled the same way
                switch (packetType)
                {
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

                packet = cubeClient.sendChannel.GetPacket();
            }
        }
        if (accum >= sendRate)
        {
            foreach (var cubeClientPair in clientManager.cubeClients)
            {
                int userID = cubeClientPair.Key;
                CubeClient cubeClient = cubeClientPair.Value;
                int lastCommandsReceived = clients[userID].cmdSeqReceived;
                // Serialize snapshot
                var packet = Packet.Obtain();
                Serializer.ServerWorldSerialize(clients, packet.buffer, seq,
                    serverTime, lastCommandsReceived);
                packet.buffer.Flush();

                string serverIP = "127.0.0.1";
                var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), cubeClient.recvPort);
                cubeClient.recvChannel.Send(packet, remoteEp);

                packet.Free();
            }

            accum -= sendRate;
            seq++;
        }
    }
    
    private void InstantiateClient(int userID, int sendPort, int recvPort)
    {
        CubeClient cubeClientComponent = Instantiate(clientPrefab);
        clientManager.CubeClients.Add(userID, cubeClientComponent);
            
        cubeClientComponent.Initialize(sendPort, recvPort, userID,
            gameObject.layer + clientCount + 1);
        cubeClientComponent.gameObject.SetActive(true);
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
        var clientPorts = clientManager.cubeClients.Values.Select(x => x.recvChannel).ToList();
        BroadcastShot(shot, playerDied);
    }

    private void BroadcastShot(Shot shot, bool playerDied)
    {
        ShotBroadcast s = new ShotBroadcast
        {
            ShotId = shotCount, UserID = shot.UserID, PlayerShotID = shot.PlayerShotID, PlayerDied = playerDied
        };
        clients[shot.UserID].unackedShotBroadcasts[s] = new List<int>(); 
        foreach (var client in clientManager.cubeClients)
        {
            int port = client.Value.recvPort;
            Channel channel = client.Value.recvChannel;
            var packet = Packet.Obtain();
            Serializer.ShotBroadcastMessage(packet.buffer, shot, playerDied);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
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

    private void StoreCommands(int userID, CubeClient cubeClient, List<Commands> commandsList)
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

        string serverIP = "127.0.0.1";
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), cubeClient.recvPort);
        cubeClient.recvChannel.Send(packet, remoteEp);

        packet.Free();
    }

    private void StoreShots(int userID, CubeClient cubeClient, List<Shot> shotsList)
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

        string serverIP = "127.0.0.1";
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), cubeClient.recvPort);
        cubeClient.recvChannel.Send(packet, remoteEp);

        packet.Free();
    }
}
