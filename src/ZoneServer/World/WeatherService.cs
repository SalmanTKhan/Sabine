using System;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Server-wide weather and day/night state. Commands flip the value
	/// here; the broadcast effect packet (when supported by the client)
	/// is sent by the command handler.
	/// </summary>
	public enum WeatherKind
	{
		None,
		Snow,
		Sakura,
		Clouds,
		Clouds2,
		Fog,
		Fireworks,
		Leaves,
	}

	public class WeatherService
	{
		public static WeatherService Instance { get; } = new WeatherService();

		public WeatherKind Current { get; set; } = WeatherKind.None;
		public bool IsNight { get; set; }
	}
}
