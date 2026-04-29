using Sabine.Shared.Const;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Per-character damage modifier accumulator. Mirrors the bucket of
	/// <c>add_race</c> / <c>sub_race</c> / <c>add_size</c> /
	/// <c>add_ele</c> bonuses that eAthena and rAthena maintain on
	/// <c>map_session_data</c>.
	/// </summary>
	/// <remarks>
	/// In v1 nothing populates these arrays — they're a hook for the
	/// future card-script and equip-bonus engine. The
	/// <see cref="BattleCalculator"/> still consults them, so once the
	/// card engine writes into the arrays the multipliers go live with
	/// no further wiring.
	/// </remarks>
	public class BonusModifiers
	{
		/// <summary>Percent damage bonus dealt to a target of the indexed race.</summary>
		public int[] AddRace { get; }

		/// <summary>Percent damage reduction taken from an attacker of the indexed race.</summary>
		public int[] SubRace { get; }

		/// <summary>Percent damage bonus dealt to a target of the indexed size (S/M/L).</summary>
		public int[] AddSize { get; }

		/// <summary>Percent damage reduction taken from an attacker of the indexed size.</summary>
		public int[] SubSize { get; }

		/// <summary>Percent damage bonus dealt to a target of the indexed defense element.</summary>
		public int[] AddElement { get; }

		/// <summary>Percent damage reduction taken from an attacker of the indexed attack element.</summary>
		public int[] SubElement { get; }

		public BonusModifiers()
		{
			this.AddRace = new int[(int)RaceType.All + 1];
			this.SubRace = new int[(int)RaceType.All + 1];
			this.AddSize = new int[3];
			this.SubSize = new int[3];
			this.AddElement = new int[10];
			this.SubElement = new int[10];
		}

		/// <summary>
		/// Returns the sum bonus the attacker grants when hitting a target
		/// of the given race, expressed as a percent (e.g. 30 = +30%).
		/// </summary>
		public int GetRaceBonus(RaceType race)
		{
			var idx = (int)race;
			return idx >= 0 && idx < this.AddRace.Length ? this.AddRace[idx] : 0;
		}

		/// <summary>
		/// Returns the target's reduction against attacks from the given
		/// race, expressed as a percent (e.g. 20 = -20% taken).
		/// </summary>
		public int GetRaceReduction(RaceType race)
		{
			var idx = (int)race;
			return idx >= 0 && idx < this.SubRace.Length ? this.SubRace[idx] : 0;
		}

		public int GetSizeBonus(SizeType size)
		{
			var idx = (int)size;
			return idx >= 0 && idx < this.AddSize.Length ? this.AddSize[idx] : 0;
		}

		public int GetSizeReduction(SizeType size)
		{
			var idx = (int)size;
			return idx >= 0 && idx < this.SubSize.Length ? this.SubSize[idx] : 0;
		}

		public int GetElementBonus(ElementType element)
		{
			var idx = (int)element;
			return idx >= 0 && idx < this.AddElement.Length ? this.AddElement[idx] : 0;
		}

		public int GetElementReduction(ElementType element)
		{
			var idx = (int)element;
			return idx >= 0 && idx < this.SubElement.Length ? this.SubElement[idx] : 0;
		}
	}
}
