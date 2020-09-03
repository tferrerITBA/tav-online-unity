using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Tests;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class SimulationTest : MonoBehaviour
{

    private Channel channel;
    private Channel clientChannel;

    [SerializeField] private Rigidbody cubeRigidBody;
    [SerializeField] private Transform clientCubeTransform;
    
    public int pps = 10;
    private float sendRate;
    
    private float accum = 0;
    private float serverTime = 0;
    private int seq = 0; // Next snapshot to send

    private bool clientPlaying = false;
    public int interpolationCount = 3;
    private int displaySeq = 0; // Wait for buffer to fill before changing
    private Snapshot currentSnapshot;
    private float clientTime = 0;

    private List<Snapshot> interpolationBuffer;

    private List<Commands> clientCommands = new List<Commands>();

    private bool connected = true;

    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000);
        clientChannel = new Channel(9001);
        sendRate = 1f / pps;
        interpolationBuffer = new List<Snapshot>();
    }

    private void OnDestroy() {
        channel.Disconnect();
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.D))
        {
            connected = !connected;
        }

        accum += Time.deltaTime;

        if (connected)
        {
            UpdateServer();
        }


        UpdateClient();
    }

    private void UpdateServer()
    {
        serverTime += Time.deltaTime;
        
        var commandPacket = clientChannel.GetPacket();
        if (commandPacket != null) {
            var buffer = commandPacket.buffer;

            List<Commands> commandsList = CubeEntity.ServerDeserializeInput(buffer);
            var packet = Packet.Obtain();
            int receivedCommandSequence = -1;
            foreach (Commands commands in commandsList)
            {
                receivedCommandSequence = commands.Seq;
                ExecuteClientInput(commands);
            }
            CubeEntity.ServerSerializeAck(packet.buffer, receivedCommandSequence);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);

            packet.Free();
        }

        if (accum >= sendRate)
        {
            //serialize
            var packet = Packet.Obtain();
            CubeEntity.ServerWorldSerialize(cubeRigidBody, packet.buffer, seq, serverTime);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);

            packet.Free();

            accum -= sendRate;
            seq++;
        }
    }

    private void UpdateClient() {
        var packet = channel.GetPacket();

        if (packet != null) {
            var buffer = packet.buffer;

            //deserialize
            CubeEntity.ClientDeserialize(interpolationBuffer, buffer, displaySeq, clientCommands);
            //networkSeq++;
        }

        ReadClientInput();

        if (interpolationBuffer.Count >= interpolationCount)
            clientPlaying = true;
        else if (interpolationBuffer.Count <= 1)
            clientPlaying = false;
        
        if (clientPlaying)
        {
            //accumCli += Time.deltaTime;
            clientTime += Time.deltaTime;
            var previousTime = interpolationBuffer[0].Time;
            var nextTime = interpolationBuffer[1].Time;
            if (clientTime >= nextTime) {
                interpolationBuffer.RemoveAt(0);
                displaySeq++;
                if (interpolationBuffer.Count < 2)
                {
                    clientPlaying = false;
                    return;
                }
                previousTime = interpolationBuffer[0].Time;
                nextTime =  interpolationBuffer[1].Time;
            }
            var t =  (clientTime - previousTime) / (nextTime - previousTime);
            Interpolate(interpolationBuffer[0], interpolationBuffer[1], t);
        }
    }

    private void ExecuteClientInput(Commands commands)
    {
        //apply input
        if (commands.Space) {
            cubeRigidBody.AddForceAtPosition(Vector3.up * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Left) {
            cubeRigidBody.AddForceAtPosition(Vector3.left * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Right) {
            cubeRigidBody.AddForceAtPosition(Vector3.right * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Up) {
            cubeRigidBody.AddForceAtPosition(Vector3.forward * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (commands.Down) {
            cubeRigidBody.AddForceAtPosition(Vector3.back * 5, Vector3.zero, ForceMode.Impulse);
        }
    }

    private void ReadClientInput()
    {
        Commands currentCommands = new Commands(
            Input.GetKeyDown(KeyCode.UpArrow),
            Input.GetKeyDown(KeyCode.DownArrow),
            Input.GetKeyDown(KeyCode.RightArrow),
            Input.GetKeyDown(KeyCode.LeftArrow),
            Input.GetKeyDown(KeyCode.Space)
            );
        
        if (currentCommands.hasCommand())
        {
            clientCommands.Add(currentCommands);
            //serialize
            var packet = Packet.Obtain();
            CubeEntity.ClientSerializeInput(clientCommands, packet.buffer);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9001;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);

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
        
        clientCubeTransform.position = position;
        clientCubeTransform.rotation = rotation;
    }

    private float InterpolateAxis(float currentSnapValue, float nextSnapValue, float t)
    {
        return currentSnapValue + (nextSnapValue - currentSnapValue) * t;
    }
}
