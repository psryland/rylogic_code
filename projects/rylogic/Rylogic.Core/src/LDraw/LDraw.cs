//***********************************************
// LineDrawer helper
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.LDraw
{
	public static class Ldr
	{
		// Notes:
		//  - The LdrBuilder is the main type for building ldr strings.
		//  - This type is basically a namespace for converting types to ldr strings. It knows when a
		//    string is necessary or how to simplify a string for a specific type.

		/// <summary>Filepath for outputting ldr script using 'LdrOut' extension</summary>
		public static string OutFile = "";

		/// <summary>Append this string to the Ldr.OutFile</summary>
		public static void LdrOut(this string s, bool app = true)
		{
			using (var sw = new StreamWriter(OutFile, app))
				sw.Write(s);
		}

		/// <summary>Write an ldr string to a file</summary>
		public static void Write(string ldr_str, string filepath, bool append = false)
		{
			try
			{
				// Ensure the directory exists
				var dir = Path_.Directory(filepath);
				if (!Path_.DirExists(dir)) Directory.CreateDirectory(dir);

				// Lock, then write the file
				using (Path_.LockFile(filepath))
				using (var f = new StreamWriter(new FileStream(filepath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read)))
					f.Write(ldr_str);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to write Ldr script to '{filepath}'. {ex.Message}");
			}
		}

		// Type to string helpers
		public static string Col(uint col)
		{
			return col != 0xFFFFFFFF ? col.ToString("X8") : string.Empty;
		}
		public static string Col(Colour32 col)
		{
			return col != 0xFFFFFFFF ? col.ARGB.ToString("X8") : string.Empty;
		}
		public static string Col(Color col)
		{
			return Col(col.ToArgbU());
		}
		public static string Vec2(v2 vec)
		{
			Debug.Assert(Math_.IsFinite(vec));
			return $"{vec.x} {vec.y}";
		}
		public static string Vec3(v2 vec)
		{
			Debug.Assert(Math_.IsFinite(vec));
			return $"{vec.x} {vec.y} 0";
		}
		public static string Vec3(v4 vec)
		{
			Debug.Assert(Math_.IsFinite(vec));
			return $"{vec.x} {vec.y} {vec.z}";
		}
		public static string Vec4(v4 vec)
		{
			Debug.Assert(Math_.IsFinite(vec));
			return $"{vec.x} {vec.y} {vec.z} {vec.w}";
		}
		public static string Mat4x4(m4x4 mat)
		{
			Debug.Assert(Math_.IsFinite(mat));
			return $"{Vec4(mat.x)} {Vec4(mat.y)} {Vec4(mat.z)} {Vec4(mat.w)}";
		}

		// Ldr element helpers
		public static string Colour(Colour32 colour)
		{
			return $"*Colour {{{colour.ARGB:X8}}}";
		}
		public static string Position(v4 position, bool newline = false)
		{
			return
				position == v4.Zero ? string.Empty :
				position == v4.Origin ? string.Empty :
				$"*o2w{{*pos{{{Vec3(position)}}}}}{(newline ? "\n" : "")}";
		}
		public static string Position(v4? position, bool newline = false)
		{
			return position != null ? Position(position.Value, newline) : string.Empty;
		}
		public static string Transform(m4x4 o2w, bool newline = false)
		{
			return
				o2w == m4x4.Identity ? string.Empty :
				o2w.rot == m3x4.Identity ? Position(o2w.pos, newline) :
				$"*o2w{{*m4x4{{{Mat4x4(o2w)}}}}}{(newline ? "\n" : "")}";
		}
		public static string Transform(m4x4? o2w, bool newline = false)
		{
			return o2w != null ? Transform(o2w.Value, newline) : string.Empty;
		}
		public static string Transform(m4x4? o2w, v4? pos, bool newline = false)
		{
			return
				o2w != null ? Transform(o2w.Value, newline) :
				pos != null ? Position(pos.Value, newline) :
				string.Empty;
		}
		public static string Name(string? name)
		{
			return name != null ? $"*Name {{{name}}}" : string.Empty;
		}
		public static string AxisId(AxisId id)
		{
			return id.Id != EAxisId.PosZ ? $"*AxisId {{{id}}}" : string.Empty;
		}
		public static string Solid(bool solid = true)
		{
			return solid ? "*Solid" : string.Empty;
		}
		public static string Billboard(bool billboard = true)
		{
			return billboard ? "*Billboard" : string.Empty;
		}
		public static string ScreenSpace(bool screen_space = true)
		{
			return screen_space ? "*ScreenSpace" : string.Empty;
		}
		public static string Size(double size)
		{
			return $"*Size {{{size}}}";
		}
		public static string Scale(double scale)
		{
			return scale != 1 ? $"*Scale {{{scale}}}" : string.Empty;
		}
		public static string Width(double width)
		{
			return width != 0 ? $"*Width {{{width}}}" : string.Empty;
		}
		public static string Facets(int facets)
		{
			return $"*Facets {{{facets}}}";
		}
		public static string CornerRadius(double rad)
		{
			return rad != 0 ? $"*CornerRadius {{{rad}}}" : string.Empty;
		}
		public static string Smooth(bool smooth)
		{
			return smooth ? "*Smooth" : string.Empty;
		}
		public static string NoZTest(bool no_ztest)
		{
			return no_ztest ? "*NoZTest" : string.Empty;
		}
		public static string NoZWrite(bool no_zwrite)
		{
			return no_zwrite ? "*NoZWrite" : string.Empty;
		}

		// Types
		public enum EArrowType
		{
			Line = 0,
			Fwd = 1 << 0,
			Back = 1 << 1,
			FwdBack = Fwd | Back,
		}
	}

	/// <summary>Like StringBuilder, but for ldr strings</summary>
	public class LdrBuilder
	{
		// Notes:
		//  - Fluent style Ldr String builder.

		private readonly StringBuilder m_sb;
		public LdrBuilder(StringBuilder sb) => m_sb = sb;
		public LdrBuilder() : this(new StringBuilder()) { }
		public LdrBuilder(int capacity) : this(new StringBuilder(capacity)) { }
		public LdrBuilder(string value) : this(new StringBuilder(value)) { }
		public LdrBuilder(int capacity, int maxCapacity) : this(new StringBuilder(capacity, maxCapacity)) { }
		public LdrBuilder(string value, int capacity) : this(new StringBuilder(value, capacity)) { }
		public LdrBuilder(string value, int startIndex, int length, int capacity) : this(new StringBuilder(value, startIndex, length, capacity)) { }

		public LdrBuilder Clear()
		{
			m_sb.Clear();
			return this;
		}
		public LdrBuilder Remove(int start_index, int length)
		{
			m_sb.Remove(start_index, length);
			return this;
		}
		public LdrBuilder Append(params object[] parts)
		{
			foreach (var p in parts) Append(p);
			return this;
		}
		public LdrBuilder Append(object part)
		{
			// Use this switch to convert a type directly into a string. No adorning.
			// E.g. 'part is Colour32' just inserts "FF00FF00", not "*Colour32{FF00FF00}"
			if (part is null) { }
			else if (part is string str) m_sb.Append(str);
			else if (part is Color col) m_sb.Append(Ldr.Col(col));
			else if (part is v4 vec4) m_sb.Append(Ldr.Vec3(vec4));
			else if (part is v2 vec2) m_sb.Append(Ldr.Vec2(vec2));
			else if (part is m4x4 o2w) m_sb.Append(Ldr.Mat4x4(o2w));
			else if (part is AxisId axisid) m_sb.Append(Ldr.AxisId(axisid.Id));
			else if (part is EAxisId axisid2) m_sb.Append(Ldr.AxisId(axisid2));
			else if (part is IEnumerable)
			{
				foreach (var x in (IEnumerable)part)
					Append(" ").Append(x);
			}
			else if (part.GetType().IsAnonymousType())
			{
				Append("{");
				foreach (var prop in part.GetType().AllProps(BindingFlags.Public | BindingFlags.Instance))
					Append($"*{prop.Name} {{", prop.GetValue(part), "}} ");
				Append("}");
				return this;
			}
			else
			{
				m_sb.Append(part.ToString());
			}
			return this;
		}

		public LdrBuilder Comment(string comments)
		{
			var lines = comments.Split('\n');
			foreach (var line in lines)
				Append($"// {line}\n");
			return this;
		}

		public Scope Group()
		{
			return Group(string.Empty);
		}
		public Scope Group(string name)
		{
			return Group(name, v4.Origin);
		}
		public Scope Group(string name, Colour32 colour)
		{
			return Group(name, v4.Origin);
		}
		public Scope Group(string name, v4 position)
		{
			return Group(name, 0xFFFFFFFF, position);
		}
		public Scope Group(string name, Colour32 colour, v4 position)
		{
			return Group(name, colour, m4x4.Translation(position));
		}
		public Scope Group(string name, m4x4 transform)
		{
			return Group(name, 0xFFFFFFFF, transform);
		}
		public Scope Group(string name, Colour32 colour, m4x4 transform)
		{
			return Scope.Create(
				() => GroupOpen(name, colour),
				() => GroupClose(transform)
				);
		}
		public LdrBuilder GroupOpen(string name)
		{
			return GroupOpen(name, 0xFFFFFFFF);
		}
		public LdrBuilder GroupOpen(string name, Colour32 colour)
		{
			return Append("*Group ", name, " ", colour, " {\n");
		}
		public LdrBuilder GroupClose()
		{
			return GroupClose(m4x4.Identity);
		}
		public LdrBuilder GroupClose(m4x4 transform)
		{
			return Append(Ldr.Transform(transform, newline: true), "}\n");
		}

		public LdrBuilder Line(v4 start, v4 end)
		{
			return Line(Color.White, start, end);
		}
		public LdrBuilder Line(Colour32 colour, v4 start, v4 end)
		{
			return Line(string.Empty, colour, start, end);
		}
		public LdrBuilder Line(string name, Colour32 colour, v4 start, v4 end)
		{
			return Append("*Line ", name, " ", colour, " {", start, " ", end, "}\n");
		}
		public LdrBuilder Line(string name, Colour32 colour, double width, bool smooth, IEnumerable<v4> points)
		{
			if (!points.Any()) return this;
			return Append("*LineStrip ", name, " ", colour, " {", Ldr.Width(width), Ldr.Smooth(smooth), points.Select(x => Ldr.Vec3(x)), "}\n");
		}
		public LdrBuilder Line(string name, Colour32 colour, double width, bool smooth, Func<int, v4?> points)
		{
			int idx = 0;
			Append("*LineStrip ", name, " ", colour, " {", Ldr.Width(width), Ldr.Smooth(smooth));
			for (v4? pt; (pt = points(idx++)) != null;) Append(Ldr.Vec3(pt.Value));
			Append("}\n");
			return this;
		}
		public LdrBuilder LineD(string name, Colour32 colour, v4 start, v4 direction)
		{
			return LineD(name, colour, start, direction, 0);
		}
		public LdrBuilder LineD(string name, Colour32 colour, v4 start, v4 direction, double width)
		{
			return Append("*LineD ", name, " ", colour, " {", Ldr.Width(width), start, " ", direction, "}");
		}

		public LdrBuilder Arrow()
		{
			return Arrow(string.Empty, Colour32.White, Ldr.EArrowType.Fwd, 10f, true, new[] { v4.Origin, v4.XAxis.w1 });
		}
		public LdrBuilder Arrow(string name, Colour32 colour, Ldr.EArrowType type, double width, bool smooth, IEnumerable<v4> points)
		{
			if (!points.Any()) return this;
			return Append("*Arrow ", name, " ", colour, " {", type.ToString(), points.Select(x => Ldr.Vec3(x)), Ldr.Width(width), Ldr.Smooth(smooth), "}\n");
		}
		public LdrBuilder Arrow(string name, Colour32 colour, Ldr.EArrowType type, double width, bool smooth, Func<int, v4?> points)
		{
			int idx = 0;
			Append("*Arrow ", name, " ", colour, " {");
			for (v4? pt; (pt = points(idx++)) != null;) Append(Ldr.Vec3(pt.Value));
			Append(Ldr.Width(width), Ldr.Smooth(smooth), type.ToString(), "}\n");
			return this;
		}

		public LdrBuilder Grid(Colour32 colour, AxisId axis_id, int width, int height)
		{
			return Grid(string.Empty, colour, axis_id, width, height);
		}
		public LdrBuilder Grid(string name, Colour32 colour, AxisId axis_id, int width, int height)
		{
			return Grid(name, colour, axis_id, width, height, width, height, v4.Origin);
		}
		public LdrBuilder Grid(string name, Colour32 colour, AxisId axis_id, int width, int height, int wdiv, int hdiv, v4 position)
		{
			return Append("*Grid ", name, " ", colour, " {", width, " ", height, " ", wdiv, " ", hdiv, " ", axis_id, " ", Ldr.Position(position), "}\n");
		}

		public LdrBuilder Box(m4x4? o2w = null, v4? pos = null)
		{
			return Box(string.Empty, Color.White, o2w, pos);
		}
		public LdrBuilder Box(Colour32 colour, double size, m4x4? o2w = null, v4? pos = null)
		{
			return Box(string.Empty, colour, size, o2w, pos);
		}
		public LdrBuilder Box(Colour32 colour, double sx, double sy, double sz, m4x4? o2w = null, v4? pos = null)
		{
			return Box(string.Empty, colour, sx, sy, sz, o2w, pos);
		}
		public LdrBuilder Box(string name, Colour32 colour, m4x4? o2w = null, v4? pos = null)
		{
			return Box(name, colour, 1f, o2w, pos);
		}
		public LdrBuilder Box(string name, Colour32 colour, double size, m4x4? o2w = null, v4? pos = null)
		{
			return Append("*Box ", name, " ", colour, " {", size, " ", Ldr.Transform(o2w, pos), "}\n");
		}
		public LdrBuilder Box(string name, Colour32 colour, double sx, double sy, double sz, m4x4? o2w = null, v4? pos = null)
		{
			return Append("*Box ", name, " ", colour, " {", sx, " ", sy, " ", sz, " ", Ldr.Transform(o2w, pos), "}\n");
		}
		public LdrBuilder Box(string name, Colour32 colour, v4 dim, m4x4? o2w = null, v4? pos = null)
		{
			return Append("*Box ", name, " ", colour, " {", dim.x, " ", dim.y, " ", dim.z, " ", Ldr.Transform(o2w, pos), "}\n");
		}

		public LdrBuilder Sphere()
		{
			return Sphere(string.Empty, Color.White);
		}
		public LdrBuilder Sphere(string name, Colour32 colour)
		{
			return Sphere(name, colour, 1f);
		}
		public LdrBuilder Sphere(string name, Colour32 colour, double radius)
		{
			return Sphere(name, colour, radius, v4.Origin);
		}
		public LdrBuilder Sphere(string name, Colour32 colour, double radius, v4 position)
		{
			return Append("*Sphere ", name, " ", colour, " {", radius, " ", Ldr.Position(position), "}\n");
		}

		public LdrBuilder Cylinder(Colour32 colour, AxisId axis_id, double height, double radius, m4x4? o2w = null, v4? pos = null)
		{
			return Cylinder(string.Empty, colour, axis_id, height, radius, o2w, pos);
		}
		public LdrBuilder Cylinder(string name, Colour32 colour, AxisId axis_id, double height, double radius, m4x4? o2w = null, v4? pos = null)
		{
			return Append("*Cylinder ", name, " ", colour, " {", height, " ", radius, " ", axis_id, " ", Ldr.Transform(o2w, pos), "}\n");
		}

		public LdrBuilder Circle()
		{
			return Circle(string.Empty, Color.White, 3, true);
		}
		public LdrBuilder Circle(string name, Colour32 colour, AxisId axis_id, bool solid)
		{
			return Circle(name, colour, axis_id, solid, v4.Origin);
		}
		public LdrBuilder Circle(string name, Colour32 colour, AxisId axis_id, bool solid, double radius)
		{
			return Circle(name, colour, axis_id, solid, radius, v4.Origin);
		}
		public LdrBuilder Circle(string name, Colour32 colour, AxisId axis_id, bool solid, v4 position)
		{
			return Circle(name, colour, axis_id, solid, 1f, position);
		}
		public LdrBuilder Circle(string name, Colour32 colour, AxisId axis_id, bool solid, double radius, v4 position)
		{
			return Append("*Circle ", name, " ", colour, " {", radius, " ", axis_id, " ", Ldr.Solid(solid), " ", Ldr.Position(position), "}\n");
		}

		public LdrBuilder Ellipse()
		{
			return Ellipse(string.Empty, Color.White, 3, true, 1f, 0.5f);
		}
		public LdrBuilder Ellipse(string name, Colour32 colour, AxisId axis_id, bool solid, double radiusx, double radiusy)
		{
			return Ellipse(name, colour, axis_id, solid, radiusx, radiusy, v4.Origin);
		}
		public LdrBuilder Ellipse(string name, Colour32 colour, AxisId axis_id, bool solid, double radiusx, double radiusy, v4 position)
		{
			return Append("*Circle ", name, " ", colour, " {", radiusx, " ", radiusy, " ", axis_id, " ", Ldr.Solid(solid), " ", Ldr.Position(position), "}\n");
		}

		public LdrBuilder Pie()
		{
			return Pie(string.Empty, Color.White, 3, true, 0f, 45f);
		}
		public LdrBuilder Pie(string name, Colour32 colour, AxisId axis_id, bool solid, double ang0, double ang1)
		{
			return Pie(name, colour, axis_id, solid, ang0, ang1, v4.Origin);
		}
		public LdrBuilder Pie(string name, Colour32 colour, AxisId axis_id, bool solid, double ang0, double ang1, v4 position)
		{
			return Pie(name, colour, axis_id, solid, ang0, ang1, 0f, 1f, position);
		}
		public LdrBuilder Pie(string name, Colour32 colour, AxisId axis_id, bool solid, double ang0, double ang1, double rad0, double rad1, v4 position)
		{
			return Pie(name, colour, axis_id, solid, ang0, ang1, rad0, rad1, 1f, 1f, 40, position);
		}
		public LdrBuilder Pie(string name, Colour32 colour, AxisId axis_id, bool solid, double ang0, double ang1, double rad0, double rad1, double sx, double sy, int facets, v4 position)
		{
			return Append("*Pie ", name, " ", colour, " {", ang0, " ", ang1, " ", rad0, " ", rad1, " ", axis_id, " ", Ldr.Solid(solid), " ", Ldr.Facets(facets), " *Scale ", sx, " ", sy, " ", Ldr.Position(position), "}\n");
		}

		public LdrBuilder Rect()
		{
			return Rect(string.Empty, Color.White, 3, 1f, 1f, false, v4.Origin);
		}
		public LdrBuilder Rect(string name, Colour32 colour, AxisId axis_id, double width, double height, bool solid, v4 position)
		{
			return Append("*Rect ", name, " ", colour, " {", width, " ", height, " ", axis_id, " ", Ldr.Solid(solid), " ", Ldr.Position(position), "}\n");
		}
		public LdrBuilder Rect(string name, Colour32 colour, AxisId axis_id, double width, double height, bool solid, double corner_radius, v4 position)
		{
			return Append("*Rect ", name, " ", colour, " {", width, " ", height, " ", axis_id, " ", Ldr.Solid(solid), " ", Ldr.CornerRadius(corner_radius), " ", Ldr.Position(position), "}\n");
		}

		public LdrBuilder Triangle()
		{
			return Triangle(string.Empty, Colour32.White, v4.Origin, v4.XAxis.w1, v4.YAxis.w1);
		}
		public LdrBuilder Triangle(string name, Colour32 colour, v4 a, v4 b, v4 c, m4x4? o2w = null)
		{
			return Append("*Triangle ", name, " ", colour, " {", a, " ", b, " ", c, " ", Ldr.Transform(o2w), "}");
		}
		public LdrBuilder Triangle(string name, Colour32 colour, IEnumerable<v4> pts, m4x4? o2w = null)
		{
			return Append("*Triangle ", name, " ", colour, " {", pts, Ldr.Transform(o2w), "}");
		}

		public LdrBuilder Quad()
		{
			return Quad(string.Empty, Color.White);
		}
		public LdrBuilder Quad(Colour32 colour)
		{
			return Quad(string.Empty, colour);
		}
		public LdrBuilder Quad(Colour32 colour, v4 tl, v4 tr, v4 br, v4 bl)
		{
			return Quad(string.Empty, colour, tl, tr, br, bl);
		}
		public LdrBuilder Quad(string name, Colour32 colour)
		{
			return Quad(name, colour, new v4(0, 1, 0, 1), new v4(1, 1, 0, 1), new v4(1, 0, 0, 1), new v4(0, 0, 0, 1));
		}
		public LdrBuilder Quad(string name, Colour32 colour, v4 tl, v4 tr, v4 br, v4 bl)
		{
			return Quad(name, colour, tl, tr, br, bl, v4.Origin);
		}
		public LdrBuilder Quad(string name, Colour32 colour, v4 tl, v4 tr, v4 br, v4 bl, v4 position)
		{
			return Append("*Quad ", name, " ", colour, " {", bl, " ", br, " ", tr, " ", tl, " ", Ldr.Position(position), "}\n");
		}
		public LdrBuilder Quad(string name, Colour32 colour, AxisId axis_id, double w, double h, v4 position)
		{
			return Rect(name, colour, axis_id, w, h, true, position);
		}

		public LdrBuilder Axis()
		{
			return Axis(m4x4.Identity);
		}
		public LdrBuilder Axis(m3x4 basis)
		{
			return Axis(new m4x4(basis, v4.Origin));
		}
		public LdrBuilder Axis(m4x4 basis)
		{
			return Axis(string.Empty, Color_.FromArgb(0xFFFFFFFF), basis);
		}
		public LdrBuilder Axis(string name, Colour32 colour, m3x4 basis)
		{
			return Axis(name, colour, new m4x4(basis, v4.Origin));
		}
		public LdrBuilder Axis(string name, Colour32 colour, m4x4 basis)
		{
			return Axis(name, colour, basis, 0.1f);
		}
		public LdrBuilder Axis(string name, Colour32 colour, m3x4 basis, double scale)
		{
			return Axis(name, colour, new m4x4(basis, v4.Origin), scale);
		}
		public LdrBuilder Axis(string name, Colour32 colour, m4x4 basis, double scale)
		{
			return Append("*Matrix3x3 ", name, " ", colour, " {", basis.x * scale, " ", basis.y * scale, " ", basis.z * scale, " ", Ldr.Position(basis.pos), "}\n");
		}

		public LdrBuilder Ribbon()
		{
			return Ribbon(string.Empty, Colour32.White, new[] { v4.Origin, v4.XAxis.w1 }, EAxisId.PosZ, 3f, false);
		}
		public LdrBuilder Ribbon(string name, Colour32 colour, IEnumerable<v4> points, AxisId axis_id, double width, bool smooth, m4x4? o2w = null)
		{
			return Append("*Ribbon ", name, " ", colour, " {", points, " ", axis_id, Ldr.Width(width), Ldr.Smooth(smooth), Ldr.Transform(o2w), "}\n");
		}

		public LdrBuilder Mesh(string name, Colour32 colour, IList<v4>? verts, IList<v4>? normals = null, IList<Colour32>? colours = null, IList<v2>? tex = null, IList<ushort>? faces = null, IList<ushort>? lines = null, IList<ushort>? tetra = null, bool generate_normals = false, v4? position = null)
		{
			Append("*Mesh ", name, " ", colour, " {\n");
			if (verts != null) Append("*Verts {").Append(verts.Select(Ldr.Vec3)).Append("}\n");
			if (normals != null) Append("*Normals {").Append(normals.Select(Ldr.Vec3)).Append("}\n");
			if (colours != null) Append("*Colours {").Append(colours.Select(Ldr.Col)).Append("}\n");
			if (tex != null) Append("*TexCoords {").Append(tex.Select(Ldr.Vec2)).Append("}\n");
			if (verts != null && faces != null) { Debug.Assert(faces.All(i => i >= 0 && i < verts.Count)); Append("*Faces {").Append(faces).Append("}\n"); }
			if (verts != null && lines != null) { Debug.Assert(lines.All(i => i >= 0 && i < verts.Count)); Append("*Lines {").Append(lines).Append("}\n"); }
			if (verts != null && tetra != null) { Debug.Assert(tetra.All(i => i >= 0 && i < verts.Count)); Append("*Tetra {").Append(tetra).Append("}\n"); }
			if (generate_normals) Append("*GenerateNormals\n");
			if (position != null) Append(Ldr.Position(position.Value));
			Append("}\n");
			return this;
		}

		public LdrBuilder Graph(string name, AxisId axis_id, bool smooth, IEnumerable<v4> data)
		{
			var bbox = BBox.Reset;
			foreach (var d in data)
				BBox.Grow(ref bbox, d);

			using var grp = Group(name);
			Grid(string.Empty, 0xFFAAAAAA, axis_id, (int)(bbox.SizeX * 1.1), (int)(bbox.SizeY * 1.1), 50, 50, new v4(bbox.Centre.x, bbox.Centre.y, 0f, 1f));
			Line("data", 0xFFFFFFFF, 1, smooth, data);
			return this;
		}

		public LdrTextBuilder Text(string name, Colour32 colour)
		{
			return new LdrTextBuilder(this, name, colour);
		}

		public override string ToString()
		{
			return m_sb.ToString();
		}
		public void ToFile(string filepath, bool append = false)
		{
			Ldr.Write(ToString(), filepath, append);
		}
		public static implicit operator string(LdrBuilder ldr) => ldr.ToString();
	}

	public class LdrTextBuilder
	{
		private readonly LdrBuilder m_builder;
		internal LdrTextBuilder(LdrBuilder builder, string name, Colour32 colour)
		{
			m_builder = builder;
			m_builder.Append("*Text ", name, " ", colour, " {");
		}
		public LdrTextBuilder String(string text)
		{
			m_builder.Append($"\"{text}\"");
			return this;
		}
		public LdrTextBuilder CString(string cstring)
		{
			m_builder.Append($"*CString \"{cstring}\"");
			return this;
		}
		public LdrTextBuilder Bkgd(Colour32 colour)
		{
			m_builder.Append("*BackColour {", colour, "}");
			return this;
		}
		public LdrTextBuilder Font(string? font_name, double size, Colour32 colour)
		{
			m_builder.Append("*Font {", Ldr.Name(font_name), Ldr.Size(size), Ldr.Colour(colour), "}");
			return this;
		}
		public LdrTextBuilder Font(double size, Colour32 colour)
		{
			m_builder.Append("*Font {", Ldr.Size(size), Ldr.Colour(colour), "}");
			return this;
		}
		public LdrTextBuilder Billboard()
		{
			m_builder.Append(Ldr.Billboard(true));
			return this;
		}
		public LdrTextBuilder ScreenSpace()
		{
			m_builder.Append(Ldr.ScreenSpace(true));
			return this;
		}
		public LdrTextBuilder Dim(v2 dimensions)
		{
			m_builder.Append($"*Dim {{{dimensions.x} {dimensions.y}}}");
			return this;
		}
		public LdrTextBuilder AxisId(AxisId axis)
		{
			m_builder.Append(Ldr.AxisId(axis));
			return this;
		}
		public LdrTextBuilder Anchor(v2 anchor)
		{
			m_builder.Append($"*Anchor {{{anchor.x} {anchor.y}}}");
			return this;
		}
		public LdrTextBuilder Format(string h_align, string v_align, bool wrap)
		{
			m_builder.Append($"*Format {{{h_align} {v_align} {(wrap ? "Wrap" : "")}}}");
			return this;
		}
		public LdrTextBuilder NoZTest()
		{
			m_builder.Append(Ldr.NoZTest(true));
			return this;
		}
		public LdrTextBuilder Append(params object[] parts)
		{
			m_builder.Append(parts);
			return this;
		}
		public LdrBuilder End()
		{
			return m_builder.Append("}\n");
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using LDraw;

	[TestFixture]
	public class TestLdr
	{
		[Test]
		public void LdrBuilder()
		{
			var ldr = new LdrBuilder();
			using (ldr.Group("g"))
			{
				ldr.Box("b", Color.FromArgb(0, 0xFF, 0));
				ldr.Sphere("s", Color.Red);
			}
			var expected =
				"*Group g FFFFFFFF {\n" +
				"*Box b FF00FF00 {1 }\n" +
				"*Sphere s FFFF0000 {1 }\n" +
				"}\n";
			Assert.Equal(expected, ldr.ToString());
		}
	}
}
#endif