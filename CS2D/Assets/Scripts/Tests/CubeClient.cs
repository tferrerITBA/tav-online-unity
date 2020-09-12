using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;

public class CubeClient : MonoBehaviour
{
    public int sendPort;
    public int recvPort;
    public Channel sendChannel;
    public Channel recvChannel;

    public int userID;
    public int displaySeq = 0;
    public float time = 0;
    public bool isPlaying;

    private List<Snapshot> interpolationBuffer = new List<Snapshot>();
    private List<Commands> commands = new List<Commands>();
    private Dictionary<int, CubeClient> cubes = new Dictionary<int, CubeClient>();

    public GameObject cubePrefab;
    
    public int minBufferElems;

    public void Initialize(int sendPort, int recvPort, int userID, int minBufferElems)
    {
        this.sendPort = sendPort;
        this.sendChannel = new Channel(sendPort);
        this.recvPort = recvPort;
        this.recvChannel = new Channel(recvPort);
        this.userID = userID;
        this.minBufferElems = minBufferElems;
    }

    private void Update()
    {
        var packet = recvChannel.GetPacket();

        if (packet != null) {
            var buffer = packet.buffer;

            //deserialize
            CubeEntity.ClientDeserialize(interpolationBuffer, buffer, displaySeq, commands);
            //networkSeq++;
        }

        ReadClientInput();

        if (interpolationBuffer.Count >= minBufferElems)
            isPlaying = true;
        else if (interpolationBuffer.Count <= 1)
            isPlaying = false;
        
        if (isPlaying)
        {
            //accumCli += Time.deltaTime;
            time += Time.deltaTime;
            var previousTime = interpolationBuffer[0].Time;
            var nextTime = interpolationBuffer[1].Time;
            if (time >= nextTime) {
                interpolationBuffer.RemoveAt(0);
                displaySeq++;
                if (interpolationBuffer.Count < 2)
                {
                    isPlaying = false;
                    return;
                }
                previousTime = interpolationBuffer[0].Time;
                nextTime =  interpolationBuffer[1].Time;
            }
            var t =  (time - previousTime) / (nextTime - previousTime);
            Interpolate(interpolationBuffer[0], interpolationBuffer[1], t);
        }
    }
    
    private void ReadClientInput()
    {
        Commands currentCommands = new Commands(
            userID,
            Input.GetKeyDown(KeyCode.UpArrow),
            Input.GetKeyDown(KeyCode.DownArrow),
            Input.GetKeyDown(KeyCode.RightArrow),
            Input.GetKeyDown(KeyCode.LeftArrow),
            Input.GetKeyDown(KeyCode.Space)
        );
        
        if (currentCommands.hasCommand())
        {
            commands.Add(currentCommands);
            //serialize
            var packet = Packet.Obtain();
            CubeEntity.ClientSerializeInput(commands, packet.buffer);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), sendPort);
            sendChannel.Send(packet, remoteEp);

            packet.Free();
        }
    }
    
    private void Interpolate(Snapshot prevSnapshot, Snapshot nextSnapshot, float t)
    {
        //Debug.Log(prevSnapshot + " " + nextSnapshot);
        var position = new Vector3();
        var rotation = new Quaternion();

        position.x = InterpolateAxis(prevSnapshot.Position.x, nextSnapshot.Position.x, t);
        position.y = InterpolateAxis(prevSnapshot.Position.y, nextSnapshot.Position.y, t);
        position.z = InterpolateAxis(prevSnapshot.Position.z, nextSnapshot.Position.z, t);
    
        rotation.w = InterpolateAxis(prevSnapshot.Rotation.w, nextSnapshot.Rotation.w, t);
        rotation.x = InterpolateAxis(prevSnapshot.Rotation.x, nextSnapshot.Rotation.x, t);
        rotation.y = InterpolateAxis(prevSnapshot.Rotation.y, nextSnapshot.Rotation.y, t);
        rotation.z = InterpolateAxis(prevSnapshot.Rotation.z, nextSnapshot.Rotation.z, t);
    
        transform.position = position;
        transform.rotation = rotation;
    }

    private float InterpolateAxis(float currentSnapValue, float nextSnapValue, float t)
    {
        return currentSnapValue + (nextSnapValue - currentSnapValue) * t;
    }
    
    private void OnDestroy() {
        sendChannel.Disconnect();
        recvChannel.Disconnect();
    }
}
