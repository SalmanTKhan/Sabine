using Sabine.Shared;
using SEra = Sabine.Shared.Const.Era;
using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	[Collection("PacketTable")]
	public class ProtocolProfileTests
	{
		[Fact]
		public void Registry_resolves_well_known_versions()
		{
			Assert.NotNull(ProtocolProfiles.GetByVersion(Versions.Alpha));
			Assert.NotNull(ProtocolProfiles.GetByVersion(Versions.Beta1));
			Assert.NotNull(ProtocolProfiles.GetByVersion(Versions.S2500));
		}

		[Fact]
		public void Registry_lookup_by_id_round_trips()
		{
			var profile = ProtocolProfiles.GetById("alpha");
			Assert.NotNull(profile);
			Assert.Equal(Versions.Alpha, profile.Version);
		}

		[Fact]
		public void Default_profile_matches_game_version()
		{
			var saved = Game.Version;
			try
			{
				Game.Version = Versions.Beta1;
				Assert.Equal(Versions.Beta1, ProtocolProfiles.Default.Version);

				Game.Version = Versions.S2500;
				Assert.Equal(Versions.S2500, ProtocolProfiles.Default.Version);
			}
			finally
			{
				Game.Version = saved;
			}
		}

		[Fact]
		public void Default_profile_falls_back_to_closest_lower_version()
		{
			var saved = Game.Version;
			try
			{
				// Pick a value between two registered profiles.
				Game.Version = 750;
				var profile = ProtocolProfiles.Default;
				Assert.NotNull(profile);
				Assert.Equal(Versions.S700, profile.Version);
			}
			finally
			{
				Game.Version = saved;
			}
		}

		[Fact]
		public void Default_profile_overlays_runtime_era()
		{
			// Era resolves from Game.Era which reads CurrentData.
			// Without data loaded, Era == Classic, so the registered
			// Classic profiles surface unchanged.
			var profile = ProtocolProfiles.Default;
			Assert.Equal(SEra.Classic, profile.Era);
		}

		[Fact]
		public void Custom_profile_can_be_registered_and_retrieved()
		{
			var keys = new ObfuscationKeys(0x12345678u, 0x9ABCDEF0u, 0xDEADBEEFu);
			var profile = new ProtocolProfile(
				"test-modern",
				99999,
				SEra.Renewal,
				ProtocolFeatures.Rodex | ProtocolFeatures.CashShop,
				keys);

			ProtocolProfiles.Register(profile);

			var fetched = ProtocolProfiles.GetById("test-modern");
			Assert.Same(profile, fetched);
			Assert.True(fetched.UsesObfuscation);
			Assert.False(fetched.UsesShuffle);
			Assert.True(fetched.Features.HasFlag(ProtocolFeatures.Rodex));
			Assert.True(fetched.Features.HasFlag(ProtocolFeatures.CashShop));
			Assert.False(fetched.Features.HasFlag(ProtocolFeatures.Achievements));
		}

		[Fact]
		public void Profile_without_keys_does_not_obfuscate()
		{
			var profile = new ProtocolProfile("plain", 12345, SEra.Classic, ProtocolFeatures.None);
			Assert.False(profile.UsesObfuscation);
			Assert.False(profile.UsesShuffle);
			Assert.Null(profile.ObfuscationKeys);
		}
	}
}
