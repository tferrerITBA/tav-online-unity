using System.Collections;
using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;

public class ServerClientInfo
{
    public int userID;
    public Channel channel;
    public IPEndPoint dest;
    public int cmdSeqReceived;
    public int shotSeqReceived;
    public List<Commands> pendingCommands = new List<Commands>();
    public List<Shot> pendingShots = new List<Shot>();
    public Dictionary<ShotBroadcast, List<int>> unackedShotBroadcasts = new Dictionary<ShotBroadcast, List<int>>();
    public CharacterController characterController;
    public int health = 100;

    public ServerClientInfo(int userID, int origPort, IPEndPoint dest, CharacterController characterController)
    {
        this.userID = userID;
        this.channel = new Channel(origPort);
        this.dest = dest;
        this.characterController = characterController;
    }
}
