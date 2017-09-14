using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Client : MonoBehaviour {

	public int serverPort;
	public int clientPort;
	Channel channel;

	public int playerId;

	void Start() {
		channel = new Channel("127.0.0.1", clientPort, serverPort);
	}

	void OnDestroy() {
		channel.Disconnect();
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Space)) {
			//send player connect message
			//channel.Send();
		}
	}
}
