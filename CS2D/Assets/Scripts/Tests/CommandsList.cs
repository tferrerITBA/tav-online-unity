using System;
using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;

public class CommandsList
{
    private List<Commands> commands = new List<Commands>();
    public int ackedCount;
    public int snapshotAckedCount;

    public void ClearAcked()
    {
        int minAckedCount = Math.Min(ackedCount, snapshotAckedCount);
        if (ackedCount > snapshotAckedCount) 
            Debug.Log($"ack: {ackedCount} snapack {snapshotAckedCount} count {commands.Count}");
        commands.RemoveRange(0, minAckedCount);
        ackedCount -= minAckedCount;
        snapshotAckedCount -= minAckedCount;
    }

    public List<Commands> GetUnackedCommands()
    {
        if (ackedCount == 0)
            return commands;
        return commands.GetRange(ackedCount, commands.Count - ackedCount);
    }

    public List<Commands> GetSnapshotUnackedCommands()
    {
        if (snapshotAckedCount == 0)
            return commands;
        return commands.GetRange(snapshotAckedCount, commands.Count - snapshotAckedCount);
    }

    public void Add(Commands newCommands)
    {
        commands.Add(newCommands);
    }

    public int Count()
    {
        return commands.Count;
    }

    public void Ack(int receivedAckSequence)
    {
        foreach (var cmds in commands)
        {
            if (cmds.Seq > receivedAckSequence)
            {
                break;
            }
            ackedCount++;
        }
        ClearAcked();
    }

    public void SnapshotAck(int receivedAckSequence)
    {
        foreach (var cmds in commands)
        {
            if (cmds.Seq > receivedAckSequence)
            {
                break;
            }
            snapshotAckedCount++;
        }
        ClearAcked();
    }

    public override string ToString()
    {
        return $"Count: {commands.Count} Acked: {ackedCount} SnapshotAcked: {snapshotAckedCount}";
    }
}
