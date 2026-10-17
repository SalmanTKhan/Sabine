using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Yggdrasil.Data.JSON;

namespace Sabine.Shared.Data
{
	/// <summary>
	/// Inclusive packet-version range for a data entry.
	/// </summary>
	public readonly struct DataVersionRange
	{
		public int Minimum { get; }
		public int Maximum { get; }

		public DataVersionRange(int minimum, int maximum)
		{
			this.Minimum = minimum;
			this.Maximum = maximum;
		}

		public bool Contains(int version)
			=> version >= this.Minimum && version <= this.Maximum;

		public override string ToString()
			=> $"{this.Minimum}..{(this.Maximum == int.MaxValue ? "max" : this.Maximum)}";
	}

	/// <summary>
	/// Common version-gating behavior for text database entries.
	/// </summary>
	public static class VersionedData
	{
		public static bool IsActive(JObject entry, out DataVersionRange range)
		{
			range = new DataVersionRange(
				entry.ReadInt("versionMin", 0),
				entry.ReadInt("versionMax", int.MaxValue));

			return range.Contains(Game.Version);
		}
	}

	/// <summary>
	/// Diagnostics collected while version-gated data is loaded.
	/// </summary>
	public sealed class VersionedDataDiagnostics
	{
		private readonly List<string> _skippedEntries = new();

		public int SkippedCount => _skippedEntries.Count;
		public IReadOnlyList<string> SkippedEntries => _skippedEntries;

		public void Reset()
			=> _skippedEntries.Clear();

		public void AddSkipped(string entry)
			=> _skippedEntries.Add(entry);
	}

	/// <summary>
	/// Optional contract for databases that report version-filtered entries.
	/// </summary>
	public interface IVersionedDataDatabase
	{
		VersionedDataDiagnostics VersionDiagnostics { get; }
	}
}
