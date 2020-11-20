using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class ClientManager : MonoBehaviour
{
    public string serverIP = "127.0.0.1";
    private IPEndPoint serverRemote;
    public int clientPort = 9001;
    public Channel channel;
    
    public CubeClient clientPrefab;
    private Dictionary<int, CubeClient> cubeClients = new Dictionary<int, CubeClient>();
    private int startingLayer = 9;
    private int clientCount;

    public Dictionary<int, CubeClient> CubeClients => cubeClients;

    public int interpolationCount = 2;

    void Start()
    {
        channel = new Channel(clientPort);
        serverRemote = new IPEndPoint(IPAddress.Parse(serverIP), ServerEntity.PlayerJoinPort);
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

            var remoteEp = serverRemote;
            channel.Send(packet, remoteEp);
            
            packet.Free();
        }

        var resp = channel.GetPacket();
        if (resp != null)
        {
            var responseData = Serializer.PlayerConnectResponseDeserialize(resp.buffer);
            var userID = responseData[0];
            var srvPort = responseData[1];
            InstantiateClient(userID, srvPort, channel);
            
            clientCount++;
            clientPort += 2;
            channel = new Channel(clientPort);
        }
    }
    
    private void InstantiateClient(int userID, int srvPort, Channel clientChannel)
    {
        CubeClient cubeClientComponent = Instantiate(clientPrefab);
        CubeClients.Add(userID, cubeClientComponent);

        var layer = startingLayer + clientCount;
        cubeClientComponent.Initialize(serverIP, srvPort, userID, layer, clientChannel);
        cubeClientComponent.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        channel.Disconnect();
    }
}
