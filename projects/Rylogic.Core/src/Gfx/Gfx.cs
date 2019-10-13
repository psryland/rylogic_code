//***************************************************
// Colour128
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.Globalization;
using Rylogic.Extn;

namespace Rylogic.Gfx
{
	/// <summary>Static functions related to graphics</summary>
	public static class Gfx_
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

		/// <summary>Add XML serialisation support for graphics types</summary>
		public static XmlConfig SupportRylogicGraphicsTypes(this XmlConfig cfg)
		{
			Xml_.ToMap[typeof(Colour32)] = (obj, node) =>
			{
				var col = ((Colour32)obj).ARGB.ToString("X8");
				node.SetValue(col);
				return node;
			};
			Xml_.AsMap[typeof(Colour32)] = (elem, type, ctor) =>
			{
				return new Colour32(uint.Parse(elem.Value, NumberStyles.HexNumber));
			};

			return cfg;
		}
	}
}
