using System;
using System.Collections.Generic;
using System.Numerics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class Snapshot : IComparable<Snapshot>
{
    private int seq;
    private float time;
    private int cmdSeq;
    private Dictionary<int, UserState> userStates;

    public Snapshot(int seq, float time, int cmdSeq, Dictionary<int, UserState> userStates)
    {
        this.seq = seq;
        this.time = time;
        this.cmdSeq = cmdSeq;
        this.userStates = userStates;
    }

    public int CompareTo(Snapshot other)
    {
        return seq.CompareTo(other.seq);
    }

    public int Seq => seq;

    public float Time => time;
    
    public int CmdSeq => cmdSeq;

    public Dictionary<int, UserState> UserStates => userStates;

    public override string ToString()
    {
        var playerCount = userStates.Count;
        var str = $"Seq: {seq}, Time: {time}, CmdSeq: {cmdSeq}, Players: {playerCount}\n";
        foreach (var userStatePair in userStates)
        {
            str += $"\tPlayer: {userStatePair.Key} Position: {userStatePair.Value.Position} " +
                   $"Rotation: {userStatePair.Value.Position}\n";
        }
        return str;
    }
}