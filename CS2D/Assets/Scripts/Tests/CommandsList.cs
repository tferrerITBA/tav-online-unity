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
        int minIndex = Math.Min(ackedIndex, snapshotAckedIndex);
        commands.RemoveRange(0, minIndex + 1);
        ackedIndex -= minIndex + 1;
        snapshotAckedIndex -= minIndex + 1;
    }

    public List<Commands> GetUnackedCommands()
    {
        if (ackedIndex < 0)
            return commands;

        if (commands.Count < ackedIndex + 1)
            return new List<Commands>(0);

        return commands.GetRange(ackedIndex + 1, commands.Count - ackedIndex - 1);
    }

    public List<Commands> GetSnapshotUnackedCommands()
    {
        if (snapshotAckedIndex < 0)
            return commands;
        if (commands.Count < snapshotAckedIndex + 1)
            return new List<Commands>(0);

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
        for(int i = ackedIndex + 1; i < commands.Count; i++)
        {
            var cmd = commands[i];
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
        for(int i = snapshotAckedIndex + 1; i < commands.Count; i++)
        {
            var cmd = commands[i];
            if (cmd.Seq > receivedAckSequence)
            {
                break;
            }
            snapshotAckedIndex++;
        }
        // ClearAcked();
    }
}
