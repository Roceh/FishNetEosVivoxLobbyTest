// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.RTCAudio
{
	/// <summary>
	/// Input parameters for the <see cref="RTCAudioInterface.GetAudioOutputDeviceByIndex" /> function.
	/// </summary>
	public struct GetAudioOutputDeviceByIndexOptions
	{
		/// <summary>
		/// Index of the device info to retrieve.
		/// </summary>
		public uint DeviceInfoIndex { get; set; }
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct GetAudioOutputDeviceByIndexOptionsInternal : ISettable<GetAudioOutputDeviceByIndexOptions>, System.IDisposable
	{
		private int m_ApiVersion;
		private uint m_DeviceInfoIndex;

		public uint DeviceInfoIndex
		{
			set
			{
				m_DeviceInfoIndex = value;
			}
		}

		public void Set(ref GetAudioOutputDeviceByIndexOptions other)
		{
			m_ApiVersion = RTCAudioInterface.GetaudiooutputdevicebyindexApiLatest;
			DeviceInfoIndex = other.DeviceInfoIndex;
		}

		public void Set(ref GetAudioOutputDeviceByIndexOptions? other)
		{
			if (other.HasValue)
			{
				m_ApiVersion = RTCAudioInterface.GetaudiooutputdevicebyindexApiLatest;
				DeviceInfoIndex = other.Value.DeviceInfoIndex;
			}
		}

		public void Dispose()
		{
		}
	}
}