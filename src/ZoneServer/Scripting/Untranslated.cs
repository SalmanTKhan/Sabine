namespace Sabine.Zone.Scripting
{
	/// <summary>
	/// Runtime sentinels that the converter
	/// (<c>SabineTools/convert_rathena.py</c>) emits in place of
	/// inline `0` / `""` constants when an eAthena builtin or
	/// expression hasn't been mapped yet.
	/// </summary>
	/// <remarks>
	/// The earlier inline-constant approach (`0 /* UNTRANSLATED... */`)
	/// produced reams of "unreachable code detected" warnings:
	/// `if (0 != 0)` and `(0 == 0) || …` are constant-foldable, and
	/// the C# compiler proves the body unreachable — noisy when
	/// you're scanning the boot log for real problems. Routing
	/// through a non-inlinable static method keeps the value 0/""
	/// at runtime but hides the constant from the compiler so the
	/// surrounding `if`/`||`/`&&` paths aren't proven dead.
	/// </remarks>
	public static class Untranslated
	{
		/// <summary>Untranslated int placeholder — returns 0 at
		/// runtime. The <paramref name="hint"/> is the original
		/// eAthena snippet, captured for future-you.</summary>
		public static int Int(string hint)
		{
			_ = hint;
			return 0;
		}

		/// <summary>Untranslated string placeholder — returns ""
		/// at runtime.</summary>
		public static string Str(string hint)
		{
			_ = hint;
			return string.Empty;
		}
	}
}
