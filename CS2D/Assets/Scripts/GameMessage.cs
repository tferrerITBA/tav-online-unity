using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public ServerMessage(ServerMessageType messageType) {
		this.messageType = messageType;
	}
}

public class ClientMessage {
	ClientMessageType messageType;

	public ClientMessage(ClientMessageType messageType) {
		this.messageType = messageType;
	}

	public virtual void Load(BitBuffer bitBuffer) {
	}
}

//from client to server

public class ConnectPlayerMessage : ClientMessage {
	int playerId;
	public ConnectPlayerMessage() : base(ClientMessageType.CONNECT_PLAYER) {		
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerId = bitBuffer.GetInt ();
	}
}
	
public class DisconnectPlayerMessage : ClientMessage {
	int playerId;
	public DisconnectPlayerMessage() : base(ClientMessageType.DISCONNECT_PLAYER) {		
	}

	public override void Load (BitBuffer bitBuffer) {
		base.Load (bitBuffer);
		playerId = bitBuffer.GetInt ();
	}
}

public class PlayerInputMessage : ClientMessage {
	public PlayerInputMessage() : base(ClientMessageType.PLAYER_INPUT) {		
	}
}

//from server to client

public class PlayerConnectedMessage : ServerMessage {
	public PlayerConnectedMessage() : base(ServerMessageType.PLAYER_CONNECTED) {
	}
}

public class PlayerDisconnectedMessage : ServerMessage {
	public PlayerDisconnectedMessage() : base(ServerMessageType.PLAYER_DISCONNECTED) {
	}
}

public class SnapshotMessage : ServerMessage {
	public SnapshotMessage() : base(ServerMessageType.SNAPSHOT) {
	}
}
