// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.AntiCheatCommon
{
	public struct LogEventParamPairParamValue
	{
		private AntiCheatCommonEventParamType m_ParamValueType;
		private System.IntPtr? m_ClientHandle;
		private Utf8String m_String;
		private uint? m_UInt32;
		private int? m_Int32;
		private ulong? m_UInt64;
		private long? m_Int64;
		private Vec3f m_Vec3f;
		private Quat m_Quat;

		/// <summary>
		/// Parameter type
		/// </summary>
		public AntiCheatCommonEventParamType ParamValueType
		{
			get
			{
				return m_ParamValueType;
			}

			private set
			{
				m_ParamValueType = value;
			}
		}

		/// <summary>
		/// Parameter value
		/// </summary>
		public System.IntPtr? ClientHandle
		{
			get
			{
				System.IntPtr? value;
				Helper.Get(m_ClientHandle, out value, m_ParamValueType, AntiCheatCommonEventParamType.ClientHandle);
				return value;
			}

			set
			{
				Helper.Set<System.IntPtr?, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_ClientHandle, AntiCheatCommonEventParamType.ClientHandle, ref m_ParamValueType);
			}
		}

		public Utf8String String
		{
			get
			{
				Utf8String value;
				Helper.Get(m_String, out value, m_ParamValueType, AntiCheatCommonEventParamType.String);
				return value;
			}

			set
			{
				Helper.Set<Utf8String, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_String, AntiCheatCommonEventParamType.String, ref m_ParamValueType);
			}
		}

		public uint? UInt32
		{
			get
			{
				uint? value;
				Helper.Get(m_UInt32, out value, m_ParamValueType, AntiCheatCommonEventParamType.UInt32);
				return value;
			}

			set
			{
				Helper.Set<uint?, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_UInt32, AntiCheatCommonEventParamType.UInt32, ref m_ParamValueType);
			}
		}

		public int? Int32
		{
			get
			{
				int? value;
				Helper.Get(m_Int32, out value, m_ParamValueType, AntiCheatCommonEventParamType.Int32);
				return value;
			}

			set
			{
				Helper.Set<int?, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_Int32, AntiCheatCommonEventParamType.Int32, ref m_ParamValueType);
			}
		}

		public ulong? UInt64
		{
			get
			{
				ulong? value;
				Helper.Get(m_UInt64, out value, m_ParamValueType, AntiCheatCommonEventParamType.UInt64);
				return value;
			}

			set
			{
				Helper.Set<ulong?, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_UInt64, AntiCheatCommonEventParamType.UInt64, ref m_ParamValueType);
			}
		}

		public long? Int64
		{
			get
			{
				long? value;
				Helper.Get(m_Int64, out value, m_ParamValueType, AntiCheatCommonEventParamType.Int64);
				return value;
			}

			set
			{
				Helper.Set<long?, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_Int64, AntiCheatCommonEventParamType.Int64, ref m_ParamValueType);
			}
		}

		public Vec3f Vec3f
		{
			get
			{
				Vec3f value;
				Helper.Get(m_Vec3f, out value, m_ParamValueType, AntiCheatCommonEventParamType.Vector3f);
				return value;
			}

			set
			{
				Helper.Set<Vec3f, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_Vec3f, AntiCheatCommonEventParamType.Vector3f, ref m_ParamValueType);
			}
		}

		public Quat Quat
		{
			get
			{
				Quat value;
				Helper.Get(m_Quat, out value, m_ParamValueType, AntiCheatCommonEventParamType.Quat);
				return value;
			}

			set
			{
				Helper.Set<Quat, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_Quat, AntiCheatCommonEventParamType.Quat, ref m_ParamValueType);
			}
		}

		public static implicit operator LogEventParamPairParamValue(System.IntPtr value)
		{
			return new LogEventParamPairParamValue() { ClientHandle = value };
		}

		public static implicit operator LogEventParamPairParamValue(Utf8String value)
		{
			return new LogEventParamPairParamValue() { String = value };
		}

		public static implicit operator LogEventParamPairParamValue(string value)
		{
			return new LogEventParamPairParamValue() { String = value };
		}

		public static implicit operator LogEventParamPairParamValue(uint value)
		{
			return new LogEventParamPairParamValue() { UInt32 = value };
		}

		public static implicit operator LogEventParamPairParamValue(int value)
		{
			return new LogEventParamPairParamValue() { Int32 = value };
		}

		public static implicit operator LogEventParamPairParamValue(ulong value)
		{
			return new LogEventParamPairParamValue() { UInt64 = value };
		}

		public static implicit operator LogEventParamPairParamValue(long value)
		{
			return new LogEventParamPairParamValue() { Int64 = value };
		}

		public static implicit operator LogEventParamPairParamValue(Vec3f value)
		{
			return new LogEventParamPairParamValue() { Vec3f = value };
		}

		public static implicit operator LogEventParamPairParamValue(Quat value)
		{
			return new LogEventParamPairParamValue() { Quat = value };
		}

		internal void Set(ref LogEventParamPairParamValueInternal other)
		{
			ClientHandle = other.ClientHandle;
			String = other.String;
			UInt32 = other.UInt32;
			Int32 = other.Int32;
			UInt64 = other.UInt64;
			Int64 = other.Int64;
			Vec3f = other.Vec3f;
			Quat = other.Quat;
		}
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Pack = 8)]
	internal struct LogEventParamPairParamValueInternal : IGettable<LogEventParamPairParamValue>, ISettable<LogEventParamPairParamValue>, System.IDisposable
	{
		[System.Runtime.InteropServices.FieldOffset(0)]
		private AntiCheatCommonEventParamType m_ParamValueType;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private System.IntPtr m_ClientHandle;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private System.IntPtr m_String;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private uint m_UInt32;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private int m_Int32;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private ulong m_UInt64;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private long m_Int64;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private Vec3fInternal m_Vec3f;
		[System.Runtime.InteropServices.FieldOffset(8)]
		private QuatInternal m_Quat;

		public System.IntPtr? ClientHandle
		{
			get
			{
				System.IntPtr? value;
				Helper.Get(m_ClientHandle, out value, m_ParamValueType, AntiCheatCommonEventParamType.ClientHandle);
				return value;
			}

			set
			{
				Helper.Set<System.IntPtr, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_ClientHandle, AntiCheatCommonEventParamType.ClientHandle, ref m_ParamValueType, this);
			}
		}

		public Utf8String String
		{
			get
			{
				Utf8String value;
				Helper.Get(m_String, out value, m_ParamValueType, AntiCheatCommonEventParamType.String);
				return value;
			}

			set
			{
				Helper.Set<AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_String, AntiCheatCommonEventParamType.String, ref m_ParamValueType, this);
			}
		}

		public uint? UInt32
		{
			get
			{
				uint? value;
				Helper.Get(m_UInt32, out value, m_ParamValueType, AntiCheatCommonEventParamType.UInt32);
				return value;
			}

			set
			{
				Helper.Set<uint, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_UInt32, AntiCheatCommonEventParamType.UInt32, ref m_ParamValueType, this);
			}
		}

		public int? Int32
		{
			get
			{
				int? value;
				Helper.Get(m_Int32, out value, m_ParamValueType, AntiCheatCommonEventParamType.Int32);
				return value;
			}

			set
			{
				Helper.Set<int, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_Int32, AntiCheatCommonEventParamType.Int32, ref m_ParamValueType, this);
			}
		}

		public ulong? UInt64
		{
			get
			{
				ulong? value;
				Helper.Get(m_UInt64, out value, m_ParamValueType, AntiCheatCommonEventParamType.UInt64);
				return value;
			}

			set
			{
				Helper.Set<ulong, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_UInt64, AntiCheatCommonEventParamType.UInt64, ref m_ParamValueType, this);
			}
		}

		public long? Int64
		{
			get
			{
				long? value;
				Helper.Get(m_Int64, out value, m_ParamValueType, AntiCheatCommonEventParamType.Int64);
				return value;
			}

			set
			{
				Helper.Set<long, AntiCheatCommon.AntiCheatCommonEventParamType>(value, ref m_Int64, AntiCheatCommonEventParamType.Int64, ref m_ParamValueType, this);
			}
		}

		public Vec3f Vec3f
		{
			get
			{
				Vec3f value;
				Helper.Get(ref m_Vec3f, out value, m_ParamValueType, AntiCheatCommonEventParamType.Vector3f);
				return value;
			}

			set
			{
				Helper.Set(ref value, ref m_Vec3f, AntiCheatCommonEventParamType.Vector3f, ref m_ParamValueType, this);
			}
		}

		public Quat Quat
		{
			get
			{
				Quat value;
				Helper.Get(ref m_Quat, out value, m_ParamValueType, AntiCheatCommonEventParamType.Quat);
				return value;
			}

			set
			{
				Helper.Set(ref value, ref m_Quat, AntiCheatCommonEventParamType.Quat, ref m_ParamValueType, this);
			}
		}

		public void Set(ref LogEventParamPairParamValue other)
		{
			ClientHandle = other.ClientHandle;
			String = other.String;
			UInt32 = other.UInt32;
			Int32 = other.Int32;
			UInt64 = other.UInt64;
			Int64 = other.Int64;
			Vec3f = other.Vec3f;
			Quat = other.Quat;
		}

		public void Set(ref LogEventParamPairParamValue? other)
		{
			if (other.HasValue)
			{
				ClientHandle = other.Value.ClientHandle;
				String = other.Value.String;
				UInt32 = other.Value.UInt32;
				Int32 = other.Value.Int32;
				UInt64 = other.Value.UInt64;
				Int64 = other.Value.Int64;
				Vec3f = other.Value.Vec3f;
				Quat = other.Value.Quat;
			}
		}

		public void Dispose()
		{
			Helper.Dispose(ref m_ClientHandle, m_ParamValueType, AntiCheatCommonEventParamType.ClientHandle);
			Helper.Dispose(ref m_String, m_ParamValueType, AntiCheatCommonEventParamType.String);
			Helper.Dispose(ref m_Vec3f);
			Helper.Dispose(ref m_Quat);
		}

		public void Get(out LogEventParamPairParamValue output)
		{
			output = new LogEventParamPairParamValue();
			output.Set(ref this);
		}
	}
}