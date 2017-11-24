using UnityEngine;

public struct CubeState {
	public int moveNum;
	public Vector2 position;
	public int timestamp;

	static public CubeState CreateStartingState () {
		return new CubeState {
			moveNum = 0,
			position = Vector2.zero,
			timestamp = 0
		};
	}

	static public CubeState Interpolate (CubeState from, CubeState to, int clientTick) {
		float t = ((float)(clientTick - from.timestamp)) / (to.timestamp - from.timestamp);
		return new CubeState {
			moveNum = 0,
			position = Vector2.Lerp (from.position, to.position, t),
			timestamp = 0
		};
	}

	static public CubeState Move (CubeState previous, Vector2 input, int timestamp) {
		return new CubeState {
			moveNum = 1 + previous.moveNum,
			position = 0.125f * Vector2.ClampMagnitude (input, 1f) + previous.position,
			timestamp = timestamp
		};
	}
}
