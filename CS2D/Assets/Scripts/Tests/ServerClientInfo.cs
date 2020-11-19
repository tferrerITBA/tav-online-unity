using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;

public class ServerClientInfo
{
    public int userID;
    public Channel channel;
    public int destPort;
    public int cmdSeqReceived;
    public int shotSeqReceived;
    public List<Commands> pendingCommands = new List<Commands>();
    public List<Shot> pendingShots = new List<Shot>();
    public Dictionary<ShotBroadcast, List<int>> unackedShotBroadcasts = new Dictionary<ShotBroadcast, List<int>>();
    public CharacterController characterController;
    public int health = 100;

    public ServerClientInfo(int userID, int origPort, int destPort, CharacterController characterController)
    {
        this.userID = userID;
        this.channel = new Channel(origPort);
        this.destPort = destPort;
        this.characterController = characterController;
    }
}
