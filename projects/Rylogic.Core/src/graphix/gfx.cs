//***************************************************
// Colour128
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System.Drawing;

namespace Rylogic.Graphix
{
	/// <summary>Static functions related to graphics</summary>
	public static class Gfx
	{
		/// <summary>Linearly interpolate two colours</summary>
		public static Color Blend(Color c0, Color c1, float t)
		{
			return Color.FromArgb(
				(int)(c0.A*(1f - t) + c1.A*t),
				(int)(c0.R*(1f - t) + c1.R*t),
				(int)(c0.G*(1f - t) + c1.G*t),
				(int)(c0.B*(1f - t) + c1.B*t));
		}
	}
}
