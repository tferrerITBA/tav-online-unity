using System;
using UnityEngine;

public class Snapshot : IComparable<Snapshot>
{
    private int seq;
    private Vector3 position;
    private Quaternion rotation;

    public Snapshot(int seq, Vector3 position, Quaternion rotation)
    {
        this.seq = seq;
        this.position = position;
        this.rotation = rotation;
    }


    public int CompareTo(Snapshot other)
    {
        return seq.CompareTo(other.seq);
    }

    public int Seq => seq;

    public Vector3 Position => position;

    public Quaternion Rotation => rotation;

    public override string ToString()
    {
        return $"Seq: {seq}, Position: {position}, Rotation: {rotation}";
    }
}