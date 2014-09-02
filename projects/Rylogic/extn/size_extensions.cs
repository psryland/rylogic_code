//***************************************************
// Size Extensions
//  Copyright (c) Rylogic Ltd 2013
//***************************************************

using System.Drawing;

namespace pr.extn
{
	public static class SizeExtensions
	{
		/// <summary>Returns the area (width * height)</summary>
		public static float Area(this Size sz)
		{
			return (float)sz.Width * sz.Height;
		}

		/// <summary>Returns the aspect ratio (width/height)</summary>
		public static float Aspect(this Size sz)
		{
			return (float)sz.Width / sz.Height;
		}
	}
}
