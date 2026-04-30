using System;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Tracks the global War of Emperium (agitstart/agitend) state. Sabine
	/// has no full WoE pipeline yet; this exists so @agitstart and friends
	/// have somewhere to flip a flag and broadcast a notice.
	/// </summary>
	public class WoeService
	{
		public static WoeService Instance { get; } = new WoeService();

		/// <summary>
		/// True once @agitstart has fired and @agitend has not. The second
		/// (renewal) edition uses the same flag — we don't distinguish.
		/// </summary>
		public bool IsActive { get; private set; }

		/// <summary>
		/// Time the most recent agit started, or null if not active.
		/// </summary>
		public DateTime? StartedAt { get; private set; }

		public void Start()
		{
			this.IsActive = true;
			this.StartedAt = DateTime.UtcNow;
		}

		public void End()
		{
			this.IsActive = false;
			this.StartedAt = null;
		}
	}
}
