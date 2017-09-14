using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour {

	public int serverPort;
	public int clientPort;
	Channel channel;
	List<Player> players = new List<Player>();

	void Start() {
		channel = new Channel(null, serverPort, clientPort);
	}

	void OnDestroy() {
		channel.Disconnect();
	}

	void Update() {
		Packet packet = channel.GetPacket ();
		if (packet != null) {
			//read it!
			BitBuffer bitBuffer = packet.buffer;
			int messageCount = bitBuffer.GetInt ();
			for (int i = 0; i < messageCount; i++) {
				//parse message
				ClientMessage clientMessage = ReadClientMessage(bitBuffer);
				if (clientMessage != null) {
					ProcessClientMessage(clientMessage);
				}
			}
		}
	}

	ClientMessage ReadClientMessage(BitBuffer bitBuffer) {
		ClientMessageType messageType = bitBuffer.GetEnum<ClientMessageType> ((int)ClientMessageType.TOTAL);
		ClientMessage clientMessage = null;
		switch (messageType) {
		case ClientMessageType.CONNECT_PLAYER:
			clientMessage = new ConnectPlayerMessage ();
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
		players.Add(player);
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
