namespace Sabine.Shared.Network
{
	/// <summary>
	/// Per-connection state for the rAthena-compatible 3-key XOR
	/// packet obfuscation scheme used by clients between PACKETVER
	/// 20110817 and 20180307. Outside that window the keys are
	/// (0, 0, 0) and the transform is a no-op, so this state can
	/// be attached unconditionally and gated by
	/// <see cref="ProtocolProfile.UsesObfuscation"/>.
	/// </summary>
	/// <remarks>
	/// Mirrors rAthena's behavior in <c>src/map/clif.cpp</c>:
	/// <list type="bullet">
	///   <item>Initial key (no session yet): <c>K_init = ((K0*K1)+K2) &amp; 0xFFFFFFFF</c></item>
	///   <item>Decode opcode: <c>cmd ^= (state &gt;&gt; 16) &amp; 0x7FFF</c></item>
	///   <item>After every packet: <c>state = (state*K1 + K2) &amp; 0xFFFFFFFF</c></item>
	///   <item>The first packet (WantToConnection) advances the state
	///   twice in rAthena (once when the session is created at line
	///   10723, once at the post-packet hook at line 25775); we mirror
	///   that quirk so packet #2 lines up with the client.</item>
	/// </list>
	/// Obfuscation is inbound-only — outbound opcodes are sent in
	/// the clear, matching rAthena.
	/// </remarks>
	public sealed class PacketObfuscation
	{
		private readonly ObfuscationKeys _keys;
		private uint _state;
		private bool _firstPacket;

		/// <summary>
		/// Creates a fresh state seeded from <paramref name="keys"/>.
		/// </summary>
		public PacketObfuscation(ObfuscationKeys keys)
		{
			_keys = keys;
			this.Reset();
		}

		/// <summary>
		/// Resets the state to the initial pre-session key, as if
		/// no packets have been received on this connection yet.
		/// </summary>
		public void Reset()
		{
			_state = unchecked((_keys.Key1 == 0 && _keys.Key2 == 0 && _keys.Key3 == 0)
				? 0u
				: ((_keys.Key1 * _keys.Key2) + _keys.Key3));
			_firstPacket = true;
		}

		/// <summary>
		/// Returns true when the configured keys are non-zero — i.e.
		/// the transform actually scrambles opcodes. Mirrors the
		/// post-2018 short-circuit that rAthena hard-codes via
		/// <c>packet_keys(0,0,0)</c>.
		/// </summary>
		public bool IsActive
			=> _keys.Key1 != 0 || _keys.Key2 != 0 || _keys.Key3 != 0;

		/// <summary>
		/// Decodes one inbound opcode using the current state and
		/// advances the state for the next packet.
		/// </summary>
		/// <param name="wireOpcode">
		/// The 16-bit opcode as read off the wire.
		/// </param>
		/// <returns>The plaintext opcode the server should dispatch on.</returns>
		public int DecodeOpcode(int wireOpcode)
		{
			if (!this.IsActive)
				return wireOpcode & 0xFFFF;

			var mask = (int)((_state >> 16) & 0x7FFF);
			var decoded = (wireOpcode ^ mask) & 0xFFFF;

			// Always advance once for this packet. On the very
			// first packet rAthena advances a second time inside
			// the WantToConnection handler (line 10723). Replicate
			// that quirk so the per-packet stream stays in sync.
			_state = unchecked((_state * _keys.Key2) + _keys.Key3);

			if (_firstPacket)
			{
				_state = unchecked((_state * _keys.Key2) + _keys.Key3);
				_firstPacket = false;
			}

			return decoded;
		}
	}
}
