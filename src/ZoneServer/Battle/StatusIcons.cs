using Sabine.Shared.Const;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Maps a Sabine <see cref="StatusId"/> to the client-side
	/// SI_* icon id used by <c>ZC_MSG_STATE_CHANGE</c>. Values mirror
	/// eAthena's <c>db/icon_db</c> / rAthena's <c>SI_*</c> enum.
	/// </summary>
	internal static class StatusIcons
	{
		/// <summary>Sentinel: no icon defined; the status is purely
		/// internal or rides on opt1/opt2/opt3 instead of a per-icon
		/// packet.</summary>
		public const int None = -1;

		public static int GetIcon(StatusId id)
		{
			return id switch
			{
				StatusId.IncreaseAgi      => 5,
				StatusId.DecreaseAgi      => 6,
				StatusId.Blessing         => 4,
				StatusId.Angelus          => None, // opt2 visual already

				StatusId.Endure           => 8,
				StatusId.Concentration    => 24,
				StatusId.LoudExclamation  => 16,
				StatusId.Sight            => 10,
				StatusId.Hiding           => 17,

				StatusId.Provoke          => 7,
				StatusId.Stone            => None, // opt1
				StatusId.Freeze           => None, // opt1
				StatusId.Stun             => None, // opt1
				StatusId.Sleep            => None, // opt1

				StatusId.Poison           => None, // opt2 normally
				StatusId.Bleeding         => 124,

				StatusId.Silence          => None, // opt2
				StatusId.Blind            => None, // opt2
				StatusId.Confusion        => None, // opt2
				StatusId.Chaos            => None, // opt2

				_ => None,
			};
		}
	}
}
