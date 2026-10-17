using Sabine.Zone.World.Instances;
using Xunit;

namespace Tests.Sabine.Shared.World
{
	/// <summary>
	/// Locks the script-visible numeric and ownership contract shared with
	/// rAthena instance commands. Runtime map/party integration is covered by
	/// the ZoneServer build and in-game script load path.
	/// </summary>
	public class InstanceContractTests
	{
		[Fact]
		public void Instance_enter_results_match_rathena_values()
		{
			Assert.Equal(0, (int)InstanceEnterResult.Ok);
			Assert.Equal(1, (int)InstanceEnterResult.NoMember);
			Assert.Equal(2, (int)InstanceEnterResult.NoInstance);
			Assert.Equal(3, (int)InstanceEnterResult.Other);
		}

		[Fact]
		public void Instance_retains_its_template_scope_and_entry_point()
		{
			var instance = new Instance(7, "Test Dungeon", InstanceMode.Party, 42,
				ownerCharId: 9, ownerPartyId: 42, entryMap: "test_map",
				entryX: 10, entryY: 20);

			Assert.Equal("Test Dungeon", instance.TemplateName);
			Assert.Equal(InstanceMode.Party, instance.Mode);
			Assert.Equal(42, instance.OwnerId);
			Assert.Equal("test_map", instance.EntryMap);
			Assert.Equal(10, instance.EntryX);
			Assert.Equal(20, instance.EntryY);
		}
	}
}
