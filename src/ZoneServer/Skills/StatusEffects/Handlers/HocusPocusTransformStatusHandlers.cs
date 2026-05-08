using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SA_REVERSEORCISH — overrides the target's display name with
	/// "ORCISH" for the duration. eAthena status.c SC_ORCISH stores
	/// the override server-side and lets the client re-fetch via
	/// CZ_REQNAME; we kick the refresh by sending a vanish + re-entry
	/// pair so cached name labels invalidate. OnEnd repeats the kick
	/// after restoring the original.
	/// </summary>
	[StatusEffectHandler(StatusId.ReverseOrcish)]
	public class ReverseOrcishStatusHandler : IStatusEffectHandler
	{
		private const string OrcishName = "ORCISH";

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.OriginalName = target.Name;
			target.Name = OrcishName;

			RefreshNameLabels(target);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (!string.IsNullOrEmpty(effect.OriginalName))
				target.Name = effect.OriginalName;

			RefreshNameLabels(target);
		}

		/// <summary>
		/// Forces every nearby client to re-render the target by
		/// vanishing and re-spawning the entity. The client will
		/// re-issue CZ_REQNAME on the next mouse-over and pick up
		/// the new value.
		/// </summary>
		private static void RefreshNameLabels(Character target)
		{
			if (target.Map == null || target.Map == Sabine.Zone.World.Maps.Map.Limbo)
				return;

			Send.ZC_NOTIFY_VANISH(target, DisappearType.Vanish);
			Send.ZC_NOTIFY_NEWENTRY(target);
		}
	}

	/// <summary>
	/// SA_MONOCELL — disguises the target as a Poring (JT_PORING) for
	/// the status duration. eAthena skill.c SA_MONOCELL: applies
	/// SC_CHANGEUNDEAD-class sprite swap + drops HP to 1; the HP drop
	/// is handled by the skill handler before the status is started so
	/// the swap and damage frames stay in lockstep.
	/// </summary>
	[StatusEffectHandler(StatusId.Monocell)]
	public class MonocellStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			// Stash the previous DisplayClassId in Val2 so OnEnd can
			// restore the original sprite, even if the target was
			// already wearing a different override at start.
			effect.Val2 = target.DisplayClassId;
			target.DisplayClassId = (int)IdentityId.JT_PORING;
			BroadcastSprite(target, target.DisplayClassId);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.DisplayClassId = effect.Val2;
			BroadcastSprite(target, target.DisplayClassId);
		}

		internal static void BroadcastSprite(Character target, int classId)
		{
			// SpriteType.Class / SpriteType.Base both fold to slot 0 on
			// the alpha / beta1 wire format; pick the version-correct
			// packet variant the same way PlayerCharacter.ChangeJob
			// does.
			if (Game.Version < Versions.S500)
				Send.ZC_SPRITE_CHANGE(target, SpriteType.Class, classId);
			else
				Send.ZC_SPRITE_CHANGE2(target, SpriteType.Class, classId, 0);
		}
	}

	/// <summary>
	/// SA_CLASSCHANGE — disguises the target as a random low-tier mob
	/// for the duration. eAthena skill.c SA_CLASSCHANGE rolls from a
	/// fixed pool; we mirror the SummonMonster roster so the cosmetic
	/// reads as obviously "Hocus Pocus".
	/// </summary>
	[StatusEffectHandler(StatusId.ClassChange)]
	public class ClassChangeStatusHandler : IStatusEffectHandler
	{
		private static readonly IdentityId[] Roster = new[]
		{
			IdentityId.JT_PORING, IdentityId.JT_LUNATIC, IdentityId.JT_FABRE,
			IdentityId.JT_CHONCHON, IdentityId.JT_PUPA, IdentityId.JT_DROPS,
		};

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = target.DisplayClassId;

			var pick = Roster[RandomProvider.Get().Next(Roster.Length)];
			target.DisplayClassId = (int)pick;
			MonocellStatusHandler.BroadcastSprite(target, target.DisplayClassId);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.DisplayClassId = effect.Val2;
			MonocellStatusHandler.BroadcastSprite(target, target.DisplayClassId);
		}
	}
}
