using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Sabine.Shared.Const;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	public static class FeatureId
	{
		public static readonly string JobLevels = "JobLevels";
		public static readonly string HigherMaxWeight = "HigherMaxWeight";

		/// <summary>
		/// When enabled the renewal damage formulas are used for physical
		/// and magic attacks; when disabled the pre-renewal formulas are
		/// used. Defaults to disabled (pre-renewal) since Sabine targets
		/// classic-era servers.
		/// </summary>
		public static readonly string Renewal = "Renewal";

		/// <summary>
		/// Selects rAthena's PRE-RE branch instead of eAthena classic.
		/// Has no effect when <see cref="Renewal"/> is enabled. See
		/// <see cref="Era"/>.
		/// </summary>
		public static readonly string PreRenewal = "PreRenewal";

		/// <summary>
		/// Master toggle for the expanded status-effect catalogue (Curse,
		/// upcoming song/dance, food, etc.). When disabled, only the
		/// originally-shipped buffs and debuffs are active. Independent of
		/// the era flags so a classic server can opt into individual SCs.
		/// </summary>
		public static readonly string StatusEffectsExpanded = "StatusEffectsExpanded";

		/// <summary>
		/// When enabled, debuff durations are scaled with the renewal
		/// resistance formula (rAthena status_get_sc_def's RENEWAL branch);
		/// when disabled, the eAthena classic formula is used. Independent
		/// of <see cref="Renewal"/> so the server operator can opt into
		/// renewal status math without flipping the damage formulas.
		/// </summary>
		public static readonly string RenewalStatusResistance = "RenewalStatusResistance";

		/// <summary>
		/// When enabled, base status durations follow rAthena's
		/// <c>db/re/status.yml</c>; when disabled, they follow
		/// <c>db/pre-re/status.yml</c>. Skill handlers that compute their
		/// own duration consult this flag where the two databases diverge.
		/// </summary>
		public static readonly string RenewalStatusDurations = "RenewalStatusDurations";
	}

	/// <summary>
	/// Represents a node in the feature tree database.
	/// </summary>
	public class FeatureNode
	{
		public string Name { get; set; }
		public bool Enabled { get; set; }

		public FeatureNode Parent { get; set; }
		public List<FeatureNode> Children { get; set; } = new List<FeatureNode>();

		/// <summary>
		/// Changes this feature and all of its children.
		/// </summary>
		public void Enable(bool enabled)
			=> this.Enable(enabled, true);

		/// <summary>
		/// Changes this feature and optionally all of its children.
		/// </summary>
		public void Enable(bool enabled, bool toggleChildren)
		{
			this.Enabled = enabled;

			if (toggleChildren)
			{
				foreach (var child in this.Children)
					child.Enable(enabled);
			}
		}
	}

	/// <summary>
	/// A hierarchical feature database.
	/// </summary>
	public class FeatureDb : DatabaseJsonIndexed<string, FeatureNode>
	{
		/// <summary>
		/// Returns true if the feature with the given name is enabled.
		/// Returns false if not or if the feature doesn't exist.
		/// </summary>
		/// <param name="featureName"></param>
		/// <returns></returns>
		public bool IsEnabled(string featureName)
			=> this.Entries.TryGetValue(featureName)?.Enabled ?? false;

		/// <summary>
		/// Returns the gameplay era resolved from the current feature flags.
		/// </summary>
		/// <remarks>
		/// Renewal beats PreRenewal beats Classic. Resolved on every call
		/// so live config reloads take effect; callers that hot-path this
		/// should cache locally.
		/// </remarks>
		public Era GetEra()
		{
			if (this.IsEnabled(FeatureId.Renewal))
				return Era.Renewal;

			if (this.IsEnabled(FeatureId.PreRenewal))
				return Era.PreRenewal;

			return Era.Classic;
		}

		/// <summary>
		/// Called to read an entry from the database.
		/// </summary>
		/// <param name="entry"></param>
		protected override void ReadEntry(JObject entry)
		{
			// The entries we get here are the first level parents.
			// We start to read each one recursively.
			this.ReadEntryRecursive(entry);
		}

		/// <summary>
		/// Reads entry and its children and adds the new nodes.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		private FeatureNode ReadEntryRecursive(JObject entry)
		{
			entry.AssertNotMissing("name", "enabled");

			var node = new FeatureNode();

			// First, get the name and whether the feature is enabled
			node.Name = entry.ReadString("name");
			node.Enabled = entry.ReadBool("enabled");

			// Next, see if we already read this feature before. If so,
			// we want to override its status and that of all of its
			// children. Then we return with null to tell the caller
			// that this element should not be considered a new child,
			// in case a feature was disabled in a later update.
			if (this.TryFind(node.Name, out var existingNode))
			{
				existingNode.Enable(node.Enabled);
				return null;
			}

			// Go through the children if there are any
			if (entry.ContainsKey("children"))
			{
				foreach (var childEntry in entry.ForEachObject("children"))
				{
					// Read the child and its children, and if it was a
					// new node, set its parent to the current node,
					// add it to its children, and check its state.
					var childNode = this.ReadEntryRecursive(childEntry);
					if (childNode != null)
					{
						childNode.Parent = node;
						node.Children.Add(childNode);

						// If the child is enabled, but the parent is not,
						// the child should be disabled as well. This check
						// is important because it doesn't work the other
						// way around, a child might be disabled while the
						// parent is enabled.
						if (childNode.Enabled && !node.Enabled)
							childNode.Enable(false);
					}
				}
			}

			// Once all is done, add the node to the entries and return it.
			this.AddOrReplace(node.Name, node);
			return node;
		}
	}
}
