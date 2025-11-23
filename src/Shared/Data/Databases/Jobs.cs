using System;
using Newtonsoft.Json.Linq;
using Sabine.Shared.Const;
using Yggdrasil.Data.JSON;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Represents a job's data.
	/// </summary>
	public class JobData
	{
		public JobId Id { get; set; }
		public string Name { get; set; }

		public JobModifiersData Modifiers { get; set; } = new();
		public JobWeaponDelaysData WeaponDelays { get; set; } = new();
	}

	/// <summary>
	/// A job's stat modifiers.
	/// </summary>
	public class JobModifiersData
	{
		public int Weight { get; set; }
		public float HpFactor { get; set; }
		public float HpMultiplier { get; set; }
		public float SpFactor { get; set; }
	}

	/// <summary>
	/// A job's weapon delays.
	/// </summary>
	public class JobWeaponDelaysData
	{
		public float BareHand { get; set; }
		public float Dagger { get; set; }
		public float Sword { get; set; }
		public float Bow { get; set; }
		public float Spear { get; set; }
		public float Axe { get; set; }
		public float Mace { get; set; }
		public float Rod { get; set; }

		public float GetDelay(WeaponType? weaponType)
		{
			return weaponType switch
			{
				// Direct mappings
				WeaponType.Dagger => this.Dagger,
				WeaponType.Bow => this.Bow,
				WeaponType.Mace => this.Mace,
				WeaponType.Staff => this.Rod,

				// Grouped mappings for one-handed and two-handed versions
				WeaponType.OneHandedSword or WeaponType.TwoHandedSword => this.Sword,
				WeaponType.OneHandedSpear or WeaponType.TwoHandedSpear => this.Spear,
				WeaponType.OneHandedAxe or WeaponType.TwoHandedAxe => this.Axe,

				// Default case for null, BareHand, or any other weapon type
				// not defined above (Knuckle, Book, Katar, etc.)
				_ => this.BareHand
			};
		}
	}

	/// <summary>
	/// A job database.
	/// </summary>
	public class JobDb : DatabaseJsonIndexed<JobId, JobData>
	{
		/// <summary>
		/// Called to read one entry from the job data.
		/// </summary>
		/// <param name="entry"></param>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("id", "name", "modifiers", "weaponDelays");

			var modifiersObj = (JObject)entry["modifiers"];
			modifiersObj.AssertNotMissing("weight", "hpFactor", "hpMultiplier", "spFactor");

			var weaponDelaysObj = (JObject)entry["weaponDelays"];
			weaponDelaysObj.AssertNotMissing("bareHand", "dagger", "sword", "bow", "spear", "axe", "mace", "rod");

			var data = new JobData();

			data.Id = entry.ReadEnum<JobId>("id");
			data.Name = entry.ReadString("name");

			data.Modifiers.Weight = modifiersObj.ReadInt("weight");
			data.Modifiers.HpFactor = modifiersObj.ReadFloat("hpFactor");
			data.Modifiers.HpMultiplier = modifiersObj.ReadFloat("hpMultiplier");
			data.Modifiers.SpFactor = modifiersObj.ReadFloat("spFactor");

			data.WeaponDelays.BareHand = weaponDelaysObj.ReadFloat("bareHand");
			data.WeaponDelays.Dagger = weaponDelaysObj.ReadFloat("dagger");
			data.WeaponDelays.Sword = weaponDelaysObj.ReadFloat("sword");
			data.WeaponDelays.Bow = weaponDelaysObj.ReadFloat("bow");
			data.WeaponDelays.Spear = weaponDelaysObj.ReadFloat("spear");
			data.WeaponDelays.Axe = weaponDelaysObj.ReadFloat("axe");
			data.WeaponDelays.Mace = weaponDelaysObj.ReadFloat("mace");
			data.WeaponDelays.Rod = weaponDelaysObj.ReadFloat("rod");

			this.AddOrReplace(data.Id, data);
		}
	}
}
