//***********************************************
// LineDrawer helper
//  Copyright © Rylogic Ltd 2008
//***********************************************

using pr.maths;

namespace pr.ldr
{
	public static class Ldr
	{
		public static bool m_enable;

		public static void Write(string ldr_str, string filepath)
		{
		    using( System.IO.StreamWriter sw = System.IO.File.CreateText(filepath) )
				sw.Write(ldr_str);
		}
		public static string GroupStart(string name)
		{
			return "*Group " + name + "\n{\n";
		}
		public static string GroupStart(string name, uint colour)
		{
			return "*Group " + name + " " + colour.ToString("X") + "\n{\n";
		}
		public static string GroupEnd()
		{
			return "}\n";
		}
		public static string Vec3(v4 vec)
		{
			return "" + vec.x + " " + vec.y + " " + vec.z;
		}
		public static string Vec4(v4 vec)
		{
			return "" + vec.x + " " + vec.y + " " + vec.z + " " + vec.w;
		}
		public static string Mat4x4(m4x4 mat)
		{
			return "" + Vec4(mat.x) + " " + Vec4(mat.y) + " " + Vec4(mat.z) + " " + Vec4(mat.w);
		}
		public static string Position(v4 position)
		{
			if (position == v4.Zero || position == v4.Origin) return "";
			return "*o2w{*pos{" + Vec3(position) + "}}";
		}
		public static string Transform(m4x4 o2w)
		{
			if (o2w == m4x4.Identity) return "";
			return "*o2w{*m4x4{" + Mat4x4(o2w) + "}}";
		}
		public static string Line(string name, uint colour, v4 start, v4 end)
		{
			return "*Line " + name + " " + colour.ToString("X") + " {" + Vec3(start) + " " + Vec3(end) + "}\n";
		}
		public static string LineD(string name, uint colour, v4 start, v4 direction)
		{
			return "*LineD " + name + " " + colour.ToString("X") + " {" + Vec3(start) + " " + Vec3(direction) + "}\n";
		}
		public static string Ellipse(string name, uint colour, int axis_id, float rx, float ry, v4 position)
		{
			return "*Ellipse " + name + " " + colour.ToString("X") + " {" + axis_id + " " + rx + " " + ry + " " + Position(position) + "}\n";
		}
		public static string Box(string name, uint colour, v4 position, float size)
		{
			return "*Box " + name + " " + colour.ToString("X") + " {" + size + " " + Position(position) + "}\n";
		}
		public static string Box(string name, uint colour, v4 position, v4 dim)
		{
			return "*Box" + name + " " + colour.ToString("X") + " {" + dim.x + " " + dim.y + " " + dim.z + " " + Position(position) + "}\n";
		}
		public static string Sphere(string name, uint colour, float radius)
		{
			return Sphere(name, colour, radius, v4.Origin);
		}
		public static string Sphere(string name, uint colour, float radius, v4 position)
		{
			return "*Sphere " + name + " " + colour.ToString("X") + " {" + radius + " " + Position(position) + "}\n";
		}
		public static string Grid(string name, uint colour, float dimx, float dimy, int divx, int divy, v4 position)
		{
			return "*GridWH " + name + " " + colour.ToString("X") + " {" + dimx + " " + dimy + " " + divx + " " + divy + " " + Position(position) + "}\n";
		}
	}
}
