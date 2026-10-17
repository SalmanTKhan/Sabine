namespace Sabine.Shared.Const
{
	/// <summary>
	/// Script-facing cell-type selector for runtime cell mutation.
	/// Mirrors rAthena's <c>cell_*</c> values used with the
	/// <c>setcell</c> command. The runtime implementation lives in
	/// <see cref="Sabine.Shared.Data.Databases.MapCacheData"/> and
	/// stores the per-cell state as a sparse <c>CellFlags</c> overlay.
	/// </summary>
	public enum CellType
	{
		/// <summary>
		/// Cell is walkable. Setting to <c>true</c> overrides the
		/// underlying terrain to allow movement; setting to <c>false</c>
		/// blocks it. rAthena: <c>cell_walkable</c>.
		/// </summary>
		Walkable,

		/// <summary>
		/// Cell allows projectiles to pass through. rAthena:
		/// <c>cell_shootable</c>.
		/// </summary>
		Shootable,

		/// <summary>
		/// Cell is water (potion-brewing, fishing, etc.). rAthena:
		/// <c>cell_water</c>.
		/// </summary>
		Water,

		/// <summary>
		/// NPC-only cell — blocks character movement but not skills or
		/// projectiles. rAthena: <c>cell_npc</c>.
		/// </summary>
		Npc,

		/// <summary>
		/// Basilica sanctuary cell. rAthena: <c>cell_basilica</c>.
		/// </summary>
		Basilica,

		/// <summary>
		/// Land Protector (skill-blocking) cell. rAthena:
		/// <c>cell_landprotector</c>.
		/// </summary>
		LandProtector,

		/// <summary>
		/// No vending allowed. rAthena: <c>cell_novending</c>.
		/// </summary>
		NoVending,

		/// <summary>
		/// No chat rooms allowed. rAthena: <c>cell_nochat</c>.
		/// </summary>
		NoChat,

		/// <summary>
		/// Ice Wall cell (absorbs hits, blocks LOS). rAthena:
		/// <c>cell_icewall</c>.
		/// </summary>
		IceWall,
		/// <summary>Legacy converter spelling compatibility.</summary>
		Icewall = IceWall,

		/// <summary>
		/// No skills usable on this cell. rAthena: <c>cell_noskill</c>.
		/// </summary>
		NoSkill,
	}
}
