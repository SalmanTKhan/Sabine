namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Centralized subscription of combat-pipeline listeners.
	/// Called once at server startup; each listener registers its
	/// own callbacks against CombatEvents.
	/// </summary>
	public static class CombatListenerInit
	{
		public static void Initialize()
		{
			AutoGuardListener.Subscribe();
			DevotionListener.Subscribe();
			ReflectShieldListener.Subscribe();
			AutoSpellListener.Subscribe();
			PlagiarismListener.Subscribe();
			TrickDeadListener.Subscribe();
			BladeStopListener.Subscribe();
		}
	}
}
