using System;

namespace Sabine.Shared.Util
{
	/// <summary>
	/// Field selector for <see cref="Time.Get"/>. Mirrors rAthena's
	/// <c>gettime(n)</c> selector — the integer values of the legacy
	/// command map onto these names so scripts can be ported by name
	/// rather than by magic number.
	/// </summary>
	public enum TimeField
	{
		/// <summary>Seconds (0–59).</summary>
		Second = 1,

		/// <summary>Minutes (0–59).</summary>
		Minute = 2,

		/// <summary>Hour of day (0–23).</summary>
		Hour = 3,

		/// <summary>Day of week (Sunday = 0, Saturday = 6).</summary>
		DayOfWeek = 4,

		/// <summary>Day of month (1–31).</summary>
		Day = 5,

		/// <summary>Month of year (1–12).</summary>
		Month = 6,

		/// <summary>Full year, e.g. 2026.</summary>
		Year = 7,

		/// <summary>Day of year (1–366).</summary>
		DayOfYear = 8,
	}

	/// <summary>
	/// Script-facing accessor for current local-time fields.
	/// Replaces rAthena's <c>gettime(n)</c>.
	/// </summary>
	public static class Time
	{
		/// <summary>
		/// Returns the requested field of <see cref="DateTime.Now"/>.
		/// </summary>
		/// <param name="field">Field to read.</param>
		/// <returns>Integer value of the requested field.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If <paramref name="field"/> is not a known <see cref="TimeField"/>.
		/// </exception>
		public static int Get(TimeField field)
		{
			var now = DateTime.Now;
			return field switch
			{
				TimeField.Second => now.Second,
				TimeField.Minute => now.Minute,
				TimeField.Hour => now.Hour,
				TimeField.DayOfWeek => (int)now.DayOfWeek,
				TimeField.Day => now.Day,
				TimeField.Month => now.Month,
				TimeField.Year => now.Year,
				TimeField.DayOfYear => now.DayOfYear,
				_ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
			};
		}
	}
}
