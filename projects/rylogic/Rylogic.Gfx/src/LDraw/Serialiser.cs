//***********************************************
// LDraw
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

using Rylogic.Maths;

namespace Rylogic.LDraw
{
	/// <summary>LDraw script writer interface</summary>
	public interface IWriter
	{
		/// <summary>Write a section to the script output</summary>
		void Write(EKeyword keyword, params object[] args);

		/// <summary>Append objects to the script</summary>
		void Append(params object[] items);
	}
}
namespace Rylogic.LDraw.Serialiser
{
	public class Name(string? name = null)
	{
		public string m_name = name ?? string.Empty;
		public static implicit operator Name(string? name) => new(name);
	}
	public class Colour(uint? colour = null, EKeyword? kw = null)
	{
		public uint m_colour = colour ?? 0xFFFFFFFF;
		public EKeyword m_kw = kw ?? EKeyword.Colour;
		public static implicit operator Colour(uint? colour) => new(colour);
	}
	public class Size(float? size = null)
	{
		public float m_size = size ?? 0f;
		public static implicit operator Size(float? size) => new(size);
	}
	public class Size2(v2? size = null)
	{
		public v2 m_size = size ?? v2.Zero;
		public static implicit operator Size2(v2? size) => new(size);
	}
	public class Width(float? width = null)
	{
		public float m_width = width ?? 0f;
		public static implicit operator Width(float? width) => new(width);
	}
	public class Scale(float? scale = null)
	{
		public float m_scale = scale ?? 1f;
		public static implicit operator Scale(float? scale) => new(scale);
	}
	public class Scale2(v2? scale = null)
	{
		public v2 m_scale = scale ?? v2.One;
		public static implicit operator Scale2(v2? scale) => new(scale);
	}
	public class Scale3(v3? scale = null)
	{
		public v3 m_scale = scale ?? v3.One;
		public static implicit operator Scale3(v3? scale) => new(scale);
	}
	public class PerItemColour(bool? pic = null)
	{
		public bool m_per_item_colour = pic ?? false;
		public static implicit operator PerItemColour(bool? pic) => new(pic);
		public static implicit operator bool(PerItemColour pic) => pic.m_per_item_colour;
	}
	public class Depth(bool? depth = null)
	{
		public bool m_depth = depth ?? false;
		public static implicit operator Depth(bool? depth) => new(depth);
		public static implicit operator bool(Depth d) => d.m_depth;
	}
	public class Wireframe(bool? wire = null)
	{
		public bool m_wire = wire ?? false;
		public static implicit operator Wireframe(bool? wire) => new(wire);
		public static implicit operator bool(Wireframe w) => w.m_wire;
	}
	public class Alpha(bool? alpha = null)
	{
		public bool m_has_alpha = alpha ?? false;
		public static implicit operator Alpha(bool? alpha) => new(alpha);
		public static implicit operator bool(Alpha a) => a.m_has_alpha;
	}
	public class Solid(bool? solid = null)
	{
		public bool m_solid = solid ?? false;
		public static implicit operator Solid(bool? solid) => new(solid);
		public static implicit operator bool(Solid s) => s.m_solid;
	}	
	public class Smooth(bool? smooth = null)
	{
		public bool m_smooth = smooth ?? false;
		public static implicit operator Smooth(bool? smooth) => new(smooth);
		public static implicit operator bool(Smooth s) => s.m_smooth;
	}
	public class LeftHanded(bool? lh = null)
	{
		public bool m_lh = lh ?? false;
		public static implicit operator LeftHanded(bool? lh) => new(lh);
		public static implicit operator bool(LeftHanded x) => x.m_lh;
	}
	public class AxisId(Maths.AxisId? axis = null)
	{
		public Maths.AxisId m_axis = axis ?? EAxisId.None;
		public static implicit operator AxisId(Maths.AxisId? axis) => new(axis);
	}
	public class ArrowType(EArrowType? type = null)
	{
		public EArrowType m_type = type ?? EArrowType.Fwd;
		public static implicit operator ArrowType(EArrowType? type) => new(type);
	}
	public class Pos(v4? pos = null)
	{
		public v4 m_pos = pos ?? v4.Origin;
		public Pos(m4x4 mat) : this(mat.pos) { }
		public static implicit operator Pos(v4? pos) => new(pos);
	}
	public class O2W(m4x4? o2w = null)
	{
		public m4x4 m_mat = o2w ?? m4x4.Identity;
		public O2W(v4 pos) : this(m4x4.Translation(pos)) { }
		public static implicit operator O2W(m4x4? o2w) => new(o2w);
	}
	public class VariableInt(int? value = null)
	{
		public int m_value = (value ?? 0) & 0x3FFFFFFF;
	}
	public class StringWithLength(string? str = null)
	{
		public string m_value = str ?? string.Empty;
	}
}
