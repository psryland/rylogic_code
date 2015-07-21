//***********************************************
// LineDrawer helper
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

using System.Drawing;
using System.IO;
using System.Text;
using pr.extn;
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
		public static string Vec3(v2 vec)
		{
			return "{0} {1} 0".Fmt(vec.x,vec.y);
		}
		public static string Vec3(v4 vec)
		{
			return "{0} {1} {2}".Fmt(vec.x,vec.y,vec.z);
		}
		public static string Vec4(v4 vec)
		{
			return "{0} {1} {2} {3}".Fmt(vec.x,vec.y,vec.z,vec.w);
		}
		public static string Mat4x4(m4x4 mat)
		{
			return "{0} {1} {2} {3}".Fmt(Vec4(mat.x),Vec4(mat.y),Vec4(mat.z),Vec4(mat.w));
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
		public static string Colour(uint col)
		{
			return col.ToString("X8");
		}
		public static string Colour(Color col)
		{
			return Colour(unchecked((uint)col.ToArgb()));
		}
		public static string Solid(bool solid = true)
		{
			return solid ? "*Solid" : string.Empty;
		}
		public static string Facets(int facets)
		{
			return "*Facets {{{0}}}".Fmt(facets);
		}
		public static string CornerRadius(float rad)
		{
			return "*CornerRadius {" + rad + "}";
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
			foreach (var p in parts)
			{
				if      (p is Color ) m_sb.Append(Ldr.Colour((Color)p));
				else if (p is v4    ) m_sb.Append(Ldr.Vec3  ((v4)p));
				else if (p is m4x4  ) m_sb.Append(Ldr.Mat4x4((m4x4)p));
				else if (p is AxisId) m_sb.Append(((AxisId)p).m_id);
				else                  m_sb.Append(p.ToString());
			}
			return this;
		}

		public Scope Group()            { return Group(string.Empty); }
		public Scope Group(string name) { return Group(name, v4.Origin); }
		public Scope Group(string name, v4 position)
		{
			return Scope.Create(
				() => Append("*Group ",name," {\n"),
				() => Append(Ldr.Position(position),"}\n")
				);
		}

		public void Line(v4 start, v4 end)               { Line(Color.White, start, end); }
		public void Line(Color colour, v4 start, v4 end) { Line(string.Empty, colour, start, end); }
		public void Line(string name, Color colour, v4 start, v4 end)
		{
			Append("*Line ",name," ",colour," {",start," ",end,"}\n");
		}

		public void Box()                                          { Box(string.Empty, Color.White); }
		public void Box(Color colour, float size)                  { Box(colour, size, v4.Origin); }
		public void Box(Color colour, float size, v4 position)     { Box(string.Empty, colour, size, position); }
		public void Box(string name, Color colour)                 { Box(name, colour, 1f); }
		public void Box(string name, Color colour, float size)     { Box(name, colour, size, v4.Origin); }
		public void Box(string name, Color colour, float size, v4 position)
		{
			Append("*Box ",name," ",colour," {",size," ",Ldr.Position(position),"}\n");
		}
		public void Box(Color colour, float sx, float sy, float sz)              { Box(colour, sx, sy, sz, v4.Origin); }
		public void Box(Color colour, float sx, float sy, float sz, v4 position) { Box(string.Empty, colour, sx, sy, sz, position); }
		public void Box(string name, Color colour, float sx, float sy, float sz) { Box(name, colour, sx, sy, sz); }
		public void Box(string name, Color colour, float sx, float sy, float sz, v4 position)
		{
			Append("*Box ",name," ",colour," {",sx," ",sy," ",sz," ",Ldr.Position(position),"}\n");
		}

		public void Sphere()                                        { Sphere(string.Empty, Color.White); }
		public void Sphere(string name, Color colour)               { Sphere(name, colour, 1f); }
		public void Sphere(string name, Color colour, float radius) { Sphere(name, colour, radius, v4.Origin); }
		public void Sphere(string name, Color colour, float radius, v4 position)
		{
			Append("*Sphere ",name," ",colour," {",radius," ",Ldr.Position(position),"}\n");
		}

		public void Circle()                                                                    { Circle(string.Empty, Color.White, 3, true); }
		public void Circle(string name, Color colour, AxisId axis_id, bool solid)               { Circle(name, colour, axis_id, solid, v4.Origin); }
		public void Circle(string name, Color colour, AxisId axis_id, bool solid, float radius) { Circle(name, colour, axis_id, solid, radius, v4.Origin); }
		public void Circle(string name, Color colour, AxisId axis_id, bool solid, v4 position)  { Circle(name, colour, axis_id, solid, 1f, position); }
		public void Circle(string name, Color colour, AxisId axis_id, bool solid, float radius, v4 position)
		{
			Append("*Circle ",name," ",colour," {",axis_id," ",radius," ",Ldr.Solid(solid)," ",Ldr.Position(position),"}\n");
		}

		public void Ellipse()                                                                                    { Ellipse(string.Empty, Color.White, 3, true, 1f, 0.5f); }
		public void Ellipse(string name, Color colour, AxisId axis_id, bool solid, float radiusx, float radiusy) { Ellipse(name, colour, axis_id, solid, radiusx, radiusy, v4.Origin); }
		public void Ellipse(string name, Color colour, AxisId axis_id, bool solid, float radiusx, float radiusy, v4 position)
		{
			Append("*Circle ",name," ",colour," {",axis_id," ",radiusx," ",radiusy," ",Ldr.Solid(solid)," ",Ldr.Position(position),"}\n");
		}

		public void Pie()                                                                                                                   { Pie(string.Empty, Color.White, 3, true, 0f, 45f); }
		public void Pie(string name, Color colour, AxisId axis_id, bool solid, float ang0, float ang1)                                      { Pie(name, colour, axis_id, solid, ang0, ang1, v4.Origin); }
		public void Pie(string name, Color colour, AxisId axis_id, bool solid, float ang0, float ang1, v4 position)                         { Pie(name, colour, axis_id, solid, ang0, ang1, 0f, 1f, position); }
		public void Pie(string name, Color colour, AxisId axis_id, bool solid, float ang0, float ang1, float rad0, float rad1, v4 position) { Pie(name, colour, axis_id, solid, ang0, ang1, rad0, rad1, 1f, 1f, 40, position); }
		public void Pie(string name, Color colour, AxisId axis_id, bool solid, float ang0, float ang1, float rad0, float rad1, float sx, float sy, int facets, v4 position)
		{
			Append("*Pie ",name," ",colour," {",axis_id," ",ang0," ",ang1," ",rad0," ",rad1," ",Ldr.Solid(solid)," ",Ldr.Facets(facets)," *Scale ",sx," ",sy," ",Ldr.Position(position),"}\n");
		}

		public void Quad()                                                      { Quad(string.Empty, Color.White); }
		public void Quad(Color colour)                                          { Quad(string.Empty, colour); }
		public void Quad(Color colour, v4 tl, v4 tr, v4 br, v4 bl)              { Quad(string.Empty, colour, tl, tr, br, bl); }
		public void Quad(string name, Color colour)                             { Quad(name, colour, new v4(0,1,0,1), new v4(1,1,0,1), new v4(1,0,0,1), new v4(0,0,0,1)); }
		public void Quad(string name, Color colour, v4 tl, v4 tr, v4 br, v4 bl) { Quad(name, colour, tl, tr, br, bl, v4.Origin); }
		public void Quad(string name, Color colour, v4 tl, v4 tr, v4 br, v4 bl, v4 position)
		{
			Append("*Quad ",name," ",colour," {",bl," ",br," ",tr," ",tl," ",Ldr.Position(position),"}\n");
		}

		public void Axis()                                     { Axis(m4x4.Identity); }
		public void Axis(m3x4 basis)                           { Axis(new m4x4(basis, v4.Origin)); }
		public void Axis(m4x4 basis)                           { Axis(string.Empty, basis); }
		public void Axis(string name, m3x4 basis)              { Axis(name, new m4x4(basis, v4.Origin)); }
		public void Axis(string name, m4x4 basis)              { Axis(name, basis, 0.1f); }
		public void Axis(string name, m3x4 basis, float scale) { Axis(name, new m4x4(basis, v4.Origin), scale); }
		public void Axis(string name, m4x4 basis, float scale)
		{
			Append("*Matrix3x3 ",name," {",basis.x*scale," ",basis.y*scale," ",basis.z*scale," ",Ldr.Position(basis.pos),"}\n");
		}

		public override string ToString()
		{
			return m_sb.ToString();
		}
		public void ToFile(string filepath, bool append = false)
		{
			using (var f = new StreamWriter(new FileStream(filepath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read)))
				f.Write(ToString());
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using ldr;

	[TestFixture] public class TestLdr
	{
		[Test] public void LdrBuilder()
		{
			var ldr = new LdrBuilder();
			using (ldr.Group("g"))
			{
				ldr.Box("b", Color.FromArgb(0,0xFF,0));
				ldr.Sphere("s", Color.Red);
			}
			var expected =
				"*Group g {\n"+
				"*Box b FF00FF00 {1 }\n"+
				"*Sphere s FFFF0000 {1 }\n"+
				"}\n";
			Assert.AreEqual(expected, ldr.ToString());
		}
	}
}
#endif