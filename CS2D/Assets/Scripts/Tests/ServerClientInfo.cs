using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;

public class ServerClientInfo
{
    public int userID;
    public int cmdSeqReceived;
    public int shotSeqReceived;
    public List<Commands> pendingCommands = new List<Commands>();
    public List<Shot> pendingShots = new List<Shot>();
    public CharacterController characterController;
    public int health = 100;

    public ServerClientInfo(int userID, CharacterController characterController)
    {
        this.userID = userID;
        this.characterController = characterController;
    }
}
