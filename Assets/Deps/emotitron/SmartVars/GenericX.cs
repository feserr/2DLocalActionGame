//Copyright 2017, Davin Carten, All rights reserved

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace emotitron.SmartVars
{
	public enum XType { Quaternion = 15, XYZ = 7, XY = 3, XZ = 5, YZ = 6, X = 1, Y = 2, Z = 4, NULL = 0 }

	/// <summary>
	/// A generic rotation struct that freely converts between quaternions, Eulers and partial Eulers (as indicated by the RotationType).
	/// </summary>
	public struct GenericX
	{
		public float x, y, z, w;
		public XType type;
		public static GenericX NULL = new GenericX(0, 0, 0, 1, XType.NULL);

		public bool IsX { get { return (((int)type & (int)XType.X) == 1); } }
		public bool IsY { get { return (((int)type & (int)XType.Y) == 1); } }
		public bool IsZ { get { return (((int)type & (int)XType.Z) == 1); } }

		// indexer

		public float this[int i]
		{
			get { return (i == 0) ? x : (i == 1) ? y : (i == 2) ? z : w; }
			set {
				if (i == 0) x = value;
				else
				if (i == 1) y = value;
				else
				if (i == 2) z = value;
				else
				w = value;
				}
		}

		#region Constructors

		public GenericX(Quaternion value, XType _type = XType.Quaternion)
		{
			type = _type;
			x = value.x;
			y = value.y;
			z = value.z;
			w = value.w;
		}

		public GenericX(Vector3 value, XType _type = XType.XYZ)
		{
			type = _type;
			x = value.x;
			y = value.y;
			z = value.z;
			w = 0;
		}

		public GenericX(Vector2 value, XType _type = XType.XY)
		{
			type = _type;

			x = (type == XType.XY || type == XType.XZ) ? value.x : 0;

			y = (type == XType.XY) ? value.y :
				(type == XType.YZ) ? value.x :
				0;

			z = (type == XType.XZ || type == XType.YZ) ? value.y :
				0;

			w = 0;
		}

		public GenericX(float _x, float _y, float _z, float _w, XType _type = XType.Quaternion)
		{
			type = _type;
			x = _x;
			y = _y;
			z = _z;
			w = _w;
		}

		public GenericX(float _x, float _y, float _z, XType _type = XType.XYZ)
		{
			type = _type;
			x = _x;
			y = _y;
			z = _z;
			w = 0;
		}


		#endregion

		public static implicit operator GenericX(Quaternion value)
		{
			return new GenericX(value);
		}

		public static implicit operator GenericX(Vector3 value)
		{
			return new GenericX(value);
		}

		public static implicit operator GenericX(Vector2 value)
		{
			return new GenericX(value);
		}

		public static implicit operator Quaternion(GenericX value)
		{
			if (value.type == XType.Quaternion)
				return new Quaternion(value.x, value.y, value.z, value.w); // value.quat;

			return Quaternion.Euler(value.x, value.y, value.z);
		}

		public static implicit operator Vector3(GenericX value)
		{
			// if this came in as a quat, return eulerangles (unliked the xyz values without w were intended)
			if (value.type == XType.Quaternion)
				return new Quaternion(value.x, value.y, value.z, value.w).eulerAngles;

			return new Vector3(value.x, value.y, value.z);
		}

		public static implicit operator Vector2(GenericX value)
		{
			if (value.type == XType.Quaternion)
				throw new InvalidCastException("Did you really want to cast a quaterion to a vector2");

			return	
				(value.type == XType.XZ) ? new Vector2(value.x, value.z) :
				(value.type == XType.YZ) ? new Vector2(value.y, value.z) :
										   new Vector2(value.x, value.y);
		}

		public static implicit operator float(GenericX value)
		{
			if (value.type == XType.Quaternion || value.type == XType.XYZ || value.type == XType.XY || value.type == XType.XZ || value.type == XType.YZ)
				throw new InvalidCastException("Did you really want to cast a multiple vectors to a single float?");

			return 
				(value.type == XType.X) ? value.x :
				(value.type == XType.Y) ? value.y :
													  value.z;
		}

		public void OverwriteRotation(ref GenericX targetRotation)
		{
			if (type == XType.Quaternion || type == XType.XYZ)
			{
				targetRotation = this;
			}
			else
			{
				Debug.Log(Time.time + "  OverwriteRotation " + type);
				targetRotation.x = (IsX) ? x : targetRotation.x;
				targetRotation.y = (IsY) ? y : targetRotation.y;
				targetRotation.z = (IsZ) ? z : targetRotation.z;
			}
		}
		/// <summary>
		/// Applies rotation using the correct localRotation/Rotation method, and only applies the axis indicated by the RotationType of the rotation. This allows you to apply an X rotation
		/// without changing the y and z values.
		/// </summary>
		/// <param name="targetTransform"></param>
		/// <param name="isLocalRotation"></param>
		public void ApplyRotation(Transform targetTransform, bool isLocalRotation = false)
		{
			if (type == XType.NULL)
			{
				DebugX.LogError("Attempt to apply a null rotation. Skipping.");
				return;
			}
			if (type == XType.Quaternion || type == XType.XYZ)
			{
				if (isLocalRotation)
				{
					targetTransform.localRotation = this;
				}
				else
				{
					targetTransform.rotation = this;
				}
			}
			else
			{
				Vector3 tempvec3 = new Vector3
					(
					(IsX) ? x : (isLocalRotation) ? targetTransform.localEulerAngles.x : targetTransform.eulerAngles.x,
					(IsY) ? y : (isLocalRotation) ? targetTransform.localEulerAngles.y : targetTransform.eulerAngles.y,
					(IsZ) ? z : (isLocalRotation) ? targetTransform.localEulerAngles.z : targetTransform.eulerAngles.z
					);
				if (isLocalRotation)
					targetTransform.eulerAngles = tempvec3;
				else
					targetTransform.localEulerAngles = tempvec3;

			}
		}

		public static void Copy(GenericX source, GenericX target)
		{
			target.x = source.x;
			target.y = source.y;
			target.z = source.z;
			target.w = source.w;
			target.type = source.type;
		}

		public static bool Compare(GenericX a, GenericX b)
		{
			return
				(a.type == b.type) &&
				(a.x == b.x) &&
				(a.y == b.y) &&
				(a.z == b.z) &&
				(a.type != XType.Quaternion || a.w == b.w);
		}

		public static GenericX operator +(GenericX a, GenericX b)
		{
			return new GenericX(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w, a.type);
		}

		public static GenericX operator -(GenericX a, GenericX b)
		{
			return new GenericX(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w, a.type);
		}
		public static GenericX operator *(GenericX a, float b)
		{
			return new GenericX(a.x * b, a.y * b, a.z * b, a.w * b, a.type);
		}
		//public static bool operator ==(GenericX a, GenericX b)
		//{
		//	Debug.LogError("GetRidOfThis");

		//	return
		//		(a.type == b.type) &&
		//		(a.x == b.x) &&
		//		(a.y == b.y) &&
		//		(a.z == b.z) &&
		//		(a.type != XType.Quaternion || a.w == b.w);
		//}

		//public override int GetHashCode()
		//{
		//	return base.GetHashCode();
		//}

		//public static bool operator !=(GenericX a, GenericX b)
		//{
		//	Debug.LogError("GetRidOfThis");
		//	return !(a == b);
		//}

		//public override bool Equals(object obj)
		//{
		//	Debug.LogError("GET RID OF THIS");
		//	GenericX b = (GenericX)obj;
		//	return
		//		(x == b.x) &&
		//		(y == b.y) &&
		//		(z == b.z) &&
		//		(type != XType.Quaternion || w == b.w);
		//}

		public override string ToString()
		{
			return (type.ToString() + " x:" + x + ", y:" + y + ", z:" + z + ", w:" + w);
		}
	}

	public static class GenericXExtensions
	{
		public static bool IsX(this XType type)
		{
			return (((int)type & (int)XType.X) != 0 && type != XType.Quaternion);
		}

		public static bool IsY(this XType type)
		{
			return (((int)type & (int)XType.Y) != 0 && type != XType.Quaternion);
		}

		public static bool IsZ(this XType type)
		{
			return (((int)type & (int)XType.Z) != 0 && type != XType.Quaternion);
		}
		/// <summary>
		/// Check if axis is used in this type.
		/// </summary>
		/// <param name="axis">0=x, y=1, z=2</param>
		public static bool IsXYZ(this XType type, int axis)
		{
			int axismask =
				(axis == 0) ? (int)XType.X :
				(axis == 1) ? (int)XType.Y :
				(axis == 2) ? (int)XType.Z :
				0;
			return (((int)type & axismask) != 0);
		}
	}
}

