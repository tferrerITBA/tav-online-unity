using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class Server : MonoBehaviour {

	[Header("Connection")]
	public int serverPort;
	public int clientPort;
	public float fakeDelay;
	public float fakePacketLoss;
	public float snapshotSendRate;

	private Channel channel;
	private float lastSnapshotSentTime;

	[Header("Game")]
	public Object playerPrefab;
	List<Player> players = new List<Player>();

	void Start() {
		channel = new Channel(null, serverPort, clientPort);
		lastSnapshotSentTime = Time.realtimeSinceStartup;
	}

	void OnDestroy() {
		channel.Disconnect();
	}

	void Update() {
		Packet inPacket = channel.GetPacket ();
		if (inPacket != null) {
			//read it!
			BitBuffer bitBuffer = inPacket.buffer;
			int messageCount = bitBuffer.GetInt ();
			for (int i = 0; i < messageCount; i++) {
				//parse message
				Message clientMessage = ReadClientMessage(bitBuffer, inPacket.fromEndPoint);
				if (clientMessage != null) {
					ProcessClientMessage(clientMessage);
				}
			}
		}

		float currentTime = Time.realtimeSinceStartup;
		float timeToSendSnapshot = 1.0f / snapshotSendRate;
		if (currentTime - lastSnapshotSentTime >= timeToSendSnapshot) {
			SnapshotMessage snapshotMessage = new SnapshotMessage (-1, BuildGameData ());
			snapshotMessage.TimeToSend = fakeDelay;
			for (int i = 0; i < players.Count; i++) {
				players[i].outMessages.Add(snapshotMessage);
			}
			lastSnapshotSentTime = currentTime;
		}

		for (int playerIdx = 0; playerIdx < players.Count; playerIdx++) {
			Player player = players[playerIdx];
			List<Message> outMessages = player.outMessages;

			outMessages.Add(AckReliableSendEveryFrameMessage.CreateAckReliableSendEveryFrameMessageMessageToSend(player.LastReliableSendInEveryPacketMessageIdReceived));

			int messagesReadyToSend = 0;
			for (int i = 0; i < outMessages.Count; i++) {			
				Message serverMessage = outMessages [i];
				serverMessage.Update (Time.deltaTime);
				if (serverMessage.NeedsToBeSent) {
					messagesReadyToSend++;
				}
			}

			if (messagesReadyToSend > 0) {
				Packet outPacket = new Packet ();
				outPacket.buffer.PutInt (messagesReadyToSend);
				for (int i = 0; i < outMessages.Count; i++) {
					Message serverMessage = outMessages [i];
					if (serverMessage.NeedsToBeSent) {
						serverMessage.Save (outPacket.buffer);
						if (serverMessage.Reliability == ReliabilityType.UNRELIABLE) {
							outMessages.RemoveAt (i);
							i--;
						} else if (serverMessage.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET) {
							serverMessage.TimeToSend = 0;
						} else {
							serverMessage.TimeToSend = serverMessage.ReliableMaxTime;
						}
					}
				}

				outPacket.buffer.Flip ();
				bool shouldDropPacket = false;//Random.Range (0.0001f, 100.0f) < fakePacketLoss;
				if (!shouldDropPacket) {
					channel.Send (outPacket, player.endPoint);
				}
			}
		}
	}

	Message ReadClientMessage(BitBuffer bitBuffer, IPEndPoint clientEndPoint) {
		MessageType messageType = bitBuffer.GetEnum<MessageType> ((int)MessageType.TOTAL);
		Message clientMessage = null;
		switch (messageType) {
		case MessageType.CONNECT_PLAYER:
			clientMessage = ConnectPlayerMessage.CreateConnectPlayerMessageToReceive (clientEndPoint);
			break;
		case MessageType.DISCONNECT_PLAYER:							
			clientMessage = new DisconnectPlayerMessage ();
			break;
		case MessageType.PLAYER_INPUT:
			clientMessage = new PlayerInputMessage ();
			break;
		case MessageType.ACK_RELIABLE_MAX_WAIT_TIME:
			clientMessage = AckReliableMessage.CreateAckReliableMessageMessageToReceive ();			
			break;
		case MessageType.ACK_RELIABLE_SEND_EVERY_PACKET:
			clientMessage = AckReliableSendEveryFrameMessage.CreateAckReliableSendEveryFrameMessageMessageToReceive ();
			break;
		default:
			Debug.LogError("Got a client message that cannot be understood");
			return null;
		}
		clientMessage.From = clientEndPoint;
		clientMessage.Load(bitBuffer);

		Player player = GetPlayerWithEndPoint(clientEndPoint);
		if (player != null) {
			if (clientMessage.Reliability == ReliabilityType.RELIABLE_MAX_WAIT_TIME) {
				if (clientMessage.ReliabilityId == (player.LastReceivedReliableMessageId + 1)) {
					//accept it... valid message since its +1 since the last received
					//send to the sender that its reliable message has been received
					player.outMessages.Add (AckReliableMessage.CreateAckReliableMessageMessageToSend (clientMessage.ReliabilityId));
					player.LastReceivedReliableMessageId = clientMessage.ReliabilityId;
				} else {
					//we need to discard it... either its been already processed or out of order
					return null;
				}
				//send to the sender that its reliable message has been received
				player.outMessages.Add (AckReliableMessage.CreateAckReliableMessageMessageToSend (clientMessage.ReliabilityId));
			} else if (clientMessage.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET) {
				if (clientMessage.ReliabilityId > player.LastReliableSendInEveryPacketMessageIdReceived) {
					//set the last "reliable send in every packet" message id.. it will get sent to the client on the next packet
					player.LastReliableSendInEveryPacketMessageIdReceived = clientMessage.ReliabilityId;
				} else {
					//must discard.. got out of order message
					return null;
				}
			}
		} else {
			Debug.Log ("null player!");
		}
			
		return clientMessage;
	}

	void ProcessClientMessage(Message clientMessage) {
		switch (clientMessage.Type) {
		case MessageType.CONNECT_PLAYER:
			ProcessConnectPlayer(clientMessage as ConnectPlayerMessage);
			break;
		case MessageType.DISCONNECT_PLAYER:
			ProcessDisconnectPlayer(clientMessage as DisconnectPlayerMessage);
			break;
		case MessageType.PLAYER_INPUT:
			ProcessPlayerInput (clientMessage as PlayerInputMessage);
			break;
		case MessageType.ACK_RELIABLE_MAX_WAIT_TIME:			
			ProcessAckReliable (clientMessage as AckReliableMessage);
			break;
		case MessageType.ACK_RELIABLE_SEND_EVERY_PACKET:
			ProcessAckReliableSendEveryFrame (clientMessage as AckReliableSendEveryFrameMessage);
			break;
		}
	}

	public void ProcessConnectPlayer(ConnectPlayerMessage connectPlayerMessage) {
		int playerId = connectPlayerMessage.PlayerId;
		Player player = GetPlayerWithId (playerId);
		if (player != null) {
			DisconnectPlayer (player);
		}

		GameObject playerGO = Instantiate (playerPrefab) as GameObject;
		playerGO.name = "Player " + playerId; 
		player = playerGO.GetComponent<Player> ();
		player.endPoint = connectPlayerMessage.EndPoint;
		player.Id = playerId;
		players.Add(player);

		//send all players that a new player has connected
		for (int i = 0; i < players.Count; i++) {
			Player playerToSendTo = players [i];
			PlayerConnectedMessage playerConnectedMessage = PlayerConnectedMessage.CreatePlayerConnectedMessageToSend (playerToSendTo, playerId);
			playerToSendTo.outMessages.Add (playerConnectedMessage);
		}
	}	

	public void ProcessDisconnectPlayer(DisconnectPlayerMessage disconnectPlayerMessage) {
		Player player = GetPlayerWithEndPoint(disconnectPlayerMessage.From);
		if (player != null) {
			DisconnectPlayer (player);
			for (int i = 0; i < players.Count; i++) {
				Player playerToSendTo = players [i];
				PlayerDisconnectedMessage playerDisconnectedMessage = PlayerDisconnectedMessage.CreatePlayerDisconnectedMessageToSend (playerToSendTo, player.Id);
				playerToSendTo.outMessages.Add (playerDisconnectedMessage);
			}
		}					
	}

	public void ProcessPlayerInput(PlayerInputMessage playerInputMessage) {
		Player player = GetPlayerWithEndPoint(playerInputMessage.From);
		if (player != null) {
			player.Input = playerInputMessage.Input;
		}
	}

	public void ProcessAckReliable(AckReliableMessage ackReliableMessage) {
		Player player = GetPlayerWithEndPoint (ackReliableMessage.From);
		if (player != null) {
			for (int i = 0; i < player.outMessages.Count; i++) {
				Message message = player.outMessages [i];
				if (message.ReliabilityId == ackReliableMessage.MessageIdToAck) {
					player.outMessages.RemoveAt (i);
					break;
				}
			}
		}
	}

	public void ProcessAckReliableSendEveryFrame(AckReliableSendEveryFrameMessage ackReliableSendEveryFrameMessage) {
		Player player = GetPlayerWithEndPoint (ackReliableSendEveryFrameMessage.From);
		if (player != null) {
			for (int i = 0; i < player.outMessages.Count; i++) {
				Message message = player.outMessages [i];
				if (message.Reliability == ReliabilityType.RELIABLE_SEND_EVERY_PACKET &&
					message.ReliabilityId <= ackReliableSendEveryFrameMessage.MessageIdToAck) {
					player.outMessages.RemoveAt (i);
					i--;
				}
			}
		}
	}

	public void DisconnectPlayer(Player player) {
		Destroy(player.gameObject);
		players.Remove(player);
	}

	Player GetPlayerWithId(int playerId) {
		for (int i = 0; i < players.Count; i++) {
			if (players[i].Id == playerId) {
				return players[i];
			}
		}
		return null;
	}

	public Player GetPlayerWithEndPoint(IPEndPoint endPoint) {
		for (int i = 0; i < players.Count; i++) {
			if (players[i].endPoint.Equals(endPoint)) {
				return players[i];
			}
		}
		return null;
	}

	private GameData BuildGameData() {
		GameData gameData = new GameData ();
		gameData.Time = Time.realtimeSinceStartup;
		for (int i = 0; i < players.Count; i++) {
			gameData.Players.Add (players [i].BuildPlayerData ());
		}
		return gameData;
	}
}
