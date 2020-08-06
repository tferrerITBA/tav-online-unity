using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{

    private Channel channel;

    [SerializeField] private Rigidbody cubeRigidBody;
    [SerializeField] private Transform clientCubeTransform;

    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000);
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

        //serialize
        var packet = Packet.Obtain();
        CubeEntity.Serialize(cubeRigidBody, packet.buffer);
        packet.buffer.Flush();

        string serverIP = "127.0.0.1";
        int port = 9000;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        channel.Send(packet, remoteEp);

        packet.Free();

        UpdateClient();
    }

    private void UpdateClient() {
        var packet = channel.GetPacket();

        if (packet == null) {
            return;
        }

        var buffer = packet.buffer;

        //deserialize
        CubeEntity.Deserialize(clientCubeTransform, buffer);
    }
}
