using System;
using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;

public class CommandsList
{
    private readonly List<Commands> commands = new List<Commands>();
    public int ackedIndex = -1;
    public int snapshotAckedIndex = -1;

    public void ClearAcked()
    {
        return;
        commands.RemoveRange(0, ackedIndex + 1);
        ackedIndex = -1;
    }

    public List<Commands> GetUnackedCommands()
    {
        if (ackedIndex < 0)
            return commands;

        if (commands.Count < ackedIndex + 1)
        {
            Debug.Log(commands.Count);
            return new List<Commands>(0);
        }
           
        return commands.GetRange(ackedIndex + 1, commands.Count - ackedIndex - 1);
    }

    public List<Commands> GetSnapshotUnackedCommands()
    {
        if (snapshotAckedIndex < 0)
            return commands;
        if (commands.Count < snapshotAckedIndex + 1)
        {
            Debug.Log(commands.Count);
            return new List<Commands>(0);
        }
        return commands.GetRange(snapshotAckedIndex + 1, commands.Count - snapshotAckedIndex - 1);
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
        foreach (var cmd in commands)
        {
            if (cmd.Seq > receivedAckSequence)
            {
                break;
            }
            ackedIndex++;
        }
        ClearAcked();
    }

    public void SnapshotAck(int receivedAckSequence)
    {
        foreach (var cmd in commands)
        {
            if (cmd.Seq > receivedAckSequence)
            {
                break;
            }
            snapshotAckedIndex++;
        }
        // ClearAcked();
    }
}
