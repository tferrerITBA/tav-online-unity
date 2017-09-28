using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData {

	List<PlayerData> playersData = new List<PlayerData>();

	public void Save(BitBuffer bitBuffer) {
		bitBuffer.PutInt (playersData.Count);
		for (int i = 0; i < playersData.Count; i++) {
			playersData [i].Save (bitBuffer); 
		}
	}

	public void Load(BitBuffer bitBuffer) {
		int playerCount = bitBuffer.GetInt ();
		for (int i = 0; i < playerCount; i++) {
			PlayerData playerData = new PlayerData ();
			playerData.Load (bitBuffer);
			playersData.Add (playerData);
		}
	}

	public List<PlayerData> Players {
		get {
			return playersData;
		}
	}
}
