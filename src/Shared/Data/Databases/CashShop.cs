using Newtonsoft.Json.Linq;
using Yggdrasil.Data.JSON;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// One entry in the cash shop. Mirrors a single row of eAthena's
	/// <c>cash_shop_db.txt</c> — item id plus the two price columns
	/// (cash points and kafra points). The EP12 client renders both
	/// columns side-by-side in the list packet.
	/// </summary>
	public class CashShopEntry
	{
		/// <summary>Item class id sold.</summary>
		public int ItemId { get; set; }

		/// <summary>Price in cash points (CashPoints / SP_CASHPOINTS).</summary>
		public int CashPrice { get; set; }

		/// <summary>Price in kafra points (KafraPoints / SP_KAFRAPOINTS).</summary>
		public int KafraPrice { get; set; }
	}

	/// <summary>
	/// db/cash_shop.txt — indexed by item id.
	/// </summary>
	public class CashShopDb : DatabaseJsonIndexed<int, CashShopEntry>
	{
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("id", "cashPrice");

			var data = new CashShopEntry
			{
				ItemId = (int)entry["id"],
				CashPrice = (int)entry["cashPrice"],
				KafraPrice = (int?)entry["kafraPrice"] ?? 0,
			};

			this.Entries[data.ItemId] = data;
		}
	}
}
