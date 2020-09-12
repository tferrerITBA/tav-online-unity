using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientManager : MonoBehaviour
{
    public int playerJoinSendPort;
    public Channel playerJoinSendChannel;

    public int playerJoinRecvPort;
    public Channel playerJoinRecvChannel;
    
    public CubeClient clientPrefab;
    public Dictionary<int, CubeClient> cubeClients = new Dictionary<int, CubeClient>();
    
    public int interpolationCount = 2;
    
    // Start is called before the first frame update
    void Start()
    {
        playerJoinSendChannel = new Channel(playerJoinSendPort);
        playerJoinRecvChannel = new Channel(playerJoinRecvPort);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            int userID = Random.Range(0, 8096);
            var packet = Packet.Obtain();
            CubeEntity.PlayerConnectSerialize(packet.buffer, userID);
            
            string serverIP = "127.0.0.1";
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), playerJoinSendPort);
            playerJoinSendChannel.Send(packet, remoteEp);
            
            packet.Free();
        }

        var newConnectionPacket = playerJoinRecvChannel.GetPacket();
        if (newConnectionPacket != null)
        {
            var buffer = newConnectionPacket.buffer; // playerJoined
            int[] userData = CubeEntity.PlayerJoinedDeserialize(buffer);
            CubeClient cubeClientComponent = Instantiate(clientPrefab);
            cubeClients.Add(userData[0], cubeClientComponent);
            
            cubeClientComponent.Initialize(userData[2], userData[3], 
                userData[0], interpolationCount);
        }
    }
}
