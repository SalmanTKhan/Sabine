using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Scripting.Items
{
	/// <summary>
	/// Pet Food (item 537). When the owner uses one, the active
	/// homunculus is fed (+11 hunger). v1 accepts any homunculus
	/// type; eAthena's per-type favored-food map is a follow-up.
	/// Falls back to a normal consumable if no homun is active.
	/// </summary>
	[ItemScript(537, 512, 513, 515, 521, 544)]
	public class HomunFoodScript : ItemScript
	{
		public override ItemUseResult OnUse(PlayerCharacter player, Item item)
		{
			if (player.Homunculus == null || !player.Homunculus.HasContract)
				return ItemUseResult.Fail;

			if (!player.Homunculus.FeedItem(item.ClassId))
				return ItemUseResult.Fail;

			player.ServerMessage($"You feed {player.Homunculus.Name}.");
			return ItemUseResult.Okay;
		}
	}
}
