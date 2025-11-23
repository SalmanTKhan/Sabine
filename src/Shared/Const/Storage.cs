using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines the type of storage being accessed.
	/// </summary>
	public enum StorageType
	{
		Inventory = 1,
		Cart,
		Storage,
		GuildStorage,
	}

	/// <summary>
	/// Defines the access mode for a storage.
	/// </summary>
	[Flags]
	public enum StorageMode : byte
	{
		None = 0x0,
		Get = 0x1,
		Put = 0x2,
		All = 0x3,
	}
}
