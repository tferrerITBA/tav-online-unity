using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Tests;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class CubeClient : MonoBehaviour
{
    public IPEndPoint serverEndpoint;
    public Channel channel;

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
    private int cubesLayer;
    public int ownPlayerLayer;

    public CharacterController ownCube;
    public int health = 100;
    private TMP_Text healthText;
    private GameObject deathPanel;
    public float shotInterval = 0.1f;
    public float shotCooldown = 0.1f;
    public LayerMask shotsLayer;
    public float shotMaxDistance;
    private RaycastHit shotRaycastHit;
    private ParticleSystem muzzleFlash;
    private GameObject mainCamera;

    public Color clientColor;
    public float playerSpeed = 5;
    
    public int interpolationCount = 2;

    private Commands currentCommands;
    private int networkLatency;

    private void Start()
    {
        muzzleFlash = GameObject.FindWithTag("MuzzleFlash").GetComponent<ParticleSystem>();
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    public void Initialize(string srvIP, int srvPort, int userID, int cubesLayer, 
        Channel channel, GameObject deathPanel, TMP_Text healthText)
    {
        this.serverEndpoint = new IPEndPoint(IPAddress.Parse(srvIP), srvPort);
        this.channel = channel;
        this.userID = userID;
        this.cubesLayer = cubesLayer;
        SetLayer(gameObject, cubesLayer);
        shotsLayer = LayerMask.GetMask("Client 1"); // LayerMask.GetMask(LayerMask.LayerToName(cubesLayer));
        shotMaxDistance = 1000000f;
        clientColor = new Color(Random.value, Random.value, Random.value);
        currentCommands = new Commands(userID);
        this.deathPanel = deathPanel;
        TMP_Text deathText = deathPanel.transform.GetChild(0).GetComponent<TMP_Text>();
        deathPanel.SetActive(false);
        deathText.text = "You were killed";
        this.healthText = healthText;
        healthText.enabled = true;
    }

    private void Update()
    {
        if (health <= 0)
            return;
        
        var packet = channel.GetPacket();

        while (packet != null)
        {
            var buffer = packet.buffer;
            int playerDisconnect, commandSnapshotAck;
            var pt = Serializer.ClientDeserialize(interpolationBuffer, playersToInstantiate, buffer,
                displaySeq, commands, currentCommands.Seq, shots, shotSeq, shotBroadcast,
                out commandSnapshotAck, out playerDisconnect);
            if (pt == PacketType.PLAYER_JOINED)
            {
                AckPlayerJoined();
            }
            else if (pt == PacketType.PLAYER_DISCONNECT)
            {
                Destroy(cubes[playerDisconnect].gameObject);
                cubes.Remove(playerDisconnect);
                if (playerDisconnect == userID)
                {
                    deathPanel.SetActive(true);
                    Destroy(this);
                }
            }
            else if (pt == PacketType.UPDATE_MESSAGE && ownCube) // a Snapshot was just received
            {
                CorrectPosition(interpolationBuffer[interpolationBuffer.Count - 1].UserStates[userID], commandSnapshotAck);
            }
            else if (pt == PacketType.SHOT_BROADCAST)
            {
                bool playerDamaged = shotBroadcast.PlayerShotID == userID;
                if (playerDamaged)
                {
                    health -= ServerEntity.DamagePerShot;
                    healthText.text = health.ToString();
                }
                if (shotBroadcast.PlayerDied)
                {
                    if (playerDamaged)
                    {
                        deathPanel.SetActive(true);
                    }
                    cubes[shotBroadcast.PlayerShotID].SetActive(false);
                }
                else
                {
                    // An animation or an effect could be shown
                }

                AckShotBroadcast();
            }
            
            packet = channel.GetPacket();
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
            while (time >= nextTime) {
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
        if (!ownCube || health <= 0)
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
        
        var remoteEp = serverEndpoint;
        if (networkLatency == 0)
        {
            channel.Send(packet, remoteEp);
            packet.Free();
        }
        else
        {
            Task.Delay(networkLatency)
                .ContinueWith(t => channel.Send(packet, remoteEp))
                .ContinueWith(t => packet.Free());
        }

        currentCommands.Seq++;
        // }
    }

    private void ReadClientInput()
    {
        if (!ownCube || !ownCube.gameObject.activeSelf) // player not respawned yet
            return;
        
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

        SetLatency();

        shotCooldown += Time.deltaTime;
        if (/*Input.GetButton("Fire1")*/ Input.GetKeyDown(KeyCode.L) && shotCooldown >= shotInterval)
        {
            muzzleFlash.Play();
            var hit = Physics.Raycast(
                mainCamera.transform.position,
                mainCamera.transform.forward, out shotRaycastHit, shotMaxDistance,
                shotsLayer);
            if (hit)
            {
                Debug.DrawLine(ownCube.transform.position, shotRaycastHit.point, Color.red, 200);
                int otherPlayerId = Int32.Parse(shotRaycastHit.transform.name);
                
                Debug.Log($"Shooting player {otherPlayerId}, shot number {shotSeq}");
                shots.Add(new Shot(shotSeq, userID, otherPlayerId));
                
                var packet = Packet.Obtain();
                Serializer.ClientSerializeShot(shots, packet.buffer);
                packet.buffer.Flush();
                
                var remoteEp = serverEndpoint;
                if (networkLatency == 0)
                {
                    channel.Send(packet, remoteEp);
                    packet.Free();
                }
                else
                {
                    Task.Delay(networkLatency)
                        .ContinueWith(t => channel.Send(packet, remoteEp))
                        .ContinueWith(t => packet.Free());
                }

                shotSeq++;
            }
            shotCooldown = 0;
        }

    }

    private void MoveOwnCube(Commands commandsToApply)
    {
        if (!ownCube.gameObject.activeSelf) // player not respawned yet
            return;
        
        if (!ownCube.isGrounded)
        {
            ownCube.SimpleMove(Vector3.zero);
        }
        Vector3 move = new Vector3();
        move.x += commandsToApply.GetXDirection() * Time.fixedDeltaTime;
        move.z += commandsToApply.GetZDirection() * Time.fixedDeltaTime;

        move = ownCube.transform.TransformDirection(move) * playerSpeed;
        ownCube.Move(move);
    }
    
    private void CorrectPosition(UserState userState, int commandSnapshotAck)
    {
        if (!ownCube || !ownCube.gameObject.activeSelf) // player not respawned yet
            return;
        
        ownCube.transform.position = userState.Position;
        var rotation = ownCube.transform.rotation.eulerAngles.y;
        
        foreach (Commands cmd in commands.GetSnapshotUnackedCommands())
        {
            if (!ownCube.isGrounded)
                ownCube.SimpleMove(Vector3.zero);
            
            Vector3 move = new Vector3(
                cmd.GetXDirection() * Time.fixedDeltaTime, 
                0,
                cmd.GetZDirection() * Time.fixedDeltaTime
            );

            ownCube.transform.rotation = Quaternion.Euler(0, cmd.Rotation, 0);
            move = ownCube.transform.TransformDirection(move) * playerSpeed;
            ownCube.Move(move);
        }

        ownCube.transform.rotation = Quaternion.Euler(0, rotation, 0);
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
                // Renderer rndr = player.GetComponent<Renderer>();
                // rndr.material.color = clientColor;
                cubes.Add(userStatePair.Key, player);
                if (userID == userStatePair.Key)
                {
                    SetLayer(player, ownPlayerLayer);
                    var cam = mainCamera.transform;
                    cam.SetParent(ownCube.transform);
                    cam.localPosition = new Vector3(0, 2, 0);
                    cam.localRotation = Quaternion.identity;
                    cam.GetComponent<MouseLook>().player = ownCube.transform;
                }
                else
                {
                    SetLayer(player, cubesLayer);
                }
            }
        }
        else // just instantiate the new player
        {
            var newPlayer = Instantiate(cubePrefab, transform);
            SetLayer(newPlayer, cubesLayer);
            newPlayer.name = playerJoined.UserID.ToString();
            // var rndr = newPlayer.GetComponent<Renderer>();
            // rndr.material.color = clientColor;
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

    private static float InterpolateAxis(float currentSnapValue, float nextSnapValue, float t)
    {
        return currentSnapValue + (nextSnapValue - currentSnapValue) * t;
    }

    private void OnDestroy()
    {
        var packet = Packet.Obtain();
        Serializer.PlayerDisconnectSerialize(packet.buffer, userID);
        packet.buffer.Flush();
        
        channel.Send(packet, serverEndpoint);
        packet.Free();
        channel.Disconnect();
    }
    
    private void AckShotBroadcast()
    {
        var newPacket = Packet.Obtain();
        Serializer.ClientSerializeShotBroadcastAck(shotBroadcast, newPacket.buffer);
        newPacket.buffer.Flush();
        
        var remoteEp = serverEndpoint;
        channel.Send(newPacket, remoteEp);
        newPacket.Free();
    }

    private void AckPlayerJoined()
    {
        var packet = Packet.Obtain();
        Serializer.PlayerJoinedAck(packet.buffer, playersToInstantiate.UserID);
        packet.buffer.Flush();
        
        var remoteEp = serverEndpoint;
        channel.Send(packet, remoteEp);
        packet.Free();
    }

    private static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            var childGO = child.gameObject;
            childGO.layer = layer;
            SetLayer(childGO, layer);
        }
    }

    private void SetLatency()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            networkLatency = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            networkLatency = 100; // ms
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            networkLatency = 200; // ms
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            networkLatency = 300; // ms
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            networkLatency = 400; // ms
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            networkLatency = 500; // ms
        }
    }
}
