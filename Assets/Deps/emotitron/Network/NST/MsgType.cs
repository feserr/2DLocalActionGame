namespace emotitron.Network.NST
{
	public enum MsgType { Position = 4000, Low_Bits, No_Positn, Cust_Msg, SvrTPort, Teleport }

	public static class MsgTypeExtensions
	{
		public static bool IsPosType(this MsgType _msgtype)
		{
			return (_msgtype != MsgType.No_Positn);
		}

		public static bool IsPosKeyType(this MsgType _msgtype)
		{
			return (
				_msgtype == MsgType.Position ||
				_msgtype == MsgType.Cust_Msg ||
				_msgtype == MsgType.SvrTPort ||
				_msgtype == MsgType.Teleport
				);
		}

		public static bool IsEventType(this MsgType _msgtype)
		{
			return _msgtype == MsgType.Cust_Msg || _msgtype == MsgType.Teleport;
		}

		public static bool IsPosLowerType(this MsgType _msgtype)
		{
			return (_msgtype == MsgType.Low_Bits);
		}
	}
}
