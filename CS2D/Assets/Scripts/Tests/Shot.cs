using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shot
{
    private int seq;
    private int userID;
    private int playerShotID;

    public Shot(int seq, int userID, int playerShotID)
    {
        this.seq = seq;
        this.userID = userID;
        this.playerShotID = playerShotID;
    }

    public int Seq => seq;

    public int UserID => userID;

    public int PlayerShotID => playerShotID;
}
