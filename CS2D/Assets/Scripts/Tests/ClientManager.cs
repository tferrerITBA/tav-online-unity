using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class ClientManager : MonoBehaviour
{
    public const int Port = 8998;
    public Channel channel;
    
    public CubeClient clientPrefab;
    private Dictionary<int, CubeClient> cubeClients = new Dictionary<int, CubeClient>();
    private int startingLayer = 9;
    private int clientCount = 0;

    public Dictionary<int, CubeClient> CubeClients => cubeClients;

    public int interpolationCount = 2;

    //public SimulationTest tralala;
    
    void Start()
    {
        channel = new Channel(Port);
    }

    // Update is called once per frame
    void Update()
    {
        /*string str = "";
        foreach (var cli in cubeClients)
        {
            str += $"{cli.Value.displaySeq} ";
        }
        Debug.Log(str);*/
        /*int jaja = -1;
        foreach (var cli in cubeClients)
        {
            if (jaja < cli.Value.displaySeq)
                jaja = cli.Value.displaySeq;
        }
        if (tralala.seq - jaja > 30 && jaja > 0)
            Debug.Log("PROBLEMA");*/
        if (Input.GetKeyDown(KeyCode.C))
        {
            int userID = Random.Range(0, 8096);
            var packet = Packet.Obtain();
            Serializer.PlayerConnectSerialize(packet.buffer, userID);
            packet.buffer.Flush();
            
            string serverIP = "127.0.0.1";
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), SimulationTest.PlayerJoinPort);
            channel.Send(packet, remoteEp);
            
            packet.Free();
        }

        var resp = channel.GetPacket();
        if (resp != null)
        {
            var responseData = Serializer.PlayerConnectResponseDeserialize(resp.buffer);
            clientCount++;
            var userID = responseData[0];
            var srvPort = responseData[1];
            var cliPort = responseData[2];
            InstantiateClient(userID, srvPort, cliPort);
        }
    }
    
    private void InstantiateClient(int userID, int srvPort, int cliPort)
    {
        CubeClient cubeClientComponent = Instantiate(clientPrefab);
        CubeClients.Add(userID, cubeClientComponent);
            
        cubeClientComponent.Initialize(srvPort, cliPort, userID,
            gameObject.layer + clientCount);
        cubeClientComponent.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        channel.Disconnect();
    }
}
