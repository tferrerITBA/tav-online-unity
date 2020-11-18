using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotBroadcast
{
    private int seq;
    private int userID;
    private int playerShotID;
    private bool playerDied;

    public ShotBroadcast()
    {
    }
    
    public int Seq
    {
        get => seq;
        set => seq = value;
    }

    public int UserID
    {
        get => userID;
        set => userID = value;
    }

    public int PlayerShotID
    {
        get => playerShotID;
        set => playerShotID = value;
    }

    public bool PlayerDied
    {
        get => playerDied;
        set => playerDied = value;
    }
}

