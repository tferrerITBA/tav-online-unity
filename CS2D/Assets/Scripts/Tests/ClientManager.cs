using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class ClientManager : MonoBehaviour
{
    private string serverIP;
    private IPEndPoint serverRemote;
    private int clientPort;
    private Channel channel;
    
    public CubeClient clientPrefab;
    private int startingLayer = 9;
    private int clientCount;

    private bool sentConnection;

    void Start()
    {
        if (PlayerPrefs.GetInt("isServer") > 0)
        {
            Destroy(gameObject);
            return;
        }
        serverIP = PlayerPrefs.GetString("serverIP", "127.0.0.1");
        clientPort = PlayerPrefs.GetInt("clientPort", 9001);
        channel = new Channel(clientPort); // TODO: Handle port in use (exception is caught!)
        serverRemote = new IPEndPoint(IPAddress.Parse(serverIP), ServerEntity.PlayerJoinPort);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
    
    void Update()
    {
        if (!sentConnection)
        {
            sentConnection = true;
            int userID = Random.Range(0, 8096);
            var packet = Packet.Obtain();
            Serializer.PlayerConnectSerialize(packet.buffer, userID);
            packet.buffer.Flush();

            var remoteEp = serverRemote;
            channel.Send(packet, remoteEp);
            
            packet.Free();
        }

        var resp = channel?.GetPacket();
        if (resp != null)
        {
            var responseData = Serializer.PlayerConnectResponseDeserialize(resp.buffer);
            var userID = responseData[0];
            var srvPort = responseData[1];
            InstantiateClient(userID, srvPort, channel);
            
            clientPort += 2;
            clientCount++;
            channel = null;
            // channel = new Channel(clientPort); // for new player connections
        }
    }
    
    private void InstantiateClient(int userID, int srvPort, Channel clientChannel)
    {
        CubeClient cubeClientComponent = Instantiate(clientPrefab);

        var layer = startingLayer + clientCount;
        cubeClientComponent.Initialize(serverIP, srvPort, userID, layer, clientChannel);
        cubeClientComponent.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        channel?.Disconnect();
    }
}
