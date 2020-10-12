using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerClientInfo
{
    public int userID;
    public int cmdSeqReceived;
    public CharacterController characterController;

    public ServerClientInfo(int userID, CharacterController characterController)
    {
        this.userID = userID;
        this.characterController = characterController;
    }
}
