// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.Ecom
{
	/// <summary>
	/// Input parameters for the <see cref="EcomInterface.CopyLastRedeemedEntitlementByIndex" /> function.
	/// </summary>
	public struct CopyLastRedeemedEntitlementByIndexOptions
	{
		/// <summary>
		/// The Epic Account ID of the local user whose last redeemed entitlement id is being copied
		/// </summary>
		public EpicAccountId LocalUserId { get; set; }

		/// <summary>
		/// Index of the last redeemed entitlement id to retrieve from the cache
		/// </summary>
		public uint RedeemedEntitlementIndex { get; set; }
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct CopyLastRedeemedEntitlementByIndexOptionsInternal : ISettable<CopyLastRedeemedEntitlementByIndexOptions>, System.IDisposable
	{
		private int m_ApiVersion;
		private System.IntPtr m_LocalUserId;
		private uint m_RedeemedEntitlementIndex;

		public EpicAccountId LocalUserId
		{
			set
			{
				Helper.Set(value, ref m_LocalUserId);
			}
		}

		public uint RedeemedEntitlementIndex
		{
			set
			{
				m_RedeemedEntitlementIndex = value;
			}
		}

		public void Set(ref CopyLastRedeemedEntitlementByIndexOptions other)
		{
			m_ApiVersion = EcomInterface.CopylastredeemedentitlementbyindexApiLatest;
			LocalUserId = other.LocalUserId;
			RedeemedEntitlementIndex = other.RedeemedEntitlementIndex;
		}

		public void Set(ref CopyLastRedeemedEntitlementByIndexOptions? other)
		{
			if (other.HasValue)
			{
				m_ApiVersion = EcomInterface.CopylastredeemedentitlementbyindexApiLatest;
				LocalUserId = other.Value.LocalUserId;
				RedeemedEntitlementIndex = other.Value.RedeemedEntitlementIndex;
			}
		}

		public void Dispose()
		{
			Helper.Dispose(ref m_LocalUserId);
		}
	}
}