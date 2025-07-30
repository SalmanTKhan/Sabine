namespace Sabine.Shared.Const
{
	/// <summary>
	/// Specifies a part of a sprite to do something to.
	/// </summary>
	public enum SpriteType : short
	{
		Base = 0,
		Hair = 1,
		Weapon = 2,
		HeadBottom = 3,
		HeadTop = 4,
		HeadMid = 5,
		HairColor = 6,
		ClothesColor = 7,
		Shield = 8,
		Shoes = 9,
		Body = 10,
		Robe = 12,
		Body2 = 13,

		// Old client compatibility alias
		Class = Base,
	}
}
