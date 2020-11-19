using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class ClientManager : MonoBehaviour
{
    public int port = 8998;
    public Channel channel;

    public Dictionary<int, CubeClient> cubeClients = new Dictionary<int, CubeClient>();

    public Dictionary<int, CubeClient> CubeClients => cubeClients;

    public int interpolationCount = 2;

    //public SimulationTest tralala;
    
    void Start()
    {
        channel = new Channel(port);
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
    }

    private void OnDestroy()
    {
        channel.Disconnect();
    }
}
