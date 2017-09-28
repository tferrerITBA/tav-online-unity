using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData {

	public Vector2 position = new Vector2();

	public void Save(BitBuffer bitBuffer) {
		bitBuffer.PutFloat (position.x);
		bitBuffer.PutFloat (position.y);
	}

	public void Load(BitBuffer bitBuffer) {
		float x = bitBuffer.GetFloat ();
		float y = bitBuffer.GetFloat ();
		position.Set (x, y);
	}
}
