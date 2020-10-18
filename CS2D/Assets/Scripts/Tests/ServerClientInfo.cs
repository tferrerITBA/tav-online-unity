using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;

public class ServerClientInfo
{
    public int userID;
    public int cmdSeqReceived;
    public List<Commands> pendingCommands = new List<Commands>();
    public CharacterController characterController;

    public ServerClientInfo(int userID, CharacterController characterController)
    {
        this.userID = userID;
        this.characterController = characterController;
    }
}
