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
	[Flags] public enum ESaveFlags
	{
		None = 0,
		Binary = 1 << 0,
		Pretty = 1 << 1,
		Append = 1 << 2,
		NoThrowOnFailure = 1 << 8,
	}

	public class Builder
	{
		/// <summary>Access the objects already in the builder</summary>
		public IReadOnlyList<Builder> Objects => m_objects;
		private List<Builder> m_objects = [];

		/// <summary>Reset the builder</summary>
		public Builder Clear(int count = -1)
		{
			if (count >= 0 && count < m_objects.Count)
				m_objects.RemoveRange(count, m_objects.Count - count);
			else
				m_objects.Clear();
	
			return this;
		}

		/// <summary>Create child object</summary>
		public LdrPoint Point(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrPoint();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrLine Line(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrLine();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrCoordFrame CoordFrame(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrCoordFrame();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrTriangle Triangle(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrTriangle();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrPlane Plane(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrPlane();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrCircle Circle(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrCircle();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrSphere Sphere(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrSphere();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrBox Box(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrBox();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrCylinder Cylinder(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrCylinder();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrCone Cone(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrCone();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrFrustum Frustum(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrFrustum();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrModel Model(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrModel();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrInstance Instance(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrInstance();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrText Text(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrText();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrGroup Group(Serialiser.Name? name = null, Serialiser.Colour? colour = null)
		{
			var child = new LdrGroup();
			m_objects.Add(child);
			return child.name(name ?? new()).colour(colour ?? new());
		}
		public LdrCommands Command()
		{
			var child = new LdrCommands();
			m_objects.Add(child);
			return child;
		}

		/// <summary>Switch data stream modes</summary>
		public LdrBinaryStream BinaryStream()
		{
			var child = new LdrBinaryStream();
			m_objects.Add(child);
			return child;
		}
		public LdrTextStream TextStream()
		{
			var child = new LdrTextStream();
			m_objects.Add(child);
			return child;
		}

		/// <summary>Serialise to ldraw script</summary>
		public override string ToString()
		{
			var res = new TextWriter();
			WriteTo(res);
			return res.ToString();
		}
		public string ToString(bool pretty)
		{
			var res = ToString();
			if (pretty) FormatScript(res);
			return res;
		}

		/// <summary>Serialise to ldraw text</summary>
		public MemoryStream ToText()
		{
			var res = new TextWriter();
			WriteTo(res);
			return res.ToText();
		}

		/// <summary>Serialise to ldraw binary</summary>
		public MemoryStream ToBinary()
		{
			var res = new BinaryWriter();
			WriteTo(res);
			return res.ToBinary();
		}

		/// <summary>Serialise to 'res'</summary>
		public virtual void WriteTo(IWriter res)
		{
			foreach (var obj in m_objects)
				obj.WriteTo(res);
		}

		/// <summary>Write to a file</summary>
		public void Save(string filepath, ESaveFlags flags = ESaveFlags.None)
		{
			try
			{
				var rng = new Random();
				var tmp_path = Path_.CombinePath(Path.GetDirectoryName(filepath) ?? string.Empty, $"{(uint)rng.Next()}.tmp");
				if (!Path_.IsValidFilepath(tmp_path, true))
					throw new Exception("Failed to create temporary file path for LDraw save");

				var binary = flags.HasFlag(ESaveFlags.Binary);
				var append = flags.HasFlag(ESaveFlags.Append);
				var pretty = flags.HasFlag(ESaveFlags.Pretty);

				if (Path.GetDirectoryName(tmp_path) is string directory)
					Directory.CreateDirectory(directory);

				{
					using var file = File.Open(tmp_path, append ? FileMode.Append : FileMode.Create);
					var mem = binary ? ToBinary() : ToText();
					if (!binary && pretty) mem = FormatScript(mem);
					mem.CopyTo(file);
				}

				var outpath = filepath;
				if (Path.GetExtension(outpath) == string.Empty)
					outpath = Path.ChangeExtension(outpath, binary ? ".bdr" : ".ldr");

				if (File.Exists(outpath))
					File.Replace(tmp_path, outpath, null);
				else
					File.Move(tmp_path, outpath);
			}
			catch (Exception ex)
			{
				if (!flags.HasFlag(ESaveFlags.NoThrowOnFailure)) throw;
				Debug.WriteLine($"Failed to save LDraw file to '{filepath}': {ex.Message}");
			}
		}

		// Pretty format Ldraw script
		public static string FormatScript(string str)
		{
			var res = new StringBuilder(str.Length);
			var last = '\0';
			var indent = 0;
			foreach (var c in str)
			{
				if (c == '{')
				{
					++indent;
					res.Append(c);
					res.Append('\n',1).Append('\t', indent);
				}
				else if (c == '}')
				{
					--indent;
					res.Append('\n',1).Append('\t', indent);
					res.Append(c);
				}
				else
				{
					if (last == '}')
						res.Append('\n',1).Append('\t', indent);

					res.Append(c);
				}
				last = c;
			}
			return res.ToString();
		}

		// Pretty format Ldraw script from UTF8 MemoryStream
		public static MemoryStream FormatScript(MemoryStream utf8)
		{
			utf8.Position = 0;
			var result = new MemoryStream();
			using var reader = new StreamReader(utf8, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
			using var writer = new StreamWriter(result, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
			writer.Write(FormatScript(reader.ReadToEnd()));
			writer.Flush();
			result.Position = 0;
			return result;
		}
	}
	public class LdrBase<TDerived> : Builder where TDerived : LdrBase<TDerived>
	{
		protected Serialiser.Name m_name = new();
		protected Serialiser.Colour m_colour = new();
		protected Serialiser.Colour m_group_colour = new();
		protected Serialiser.O2W m_o2w = new();
		protected Serialiser.Wireframe m_wire = new();
		protected Serialiser.AxisId m_axis_id = new();
		protected Serialiser.Solid m_solid = new();
		protected Serialiser.Hidden m_hidden = new();
		protected Serialiser.ZTest m_ztest = new();
		protected Serialiser.ZWrite m_zwrite = new();
		private LdrTransform? m_bake = null;
		private LdrFont? m_font = null;

		/// <summary>Object name</summary>
		public TDerived name(Serialiser.Name name)
		{
			m_name = name;
			return (TDerived)this;
		}

		/// <summary>Object colour</summary>
		public TDerived colour(Serialiser.Colour colour)
		{
			m_colour = colour;
			return (TDerived)this;
		}

		/// <summary>Object colour mask</summary>
		public TDerived group_colour(Serialiser.Colour colour)
		{
			m_group_colour = colour;
			m_group_colour.m_kw = EKeyword.GroupColour;
			return (TDerived)this;
		}

		/// <summary>Object to world transform</summary>
		public TDerived o2w(m4x4 o2w)
		{
			m_o2w.m_mat = o2w * m_o2w.m_mat;
			return (TDerived)this;
		}
		public TDerived o2w(m3x4 rot, v4 pos)
		{
			return o2w(new m4x4(rot, pos));
		}
		public TDerived ori(v4 dir, AxisId axis)
		{
			return ori(m3x4.Rotation(axis.Axis, dir));
		}
		public TDerived ori(m3x4 rot)
		{
			return o2w(rot, v4.Origin);
		}
		public TDerived ori(Quat q)
		{
			return o2w(m4x4.Transform(q, v4.Origin));
		}
		public TDerived pos(float x, float y, float z)
		{
			return o2w(m4x4.Translation(x, y, z));
		}
		public TDerived pos(v4 pos)
		{
			return o2w(m4x4.Translation(pos));
		}
		public TDerived pos(v3 pos)
		{
			return this.pos(pos.w1);
		}
		public TDerived scale(float s)
		{
			return scale(s, s, s);
		}
		public TDerived scale(float sx, float sy, float sz)
		{
			return ori(m3x4.Scale(sx, sy, sz));
		}
		public TDerived scale(v4 s)
		{
			return ori(m3x4.Scale(s.x, s.y, s.z));
		}
		public TDerived euler(float pitch_deg, float yaw_deg, float roll_deg)
		{
			return ori(m3x4.Rotation(
				Math_.DegreesToRadians(pitch_deg),
				Math_.DegreesToRadians(yaw_deg),
				Math_.DegreesToRadians(roll_deg)));
		}

		/// <summary>Wire frame</summary>
		public TDerived wireframe(bool w = true)
		{
			m_wire = new(w);
			return (TDerived)this;
		}

		/// <summary>Axis Id</summary>
		public TDerived axis(AxisId axis_id)
		{
			m_axis_id = new(axis_id);
			return (TDerived)this;
		}

		/// <summary>Solid</summary>
		public TDerived solid(bool solid = true)
		{
			m_solid = new(solid);
			return (TDerived)this;
		}

		/// <summary>Solid</summary>
		public TDerived hide(bool hidden = true)
		{
			m_hidden = new(hidden);
			return (TDerived)this;
		}

		/// <summary>NoZTest</summary>
		public TDerived ztest(bool on = true)
		{
			m_ztest = new(on);
			return (TDerived)this;
		}
		
		/// <summary>NoZWrite</summary>
		public TDerived zwrite(bool on = true)
		{
			m_zwrite = new(on);
			return (TDerived)this;
		}

		/// <summary>Bake a transform into the object</summary>
		public LdrTransform bake()
		{
			m_bake ??= new(EKeyword.BakeTransform);
			return m_bake;
		}

		/// <summary>The text font</summary>
		public LdrFont font()
		{
			m_font ??= new();
			return m_font;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter writer)
		{
			writer.Append(m_axis_id, m_wire, m_solid, m_hidden, m_group_colour, m_ztest, m_zwrite, m_o2w);
			if (m_font != null) m_font.WriteTo(writer);
			if (m_bake != null) m_bake.WriteTo(writer);
			base.WriteTo(writer);
		}
	}

	// Modifiers
	public class LdrTransform(EKeyword kw)
	{
		private EKeyword m_kw = kw;
		private Serialiser.O2W m_o2w = new();

		public LdrTransform o2w(m4x4 o2w)
		{
			m_o2w.m_mat = o2w * m_o2w.m_mat;
			return this;
		}
		public LdrTransform o2w(m3x4 rot, v4 pos)
		{
			return o2w(new m4x4(rot, pos));
		}
		public LdrTransform ori(v4 dir, AxisId axis)
		{
			return ori(m3x4.Rotation(axis.Axis, dir));
		}
		public LdrTransform ori(m3x4 rot)
		{
			return o2w(rot, v4.Origin);
		}
		public LdrTransform ori(Quat q)
		{
			return o2w(m4x4.Transform(q, v4.Origin));
		}
		public LdrTransform pos(float x, float y, float z)
		{
			return o2w(m4x4.Translation(x, y, z));
		}
		public LdrTransform pos(v4 pos)
		{
			return o2w(m4x4.Translation(pos));
		}
		public LdrTransform pos(v3 pos)
		{
			return this.pos(pos.w1);
		}
		public LdrTransform scale(float s)
		{
			return scale(s, s, s);
		}
		public LdrTransform scale(float sx, float sy, float sz)
		{
			return ori(m3x4.Scale(sx, sy, sz));
		}
		public LdrTransform scale(v4 s)
		{
			return ori(m3x4.Scale(s.x, s.y, s.z));
		}
		public LdrTransform euler(float pitch_deg, float yaw_deg, float roll_deg)
		{
			return ori(m3x4.Rotation(
				Math_.DegreesToRadians(pitch_deg),
				Math_.DegreesToRadians(yaw_deg),
				Math_.DegreesToRadians(roll_deg)));
		}

		// Write to 'out'
		public void WriteTo(IWriter res)
		{
			res.Write(m_kw, m_o2w);
		}
	}
	public class LdrTexture
	{
		private string m_filepath = string.Empty;
		private View3d.EAddrMode[] m_addr = [View3d.EAddrMode.Wrap, View3d.EAddrMode.Wrap];
		private View3d.EFilter m_filter = View3d.EFilter.Linear;
		private Serialiser.Alpha m_has_alpha = new();
		private Serialiser.O2W m_t2s = new();

		// Texture filepath
		public LdrTexture path(string filepath)
		{
			m_filepath = filepath;
			return this;
		}

		// Addressing mode
		public LdrTexture addr(View3d.EAddrMode addrU, View3d.EAddrMode addrV)
		{
			m_addr[0] = addrU;
			m_addr[1] = addrV;
			return this;
		}

		// Filtering mode
		public LdrTexture filter(View3d.EFilter filter)
		{
			m_filter = filter;
			return this;
		}

		// Texture to surface transform
		public LdrTexture t2s(Serialiser.O2W t2s)
		{
			m_t2s = t2s;
			return this;
		}

		// Has alpha flag
		public LdrTexture alpha(Serialiser.Alpha has_alpha)
		{
			m_has_alpha = has_alpha;
			return this;
		}

		// Write to 'out'
		public void WriteTo(IWriter res)
		{
			if (m_filepath.Length == 0) return;
			res.Write(EKeyword.Texture, () =>
			{
				res.Write(EKeyword.FilePath, "\"", m_filepath, "\"");
				res.Write(EKeyword.Addr, m_addr[0], m_addr[1]);
				res.Write(EKeyword.Filter, m_filter);
				res.Append(m_has_alpha);
				res.Append(m_t2s);
			});
		}
	}
	public class LdrAnimation
	{
		private RangeI? m_frame_range = null;
		private List<(int, float)> m_frames = [];
		private bool m_per_frame_durations = false;
		private bool m_no_translation = false;
		private bool m_no_rotation = false;

		/// <summary>Limit frame range</summary>
		public LdrAnimation frame_range(int beg, int end)
		{
			m_frame_range = new RangeI(beg, end);
			return this;
		}
		public LdrAnimation frame(int frame)
		{
			return frame_range(frame, frame + 1);
		}

		/// <summary>Construct an animation from specific frames</summary>
		public LdrAnimation frames(IEnumerable<(int, float)> frames)
		{
			m_per_frame_durations = true;
			m_frames = frames.ToList();
			return this;
		}
		public LdrAnimation frames(IEnumerable<int> frames)
		{
			m_per_frame_durations = false;
			m_frames = frames.Select(x => (x, 0f)).ToList();
			return this;
			
		}

		// Anim flags
		public LdrAnimation no_translation(bool on = true)
		{
			m_no_translation = on;
			return this;
		}
		public LdrAnimation no_rotation(bool on = true)
		{
			m_no_rotation = on;
			return this;
		}

		// Write to 'out'
		public void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Animation, () =>
			{
				if (m_frame_range is RangeI range)
				{
					if (range.Count == 1)
						res.Write(EKeyword.Frame, range.Beg);
					else
						res.Write(EKeyword.FrameRange, range.Beg, range.End);
				}
				if (m_frames.Count != 0)
				{
					if (m_per_frame_durations)
					{
						res.Write(EKeyword.PerFrameDurations);
						res.Write(EKeyword.Frames, () =>
						{
							foreach (var (frame, duration) in m_frames)
								res.Append(frame, duration);
						});
					}
					else
					{
						res.Write(EKeyword.Frames, () =>
						{
							foreach (var (frame, _) in m_frames)
								res.Append(frame);
						});
					}
				}
				if (m_no_translation)
				{
					res.Write(EKeyword.NoRootTranslation);
				}
				if (m_no_rotation)
				{
					res.Write(EKeyword.NoRootRotation);
				}
			});
		}
	}
	public class LdrFont
	{
		private string? m_name = null;
		private float? m_size = null;
		private Colour32? m_colour = null;
		private int? m_weight = null;
		private string? m_style = null;
		private int? m_stretch = null;
		private bool? m_underline = null;
		private bool? m_strikeout = null;

		public LdrFont name(string name)
		{
			m_name = !string.IsNullOrEmpty(name) ? name : null;
			return this;
		}
		public LdrFont size(float sz)
		{
			m_size = sz != 0 ? sz : null;
			return this;
		}
		public LdrFont colour(Colour32 col)
		{
			m_colour = col != Colour32.Zero ? col : null;
			return this;
		}
		public LdrFont weight(int w)
		{
			m_weight = w != 0 ? w : null;
			return this;
		}
		public LdrFont style(string s)
		{
			m_style = !string.IsNullOrEmpty(s) ? s : null;
			return this;
		}
		public LdrFont stretch(int s)
		{
			m_stretch = s != 0 ? s : null;
			return this;
		}
		public LdrFont underline(bool on = true)
		{
			m_underline = on ? true : null;
			return this;
		}
		public LdrFont strikeout(bool on = true)
		{
			m_strikeout = on ? true : null;
			return this;
		}

		// Write to 'out'
		public void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Font, () =>
			{
				if (m_name != null)
					res.Write(EKeyword.Name, "\"", m_name, "\"");
				if (m_size != null)
					res.Write(EKeyword.Size, m_size.Value);
				if (m_colour != null)
					res.Write(EKeyword.Colour, m_colour.Value);
				if (m_weight != null)
					res.Write(EKeyword.Weight, m_weight.Value);
				if (m_style != null)
					res.Write(EKeyword.Style, m_style);
				if (m_stretch != null)
					res.Write(EKeyword.Stretch, m_stretch.Value);
				if (m_underline != null)
					res.Write(EKeyword.Underline, m_underline.Value);
				if (m_strikeout != null)
					res.Write(EKeyword.Strikeout, m_strikeout.Value);
			});
		}
	}

	// Object types
	public class LdrPoint : LdrBase<LdrPoint>
	{
		private class Pt { public v4 pt; public Colour32 col; };
		private readonly List<Pt> m_points = [];
		private Serialiser.Size2 m_size = new();
		private Serialiser.Depth m_depth = new();
		private Serialiser.PointStyle m_style = new();
		private Serialiser.PerItemColour m_per_item_colour = new();

		public LdrPoint style(EPointStyle s)
		{
			m_style = new(s);
			return this;
		}

		// Points
		public LdrPoint pt(v4 point, Colour32? colour = null)
		{
			m_points.Add(new Pt { pt = point, col = colour ?? Colour32.White });
			if (colour != null) m_per_item_colour.m_per_item_colour = true;
			return this;
		}
		public LdrPoint pt(v3 point, Colour32? colour = null)
		{
			return pt(point.w1, colour);
		}

		// Point size (in pixels if depth == false, in world space if depth == true)
		public LdrPoint size(float s)
		{
			m_size = new v2(s);
			return this;
		}
		public LdrPoint size(v2 s)
		{
			m_size = s;
			return this;
		}

		// Points have depth
		public LdrPoint depth(bool d = true)
		{
			m_depth = new(d);
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Point, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Style, m_style);
				res.Append(m_size, m_depth, m_per_item_colour);
				res.Write(EKeyword.Data, () =>
				{
					foreach (var point in m_points)
					{
						res.Append(point.pt.xyz);
						if (m_per_item_colour.m_per_item_colour)
							res.Append(point.col);
					}
				});
				base.WriteTo(res);
			});
		}
	}
	public class LdrLine : LdrBase<LdrLine>
	{
		private class Pt { public v4 a; public Colour32 col; };
		private class Ln { public v4 a, b; public Colour32 col; };
		private class Block
		{
			public readonly List<Ln> m_lines = [];
			public readonly List<Pt> m_strip = [];
			public Serialiser.LineStyle m_style = new();
			public Serialiser.Smooth m_smooth = new();
			public Serialiser.Width m_width = new();
			public Serialiser.Dashed m_dashed = new();
			public Serialiser.ArrowHeads m_arrow = new();
			public Serialiser.DataPoints m_data_points = new();
			public Serialiser.PerItemColour m_per_item_colour = new();
			public static implicit operator bool(Block? b) => b != null && (b.m_lines.Count > 0 || b.m_strip.Count > 0);
		}

		private readonly List<Block> m_blocks = [];
		private Block m_current = new();

		public LdrLine style(ELineStyle sty)
		{
			m_current.m_style = new(sty);
			return this;
		}
		public LdrLine per_item_colour(bool on = true)
		{
			m_current.m_per_item_colour = new(on);
			return this;
		}
		public LdrLine smooth(bool smooth = true)
		{
			m_current.m_smooth = new(smooth);
			return this;
		}
		public LdrLine width(float w)
		{
			m_current.m_width = new(w);
			return this;
		}
		public LdrLine data_points(float size, Colour32? colour = null, EPointStyle? style = null)
		{
			return data_points(new v2(size, size), colour, style);
		}
		public LdrLine data_points(v2 size, Colour32? colour = null, EPointStyle? style = null)
		{
			m_current.m_data_points = new Serialiser.DataPoints(size, colour, style);
			return this;
		}
		public LdrLine dashed(v2 dash)
		{
			m_current.m_dashed = new Serialiser.Dashed(dash);
			return this;
		}
		public LdrLine arrow(EArrowType style = EArrowType.Fwd, float size = 10f)
		{
			m_current.m_arrow = new Serialiser.ArrowHeads(style, size);
			return this;
		}

		// Lines
		public LdrLine line(v4 a, v4 b, Colour32? colour = null)
		{
			m_current.m_lines.Add(new Ln{ a = a, b = b, col = colour ?? Colour32.White });
			if (colour != null) m_current.m_per_item_colour.m_per_item_colour = true;
			m_current.m_strip.Clear();
			return this;
		}
		public LdrLine line(v3 a, v3 b, Colour32? colour = null)
		{
			return line(a.w1, b.w1, colour);
		}
		public LdrLine lines(Span<v4> verts, Span<int> indices)
		{
			Debug.Assert((indices.Length % 2) == 0);
			for (var i = 0; i < indices.Length; i += 2)
				line(verts[indices[i+0]], verts[indices[i+1]]);

			return this;
		}

		// Line strip
		public LdrLine strip(v4 start, Colour32? colour = null)
		{
			m_current.m_strip.Add(new Pt { a = start, col = colour ?? Colour32.White });
			if (colour != null) m_current.m_per_item_colour.m_per_item_colour = true;
			m_current.m_lines.Clear();
			return this;
		}
		public LdrLine strip(v3 start, Colour32? colour = null)
		{
			return strip(start.w1, colour);
		}
		public LdrLine line_to(v4 pt, Colour32? colour = null)
		{
			if (m_current.m_strip.Count == 0) strip(pt, colour);
			return strip(pt, colour);
		}
		public LdrLine line_to(v3 pt, Colour32? colour = null)
		{
			return line_to(pt.w1, colour);
		}

		public LdrLine new_block()
		{
			m_blocks.Add(m_current);
			m_current = new();
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Line, m_name, m_colour, () =>
			{
				void WriteBlock(Block block)
				{
					res.Append(block.m_style, block.m_smooth, block.m_width, block.m_dashed, block.m_arrow, block.m_data_points, block.m_per_item_colour);
					res.Write(EKeyword.Data, () =>
					{
						foreach (var line in block.m_lines)
						{
							res.Append(line.a.xyz, line.b.xyz);
							if (block.m_per_item_colour)
								res.Append(line.col);
						}
						foreach (var pt in block.m_strip)
						{
							res.Append(pt.a.xyz);
							if (block.m_per_item_colour)
								res.Append(pt.col);
						}
					});
				}

				foreach (var block in m_blocks)
					WriteBlock(block);
				if (m_current)
					WriteBlock(m_current);

				base.WriteTo(res);
			});
		}
	}
	public class LdrCoordFrame : LdrBase<LdrCoordFrame>
	{
		private Serialiser.Scale m_scale = new();
		private Serialiser.LeftHanded m_lh = new();

		// Scale size
		public new LdrCoordFrame scale(float s)
		{
			m_scale = s;
			return this;
		}

		// Left handed axis
		public LdrCoordFrame left_handed(bool lh = true)
		{
			m_lh = new(lh);
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.CoordFrame, m_name, m_colour, () =>
			{
				res.Append(m_scale, m_lh);
				base.WriteTo(res);
			});
		}
	}
	public class LdrTriangle : LdrBase<LdrTriangle>
	{
		private struct Tri { public v4 a, b, c; public Colour32 col; };
		private readonly List<Tri> m_tris = [];
		private Serialiser.PerItemColour m_per_item_colour = new();

		public LdrTriangle tri(v4 a, v4 b, v4 c, Colour32? colour = null)
		{
			m_tris.Add(new Tri{ a = a, b = b, c = c, col = colour ?? Colour32.White });
			if (colour != null) m_per_item_colour.m_per_item_colour = true;
			return this;
		}
		public LdrTriangle tris(Span<v4> verts, Span<int> faces)
		{
			Debug.Assert((faces.Length % 3) == 0);
			for (var i = 0; i < faces.Length; i += 3)
				tri(verts[faces[i+0]], verts[faces[i+1]], verts[faces[i+2]]);

			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Triangle, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, () =>
				{
					foreach (var tri in m_tris)
					{
						res.Append(tri.a.xyz, tri.b.xyz, tri.c.xyz);
						if (m_per_item_colour.m_per_item_colour)
							res.Append(tri.col);
					}
				});
				base.WriteTo(res);
			});
		}
	}
	public class LdrPlane : LdrBase<LdrPlane>
	{
		private v4 m_position = v4.Origin;
		private v4 m_direction = v4.ZAxis;
		private v2 m_wh = new(1,1);
		private LdrTexture m_tex = new();

		public LdrPlane plane(v4 p)
		{
			pos((p.xyz * -p.w).w1);
			ori(Math_.Normalise(p.xyz.w0), EAxisId.PosZ);
			return this;
		}
		public LdrPlane wh(float width, float height)
		{
			m_wh = new(width, height);
			return this;
		}
		public LdrPlane wh(v2 wh)
		{
			m_wh = wh;
			return this;
		}

		public LdrTexture texture()
		{
			return m_tex;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Plane, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, m_wh);
				m_tex.WriteTo(res);
				base.WriteTo(res);
			});
		}
	}
	public class LdrCircle : LdrBase<LdrCircle>
	{
		private float m_radius = 1.0f;

		public LdrCircle radius(float r)
		{
			m_radius = r;
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Circle, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, m_radius);
				base.WriteTo(res);
			});
		}
	}
	public class LdrSphere : LdrBase<LdrSphere>
	{
		private v4 m_radius = new(1.0f);

		// Radius
		public LdrSphere radius(float r)
		{
			return radius(new v4(r));
		}
		public LdrSphere radius(v4 r)
		{
			m_radius = r;
			return this;
		}

		// Create from bounding sphere
		public LdrSphere bsphere(BSphere bsphere)
		{
			if (bsphere == BSphere.Reset) return this;
			return radius(bsphere.Radius).pos(bsphere.Centre);
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Sphere, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, m_radius.xyz);
				base.WriteTo(res);
			});
		}

	}
	public class LdrBox : LdrBase<LdrBox>
	{
		private v4 m_dim = new(0.5f);

		// Box dimensions
		public LdrBox radii(float radii)
		{
			return dim(radii * 2);
		}
		public LdrBox radii(v4 radii)
		{
			return dim(radii * 2);
		}
		public LdrBox dim(float d)
		{
			return dim(d, d, d);
		}
		public LdrBox dim(float sx, float sy, float sz)
		{
			return dim(new v4(sx, sy, sz, 0));
		}
		public LdrBox dim(v4 dim)
		{
			m_dim = new v4(dim.x, dim.y, dim.z, 0);
			return this;
		}

		// Create from bounding box
		public LdrBox bbox(BBox bbox)
		{
			if (bbox == BBox.Reset) return this;
			return dim(2 * bbox.Radius).pos(bbox.Centre);
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Box, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, m_dim.xyz);
				base.WriteTo(res);
			});
		}
	}
	public class LdrCylinder : LdrBase<LdrCylinder>
	{
		private v2 m_radius = new(0.5f);
		private Serialiser.Scale2 m_scale = new();
		private float m_height = 1.0f;

		// Height/Radius
		public LdrCylinder cylinder(float height, float radius)
		{
			return cylinder(height, radius, radius);
		}
		public LdrCylinder cylinder(float height, float radius_base, float radius_tip)
		{
			m_radius = new v2(radius_base, radius_tip);
			m_height = height;
			return this;
		}

		// Scale
		public LdrCylinder scale(Serialiser.Scale2 scale)
		{
			m_scale = scale;
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Cylinder, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, m_height, m_radius.x, m_radius.y);
				res.Append(m_scale);
				base.WriteTo(res);
			});
		}
	}
	public class LdrCone : LdrBase<LdrCone>
	{
		private v2 m_distance = new(0f, 1f);
		private Serialiser.Scale2 m_scale = new();
		private float m_angle = 45.0f;

		// Height/Radius
		public LdrCone angle(float solid_angle_deg)
		{
			m_angle = solid_angle_deg;
			return this;
		}
		public LdrCone height(float height)
		{
			m_distance = new v2(m_distance.x, m_distance.x + height);
			return this;
		}
		public LdrCone dist(float dist0, float dist1)
		{
			m_distance = new v2(dist0, dist1);
			return this;
		}

		// Scale
		public LdrCone scale(Serialiser.Scale2 scale)
		{
			m_scale = scale;
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Cone, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, m_angle, m_distance.x, m_distance.y);
				res.Append(m_scale);
				base.WriteTo(res);
			});
		}
	}
	public class LdrFrustum : LdrBase<LdrFrustum>
	{
		private v2 m_wh = new(1f, 1f);
		private v2 m_nf = new(0.1f, 2f);
		private float m_fovY = Math_.TauBy4F;
		private float m_aspect = 1f;
		private bool m_ortho = false;

		// Orthographic
		public LdrFrustum ortho(bool ortho = true)
		{
			m_ortho = ortho;
			return this;
		}

		// Near/Far
		public LdrFrustum nf(float n, float f)
		{
			m_nf = new v2(n, f);
			return this;
		}
		public LdrFrustum nf(v2 nf_)
		{
			return nf(nf_.x, nf_.y);
		}

		// Frustum dimensions
		public LdrFrustum wh(float w, float h)
		{
			m_wh = new v2(w, h);
			m_fovY = 0;
			m_aspect = 0;
			return this;
		}
		public LdrFrustum wh(v2 sz)
		{
			return wh(sz.x, sz.y);
		}

		// Frustum angles
		public LdrFrustum fov(float fovY, float aspect)
		{
			m_ortho = false;
			m_wh = v2.Zero;
			m_fovY = fovY;
			m_aspect = aspect;
			return this;
		}

		//// From maths frustum
		//public LdrFrustum frustum(Frustum const& f)
		//{
		//	return nf(0, f.zfar()).fov(f.fovY(), f.aspect());
		//}

		// From projection matrix
		public LdrFrustum proj(m4x4 c2s)
		{
			if (c2s.w.w == 1) // If orthographic
			{
				var rh = -Math_.Sign(c2s.z.z);
				var zn = Math_.Div(c2s.w.z, c2s.z.z, 0.0f);
				var zf = Math_.Div(zn * (c2s.w.z - rh), c2s.w.z, 1.0f);
				var w = 2.0f / c2s.x.x;
				var h = 2.0f / c2s.y.y;
				return ortho(true).nf(zn, zf).wh(w,h);
			}
			else // Otherwise perspective
			{
				var rh = -Math_.Sign(c2s.z.w);
				var zn = rh * c2s.w.z / c2s.z.z;
				var zf = Math_.Div(zn * c2s.z.z, (rh + c2s.z.z), zn * 1000.0f);
				var w = 2.0f * zn / c2s.x.x;
				var h = 2.0f * zn / c2s.y.y;
				return ortho(false).nf(zn, zf).wh(w, h);
			}
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			if (m_ortho)
			{
				res.Write(EKeyword.Box, m_name, m_colour, () =>
				{
					res.Write(EKeyword.Data, m_wh.x, m_wh.y, m_nf.y - m_nf.x);
					res.Append(new Serialiser.O2W(new v4(0, 0, -0.5f * (float)(m_nf.x + m_nf.y), 1)));
					base.WriteTo(res);
				});
			}
			else if (m_wh != v2.Zero)
			{
				res.Write(EKeyword.FrustumWH, m_name, m_colour, () =>
				{
					res.Write(EKeyword.Data, m_wh.x, m_wh.y, m_nf.x, m_nf.y);
					base.WriteTo(res);
				});
			}
			else
			{
				res.Write(EKeyword.FrustumFA, m_name, m_colour, () =>
				{
					res.Write(EKeyword.Data, Math_.RadiansToDegrees(m_fovY), m_aspect, m_nf.x, m_nf.y);
					base.WriteTo(res);
				});
			}
		}

	}
	public class LdrModel : LdrBase<LdrModel>
	{
		private string m_filepath = string.Empty;
		private LdrAnimation? m_anim = null;
		private bool m_no_materials = false;

		/// <summary>Model filepath</summary>
		public LdrModel filepath(string filepath)
		{
			m_filepath = Path_.Canonicalise(filepath);
			return this;
		}

		/// <summary>Add animation to the model</summary>
		public LdrAnimation anim()
		{
			m_anim ??= new();
			return m_anim;
		}

		public LdrModel no_materials(bool on = true)
		{
			m_no_materials = on;
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Model, m_name, m_colour, () =>
			{
				res.Write(EKeyword.FilePath, $"\"{m_filepath}\"");
				m_anim?.WriteTo(res);
				if (m_no_materials) res.Write(EKeyword.NoMaterials);
				base.WriteTo(res);
			});
		}
	}
	public class LdrInstance : LdrBase<LdrInstance>
	{
		private string m_address = string.Empty;
		private LdrAnimation? m_anim = null;

		/// <summary>Set the object address that this is an instance of</summary>
		public LdrInstance inst(string address)
		{
			m_address = address;
			return this;
		}

		/// <summary>Add animation to the instance</summary>
		public LdrAnimation anim()
		{
			m_anim ??= new();
			return m_anim;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Instance, m_name, m_colour, () =>
			{
				res.Write(EKeyword.Data, m_address);
				m_anim?.WriteTo(res);
				base.WriteTo(res);
			});
		}
	}
	public class LdrText : LdrBase<LdrText>
	{
		private class Block
		{
			public readonly StringBuilder m_text = new();
			public LdrFont? m_font = null;
			public bool m_screen_space = false;
			public bool m_billboard = false;
			public bool m_billboard_3d = false;
			public Colour32? m_back_colour = null;
			public static implicit operator bool(Block? b) => b != null && b.m_text.Length != 0;
		}
		private readonly List<Block> m_blocks = [];
		private Block m_current = new();

		/// <summary>Direct access to the string builder for the current block</summary>
		public new StringBuilder Text => m_current.m_text;

		/// <summary>Append text to the current block</summary>
		public LdrText text(string s)
		{
			m_current.m_text.Append(s);
			return this;
		}

		/// <summary>Screen space text</summary>
		public LdrText screen_space(bool on = true)
		{
			m_current.m_screen_space = on;
			return this;
		}

		/// <summary>Billboard text</summary>
		public LdrText billboard(bool on = true)
		{
			m_current.m_billboard = on;
			return this;
		}

		/// <summary>3D Billboard text</summary>
		public LdrText billboard_3d(bool on = true)
		{
			m_current.m_billboard_3d = on;
			return this;
		}

		/// <summary>Background colour</summary>
		public LdrText back_colour(Colour32 col)
		{
			m_current.m_back_colour = col;
			return this;
		}

		/// <summary>The text font</summary>
		public new LdrFont font()
		{
			m_current.m_font ??= new();
			return m_current.m_font;
		}

		// 
		public LdrText new_block()
		{
			m_blocks.Add(m_current);
			m_current = new();
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Text, m_name, m_colour, () =>
			{
				void WriteBlock(Block block)
				{
					if (block.m_font != null)
						block.m_font.WriteTo(res);
					if (block.m_screen_space)
						res.Write(EKeyword.ScreenSpace);
					if (block.m_billboard)
						res.Write(EKeyword.Billboard);
					if (block.m_billboard_3d)
						res.Write(EKeyword.Billboard3D);
					if (block.m_back_colour != null)
						res.Write(EKeyword.BackColour, block.m_back_colour.Value);

					res.Write(EKeyword.Data, $"\"{block.m_text.ToString()}\"");
				}

				foreach (var block in m_blocks)
					WriteBlock(block);
				if (m_current)
					WriteBlock(m_current);

				base.WriteTo(res);
			});
		}

	}
	public class LdrGroup : LdrBase<LdrGroup>
	{
		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Group, m_name, m_colour, () =>
			{
				base.WriteTo(res);
			});
		}
	}
	public class LdrCommands : LdrBase<LdrCommands>
	{
		private struct Cmd
		{
			public ECommandId m_id;
			public List<object> m_params;
		}

		private readonly List<Cmd> m_cmds = [];

		// Add objects created by this script to scene 'scene_id'
		public LdrCommands add_to_scene(int scene_id)
		{
			m_cmds.Add(new Cmd{ m_id = ECommandId.AddToScene, m_params = [scene_id] });
			return this;
		}

		// Apply a transform to an object with the given name
		public LdrCommands object_transform(string object_name, m4x4 o2w)
		{
			m_cmds.Add(new Cmd{ m_id = ECommandId.ObjectToWorld, m_params = [new Serialiser.StringWithLength(object_name), o2w] });
			return this;
		}

		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.Commands, () =>
			{
				foreach (var cmd in m_cmds)
				{
					res.Write(EKeyword.Data, () =>
					{
						res.Append((int)cmd.m_id);
						foreach (var p in cmd.m_params)
						{
							if (p is bool   bool_ ) { res.Append(bool_ ); continue; }
							if (p is int    int_  ) { res.Append(int_  ); continue; }
							if (p is float  float_) { res.Append(float_); continue; }
							if (p is Serialiser.StringWithLength str_  ) { res.Append(str_  ); continue; }
							if (p is v2     v2_   ) { res.Append(v2_   ); continue; }
							if (p is v4     v4_   ) { res.Append(v4_   ); continue; }
							if (p is m4x4   m4_   ) { res.Append(m4_   ); continue; }
						}
					});
				}
			});
		}
	}
	public class LdrBinaryStream : LdrBase<LdrBinaryStream>
	{
		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.BinaryStream);
		}
	}
	public class LdrTextStream : LdrBase<LdrTextStream>
	{
		/// <inheritdoc/>
		public override void WriteTo(IWriter res)
		{
			res.Write(EKeyword.TextStream);
		}
	}

	// DEPRECATED 
	#region DEPRECATED 
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
			using var sw = new StreamWriter(OutFile, app);
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
		public static string Billboard3D(bool billboard = true)
		{
			return billboard ? "*Billboard3D" : string.Empty;
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
			switch (part)
			{
				case null: break;
				case string str: m_sb.Append(str); break;
				case Color col: m_sb.Append(Ldr.Col(col)); break;
				case v4 vec4: m_sb.Append(Ldr.Vec3(vec4)); break;
				case v2 vec2: m_sb.Append(Ldr.Vec2(vec2)); break;
				case m4x4 o2w: m_sb.Append(Ldr.Mat4x4(o2w)); break;
				case AxisId axisid: m_sb.Append(Ldr.AxisId(axisid.Id)); break;
				case EAxisId axisid2: m_sb.Append(Ldr.AxisId(axisid2)); break;
				case IEnumerable:
				{
					foreach (var x in (IEnumerable)part)
						Append(" ").Append(x);
					break;
				}
				default:
				{
					if (part.GetType().IsAnonymousType())
					{
						Append("{");
						foreach (var prop in part.GetType().AllProps(BindingFlags.Public | BindingFlags.Instance))
						{
							if (prop is null || prop.GetValue(part) is not object val) continue;
							Append($"*{prop.Name} {{", val, "}} ");
						}
						Append("}");
						return this;
					}
					else
					{
						m_sb.Append(part.ToString());
					}
					break;
				}
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
			return Group(name, colour, v4.Origin);
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
			var idx = 0;
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
			var idx = 0;
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

		public LdrBuilder Instance(string name, Colour32 colour, m4x4? o2w = null)
		{
			return Append("*Instance ", name, " ", colour, " {", Ldr.Transform(o2w), "}\n");
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
		public LdrTextBuilder Billboard3D()
		{
			m_builder.Append(Ldr.Billboard3D(true));
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
	#endregion
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	[TestFixture]
	public class TestLdrawBuilderText
	{
		[Test]
		public void TestTextBox()
		{
			var builder = new LDraw.Builder();
			builder.Box("b", new Colour32(0xFF00FF00)).dim(1).o2w(m4x4.Identity);
			var str = builder.ToString();
			Assert.Equal(str, "*Box b FF00FF00 {*Data {1 1 1}}");
		}

		[Test]
		public void TestLine()
		{
			var builder = new LDraw.Builder();
			builder.Line("a", 0xFF00FF00).style(LDraw.ELineStyle.LineStrip).line_to(v4.ZAxis.w1);
			var str = builder.ToString();
			Assert.Equal(str, "*Line a FF00FF00 {*Style {LineStrip} *Data {0 0 1 0 0 1}}");
		}
	}

	[TestFixture]
	public class TestLdrawBulderBinary
	{
		[Test]
		public void TestTextBox()
		{
			var builder = new LDraw.Builder();
			builder.Box("b", 0xFF00FF00).dim(1).o2w(m4x4.Identity);
			var mem = builder.ToBinary().ToArray();
			Assert.Equal(mem.Length, 49);
		}

		[Test]
		public void TestLine()
		{
			var builder = new LDraw.Builder();
			builder.Line("a", 0xFF00FF00).style(LDraw.ELineStyle.LineStrip).line_to(v4.ZAxis.w1);
			//builder.Save("E://Dump//line.bdr", LDraw.ESaveFlags.Binary);
			var mem = builder.ToBinary().ToArray();
		}

		[Test]
		public void TestArrow()
		{
			var builder = new LDraw.Builder();
			builder.Line("a", 0xFF00FF00).arrow(LDraw.EArrowType.Fwd).strip(v4.Origin).line_to(v4.ZAxis.w1);
			//builder.Save("E://Dump//arrow.bdr", LDraw.ESaveFlags.Binary);
			var mem = builder.ToBinary().ToArray();
		}

		[Test]
		public void TestCommands()
		{
			var builder = new LDraw.Builder();
			builder.Box("b", 0xFF00FF00).dim(1);
			builder.Command()
				.add_to_scene(0)
				.object_transform("b", m4x4.Transform(v4.ZAxis, 0.3f, v4.Origin));
			var mem = builder.ToBinary();

			#if false
			{
				using var ofile = File.Create("E:/Dump/LDraw/test.bdr");
				mem.CopyTo(ofile);
			}
			#endif
		}
	}
}

#region DEPRECATED
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
#endregion
#endif