//***********************************************
// LDraw
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

using Rylogic.Maths;

namespace Rylogic.LDraw.Serialiser
{
	public struct Name(string? name = null)
	{
		public string m_name = name ?? string.Empty;
		public static implicit operator Name(string? name) => new(name);
	}
	public struct Colour(uint? colour = null, EKeyword? kw = null)
	{
		public uint m_colour = colour ?? 0xFFFFFFFF;
		public EKeyword m_kw = kw ?? EKeyword.Colour;
		public static implicit operator Colour(uint? colour) => new(colour);
	}
	public struct Size(float? size = null)
	{
		public float m_size = size ?? 0f;
		public static implicit operator Size(float? size) => new(size);
	}
	public struct Size2(v2? size = null)
	{
		public v2 m_size = size ?? v2.Zero;
		public static implicit operator Size2(v2? size) => new(size);
	}
	public struct Width(float? width = null)
	{
		public float m_width = width ?? 0f;
		public static implicit operator Width(float? width) => new(width);
	}
	public struct Scale2(v2? scale = null)
	{
		public v2 m_scale = scale ?? v2.One;
		public static implicit operator Scale2(v2? scale) => new(scale);
	}
	public struct Scale3(v3? scale = null)
	{
		public v3 m_scale = scale ?? v3.One;
		public static implicit operator Scale3(v3? scale) => new(scale);
	}
	public struct PerItemColour(bool? pic = null)
	{
		public bool m_per_item_colour = pic ?? false;
		public static implicit operator PerItemColour(bool? pic) => new(pic);
		public static implicit operator bool(PerItemColour pic) => pic.m_per_item_colour;
	}
	public struct Depth(bool? depth = null)
	{
		public bool m_depth = depth ?? false;
		public static implicit operator Depth(bool? depth) => new(depth);
		public static implicit operator bool(Depth d) => d.m_depth;
	}
	public struct Wireframe(bool? wire = null)
	{
		public bool m_wire = wire ?? false;
		public static implicit operator Wireframe(bool? wire) => new(wire);
		public static implicit operator bool(Wireframe w) => w.m_wire;
	}
	public struct Alpha(bool? alpha = null)
	{
		public bool m_has_alpha = alpha ?? false;
		public static implicit operator Alpha(bool? alpha) => new(alpha);
		public static implicit operator bool(Alpha a) => a.m_has_alpha;
	}
	public struct Solid(bool? solid = null)
	{
		public bool m_solid = solid ?? false;
		public static implicit operator Solid(bool? solid) => new(solid);
		public static implicit operator bool(Solid s) => s.m_solid;
	}
	public struct Pos(v4? pos = null)
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
	public struct VariableInt(int? value = null)
	{
		public int m_value = (value ?? 0) & 0x3FFFFFFF;
	}
	public struct StringWithLength(string? str = null)
	{
		public string m_value = str ?? string.Empty;
	}
}
