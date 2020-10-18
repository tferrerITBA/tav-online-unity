using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class ClientManager : MonoBehaviour
{
    public int playerJoinSendPort;
    public Channel playerJoinSendChannel;

    public int playerJoinRecvPort;
    public Channel playerJoinRecvChannel;

    public Dictionary<int, CubeClient> cubeClients = new Dictionary<int, CubeClient>();

    public Dictionary<int, CubeClient> CubeClients => cubeClients;

    public int interpolationCount = 2;

    //public SimulationTest tralala;
    
    
    // Start is called before the first frame update
    void Start()
    {
        playerJoinSendChannel = new Channel(playerJoinSendPort);
        playerJoinRecvChannel = new Channel(playerJoinRecvPort);
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
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), playerJoinSendPort);
            playerJoinSendChannel.Send(packet, remoteEp);
            
            packet.Free();
        }
    }

    private void OnDestroy()
    {
        playerJoinRecvChannel.Disconnect();
        playerJoinSendChannel.Disconnect();
    }
}
