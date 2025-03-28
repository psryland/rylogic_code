﻿namespace Rylogic.LDraw;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Rylogic.Gfx;
using Rylogic.Maths;

public class TextWriter : IWriter
{
	private readonly StringBuilder m_sb = new();

	/// <inheritdoc/>
	public void Write(EKeyword keyword, params object[] args)
	{
		Append(keyword);

		// Optional name/colour
		var i = 0;
		if (i != args.Length && args[i] is Serialiser.Name name)
		{
			Append(name.m_name);
			++i;
		}
		if (i != args.Length && args[i] is Serialiser.Colour colour)
		{
			Append(colour.m_colour);
			++i;
		}

		// Write section content
		Append("{");
		Append(args.Skip(i));
		Append("}");
	}

	/// <inheritdoc/>
	public void Append(params object[] args)
	{
		foreach (var arg in args)
		{
			if (arg == null)
				continue;
			else if (typeof(TextWriter).GetMethod(nameof(Append), BindingFlags.Instance | BindingFlags.NonPublic, null, [arg.GetType()], []) is MethodInfo meth)
				meth.Invoke(this, [arg]);
			else if (arg is Action action)
				action();
			else
				throw new Exception($"Unsupported type ({arg.GetType().Name}) for Append");
		}
	}
	private void Append(string s)
	{
		if (s.Length == 0) return;
		if (m_sb.Length != 0 && s[s.Length-1] != '}' && s[s.Length-1] != ')' && s[s.Length-1] != ' ' && m_sb[m_sb.Length - 1] != '{')
			m_sb.Append(' ');

		m_sb.Append(s);
	}
	private void Append(EKeyword keyword)
	{
		Append($"*{keyword}");
	}
	private void Append(bool b)
	{
		Append(b ? "true" : "false");
	}
	private void Append(int i)
	{
		Append(i.ToString());
	}
	private void Append(long i)
	{
		Append(i.ToString());
	}
	private void Append(float f)
	{
		Append(f.ToString());
	}
	private void Append(double f)
	{
		Append(f.ToString());
	}
	private void Append(uint u)
	{
		Append($"{u:X8}");
	}
	private void Append(Colour32 c)
	{
		Append(c.ARGB);
	}
	private void Append(v2 v)
	{
		Append(v.ToString());
	}
	private void Append(v3 v)
	{
		Append(v.ToString());
	}
	private void Append(v4 v)
	{
		Append(v.ToString());
	}
	private void Append(m4x4 m)
	{
		Append(m.ToString());
	}
	private void Append(View3d.EAddrMode addr)
	{
		switch (addr)
		{
			case View3d.EAddrMode.Wrap: Append("Wrap"); break;
			case View3d.EAddrMode.Mirror: Append("Mirror"); break;
			case View3d.EAddrMode.Clamp: Append("Clamp"); break;
			case View3d.EAddrMode.Border: Append("Border"); break;
			case View3d.EAddrMode.MirrorOnce: Append("MirrorOnce"); break;
			default: throw new Exception($"Unknown texture addressing mode ({addr})");
		}
	}
	private void Append(View3d.EFilter filter)
	{
		switch (filter)
		{
			case View3d.EFilter.Point: Append("Point"); break;
			case View3d.EFilter.PointPointLinear: Append("PointPointLinear"); break;
			case View3d.EFilter.PointLinearPoint: Append("PointLinearPoint"); break;
			case View3d.EFilter.PointLinearLinear: Append("PointLinearLinear"); break;
			case View3d.EFilter.LinearPointPoint: Append("LinearPointPoint"); break;
			case View3d.EFilter.LinearPointLinear: Append("LinearPointLinear"); break;
			case View3d.EFilter.LinearLinearPoint: Append("LinearLinearPoint"); break;
			case View3d.EFilter.Linear: Append("Linear"); break;
			case View3d.EFilter.Anisotropic: Append("Anisotropic"); break;
			default: throw new Exception($"Unknown texture addressing mode ({filter})");
		}
	}
	private void Append(EArrowType type)
	{
		switch (type)
		{
			case EArrowType.Fwd: Append("Fwd"); break;
			case EArrowType.Back: Append("Back"); break;
			case EArrowType.FwdBack: Append("FwdBack"); break;
			default: throw new Exception("Unknown arrow type");
		}
	}
	private void Append(EPointStyle style)
	{
		switch (style)
		{
			case EPointStyle.Square: Append("Square"); break;
			case EPointStyle.Circle: Append("Circle"); break;
			case EPointStyle.Triangle: Append("Triangle"); break;
			case EPointStyle.Star: Append("Star"); break;
			case EPointStyle.Annulus: Append("Annulus"); break;
			default: throw new Exception($"Unknown arrow type ({style})");
		}
	}
	private void Append(Serialiser.VariableInt vint)
	{
		Append(vint.m_value);
	}
	private void Append(Serialiser.StringWithLength str)
	{
		Append(str.m_value);
	}
	private void Append(Serialiser.Name n)
	{
		if (n.m_name.Length == 0) return;
		Write(EKeyword.Name, n.m_name);
	}
	private void Append(Serialiser.Colour c)
	{
		if (c.m_colour == 0xFFFFFFFF) return;
		Write(c.m_kw, c.m_colour);
	}
	private void Append(Serialiser.Size s)
	{
		if (s.m_size == 0) return;
		Write(EKeyword.Size, s.m_size);
	}
	private void Append(Serialiser.Size2 s)
	{
		if (s.m_size == v2.Zero) return;
		Write(EKeyword.Size, s.m_size);
	}
	private void Append(Serialiser.Width w)
	{
		if (w.m_width == 0) return;
		Write(EKeyword.Width, w.m_width);
	}
	private void Append(Serialiser.Scale2 s)
	{
		if (s.m_scale == v2.One) return;
		Write(EKeyword.Scale, s.m_scale);
	}
	private void Append(Serialiser.Scale3 s)
	{
		if (s.m_scale == v3.One) return;
		Write(EKeyword.Scale, s.m_scale);
	}
	private void Append(Serialiser.PerItemColour c)
	{
		if (!c.m_per_item_colour) return;
		Write(EKeyword.PerItemColour);
	}
	private void Append(Serialiser.Depth d)
	{
		if (d.m_depth == false) return;
		Write(EKeyword.Depth);
	}
	private void Append(Serialiser.Wireframe w)
	{
		if (!w.m_wire) return;
		Write(EKeyword.Wireframe);
	}
	private void Append(Serialiser.Solid s)
	{
		if (!s.m_solid) return;
		Write(EKeyword.Solid);
	}
	private void Append(Serialiser.Alpha a)
	{
		if (!a.m_has_alpha) return;
		Write(EKeyword.Alpha);
	}
	private void Append(AxisId axis_id)
	{
		if (axis_id == EAxisId.None) return;
		Write(EKeyword.AxisId, (int)axis_id.Id);
	}
	private void Append(Serialiser.Pos p)
	{
		if (p.m_pos == v4.Origin) return;
		Write(EKeyword.O2W, () =>
		{
			Write(EKeyword.Pos, p.m_pos.xyz);
		});
	}
	private void Append(Serialiser.O2W o2w)
	{
		if (o2w.m_mat == m4x4.Identity)
			return;

		if (o2w.m_mat.rot == m3x4.Identity && o2w.m_mat.pos.w == 1)
		{
			Write(EKeyword.O2W, () =>
			{
				Write(EKeyword.Pos, o2w.m_mat.pos.xyz);
			});
			return;
		}

		Write(EKeyword.O2W, () =>
		{
			if (!Math_.IsAffine(o2w.m_mat)) Write(EKeyword.NonAffine);
			Write(EKeyword.M4x4, o2w.m_mat);
		});
	}

	public override string ToString() => m_sb.ToString();
}
