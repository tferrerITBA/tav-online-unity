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
    public bool instantiateCubesPending;
    public int[] playersToInstantiate;

    private List<Snapshot> interpolationBuffer = new List<Snapshot>();
    private List<Commands> commands = new List<Commands>();
    private Dictionary<int, GameObject> cubes = new Dictionary<int, GameObject>();

    public GameObject cubePrefab;

    public int clientColor;
    
    public int interpolationCount = 2;

    public void Initialize(int sendPort, int recvPort, int userID)
    {
        this.sendPort = sendPort;
        this.sendChannel = new Channel(sendPort);
        this.recvPort = recvPort;
        this.recvChannel = new Channel(recvPort);
        this.userID = userID;
        clientColor = userID % 255;
    }

    private void Update()
    {
        var packet = recvChannel.GetPacket();

        if (packet != null) {
            var buffer = packet.buffer;

            int[] playerJoined = {-1, -1};
            //deserialize
            CubeEntity.ClientDeserialize(interpolationBuffer, playerJoined, buffer, displaySeq, commands);
            //networkSeq++;
            
            if (playerJoined[0] != -1)
            {
                instantiateCubesPending = true;
                playersToInstantiate = playerJoined;
            }
        }

        ReadClientInput();

        if (interpolationBuffer.Count >= interpolationCount)
            isPlaying = true;
        else if (interpolationBuffer.Count <= 1)
            isPlaying = false;
        
        if (isPlaying)
        {
            //accumCli += Time.deltaTime;
            time += Time.deltaTime;

            if (instantiateCubesPending)
            {
                InstantiateCubes(playersToInstantiate);
                instantiateCubesPending = false;
            }

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
            
            Debug.Log($"Marcha commands {currentCommands} a puerto {sendPort}");

            packet.Free();
        }
    }

    private void InstantiateCubes(int[] playerJoined)
    {
        Debug.Log($"Instanciando cubos {cubes.Count}");
        if (cubes.Count == 0) // this client is the player who just joined
        {
            Debug.Log($"Iniciando {interpolationBuffer[0].UserStates.Count} cubos");
            foreach (var userStatePair in interpolationBuffer[0].UserStates)
            {
                var player = Instantiate(cubePrefab, transform);
                Renderer rndr = player.GetComponent<Renderer>();
                rndr.material.color = new Color(clientColor, clientColor, clientColor);
                // player.GetComponent<>()
                cubes.Add(userStatePair.Key, player);
            }
        }
        else // just instantiate the new player
        {
            var newPlayer = Instantiate(cubePrefab, transform);
            var rndr = newPlayer.GetComponent<Renderer>();
            rndr.material.color = new Color(clientColor, clientColor, clientColor);
            cubes.Add(playerJoined[0], newPlayer);
        }
    }
    
    private void Interpolate(Snapshot prevSnapshot, Snapshot nextSnapshot, float t)
    {
        //Debug.Log(prevSnapshot + " " + nextSnapshot);
        foreach (var userCubePair in cubes)
        {
            var position = new Vector3();
            var rotation = new Quaternion();
            UserState prevUserState = prevSnapshot.UserStates[userCubePair.Key];
            UserState nextUserState = nextSnapshot.UserStates[userCubePair.Key];
            
            position.x = InterpolateAxis(prevUserState.Position.x, nextUserState.Position.x, t);
            position.y = InterpolateAxis(prevUserState.Position.y, nextUserState.Position.y, t);
            position.z = InterpolateAxis(prevUserState.Position.z, nextUserState.Position.z, t);
    
            rotation.w = InterpolateAxis(prevUserState.Rotation.w, nextUserState.Rotation.w, t);
            rotation.x = InterpolateAxis(prevUserState.Rotation.x, nextUserState.Rotation.x, t);
            rotation.y = InterpolateAxis(prevUserState.Rotation.y, nextUserState.Rotation.y, t);
            rotation.z = InterpolateAxis(prevUserState.Rotation.z, nextUserState.Rotation.z, t);
    
            transform.position = position;
            transform.rotation = rotation;
        }
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
