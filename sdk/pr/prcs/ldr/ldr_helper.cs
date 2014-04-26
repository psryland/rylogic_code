//***********************************************
// LineDrawer helper
//  Copyright © Rylogic Ltd 2008
//***********************************************

using System.IO;
using System.Text;
using pr.maths;
using pr.util;

namespace pr.ldr
{
	public static class Ldr
	{
		public static bool m_enable;

		public static void Write(string ldr_str, string filepath)
		{
			using (var sw = File.CreateText(filepath))
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

	/// <summary>Like StringBuilder, but for ldr strings</summary>
	public class LdrBuilder
	{
		private readonly StringBuilder m_sb;

		public LdrBuilder()                                                       { m_sb = new StringBuilder();                                    }
		public LdrBuilder(int capacity)                                           { m_sb = new StringBuilder(capacity);                            }
		public LdrBuilder(string value)                                           { m_sb = new StringBuilder(value);                               }
		public LdrBuilder(int capacity, int maxCapacity)                          { m_sb = new StringBuilder(capacity, maxCapacity);               }
		public LdrBuilder(string value, int capacity)                             { m_sb = new StringBuilder(value, capacity);                     }
		public LdrBuilder(string value, int startIndex, int length, int capacity) { m_sb = new StringBuilder(value, startIndex, length, capacity); }

		public void Clear()
		{
			m_sb.Clear();
		}
		public void Remove(int start_index, int length)
		{
			m_sb.Remove(start_index, length);
		}
		public LdrBuilder Append(params object[] parts)
		{
			foreach (var p in parts) m_sb.Append(p.ToString());
			return this;
		}

		public Scope Group() { return Group(string.Empty); }
		public Scope Group(string name)
		{
			return new Scope(
				() => Append("*Group ",name," {\n"),
				() => Append("}\n")
				);
		}

		public void Box()                                          { Box(string.Empty, 0xFFFFFFFF); }
		public void Box(uint colour, float size)                   { Box(colour, size, v4.Origin); }
		public void Box(uint colour, float size, v4 position)      { Box(string.Empty, colour, size, position); }
		public void Box(string name, uint colour)                  { Box(name, colour, 1f); }
		public void Box(string name, uint colour, float size)      { Box(name, colour, size, v4.Origin); }
		public void Box(string name, uint colour, float size, v4 position)
		{
			Append("*Box ",name," ",colour.ToString("X")," {",size," ",Ldr.Position(position),"}\n");
		}
		public void Box(uint colour, float sx, float sy, float sz)              { Box(colour, sx, sy, sz, v4.Origin); }
		public void Box(uint colour, float sx, float sy, float sz, v4 position) { Box(string.Empty, colour, sx, sy, sz, position); }
		public void Box(string name, uint colour, float sx, float sy, float sz) { Box(name, colour, sx, sy, sz); }
		public void Box(string name, uint colour, float sx, float sy, float sz, v4 position)
		{
			Append("*Box ",name," ",colour.ToString("X")," {",sx,",",sy,",",sz," ",Ldr.Position(position),"}\n");
		}

		public void Sphere()                                       { Sphere(string.Empty, 0xFFFFFFFF); }
		public void Sphere(string name, uint colour)               { Sphere(name, colour, 1f); }
		public void Sphere(string name, uint colour, float radius) { Sphere(name, colour, radius, v4.Origin); }
		public void Sphere(string name, uint colour, float radius, v4 position)
		{
			Append("*Sphere ",name," ",colour.ToString("X")," {",radius," ",Ldr.Position(position),"}\n");
		}

		public void Quad()                                                     { Quad(string.Empty, 0xFFFFFFFF); }
		public void Quad(uint colour)                                          { Quad(string.Empty, colour); }
		public void Quad(uint colour, v4 tl, v4 tr, v4 br, v4 bl)              { Quad(string.Empty, colour, tl, tr, br, bl); }
		public void Quad(string name, uint colour)                             { Quad(name, colour, new v4(0,1,0,1), new v4(1,1,0,1), new v4(1,0,0,1), new v4(0,0,0,1)); }
		public void Quad(string name, uint colour, v4 tl, v4 tr, v4 br, v4 bl) { Quad(name, colour, tl, tr, br, bl, v4.Origin); }
		public void Quad(string name, uint colour, v4 tl, v4 tr, v4 br, v4 bl, v4 position)
		{
			Append("*Quad ",name," ",colour.ToString("X")," {",Ldr.Vec3(bl)," ",Ldr.Vec3(br)," ",Ldr.Vec3(tr)," ",Ldr.Vec3(tl)," ",Ldr.Position(position), "}\n");
		}

		public override string ToString()
		{
			return m_sb.ToString();
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using ldr;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestLdr
		{
			[Test] public static void LdrBuilder()
			{
				var ldr = new LdrBuilder();
				using (ldr.Group("g"))
				{
					ldr.Box("b", 0xFF00FF00);
					ldr.Sphere("s", 0xFFFF0000);
				}
				var expected =
					"*Group g {\n"+
					"*Box b FF00FF00 {1 1 1 *o2w{*pos{0 0 0}}}\n"+
					"*Sphere s FFFF0000 {1 *o2w{*pos{0 0 0}}}\n"+
					"}\n";
				Assert.AreEqual(expected, ldr.ToString());
			}
		}
	}
}
#endif