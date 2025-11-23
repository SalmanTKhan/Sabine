using System;
using System.Threading;
using System.Threading.Tasks;
using Sabine.Shared.Const;
// ... existing imports

namespace Sabine.Zone.World.Entities
{
	public partial class PlayerCharacter : Character
	{
		private CancellationTokenSource _castCts;

		/// <summary>
		/// Returns true if the character is currently casting a skill.
		/// </summary>
		public override bool IsCasting => _castCts != null;

		/// <summary>
		/// Stops the current cast if one is in progress.
		/// </summary>
		public void StopCasting()
		{
			if (_castCts != null)
			{
				_castCts.Cancel();
				_castCts.Dispose();
				_castCts = null;

				// Determine what packet to send to visually cancel the cast on client
				// Usually changing sprite or sending a specific packet handles this
				this.ChangeLook(SpriteType.Base, this.DisplayClassId);
			}
		}

		/// <summary>
		/// Cancels all current actions: Movement, Attacking, and Casting.
		/// </summary>
		public void CancelAction()
		{
			this.Controller.StopMove();
			this.CancelAttack();
			this.StopCasting();
		}

		/// <summary>
		/// Registers a cancellation token for a new cast action.
		/// </summary>
		public CancellationToken RegisterCast()
		{
			this.StopCasting(); // Stop previous cast if any
			_castCts = new CancellationTokenSource();
			return _castCts.Token;
		}

		/// <summary>
		/// Clears the casting state (called when cast completes successfully).
		/// </summary>
		public void FinishCasting()
		{
			_castCts?.Dispose();
			_castCts = null;
		}
	}
}
