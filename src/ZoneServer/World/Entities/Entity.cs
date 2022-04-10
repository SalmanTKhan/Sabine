﻿using Sabine.Shared.World;
using Sabine.Zone.World.Maps;

namespace Sabine.Zone.World.Entities
{
	/// <summary>
	/// Represents something that can exist on a map.
	/// </summary>
	public interface IEntity
	{
		/// <summary>
		/// Returns the name of the map the entity is currently on.
		/// </summary>
		string MapName { get; }

		/// <summary>
		/// Returns the entity's current position.
		/// </summary>
		Position Position { get; }

		/// <summary>
		/// Returns a reference to the map the entity is currently on.
		/// </summary>
		Map Map { get; }
	}
}
