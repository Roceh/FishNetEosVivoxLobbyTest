// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.Presence
{
	/// <summary>
	/// Data for the <see cref="PresenceInterface.HasPresence" /> function.
	/// </summary>
	public struct HasPresenceOptions
	{
		/// <summary>
		/// The Epic Account ID of the local, logged-in user making the request
		/// </summary>
		public EpicAccountId LocalUserId { get; set; }

		/// <summary>
		/// The Epic Account ID of the user whose cached presence data you want to locate
		/// </summary>
		public EpicAccountId TargetUserId { get; set; }
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct HasPresenceOptionsInternal : ISettable<HasPresenceOptions>, System.IDisposable
	{
		private int m_ApiVersion;
		private System.IntPtr m_LocalUserId;
		private System.IntPtr m_TargetUserId;

		public EpicAccountId LocalUserId
		{
			set
			{
				Helper.Set(value, ref m_LocalUserId);
			}
		}

		public EpicAccountId TargetUserId
		{
			set
			{
				Helper.Set(value, ref m_TargetUserId);
			}
		}

		public void Set(ref HasPresenceOptions other)
		{
			m_ApiVersion = PresenceInterface.HaspresenceApiLatest;
			LocalUserId = other.LocalUserId;
			TargetUserId = other.TargetUserId;
		}

		public void Set(ref HasPresenceOptions? other)
		{
			if (other.HasValue)
			{
				m_ApiVersion = PresenceInterface.HaspresenceApiLatest;
				LocalUserId = other.Value.LocalUserId;
				TargetUserId = other.Value.TargetUserId;
			}
		}

		public void Dispose()
		{
			Helper.Dispose(ref m_LocalUserId);
			Helper.Dispose(ref m_TargetUserId);
		}
	}
}