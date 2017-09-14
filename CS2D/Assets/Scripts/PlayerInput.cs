using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput {

	bool move; //1 bit
	Vector2 lookDir; //si moving esta en true mandar 2 floats
	public bool shoot;

	void Save(BitBuffer buffer) {
		//TODO: para el tp
	}

	void Load(BitBuffer buffer) {
		move = buffer.GetBit ();
		if (move) {
			lookDir = new Vector2(buffer.GetFloat(), buffer.GetFloat());
		}
		shoot = buffer.GetBit ();
	}
}
