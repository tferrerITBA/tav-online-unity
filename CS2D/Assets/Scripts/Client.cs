using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Client : MonoBehaviour {

	public int port;
	Channel channel;

	public int playerId;

	void Start() {
		channel = new Channel(null, port);
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
