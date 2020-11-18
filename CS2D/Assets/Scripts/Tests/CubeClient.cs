using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;
using Random = UnityEngine.Random;

public class CubeClient : MonoBehaviour
{
    public int sendPort;
    public int recvPort;
    public Channel sendChannel;
    public Channel recvChannel;

    public int userID;
    public int displaySeq;
    public float time;
    public bool isPlaying;
    private PlayerJoined playersToInstantiate = new PlayerJoined();
    private ShotBroadcast shotBroadcast = new ShotBroadcast();

    private readonly List<Snapshot> interpolationBuffer = new List<Snapshot>();
    private readonly CommandsList commands = new CommandsList();
    private readonly Dictionary<int, GameObject> cubes = new Dictionary<int, GameObject>();
    private readonly List<Shot> shots = new List<Shot>();
    private int shotSeq = 1;

    public GameObject cubePrefab;
    public GameObject playerCubePrefab;

    public CharacterController ownCube;
    public int health = 100;
    public float shotInterval = 1f;
    public float shotCooldown = 0.1f;
    public LayerMask shotsLayer;
    public float shotMaxDistance;
    private RaycastHit shotRaycastHit;

    public Color clientColor;
    public float speed = 5;
    
    public int interpolationCount = 2;

    private Commands currentCommands;

    public void Initialize(int sendPort, int recvPort, int userID, int cubesLayer)
    {
        this.sendPort = sendPort;
        this.sendChannel = new Channel(sendPort);
        this.recvPort = recvPort;
        this.recvChannel = new Channel(recvPort);
        this.userID = userID;
        gameObject.layer = cubesLayer;
        clientColor = new Color(Random.value, Random.value, Random.value);
        currentCommands = new Commands(userID);
    }

