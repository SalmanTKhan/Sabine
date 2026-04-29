using Sabine.Shared.Const;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Attack-element vs defense-element multiplier table. Mirrors
	/// eAthena's <c>db/attr_fix.txt</c> at all four defense-element
	/// levels. Storage layout: <c>[defElement, attackElement, defLevel-1]</c>.
	/// </summary>
	internal static class ElementTable
	{
		// eAthena classic db/attr_fix.txt
		// Rows = defense element, columns = attack element.
		// Sabine ElementType order matches eAthena:
		//   Neutral, Water, Earth, Fire, Wind, Poison, Holy, Dark(Shadow), Ghost, Undead
		private static readonly int[,,] _modifier = BuildTable();

		private static int[,,] BuildTable()
		{
			// Levels 1..4. Each level holds 10 def-rows × 10 atk-cols.
			var t = new int[10, 10, 4];

			// Lv 1
			int[,] lv1 = new int[10, 10]
			{
				/* Neu */ { 100, 100, 100, 100, 100, 100, 100, 100,  25, 100 },
				/* Wat */ { 100,  25, 100, 150,  50, 100,  75, 100, 100, 100 },
				/* Ear */ { 100, 100, 100,  50, 150, 100,  75, 100, 100, 100 },
				/* Fir */ { 100,  50, 150,  25, 100, 100,  75, 100, 100, 125 },
				/* Win */ { 100, 175,  50, 100,  25, 100,  75, 100, 100, 100 },
				/* Poi */ { 100, 100, 125, 125, 125,   0,  75,  50, 100, -25 },
				/* Hol */ { 100, 100, 100, 100, 100, 100,   0, 125, 100, 150 },
				/* Dar */ { 100, 100, 100, 100, 100,  50, 125,   0, 100, -25 },
				/* Gho */ {  25, 100, 100, 100, 100, 100,  75,  75, 125, 100 },
				/* Und */ { 100, 100, 100, 100, 100,  50, 100,   0, 100,   0 },
			};
			// Lv 2
			int[,] lv2 = new int[10, 10]
			{
				{ 100, 100, 100, 100, 100, 100, 100, 100,  25, 100 },
				{ 100,   0, 100, 175,  25, 100,  50,  75, 100, 100 },
				{ 100, 100,  50,  25, 175, 100,  50,  75, 100, 100 },
				{ 100,  25, 175,   0, 100, 100,  50,  75, 100, 150 },
				{ 100, 175,  25, 100,   0, 100,  50,  75, 100, 100 },
				{ 100,  75, 125, 125, 125,   0,  50,  25,  75, -50 },
				{ 100, 100, 100, 100, 100, 100, -25, 150, 100, 175 },
				{ 100, 100, 100, 100, 100,  25, 150, -25, 100, -50 },
				{   0,  75,  75,  75,  75,  75,  50,  50, 150, 125 },
				{ 100,  75,  75,  75,  75,  25, 125,   0, 100,   0 },
			};
			// Lv 3
			int[,] lv3 = new int[10, 10]
			{
				{ 100, 100, 100, 100, 100, 100, 100, 100,   0, 100 },
				{ 100, -25, 100, 200,   0, 100,  25,  50, 100, 125 },
				{ 100, 100,   0,   0, 200, 100,  25,  50, 100,  75 },
				{ 100,   0, 200, -25, 100, 100,  25,  50, 100, 175 },
				{ 100, 200,   0, 100, -25, 100,  25,  50, 100, 100 },
				{ 100,  50, 100, 100, 100,   0,  25,   0,  50, -75 },
				{ 100, 100, 100, 100, 100, 125, -50, 175, 100, 200 },
				{ 100, 100, 100, 100, 100,   0, 175, -50, 100, -75 },
				{   0,  50,  50,  50,  50,  50,  25,  25, 175, 150 },
				{ 100,  50,  50,  50,  50,   0, 150,   0, 100,   0 },
			};
			// Lv 4
			int[,] lv4 = new int[10, 10]
			{
				{ 100, 100, 100, 100, 100, 100, 100, 100,   0, 100 },
				{ 100, -50, 100, 200,   0,  75,   0,  25, 100, 150 },
				{ 100, 100, -25,   0, 200,  75,   0,  25, 100,  50 },
				{ 100,   0, 200, -50, 100,  75,   0,  25, 100, 200 },
				{ 100, 200,   0, 100, -50,  75,   0,  25, 100, 100 },
				{ 100,  25,  75,  75,  75,   0,   0, -25,  25,-100 },
				{ 100,  75,  75,  75,  75, 125,-100, 200, 100, 200 },
				{ 100,  75,  75,  75,  75, -25, 200,-100, 100,-100 },
				{   0,  25,  25,  25,  25,  25,   0,   0, 200, 175 },
				{ 100,  25,  25,  25,  25, -25, 175,   0, 100,   0 },
			};

			for (var d = 0; d < 10; d++)
			{
				for (var a = 0; a < 10; a++)
				{
					t[d, a, 0] = lv1[d, a];
					t[d, a, 1] = lv2[d, a];
					t[d, a, 2] = lv3[d, a];
					t[d, a, 3] = lv4[d, a];
				}
			}

			return t;
		}

		/// <summary>
		/// Returns the percent multiplier (100 = 1.0x) for an attack of
		/// <paramref name="attackElement"/> hitting a target of
		/// <paramref name="defenseElement"/> at the given defense level.
		/// </summary>
		public static int GetModifier(ElementType attackElement, ElementType defenseElement, int defenseLevel)
		{
			var a = (int)attackElement;
			var d = (int)defenseElement;

			if (a < 0 || a > 9 || d < 0 || d > 9)
				return 100;

			var lvl = defenseLevel < 1 ? 1 : (defenseLevel > 4 ? 4 : defenseLevel);
			return _modifier[d, a, lvl - 1];
		}

		/// <summary>
		/// Convenience overload that defaults to defense level 1.
		/// </summary>
		public static int GetModifier(ElementType attackElement, ElementType defenseElement)
			=> GetModifier(attackElement, defenseElement, 1);
	}
}
