using Rylogic.Gfx;

namespace BorderOfPeace.Model
{
	/// <summary>A named color preset</summary>
	public class ColorPreset
	{
		public string Name { get; set; } = string.Empty;

		/// <summary>Color stored as ARGB (0xAARRGGBB)</summary>
		public uint Argb { get; set; }

		public ColorPreset()
		{
		}
		public ColorPreset(string name, uint argb)
		{
			Name = name;
			Argb = argb;
		}
		public ColorPreset(string name, byte r, byte g, byte b)
		{
			Name = name;
			Argb = new Colour32(0xFF, r, g, b).ARGB;
		}

		/// <summary>Get as a Colour32</summary>
		public Colour32 Colour => new(Argb);

		/// <summary>Convert to COLORREF (0x00BBGGRR) for DWM APIs</summary>
		public uint ColorRef
		{
			get
			{
				var c = Colour;
				return (uint)((c.B << 16) | (c.G << 8) | c.R);
			}
		}
	}
}
