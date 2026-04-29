using System;
using System.Threading;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.Scripting.Dialogues;
using Sabine.Zone.World.Actors.Components.Characters;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// Represents a non-player character.
	/// </summary>
	public class Npc : Character
	{
		private static int HandlePool = 400_000_000;

		/// <summary>
		/// Returns the NPC's unique handle.
		/// </summary>
		public override int Handle { get; protected set; }

		/// <summary>
		/// Returns the NPC's class id, defining its look.
		/// </summary>
		public override IdentityId IdentityId { get; protected set; }

		/// <summary>
		/// Gets or sets the function called when a dialog with this
		/// NPC is initiated.
		/// </summary>
		public DialogFunc DialogFunc { get; set; }

		/// <summary>
		/// Gets or sets the NPC's trigger area.
		/// </summary>
		/// <remarks>
		/// The trigger area is a rectangle centered on the NPC. If it's
		/// not empty (meaning both width and height are greater than 0),
		/// the server will check for characters entering and leaving the
		/// area and call the NPC's appropriate functions.
		/// </remarks>
		public TriggerArea TriggerArea { get; set; }

		/// <summary>
		/// Creates new NPC.
		/// </summary>
		/// <param name="classId"></param>
		public Npc(IdentityId identityId)
		{
			this.Handle = GetNewHandle();
			this.IdentityId = identityId;

			this.Parameters = new NpcParameters(this);

			this.Direction = Direction.South;
			this.Parameters.Speed = 400;
		}

		/// <summary>
		/// Returns a new handle.
		/// </summary>
		/// <returns></returns>
		private static int GetNewHandle()
			=> Interlocked.Increment(ref HandlePool);

		/// <summary>
		/// Warps NPC to the given position.
		/// </summary>
		/// <param name="location"></param>
		/// <exception cref="ArgumentException"></exception>
		public override void Warp(Location location)
		{
			if (!ZoneServer.Instance.World.Maps.TryGet(location.MapId, out var newMap))
				throw new ArgumentException($"Map '{location.MapId}' not found.");

			var curMap = this.Map;

			this.MapId = location.MapId;
			this.Position = location.Position;

			curMap.RemoveNpc(this);
			newMap.AddNpc(this);
		}

		/// <summary>
		/// Returns true if the NPC is currently visible to players and
		/// can be interacted with. Toggled by <see cref="Hide"/> and
		/// <see cref="Show"/>; mirrors rAthena's <c>enablenpc</c>/
		/// <c>disablenpc</c>.
		/// </summary>
		public bool Visible { get; private set; } = true;

		/// <summary>
		/// Hides the NPC from all players on its map. Subsequent click/
		/// touch interactions are ignored until <see cref="Show"/> is
		/// called. Mirrors rAthena's <c>disablenpc</c>.
		/// </summary>
		public void Hide()
		{
			if (!this.Visible)
				return;
			this.Visible = false;

			if (this.Map != null && this.Map != Sabine.Zone.World.Maps.Map.Limbo)
				Send.ZC_NOTIFY_VANISH(this, DisappearType.Vanish);
		}

		/// <summary>
		/// Re-shows a previously hidden NPC. Mirrors rAthena's
		/// <c>enablenpc</c>.
		/// </summary>
		public void Show()
		{
			if (this.Visible)
				return;
			this.Visible = true;

			if (this.Map != null && this.Map != Sabine.Zone.World.Maps.Map.Limbo)
				Send.ZC_NOTIFY_STANDENTRY_NPC(this);
		}

		/// <summary>
		/// Per-player cloak toggle. Stub: alpha-era clients do not have
		/// a per-recipient cloak packet, so this is a no-op + debug log.
		/// </summary>
		public void SetCloaked(PlayerCharacter player, bool cloaked)
		{
			Log.Debug("Npc.SetCloaked: stubbed for alpha client (npc='{0}', player='{1}', cloaked={2}).", this.Name, player?.Name, cloaked);
		}

		/// <summary>
		/// Sets the quest indicator displayed above this NPC. Stub:
		/// pre-renewal clients do not support quest icon packets.
		/// State is cached on the NPC for future use.
		/// </summary>
		public QuestState QuestIcon { get; private set; }

		/// <summary>
		/// Sets the quest indicator displayed above this NPC. See
		/// <see cref="QuestIcon"/>. Stub for the alpha client.
		/// </summary>
		public void SetQuestIcon(QuestState state)
		{
			this.QuestIcon = state;
			Log.Debug("Npc.SetQuestIcon: stubbed for alpha client (npc='{0}', state={1}).", this.Name, state);
		}

		/// <summary>
		/// Returns metadata about this NPC. Field selector mirrors
		/// rAthena's <c>strnpcinfo</c>:
		/// 0 = name, 1 = visible name (alias), 2 = map id (string),
		/// 3 = x, 4 = y, 5 = direction.
		/// </summary>
		public string GetInfo(int field)
		{
			return field switch
			{
				0 => this.Name ?? string.Empty,
				1 => this.Name ?? string.Empty,
				2 => this.Map?.StringId ?? string.Empty,
				3 => this.Position.X.ToString(),
				4 => this.Position.Y.ToString(),
				5 => ((int)this.Direction).ToString(),
				_ => string.Empty,
			};
		}

		private NpcTimer _timer;

		/// <summary>
		/// Returns the NPC's timer, used by converted scripts to drive
		/// <c>OnTimer&lt;ms&gt;</c>-style callbacks. The timer is created
		/// on first access; <c>initnpctimer</c> maps to <c>Timer.Start()</c>
		/// and <c>stopnpctimer</c> to <c>Timer.Stop()</c>.
		/// </summary>
		public NpcTimer Timer => _timer ??= new NpcTimer(this);
	}

	/// <summary>
	/// Drives <c>OnTimer&lt;ms&gt;</c>-style callbacks on an NPC. Tracks
	/// elapsed milliseconds since the last <see cref="Start"/> and fires
	/// <see cref="Tick"/> on a configurable interval.
	/// </summary>
	public class NpcTimer
	{
		private readonly Npc _owner;
		private Timer _timer;
		private DateTime _startedAt;

		/// <summary>
		/// Returns the milliseconds elapsed since the last
		/// <see cref="Start"/>, or 0 if the timer is stopped.
		/// </summary>
		public int Elapsed => this.IsRunning ? (int)(DateTime.Now - _startedAt).TotalMilliseconds : 0;

		/// <summary>True while the timer is running.</summary>
		public bool IsRunning { get; private set; }

		/// <summary>
		/// Fired on each tick. Argument is the elapsed milliseconds
		/// since <see cref="Start"/> was called.
		/// </summary>
		public Action<int> Tick;

		/// <summary>
		/// Tick interval. Defaults to 100ms to match rAthena's npc
		/// timer resolution closely enough for label dispatch.
		/// </summary>
		public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100);

		internal NpcTimer(Npc owner)
		{
			_owner = owner;
		}

		/// <summary>
		/// Starts (or restarts) the timer. Mirrors <c>initnpctimer</c>.
		/// </summary>
		public void Start()
		{
			this.Stop();
			_startedAt = DateTime.Now;
			this.IsRunning = true;
			_timer = new Timer(this.OnTick, null, this.Interval, this.Interval);
		}

		/// <summary>
		/// Stops the timer. Mirrors <c>stopnpctimer</c>.
		/// </summary>
		public void Stop()
		{
			if (_timer != null)
			{
				_timer.Dispose();
				_timer = null;
			}
			this.IsRunning = false;
		}

		private void OnTick(object? _)
		{
			try
			{
				this.Tick?.Invoke(this.Elapsed);
			}
			catch (Exception ex)
			{
				Log.Error("NpcTimer: tick handler for '{0}' threw: {1}", _owner.Name, ex);
			}
		}
	}

	/// <summary>
	/// Defines a rectangular area around a character that can trigger
	/// functions when other character enter or leave it.
	/// </summary>
	/// <remarks>
	/// The trigger area is a rectangle centered on the character. With a
	/// range of 0 on both axes, a character will trigger the area when
	/// they step on the same tile as the owner. With a range of 1, the
	/// area will also trigger when a character is on any of the 8 tiles
	/// surrounding the owner. With a range of 2, it will trigger on 2
	/// tiles in all directions from the owner, and so on.
	/// </remarks>
	/// <param name="owner">The owner of the area, on which the area is centered.</param>
	/// <param name="rangeX">The horizontal range of the trigger area.</param>
	/// <param name="rangeY">The vertical range of the trigger area.</param>
	public class TriggerArea(Character owner, int rangeX, int rangeY)
	{
		/// <summary>
		/// Returns the character this trigger area belongs to.
		/// </summary>
		public Character Owner { get; } = owner;

		/// <summary>
		/// Gets or sets the width of the trigger area.
		/// </summary>
		public int RangeX { get; set; } = rangeX;

		/// <summary>
		/// Gets or sets the height of the trigger area.
		/// </summary>
		public int RangeY { get; set; } = rangeY;

		/// <summary>
		/// Gets or sets a function called when a character enters the
		/// trigger area.
		/// </summary>
		public TriggerFunc Enter { get; set; }

		/// <summary>
		/// Gets or sets a function called when a character leaves the
		/// trigger area.
		/// </summary>
		public TriggerFunc Exit { get; set; }

		/// <summary>
		/// Returns true if the given position is inside the trigger area.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool Contains(Position pos)
		{
			var ownerPos = this.Owner.Position;
			var rangeX = this.RangeX;
			var rangeY = this.RangeY;

			if (pos.X < ownerPos.X - rangeX || pos.X > ownerPos.X + rangeX)
				return false;

			if (pos.Y < ownerPos.Y - rangeY || pos.Y > ownerPos.Y + rangeY)
				return false;

			return true;
		}
	}

	/// <summary>
	/// Represents a method that handles a trigger event involving two
	/// characters.
	/// </summary>
	/// <param name="character">The character that triggered the event.</param>
	/// <param name="other">The owner of the trigger area.</param>
	public delegate void TriggerFunc(Character character, Character other);
}
