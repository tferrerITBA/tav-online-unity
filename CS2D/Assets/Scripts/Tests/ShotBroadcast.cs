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
    
    public override bool Equals(object obj)
    {
        ShotBroadcast other = obj as ShotBroadcast;

        if (other == null)
        {
            return false;
        }

        return seq == other.Seq && userID == other.UserID && playerShotID == other.PlayerShotID
               && playerDied == other.playerDied;
    }

    public override int GetHashCode()
    {
        int hash = 13;
        hash = (hash * 7) + seq.GetHashCode();
        hash = (hash * 7) + userID.GetHashCode();
        hash = (hash * 7) + playerShotID.GetHashCode();
        hash = (hash * 7) + playerDied.GetHashCode();
        return hash;
    }
}

