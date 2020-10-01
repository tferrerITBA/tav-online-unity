using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Tests;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using Random = System.Random;

public class SimulationTest : MonoBehaviour
{
    [SerializeField] private Dictionary<int, Rigidbody> serverCubes = new Dictionary<int, Rigidbody>();

    public const int PortsPerClient = 2;
    public int sendBasePort = 9000;
    public int recvBasePort = 9001;
    public int clientCount = 0;
    
    public int pps = 10;
    private float sendRate;
    
    private float accum = 0;
    private float serverTime = 0;
    private int seq = 0; // Next snapshot to send
    private bool serverConnected;

    public Rigidbody cubePrefab;
    public CubeClient clientPrefab;

    public ClientManager clientManager;

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
            int userID = CubeEntity.PlayerConnectDeserialize(packet.buffer);
            
            Rigidbody newCube = Instantiate(cubePrefab, transform); // instantiate server cube (gray)
            serverCubes.Add(userID, newCube);
            
            InstantiateClient(userID, sendBasePort + clientCount * PortsPerClient,
                recvBasePort + clientCount * PortsPerClient);
            clientCount++;

            PlayerJoined playerJoined = new PlayerJoined(userID, clientCount, seq, serverTime);
            foreach (var clientPair in clientManager.cubeClients)
            {
                var playerJoinedPacket = Packet.Obtain();
                CubeEntity.PlayerJoinedSerialize(playerJoinedPacket.buffer, playerJoined);
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

    private void UpdateServer()
    {
        serverTime += Time.deltaTime;

        foreach (var cubeClientPair in clientManager.cubeClients)
        {
            int userID = cubeClientPair.Key;
            CubeClient cubeClient = cubeClientPair.Value;
            var commandPacket = cubeClient.sendChannel.GetPacket();
            
            if (commandPacket != null) {
                var buffer = commandPacket.buffer;

                List<Commands> commandsList = CubeEntity.ServerDeserializeInput(buffer);

                var packet = Packet.Obtain();
                int receivedCommandSequence = -1;
                foreach (Commands commands in commandsList)
                {
                    receivedCommandSequence = commands.Seq;
                    ExecuteClientInput(commands);
                }
                CubeEntity.ServerSerializeAck(packet.buffer, receivedCommandSequence);
                packet.buffer.Flush();

                string serverIP = "127.0.0.1";
                var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), cubeClient.recvPort);
                cubeClient.recvChannel.Send(packet, remoteEp);

                packet.Free();
            }
        }
        if (accum >= sendRate)
        {
            foreach (var cubeClientPair in clientManager.cubeClients)
            {
                CubeClient cubeClient = cubeClientPair.Value;
                //serialize
                var packet = Packet.Obtain();
                CubeEntity.ServerWorldSerialize(serverCubes, packet.buffer, seq, serverTime);
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
        Rigidbody cubeRigidBody = serverCubes[commands.UserID];
        //apply input
        if (commands.Space) {
            cubeRigidBody.AddForceAtPosition(Vector3.up * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Left) {
            cubeRigidBody.AddForceAtPosition(Vector3.left * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Right) {
            cubeRigidBody.AddForceAtPosition(Vector3.right * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Up) {
            cubeRigidBody.AddForceAtPosition(Vector3.forward * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Down) {
            cubeRigidBody.AddForceAtPosition(Vector3.back * 5, Vector3.zero, ForceMode.Impulse);
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
}