    private void Update()
    {
        var packet = recvChannel.GetPacket();

        while (packet != null)
        {
            var buffer = packet.buffer;
            var pt = Serializer.ClientDeserialize(interpolationBuffer, playersToInstantiate, buffer,
                displaySeq, commands, currentCommands.Seq, shots, shotSeq, shotBroadcast);
            if (pt == PacketType.UPDATE_MESSAGE && ownCube) // a Snapshot was just received
            {
                CorrectPosition(interpolationBuffer[interpolationBuffer.Count - 1].UserStates[userID]);
            }
            else if (pt == PacketType.SHOT_BROADCAST)
            {
                if (shotBroadcast.PlayerDied)
                {
                    cubes[shotBroadcast.PlayerShotID].SetActive(false);
                }
                else
                {
                    // An animation or an effect could be shown
                }
            }
            
            packet = recvChannel.GetPacket();
        }

        ReadClientInput();

        if (interpolationBuffer.Count >= interpolationCount)
            isPlaying = true;
        else if (interpolationBuffer.Count <= 1)
            isPlaying = false;
        
        if (isPlaying)
        {
            time += Time.deltaTime;

            if (playersToInstantiate.InstantiateCubesPending)
            {
                InstantiateCubes(playersToInstantiate);
                playersToInstantiate.InstantiateCubesPending = false;
                if (displaySeq < interpolationBuffer[0].Seq)
                {
                    displaySeq = interpolationBuffer[0].Seq;
                    time = interpolationBuffer[0].Time;
                }
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

    private void FixedUpdate()
    {
        if (!ownCube)
            return;
        // if (currentCommands.HasCommand())
        // {

        currentCommands.Rotation = ownCube.transform.rotation.eulerAngles.y;
        commands.Add(new Commands(currentCommands));
        MoveOwnCube(currentCommands);
        //serialize
        var packet = Packet.Obtain();
        Serializer.ClientSerializeInput(commands, packet.buffer);
        packet.buffer.Flush();

        string serverIP = "127.0.0.1";
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), sendPort);
        sendChannel.Send(packet, remoteEp);
        packet.Free();

        currentCommands.Seq++;
        // }
    }

    private void ReadClientInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentCommands.Up = true;
        } else if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            currentCommands.Up = false;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentCommands.Down = true;
        } else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            currentCommands.Down = false;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentCommands.Left = true;
        } else if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            currentCommands.Left = false;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentCommands.Right = true;
        } else if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            currentCommands.Right = false;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentCommands.Space = true;
        } else if (Input.GetKeyUp(KeyCode.Space))
        {
            currentCommands.Space = false;
        }

        if (Input.GetButton("Fire1") && shotCooldown >= shotInterval)
        {
            if (Physics.Raycast(transform.position, transform.forward, out shotRaycastHit, shotMaxDistance,
                shotsLayer))
            {
                Debug.DrawLine(transform.position, shotRaycastHit.point);
                int otherPlayerId = Int32.Parse(shotRaycastHit.transform.name);
                shots.Add(new Shot(shotSeq, userID, otherPlayerId));
                
                var packet = Packet.Obtain();
                Serializer.ClientSerializeShot(shots, packet.buffer);
                packet.buffer.Flush();

                string serverIP = "127.0.0.1";
                var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), sendPort);
                sendChannel.Send(packet, remoteEp);
                packet.Free();
            }
            
            shotCooldown = 0;
        }

        if (shotCooldown < shotInterval)
            shotCooldown += Time.deltaTime;
    }

    private void MoveOwnCube(Commands commandsToApply)
    {
        if (!ownCube.isGrounded)
        {
            // Vector3 vel = new Vector3(0, gravity * Time.deltaTime, 0);
            // cube.Move(vel * Time.deltaTime);
            ownCube.SimpleMove(Vector3.zero);
        }
        Vector3 move = new Vector3();
        move.x += commandsToApply.GetXDirection() * Time.fixedDeltaTime;
        move.z += commandsToApply.GetZDirection() * Time.fixedDeltaTime;

        move = ownCube.transform.TransformDirection(move);
        ownCube.Move(move);
    }
    
    private void CorrectPosition(UserState userState)
    {
        ownCube.transform.position = userState.Position;
        foreach (var cmd in commands.GetSnapshotUnackedCommands())
        {
            if (!ownCube.isGrounded)
                ownCube.SimpleMove(Vector3.zero);
            
            Vector3 move = new Vector3(
                cmd.GetXDirection() * Time.fixedDeltaTime, 
                0,
                cmd.GetZDirection() * Time.fixedDeltaTime
            );

            ownCube.Move(move);
        }
    }

    private void InstantiateCubes(PlayerJoined playerJoined)
    {
        if (cubes.Count == 0) // this client is the player who just joined
        {
            foreach (var userStatePair in interpolationBuffer[0].UserStates)
            {
                GameObject player;
                if (userStatePair.Key == userID)
                {
                    player = Instantiate(playerCubePrefab, transform);
                    ownCube = player.GetComponent<CharacterController>();
                }
                else
                {
                    player = Instantiate(cubePrefab, transform);
                }

                player.name = userStatePair.Key.ToString();
                player.layer = gameObject.layer;
                Renderer rndr = player.GetComponent<Renderer>();
                rndr.material.color = clientColor;
                cubes.Add(userStatePair.Key, player);
                if (playerJoined.PlayerCount == 1)
                {
                    var cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
                    cam.SetParent(ownCube.transform);
                    cam.localPosition = new Vector3(0, 1, 0);
                    cam.localRotation = Quaternion.identity;
                    cam.GetComponent<MouseLook>().player = ownCube.transform;
                }
            }
        }
        else // just instantiate the new player
        {
            var newPlayer = Instantiate(cubePrefab, transform);
            newPlayer.layer = gameObject.layer;
            var rndr = newPlayer.GetComponent<Renderer>();
            rndr.material.color = clientColor;
            cubes.Add(playerJoined.UserID, newPlayer);
        }
    }
    
    private void Interpolate(Snapshot prevSnapshot, Snapshot nextSnapshot, float t)
    {
        foreach (var userCubePair in cubes)
        {
            if (!prevSnapshot.UserStates.ContainsKey(userCubePair.Key) // snapshot where player did not exist yet 
                    || userCubePair.Key == userID // no interpolation for client's own cube
                    || !userCubePair.Value.activeSelf) // player is dead and has not respawned
                continue;
            
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
    
            userCubePair.Value.transform.position = position;
            userCubePair.Value.transform.rotation = rotation;
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
