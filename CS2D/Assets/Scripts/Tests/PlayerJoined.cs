using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJoined
{
    private int userID;
    private int playerCount;
    private int seq;
    private float time;
    private bool instantiateCubesPending;

    public PlayerJoined()
    {
    }

    public PlayerJoined(int userID, int playerCount, int seq, float time)
    {
        this.userID = userID;
        this.playerCount = playerCount;
        this.seq = seq;
        this.time = time;
    }

    public int UserID
    {
        get => userID;
        set => userID = value;
    }

    public int PlayerCount
    {
        get => playerCount;
        set => playerCount = value;
    }

    public int Seq
    {
        get => seq;
        set => seq = value;
    }

    public float Time
    {
        get => time;
        set => time = value;
    }
    
    public bool InstantiateCubesPending
    {
        get => instantiateCubesPending;
        set => instantiateCubesPending = value;
    }
}
