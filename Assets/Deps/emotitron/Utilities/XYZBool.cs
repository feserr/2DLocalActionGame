using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct XYZBool
{
	public bool x, y, z;
	// Indexer
	public bool this[int index]
	{
		get
		{
			return (index == 0) ? x : (index == 1) ? y : z;
		}
		set
		{
			if (index == 0) x = value;
			else if (index == 1) y = value;
			else if (index == 2) z = value;
		}
	}
	public XYZBool (bool b) { x = b; y = b; z = b; }
	public XYZBool (bool _x, bool _y, bool _z) { x = _x; y = _y; z = _z; }
}

public static class XYZUtils
{
	public static float SqrMagnitude(this Vector3 v, XYZBool includexyz)
	{
		return 
		((includexyz.x) ? v.x * v.x : 0) +
		((includexyz.y) ? v.y * v.y : 0) +
		((includexyz.z) ? v.z * v.z : 0);
	}

	public static float Magnitude(this Vector3 v, XYZBool includexyz)
	{
		return Mathf.Sqrt(
		((includexyz.x) ? v.x * v.x : 0) +
		((includexyz.y) ? v.y * v.y : 0) +
		((includexyz.z) ? v.z * v.z : 0));

	}

	/// <summary>
	/// Set position applying ONLY the values indicated as included, non-included axis use the current axis of the gameobject.
	/// </summary>
	public static void SetPosition(this GameObject go, Vector3 pos, XYZBool includexyz, bool localPosition = false)
	{
		Vector3 newpos = new Vector3(
			(includexyz[0]) ? pos[0] : (localPosition) ? go.transform.localPosition[0] : go.transform.position[0],
			(includexyz[1]) ? pos[1] : (localPosition) ? go.transform.localPosition[1] : go.transform.position[1],
			(includexyz[2]) ? pos[2] : (localPosition) ? go.transform.localPosition[2] : go.transform.position[2]);

		if (!localPosition)
			go.transform.position = newpos;
		else
			go.transform.localPosition = newpos;
	}

	/// <summary>
	/// Lerp extension that only applies the lerp to axis indicated by the XYZ. Other axis return the same axis value as start.
	/// </summary>
	public static Vector3 Lerp(this GameObject go, Vector3 start, Vector3 end, XYZBool includexyz, float t, bool localPosition = false)
	{
		// the lerped position, not accounting for any axis that are not included
		Vector3 rawLerpedPos = Vector3.Lerp(start, end, t);

		// for non-included axis, use the current postion
		return new Vector3(
			(includexyz[0]) ? rawLerpedPos[0] : ((localPosition) ? go.transform.localPosition[0] : go.transform.position[0]),
			(includexyz[1]) ? rawLerpedPos[1] : ((localPosition) ? go.transform.localPosition[1] : go.transform.position[1]),
			(includexyz[2]) ? rawLerpedPos[2] : ((localPosition) ? go.transform.localPosition[2] : go.transform.position[2]));
	}

}
