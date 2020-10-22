using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class SimulationTest : MonoBehaviour
{
    private Dictionary<int, ServerClientInfo> clients = new Dictionary<int, ServerClientInfo>();

    private const int PortsPerClient = 2;
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

    // Start is called before the first frame update
    void Start() {
        sendRate = 1f / pps;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.D))
        {
            serverConnected = !serverConnected;
        }

        accum += Time.deltaTime;

        var packet = clientManager.playerJoinSendChannel.GetPacket();
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
            cli.pendingCommands.Clear();
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
            var commandPacket = cubeClient.sendChannel.GetPacket();
            
            if (commandPacket != null) {
                var buffer = commandPacket.buffer;

                List<Commands> commandsList = Serializer.ServerDeserializeInput(buffer);

                var packet = Packet.Obtain();
                int receivedCommandSequence = 0;
                foreach (Commands commands in commandsList)
                {
                    receivedCommandSequence = commands.Seq;
                    // Debug.Log($"rcvd {receivedCommandSequence} vs {clients[userID].cmdSeqReceived}");
                    if (receivedCommandSequence > clients[userID].cmdSeqReceived)
                        clients[userID].pendingCommands.Add(commands);
                }
                Serializer.ServerSerializeAck(packet.buffer, receivedCommandSequence);
                packet.buffer.Flush();

                string serverIP = "127.0.0.1";
                var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), cubeClient.recvPort);
                cubeClient.recvChannel.Send(packet, remoteEp);

                packet.Free();

                clients[userID].cmdSeqReceived = receivedCommandSequence;
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

    private void ExecuteClientInput(Commands commands)
    {
        CharacterController cubeCharacterCtrl = clients[commands.UserID].characterController;
        
        Vector3 move = new Vector3();
        //Debug.Log(commands);
        move.x += commands.GetXDirection() * Time.fixedDeltaTime;
        move.z += commands.GetZDirection() * Time.fixedDeltaTime;

        cubeCharacterCtrl.Move(move);
    }

    private void InstantiateClient(int userID, int sendPort, int recvPort)
    {
        CubeClient cubeClientComponent = Instantiate(clientPrefab);
        clientManager.CubeClients.Add(userID, cubeClientComponent);
            
        cubeClientComponent.Initialize(sendPort, recvPort, userID,
            gameObject.layer + clientCount + 1);
        cubeClientComponent.gameObject.SetActive(true);
    }
}
