using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{

    private Channel channel;

    [SerializeField] private Rigidbody cubeRigidBody;
    [SerializeField] private Transform clientCubeTransform;
    
    public int pps = 10;
    private float sendRate;
    
    private float accum = 0;
    private float serverTime = 0;
    private int seq = 0; // Next snapshot to send

    private bool clientPlaying = false;
    public int interpolationCount = 3;
    private float accumCli = 0;
    private int networkSeq = 0;
    private int displaySeq = 0; // Wait for buffer to fill before changing
    private Snapshot currentSnapshot;
    private float clientTime = 0;

    private List<Snapshot> interpolationBuffer;

    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000);
        sendRate = 1f / pps;
        interpolationBuffer = new List<Snapshot>();
    }

    private void OnDestroy() {
        channel.Disconnect();
    }

    // Update is called once per frame
    void Update() {
        //apply input
        if (Input.GetKeyDown(KeyCode.Space)) {
            cubeRigidBody.AddForceAtPosition(Vector3.up * 5, Vector3.zero, ForceMode.Impulse);
        }

        UpdateClient();

        accum += Time.deltaTime;
        serverTime += Time.deltaTime;
        
        if (accum >= sendRate)
        {
            //serialize
            var packet = Packet.Obtain();
            CubeEntity.Serialize(cubeRigidBody, packet.buffer, seq, serverTime);
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
            CubeEntity.Deserialize(interpolationBuffer, buffer, displaySeq);
            networkSeq++;
        }

        if (interpolationBuffer.Count >= interpolationCount)
            clientPlaying = true;
        else if (interpolationBuffer.Count <= 1)
            clientPlaying = false;
        
        if (clientPlaying)
        {
            accumCli += Time.deltaTime;
            clientTime += Time.deltaTime;
            var previousTime = interpolationBuffer[0].Time;
            var nextTime = interpolationBuffer[1].Time;
            if (clientTime >= nextTime) {
                interpolationBuffer.RemoveAt(0);
                previousTime = interpolationBuffer[0].Time;
                nextTime =  interpolationBuffer[1].Time;
                displaySeq++;
                //accumCli -= sendRate;
            }
            var t =  (clientTime - previousTime) / (nextTime - previousTime);
            Interpolate(interpolationBuffer[0], interpolationBuffer[1], t);
        }
        //else
        //{
            //Interpolate(interpolationBuffer[0], interpolationBuffer[1], t);
        //}
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
