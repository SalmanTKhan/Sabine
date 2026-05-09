using Sabine.Shared.Data.Databases;

namespace Sabine.Shared.Data
{
	/// <summary>
	/// Interface providing access to all of the server's data.
	/// </summary>
	public class SabineData
	{
		/// <summary>
		/// db/exp.txt
		/// </summary>
		public ExpDb ExpTables { get; } = new();

		/// <summary>
		/// db/features.txt
		/// </summary>
		public FeatureDb Features { get; } = new();

		/// <summary>
		/// db/items.txt
		/// </summary>
		public ItemDb Items { get; } = new();

		/// <summary>
		/// db/item_names.txt
		/// </summary>
		public ItemNameDb ItemNames { get; } = new();

		/// <summary>
		/// db/jobs.txt
		/// </summary>
		public JobDb Jobs { get; } = new();

		/// <summary>
		/// db/map_cache.dat
		/// </summary>
		public MapCacheDb MapCache { get; } = new();

		/// <summary>
		/// db/maps.txt
		/// </summary>
		public MapsDb Maps { get; } = new();

		/// <summary>
		/// db/monsters.txt
		/// </summary>
		public MonsterDb Monsters { get; } = new();

		/// <summary>
		/// db/castles.txt — WoE castle templates (map id, display
		/// name, on-break event hook). Runtime ownership state lives
		/// in <c>Sabine.Zone.World.Woe.CastleManager</c>.
		/// </summary>
		public CastleDb Castles { get; } = new();

		/// <summary>
		/// db/instances.txt — instance template definitions used by
		/// the runtime <c>InstanceManager</c> to resolve
		/// <c>instance_create("name")</c> back to a list of source
		/// maps + ttl.
		/// </summary>
		public InstanceDb Instances { get; } = new();

		/// <summary>
		/// db/pets.txt — pet definitions (mob id, name, lure / egg /
		/// equip item ids, food, fullness, hunger interval, intimacy
		/// bands). Mirrors eAthena's <c>db/pet_db.txt</c>; consumed by
		/// the Zone-side <c>PetTypeData</c> facade.
		/// </summary>
		public PetDb Pets { get; } = new();

		/// <summary>
		/// db/mob_skills.txt — slim mob-skill table consumed by the
		/// reactive AI's combat tick. Mirrors a subset of eAthena's
		/// <c>db/mob_skill_db.txt</c>; the omitted state / condition /
		/// val1–5 columns default to "always-true while in combat".
		/// </summary>
		public MobSkillDb MobSkills { get; } = new();

		/// <summary>
		/// db/mercenaries.txt — mercenary contract metadata (faction,
		/// rank, contract item, duration, faith reward). Stat lines
		/// come from the standard monster table; this row only carries
		/// the rental-side bookkeeping.
		/// </summary>
		public MercenaryDb Mercenaries { get; } = new();

		/// <summary>
		/// db/produce_db.txt
		/// </summary>
		public ProduceDb Produce { get; } = new();

		/// <summary>
		/// db/refine_db.txt (or refine_db_renewal.txt when the Renewal
		/// feature flag is on; selected at startup in Server.LoadData).
		/// </summary>
		public RefineDb Refine { get; } = new();

		/// <summary>
		/// db/skills.txt
		/// </summary>
		public SkillDb Skills { get; } = new();

		/// <summary>
		/// db/skill_trees.txt
		/// </summary>
		public SkillTreeDb SkillTrees { get; }

		/// <summary>
		/// Creates new instance.
		/// </summary>
		public SabineData()
		{
			this.SkillTrees = new SkillTreeDb(this.Jobs, this.Skills);
		}
	}
}
