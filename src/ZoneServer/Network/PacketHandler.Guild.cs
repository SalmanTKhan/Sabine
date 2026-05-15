using Sabine.Shared.Network;
using Sabine.Shared.Util;
using Sabine.Zone.World;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Groups;
using Yggdrasil.Logging;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Core guild flow handlers: create / invite / accept-reject / leave
	/// / kick / disband / chat / notice / menu. Senders for the
	/// corresponding ZC_* responses already live in Send.cs from earlier
	/// work; these handlers wire the state mutation through
	/// <see cref="GuildManager"/> and emit the right responses.
	/// </summary>
	/// <remarks>
	/// Out of scope for this commit: emblem upload
	/// (CZ_REGISTER_GUILD_EMBLEM_IMG), ally / war declarations, position
	/// editing (CZ_REG_CHANGE_GUILD_POSITIONINFO /
	/// CZ_REQ_CHANGE_MEMBERPOS). Those involve additional UI surfaces
	/// that haven't shown up in client traces yet.
	/// </remarks>
	public partial class PacketHandler
	{
		// rA clif_guild_inviteack result codes (mmo.h GUILD_*).
		private const byte GUILD_INVITE_REJECTED = 1;
		private const byte GUILD_INVITE_ACCEPTED = 2;
		private const byte GUILD_INVITE_FULL = 3;

		// rA clif_guild_created result codes.
		private const byte GUILD_CREATE_OK = 0;
		private const byte GUILD_CREATE_NAME_TAKEN = 1;
		private const byte GUILD_CREATE_ALREADY_IN_GUILD = 3;

		// Vars.Temp pin so a tampered CZ_JOIN_GUILD can't be redirected
		// to a guild the invitee never got a popup for.
		private const string PendingInviteKey = "Sabine.GuildInviteFrom";

		/// <summary>
		/// CZ_REQ_MAKE_GUILD. Wire: 30 bytes. Body: u32 charId + char[24] name.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_MAKE_GUILD)]
		public void CZ_REQ_MAKE_GUILD(ZoneConnection conn, Packet packet)
		{
			var charIdEcho = packet.GetInt(); // unused; trust server-side identity
			var name = packet.GetString(24);

			var character = conn.GetCurrentCharacter();
			_ = charIdEcho; // discard

			if (character.Guild != null)
			{
				Send.ZC_RESULT_MAKE_GUILD(character, GUILD_CREATE_ALREADY_IN_GUILD);
				return;
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				Send.ZC_RESULT_MAKE_GUILD(character, GUILD_CREATE_NAME_TAKEN);
				return;
			}

			var guild = GuildManager.Instance.Create(character, name);
			if (guild == null)
			{
				Send.ZC_RESULT_MAKE_GUILD(character, GUILD_CREATE_NAME_TAKEN);
				return;
			}

			Send.ZC_RESULT_MAKE_GUILD(character, GUILD_CREATE_OK);
			Log.Info("Char '{0}' created guild '{1}' (id {2}).", character.Name, name, guild.Id);
		}

		/// <summary>
		/// CZ_REQ_JOIN_GUILD. Wire: 14 bytes. Body: u32 targetAID +
		/// u32 inviterAID + u32 inviterCID.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_JOIN_GUILD)]
		public void CZ_REQ_JOIN_GUILD(ZoneConnection conn, Packet packet)
		{
			var targetAccountId = packet.GetInt();
			var inviterAccountId = packet.GetInt();
			var inviterCharId = packet.GetInt();
			_ = inviterAccountId; _ = inviterCharId;

			var inviter = conn.GetCurrentCharacter();
			var guild = inviter.Guild;
			if (guild == null) return;
			if (inviter.Id != guild.MasterCharId) return;

			var target = ResolveByAccountId(targetAccountId);
			if (target == null) return;
			if (target.Guild != null)
			{
				Send.ZC_ACK_REQ_JOIN_GUILD(inviter, 0); // already in guild
				return;
			}

			target.Vars.Temp.SetInt(PendingInviteKey, guild.Id);
			Send.ZC_REQ_JOIN_GUILD(target, guild.Id, guild.Name);
		}

		/// <summary>
		/// CZ_JOIN_GUILD. Wire: 10 bytes. Body: u32 guildId + u32 answer
		/// (1 = accept, 0 = reject).
		/// </summary>
		[PacketHandler(Op.CZ_JOIN_GUILD)]
		public void CZ_JOIN_GUILD(ZoneConnection conn, Packet packet)
		{
			var guildIdEcho = packet.GetInt();
			var answer = packet.GetInt();

			var target = conn.GetCurrentCharacter();

			var pendingGuildId = target.Vars.Temp.GetInt(PendingInviteKey, 0);
			target.Vars.Temp.SetInt(PendingInviteKey, 0);

			if (pendingGuildId == 0 || pendingGuildId != guildIdEcho)
				return;

			var guild = GuildManager.Instance.Get(pendingGuildId);
			if (guild == null) return;

			var master = FriendsService.FindOnline(guild.MasterCharId);

			if (answer != 1)
			{
				if (master != null)
					Send.ZC_ACK_REQ_JOIN_GUILD(master, GUILD_INVITE_REJECTED);
				return;
			}

			if (target.Guild != null)
			{
				if (master != null)
					Send.ZC_ACK_REQ_JOIN_GUILD(master, 0); // already in guild
				return;
			}

			if (!GuildManager.Instance.AddMember(guild.Id, target))
			{
				if (master != null)
					Send.ZC_ACK_REQ_JOIN_GUILD(master, GUILD_INVITE_FULL);
				return;
			}

			if (master != null)
				Send.ZC_ACK_REQ_JOIN_GUILD(master, GUILD_INVITE_ACCEPTED);
		}

		/// <summary>
		/// CZ_REQ_LEAVE_GUILD. Wire: 54 bytes. Body: u32 guildId +
		/// u32 accountId + u32 charId + char[40] reason.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_LEAVE_GUILD)]
		public void CZ_REQ_LEAVE_GUILD(ZoneConnection conn, Packet packet)
		{
			var guildId = packet.GetInt();
			var accountId = packet.GetInt();
			var charId = packet.GetInt();
			var reason = packet.GetString(40);
			_ = accountId;

			var character = conn.GetCurrentCharacter();
			var guild = character.Guild;
			if (guild == null || guild.Id != guildId) return;
			if (character.Id != charId) return;
			if (character.Id == guild.MasterCharId)
			{
				character.ServerMessage(Localization.Get("Guild master cannot leave; disband or transfer first."));
				return;
			}

			GuildManager.Instance.RemoveMember(guildId, character.Id);
			Send.ZC_ACK_LEAVE_GUILD(character, character.Name, reason);
		}

		/// <summary>
		/// CZ_REQ_BAN_GUILD (kick). Wire: 54 bytes. Body: u32 guildId +
		/// u32 accountId (target) + u32 charId (target) + char[40] reason.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_BAN_GUILD)]
		public void CZ_REQ_BAN_GUILD(ZoneConnection conn, Packet packet)
		{
			var guildId = packet.GetInt();
			var targetAccountId = packet.GetInt();
			var targetCharId = packet.GetInt();
			var reason = packet.GetString(40);
			_ = targetAccountId;

			var character = conn.GetCurrentCharacter();
			var guild = character.Guild;
			if (guild == null || guild.Id != guildId) return;
			if (character.Id != guild.MasterCharId) return;
			if (targetCharId == guild.MasterCharId) return;

			var targetMember = guild.GetMember(targetCharId);
			if (targetMember == null) return;
			var targetName = targetMember.Name;

			if (!GuildManager.Instance.RemoveMember(guildId, targetCharId))
				return;

			// Notify the kicker and (if online) the kickee.
			Send.ZC_ACK_LEAVE_GUILD(character, targetName ?? string.Empty, reason);
			var kicked = FriendsService.FindOnline(targetCharId);
			if (kicked != null)
				Send.ZC_ACK_LEAVE_GUILD(kicked, targetName ?? string.Empty, reason);
		}

		/// <summary>
		/// CZ_REQ_DISORGANIZE_GUILD. Wire: 42 bytes. Body: char[40] key
		/// (rA expects an exact match against the guild master's password
		/// confirmation; Sabine accepts any non-empty string today).
		/// </summary>
		[PacketHandler(Op.CZ_REQ_DISORGANIZE_GUILD)]
		public void CZ_REQ_DISORGANIZE_GUILD(ZoneConnection conn, Packet packet)
		{
			var key = packet.GetString(40);
			_ = key;

			var character = conn.GetCurrentCharacter();
			var guild = character.Guild;
			if (guild == null) return;
			if (character.Id != guild.MasterCharId) return;

			var members = guild.Members;
			GuildManager.Instance.Disband(guild.Id);

			// Notify every online member so their UI tears the guild down.
			foreach (var m in members)
			{
				var pc = FriendsService.FindOnline(m.CharacterId);
				if (pc != null)
					Send.ZC_ACK_DISORGANIZE_GUILD(pc, "Guild disbanded.");
			}
		}

		/// <summary>
		/// CZ_GUILD_CHAT. Variable wire. Body: u16 packetLen +
		/// char[N] message. Sabine broadcasts the message to every online
		/// member of the sender's guild.
		/// </summary>
		[PacketHandler(Op.CZ_GUILD_CHAT)]
		public void CZ_GUILD_CHAT(ZoneConnection conn, Packet packet)
		{
			// Variable-length chat packet: u16 packetLen + char[N] message.
			// Mirrors the CZ_REQUEST_CHAT read shape so callers like
			// open-midgard's PV23 path drop straight in.
			var len = packet.GetShort();
			var raw = packet.GetString(len - sizeof(short) * 2);

			var character = conn.GetCurrentCharacter();
			var guild = character.Guild;
			if (guild == null) return;
			if (string.IsNullOrEmpty(raw)) return;

			// Strip the client-supplied "Name : " prefix and reapply the
			// server-trusted name so the line can't be spoofed.
			var idx = raw.IndexOf(':');
			var body = idx != -1 ? raw.Substring(idx + 1).Trim() : raw.Trim();
			if (body.Length == 0) return;

			var line = character.Name + " : " + body;
			foreach (var m in guild.Members)
			{
				var pc = FriendsService.FindOnline(m.CharacterId);
				if (pc != null)
					Send.ZC_GUILD_CHAT(pc, line);
			}
		}

		/// <summary>
		/// CZ_GUILD_NOTICE. Wire: 186 bytes. Body: u32 guildId +
		/// char[60] subject + char[120] notice. Master-only; persists the
		/// new notice and broadcasts to every online member.
		/// </summary>
		[PacketHandler(Op.CZ_GUILD_NOTICE)]
		public void CZ_GUILD_NOTICE(ZoneConnection conn, Packet packet)
		{
			var guildId = packet.GetInt();
			var subject = packet.GetString(60);
			var notice = packet.GetString(120);
			_ = subject;

			var character = conn.GetCurrentCharacter();
			var guild = character.Guild;
			if (guild == null || guild.Id != guildId) return;
			if (character.Id != guild.MasterCharId) return;

			guild.Notice = notice ?? string.Empty;
			GuildManager.Instance.Save(guild);

			foreach (var m in guild.Members)
			{
				var pc = FriendsService.FindOnline(m.CharacterId);
				if (pc != null)
					Send.ZC_GUILD_NOTICE(pc, guild.Name, guild.Notice);
			}
		}

		/// <summary>
		/// CZ_REQ_GUILD_MENUINTERFACE. Wire: 2 bytes (opcode only).
		/// Client asks which guild-menu buttons should be enabled.
		/// Server replies with a bitfield via ZC_ACK_GUILD_MENUINTERFACE.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_GUILD_MENUINTERFACE)]
		public void CZ_REQ_GUILD_MENUINTERFACE(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var guild = character.Guild;

			// Bitfield matching rA mmo.h GD_ACT_* slots: 0x01=basic info,
			// 0x02=member list, 0x04=position, 0x08=skill, 0x10=alliance,
			// 0x20=notice, 0x40=expel/leave. Masters get everything; plain
			// members get the read-only subset.
			int flag;
			if (guild == null)
				flag = 0;
			else if (character.Id == guild.MasterCharId)
				flag = 0x7F;
			else
				flag = 0x0F;

			Send.ZC_ACK_GUILD_MENUINTERFACE(character, flag);
		}

		/// <summary>
		/// Resolves an online player by account id by walking every map's
		/// player roster. Acceptable while populations stay alpha-era;
		/// promote to an inverted index when invite churn matters.
		/// </summary>
		private static PlayerCharacter ResolveByAccountId(int accountId)
		{
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.TryGetPlayerByAccountId(accountId, out var pc))
					return pc;
			}
			return null;
		}
	}
}
