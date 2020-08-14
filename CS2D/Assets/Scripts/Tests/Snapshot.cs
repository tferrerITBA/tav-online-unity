using System;
using UnityEngine;

public class Snapshot : IComparable<Snapshot>
{
    private int seq;
    private float time;
    private Vector3 position;
    private Quaternion rotation;

    public Snapshot(int seq, float time, Vector3 position, Quaternion rotation)
    {
        this.seq = seq;
        this.time = time;
        this.position = position;
        this.rotation = rotation;
    }

    public int CompareTo(Snapshot other)
    {
        return seq.CompareTo(other.seq);
    }

    public int Seq => seq;

    public float Time => time;

    public Vector3 Position => position;

    public Quaternion Rotation => rotation;

    public override string ToString()
    {
        return $"Seq: {seq}, Time: {time}, Position: {position}, Rotation: {rotation}";
    }
}