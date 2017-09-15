using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class Server : MonoBehaviour {

	public int serverPort;
	public int clientPort;
	Channel channel;
	List<Player> players = new List<Player>();
	List<ServerMessage> outMessages = new List<ServerMessage>();

	void Start() {
		channel = new Channel(null, serverPort, clientPort);
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
				ClientMessage clientMessage = ReadClientMessage(bitBuffer, inPacket.fromEndPoint);
				if (clientMessage != null) {
					ProcessClientMessage(clientMessage);
				}
			}
		}

		if (outMessages.Count > 0) {
			Packet outPacket = new Packet ();
			outPacket.buffer.PutInt (outMessages.Count);
			for (int i = 0; i < outMessages.Count; i++) {
				ServerMessage serverMessage = outMessages [i];
				serverMessage.Save (outPacket.buffer);
			}
			outMessages.Clear ();

			outPacket.buffer.Flip ();
			for (int i = 0; i < players.Count; i++) {
				Player player = players [i];
				channel.Send (outPacket, player.endPoint);
			}
		}
	}

	ClientMessage ReadClientMessage(BitBuffer bitBuffer, IPEndPoint clientEndPoint) {
		ClientMessageType messageType = bitBuffer.GetEnum<ClientMessageType> ((int)ClientMessageType.TOTAL);
		ClientMessage clientMessage = null;
		switch (messageType) {
		case ClientMessageType.CONNECT_PLAYER:
			clientMessage = new ConnectPlayerMessage (clientEndPoint);
			break;
		case ClientMessageType.DISCONNECT_PLAYER:
			clientMessage = new DisconnectPlayerMessage ();
			break;
		case ClientMessageType.PLAYER_INPUT:
			clientMessage = new PlayerInputMessage ();
			break;
		default:
			Debug.LogError("Got a client message that cannot be understood");
			return null;
		}
		clientMessage.Load(bitBuffer);
		return clientMessage;
	}

	void ProcessClientMessage(ClientMessage clientMessage) {
		switch (clientMessage.Type) {
		case ClientMessageType.CONNECT_PLAYER:
			ProcessConnectPlayer(clientMessage as ConnectPlayerMessage);
			break;
		case ClientMessageType.PLAYER_INPUT:
			ProcessPlayerInput (clientMessage as PlayerInputMessage);
			break;
		}
	}

	public void ProcessConnectPlayer(ConnectPlayerMessage connectPlayerMessage) {
		int playerId = connectPlayerMessage.PlayerId;
		Player player = GetPlayerWithId (playerId);
		if (player != null) {
			DisconnectPlayer (player);
		}
		GameObject playerGO = new GameObject("Player " + playerId);
		player = playerGO.AddComponent<Player> ();
		player.endPoint = connectPlayerMessage.EndPoint;
		players.Add(player);

		PlayerConnectedMessage playerConnectedMessage = new PlayerConnectedMessage (playerId);
		outMessages.Add (playerConnectedMessage);
	}

	public void ProcessPlayerInput(PlayerInputMessage playerInputMessage) {
		if (playerInputMessage.Input.up) {
			Debug.Log ("up!");
		}
	}

	public void DisconnectPlayer(Player player) {
		Destroy(player.gameObject);
		players.Remove(player);
	}

	Player GetPlayerWithId(int playerId) {
		for (int i = 0; i < players.Count; i++) {
			if (players[i].id == playerId) {
				return players[i];
			}
		}
		return null;
	}
}
