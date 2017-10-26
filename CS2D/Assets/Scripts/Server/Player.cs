using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class Player : MonoBehaviour {

	private int id;
	PlayerInput input = new PlayerInput();
	public IPEndPoint endPoint;
	public float maxSpeed;
	public bool showVisualRepresentation;
	public GameObject visualRepresentationGO;

	public List<Message> outMessages = new List<Message> ();

	int lastReceivedReliableMessageId = -1;
	int lastReceivedSendInEveryFramePacketMessageId = -1;
	int reliableSendInEveryPacketMessageId = 0;
	int reliableMessageId = 0;

	private Transform ownTransform;

	public int Id {
		get {
			return id;
		}
		set {
			id = value;
		}
	}

	void Awake() {
		ownTransform = transform;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		visualRepresentationGO.SetActive (showVisualRepresentation);

		//update orientation
		int vertical = 0;
		int horizontal = 0;

		if (input.up) {
			vertical += 1;
		}
		if (input.down) {
			vertical -= 1;
		}
		if (input.left) {
			horizontal -= 1;
		}
		if (input.right) {
			horizontal += 1;
		}

		if (vertical > 0) {
			if (horizontal > 0) {
				ownTransform.forward = Vector3.up + Vector3.right;
			} else if (horizontal < 0) {
				ownTransform.forward = Vector3.up + Vector3.left;
			} else {
				ownTransform.forward = Vector3.up;
			}
		} else if (vertical < 0) {
			if (horizontal > 0) {
				ownTransform.forward = Vector3.down + Vector3.right;
			} else if (horizontal < 0) {
				ownTransform.forward = Vector3.down + Vector3.left;
			} else {
				ownTransform.forward = Vector3.down;
			}
		} else {
			if (horizontal > 0) {
				ownTransform.forward = Vector3.right;
			} else if (horizontal < 0) {
				ownTransform.forward = Vector3.left;
			}
		}

		//update position
		if (Mathf.Abs (vertical) + Mathf.Abs (horizontal) > 0) {
			ownTransform.position += (ownTransform.forward * maxSpeed * Time.deltaTime);
		}

	}		

	public PlayerInput Input {
		set {
			input = value;
		}
	}

	public PlayerData BuildPlayerData() {
		PlayerData playerData = new PlayerData ();
		playerData.PlayerId = id;
		playerData.Position = new Vector2 (ownTransform.position.x, ownTransform.position.y);
		return playerData;
	}

	public int GetNewReliableSendInEveryPacketMessageId() {
		return ++reliableSendInEveryPacketMessageId;
	}

	public int GetNewReliableMessageId() {
		return ++reliableMessageId;
	}

	public int ReliableSendInEveryPacketMessageId {
		get {
			return reliableSendInEveryPacketMessageId;
		}
	}

	public int ReliableMessageId {
		get {
			return reliableMessageId;
		}
	}

	public int LastReceivedReliableMessageId {
		get {
			return lastReceivedReliableMessageId;
		}
		set {
			lastReceivedReliableMessageId = value;
		}
	}

	public int LastReliableSendInEveryPacketMessageIdReceived {
		get {
			return lastReceivedSendInEveryFramePacketMessageId;
		}
		set {
			lastReceivedSendInEveryFramePacketMessageId = value;
		}
	}
}
