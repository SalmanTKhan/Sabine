using System;
using Sabine.Shared.Data.Databases;

namespace Sabine.Zone.World.Woe
{
	/// <summary>
	/// Runtime ownership state for one WoE castle. The static
	/// template (display name, on-break event) lives in
	/// <see cref="CastleData"/>; this class holds the mutable
	/// per-run state — owning guild, economy/defense investment,
	/// daily trigger flags, treasure-spawn bookkeeping. Mutations
	/// flow through <see cref="CastleManager"/> so the database
	/// (`castle_state` table) stays in sync.
	/// </summary>
	public class Castle
	{
		/// <summary>The castle's static template (id, name, hooks).</summary>
		public CastleData Template { get; }

		/// <summary>Convenience: map string id from the template.</summary>
		public string MapStringId => this.Template.MapStringId;

		/// <summary>Convenience: display name from the template.</summary>
		public string Name => this.Template.Name;

		/// <summary>
		/// Owning guild's id, or <c>0</c> when the castle is
		/// unoccupied. eAthena's <c>GetCastleData(map, 1)</c>.
		/// </summary>
		public int OwnerGuildId { get; set; }

		/// <summary>
		/// Economy investment level (0–100ish, server-config
		/// capped). eAthena's <c>GetCastleData(map, 2)</c>.
		/// </summary>
		public int Economy { get; set; }

		/// <summary>
		/// Defense investment level. eAthena's
		/// <c>GetCastleData(map, 3)</c>.
		/// </summary>
		public int Defense { get; set; }

		/// <summary>
		/// "Did we already invest in economy today?" daily flag.
		/// eAthena's <c>GetCastleData(map, 4)</c>.
		/// </summary>
		public int TriggerE { get; set; }

		/// <summary>
		/// "Did we already invest in defense today?" daily flag.
		/// eAthena's <c>GetCastleData(map, 5)</c>.
		/// </summary>
		public int TriggerD { get; set; }

		/// <summary>
		/// Bitmask tracking treasure-spawn day flags
		/// (<c>GetCastleData(map, 10..17)</c> in eAthena). Stored
		/// packed in one int; bit N corresponds to type 10+N.
		/// </summary>
		public int TreasureFlags { get; set; }

		public Castle(CastleData template)
		{
			this.Template = template;
		}

		/// <summary>
		/// Reads one of eAthena's numeric castle-data slots. Unknown
		/// types return 0 (matches eAthena's permissive contract).
		/// </summary>
		public int GetData(int dataType)
		{
			return dataType switch
			{
				0 => this.Template.Id,
				1 => this.OwnerGuildId,
				2 => this.Economy,
				3 => this.Defense,
				4 => this.TriggerE,
				5 => this.TriggerD,
				int n when n >= 10 && n <= 17 =>
					(this.TreasureFlags >> (n - 10)) & 1,
				_ => 0,
			};
		}

		/// <summary>
		/// Investment cap for economy / defense. Mirrors rAthena's
		/// MAX_INVESTMENT_VALUE (defaults to 100 in pre-renewal).
		/// </summary>
		public const int MaxInvestmentLevel = 100;

		/// <summary>
		/// Writes one of eAthena's numeric castle-data slots.
		/// Returns true when the type was recognised. Economy /
		/// defense are clamped to [0, <see cref="MaxInvestmentLevel"/>]
		/// and trigger flags to {0, 1} so scripts can't push invalid
		/// values into the persisted state.
		/// </summary>
		public bool SetData(int dataType, int value)
		{
			switch (dataType)
			{
				case 1: this.OwnerGuildId = value; return true;
				case 2: this.Economy = Math.Clamp(value, 0, MaxInvestmentLevel); return true;
				case 3: this.Defense = Math.Clamp(value, 0, MaxInvestmentLevel); return true;
				case 4: this.TriggerE = value != 0 ? 1 : 0; return true;
				case 5: this.TriggerD = value != 0 ? 1 : 0; return true;
				case int n when n >= 10 && n <= 17:
				{
					var bit = 1 << (n - 10);
					if (value != 0)
						this.TreasureFlags |= bit;
					else
						this.TreasureFlags &= ~bit;
					return true;
				}
				default:
					return false;
			}
		}

		/// <summary>Snapshot to a DB row for persistence.</summary>
		internal CastleStateRow ToRow() => new CastleStateRow
		{
			MapStringId = this.MapStringId,
			OwnerGuildId = this.OwnerGuildId,
			Economy = this.Economy,
			Defense = this.Defense,
			TriggerE = this.TriggerE,
			TriggerD = this.TriggerD,
			TreasureFlags = this.TreasureFlags,
		};
	}
}
