using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public enum ServerMessageType {
	PLAYER_CONNECTED,
	PLAYER_DISCONNECTED,
	SNAPSHOT,
	TOTAL
}

public enum ClientMessageType {
	CONNECT_PLAYER,
	DISCONNECT_PLAYER,
	PLAYER_INPUT,
	TOTAL
}

public class ServerMessage {
	ServerMessageType messageType;
	float delayToSend;

	public ServerMessage(ServerMessageType messageType) {
		this.messageType = messageType;
	}

	public virtual void Load(BitBuffer bitBuffer) {
	}

	public virtual void Save(BitBuffer bitBuffer) {
		bitBuffer.PutEnum(messageType, (int)ServerMessageType.TOTAL);
	}

	public void Update(float dt) {
		delayToSend -= dt;
	}		

	public ServerMessageType Type {
		get {
			return messageType;
		}
	}

	public bool NeedsToBeSent {
		get {
			return delayToSend <= 0;
		}
	}

	public float DelayToSend {
		set {
			delayToSend = value;
		}
	}
}

public class ClientMessage {
	ClientMessageType messageType;

	public ClientMessage(ClientMessageType messageType) {
		this.messageType = messageType;
	}

	public virtual void Load(BitBuffer bitBuffer) {
	}

	public virtual void Save(BitBuffer bitBuffer) {
		bitBuffer.PutEnum(messageType, (int)ClientMessageType.TOTAL);
	}

	public ClientMessageType Type {
		get {
			return messageType;
		}
	}		
}

//from client to server

public class ConnectPlayerMessage : ClientMessage {
	int playerId;
	IPEndPoint endPoint;

	public ConnectPlayerMessage(IPEndPoint endPoint) : base(ClientMessageType.CONNECT_PLAYER) {
		this.endPoint = endPoint;
	}

	public ConnectPlayerMessage(int playerId) : base(ClientMessageType.CONNECT_PLAYER) {		
		this.playerId = playerId;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerId = bitBuffer.GetInt ();
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		bitBuffer.PutInt(playerId);
	}

	public int PlayerId {
		get {
			return playerId;
		}
	}

	public IPEndPoint EndPoint {
		get {
			return endPoint;
		}
	}
}
	
public class DisconnectPlayerMessage : ClientMessage {
	int playerId;

	public DisconnectPlayerMessage() : base(ClientMessageType.DISCONNECT_PLAYER) {		
	}

	public DisconnectPlayerMessage(int playerId) : base(ClientMessageType.DISCONNECT_PLAYER) {		
		this.playerId = playerId;
	}
		
	public int PlayerId {
		get {
			return playerId;
		}
	}
}

public class PlayerInputMessage : ClientMessage {
	PlayerInput playerInput;
	int playerId;

	public PlayerInputMessage(int playerId) : base(ClientMessageType.PLAYER_INPUT) {
		this.playerInput = new PlayerInput ();
		this.playerId = playerId;			
	}

	public PlayerInputMessage(PlayerInput playerInput) : base(ClientMessageType.PLAYER_INPUT) {
		this.playerInput = playerInput;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerInput.Load (bitBuffer);
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		playerInput.Save (bitBuffer);
	}

	public PlayerInput Input {
		get {
			return playerInput;
		}
	}

	public int PlayerId {
		get {
			return playerId;
		}
	}
}

//from server to client

public class PlayerConnectedMessage : ServerMessage {
	int playerId;

	public PlayerConnectedMessage() : base(ServerMessageType.PLAYER_CONNECTED) {
	}

	public PlayerConnectedMessage(int playerId) : base(ServerMessageType.PLAYER_CONNECTED) {
		this.playerId = playerId;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerId = bitBuffer.GetInt ();
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		bitBuffer.PutInt(playerId);
	}

	public int PlayerId {
		get {
			return playerId;
		}
	}
}

public class PlayerDisconnectedMessage : ServerMessage {
	int playerId;

	public PlayerDisconnectedMessage() : base(ServerMessageType.PLAYER_DISCONNECTED) {
	}

	public PlayerDisconnectedMessage(int playerId) : base(ServerMessageType.PLAYER_DISCONNECTED) {
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerId = bitBuffer.GetInt ();
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		bitBuffer.PutInt(playerId);
	}

	public int PlayerId {
		get {
			return playerId;
		}
	}
}

public class SnapshotMessage : ServerMessage {
	GameData gameData;

	public SnapshotMessage() : base(ServerMessageType.SNAPSHOT) {
		gameData = new GameData ();
	}

	public SnapshotMessage(GameData gameData) : base(ServerMessageType.SNAPSHOT) {
		this.gameData = gameData;
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		gameData.Load (bitBuffer);
	}

	public override void Save(BitBuffer bitBuffer) {
		base.Save(bitBuffer);
		gameData.Save (bitBuffer);
	}

	public GameData GameSnapshot {
		get {
			return gameData;
		}
	}
}
