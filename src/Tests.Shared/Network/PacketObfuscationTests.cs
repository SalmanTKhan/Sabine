using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	public class PacketObfuscationTests
	{
		// Reference keys: 2018-03-07bRagexeRE from rAthena
		// clif_obfuscation.hpp. Last keys before obfuscation was
		// retired client-side.
		private static readonly ObfuscationKeys Keys20180307 = new(0x47DA10EBu, 0x4B922CCFu, 0x765C5055u);

		[Fact]
		public void Zero_keys_short_circuit_is_inactive()
		{
			var obf = new PacketObfuscation(new ObfuscationKeys(0, 0, 0));
			Assert.False(obf.IsActive);

			// Inactive obfuscation must pass opcodes through unchanged.
			Assert.Equal(0x0436, obf.DecodeOpcode(0x0436));
			Assert.Equal(0x0890, obf.DecodeOpcode(0x0890));
		}

		[Fact]
		public void Real_keys_report_active()
		{
			var obf = new PacketObfuscation(Keys20180307);
			Assert.True(obf.IsActive);
		}

		[Fact]
		public void Decode_matches_reference_state_machine()
		{
			// Mirror rAthena's algorithm directly so the test would
			// fail if the port drifts. K = clif_cryptKey index.
			var keys = Keys20180307;
			uint state = unchecked((keys.Key1 * keys.Key2) + keys.Key3);

			// Encode three opcodes the way a 20180307 client would
			// (XOR with the high 15 bits of the running state, with
			// rAthena's first-packet double-advance).
			var plain1 = 0x0436;
			var wire1 = plain1 ^ (int)((state >> 16) & 0x7FFF);
			state = unchecked((state * keys.Key2) + keys.Key3); // post-packet advance
			state = unchecked((state * keys.Key2) + keys.Key3); // first-packet extra advance

			var plain2 = 0x0890;
			var wire2 = plain2 ^ (int)((state >> 16) & 0x7FFF);
			state = unchecked((state * keys.Key2) + keys.Key3);

			var plain3 = 0x023B;
			var wire3 = plain3 ^ (int)((state >> 16) & 0x7FFF);
			state = unchecked((state * keys.Key2) + keys.Key3);

			// Now feed those wire opcodes through our implementation
			// and assert it recovers the plaintext sequence.
			var obf = new PacketObfuscation(keys);
			Assert.Equal(plain1, obf.DecodeOpcode(wire1));
			Assert.Equal(plain2, obf.DecodeOpcode(wire2));
			Assert.Equal(plain3, obf.DecodeOpcode(wire3));
		}

		[Fact]
		public void Reset_returns_state_to_initial()
		{
			var obf = new PacketObfuscation(Keys20180307);

			// Consume a few packets to advance the state.
			obf.DecodeOpcode(0x1234);
			obf.DecodeOpcode(0x5678);
			obf.DecodeOpcode(0x9ABC);

			// After reset, decoding the original wire-encoded first
			// packet should once again recover plaintext 0x0436.
			obf.Reset();

			uint state = unchecked((Keys20180307.Key1 * Keys20180307.Key2) + Keys20180307.Key3);
			var wire = 0x0436 ^ (int)((state >> 16) & 0x7FFF);

			Assert.Equal(0x0436, obf.DecodeOpcode(wire));
		}

		[Fact]
		public void Modern_profile_does_not_attach_obfuscation()
		{
			// rAthena disables obfuscation post-2018; the 20211103
			// profile must report that its transform is absent.
			var profile = ProtocolProfiles.GetById("ragexe-20211103");
			Assert.NotNull(profile);
			Assert.False(profile.UsesObfuscation);
			Assert.Null(profile.ObfuscationKeys);
		}

		[Fact]
		public void Pre2018_profile_attaches_obfuscation()
		{
			var profile = ProtocolProfiles.GetById("ragexe-re-20180307");
			Assert.NotNull(profile);
			Assert.True(profile.UsesObfuscation);
			Assert.NotNull(profile.ObfuscationKeys);
			Assert.Equal(0x47DA10EBu, profile.ObfuscationKeys.Value.Key1);
		}
	}
}
