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
    private int seq = 0; // Next snapshot to send

    public int interpolationCount = 3;
    private float accumCli = 0;
    private int networkSeq = 0;
    private int displaySeq = 0; // Wait for buffer to fill before changing

    private SortedList<int, Snapshot> interpolationBuffer;

    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000);
        sendRate = 1f / pps;
        interpolationBuffer = new SortedList<int, Snapshot>();
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
        //Debug.Log("SERV " + accum + " " + sendRate);
        if (accum >= sendRate)
        {
            //serialize
            var packet = Packet.Obtain();
            CubeEntity.Serialize(cubeRigidBody, packet.buffer, seq);
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
            //Debug.Log("Packet Received: " + interpolationBuffer.Count);
            networkSeq++;
        }
        
        accumCli += Time.deltaTime;
        //Debug.Log(accumCli + " " + sendRate);
        //Debug.Log("CLI " + accumCli + " " + sendRate + " " + interpolationBuffer.Count);
        if (accumCli >= sendRate && networkSeq >= interpolationCount)
        {
            try
            {
                Snapshot snap = interpolationBuffer[displaySeq];
                //Debug.Log(snap);
                //Debug.Log(interpolationBuffer.Count + " " + seqCli + " " + snap.Seq + " " + snap.Position);
                //Debug.Log(accumCli);
                interpolationBuffer.RemoveAt(0);
                //Debug.Log("SUCC " + interpolationBuffer.Count);
                clientCubeTransform.position = snap.Position;
                clientCubeTransform.rotation = snap.Rotation;
                displaySeq++;
            }
            catch (KeyNotFoundException e)
            {
                // no lo tengo, interpolar
                //Debug.Log("CATCH");
                // Aumentar seq (no siempre)
            }
            
            accumCli -= sendRate;
            
        }
        else
        {
            //Debug.Log(accumCli);
            
        }
    }
}
