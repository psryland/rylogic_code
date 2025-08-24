﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.LDraw
{
	public class BinaryWriter : IWriter
	{
		private readonly MemoryStream m_res = new();

		[StructLayout(LayoutKind.Sequential)]
		private struct SectionHeader
		{
			public EKeyword keyword;
			public int size;
		}

		/// <inheritdoc/>
		public void Write(EKeyword keyword, params object[] args)
		{
			var ofs = m_res.Position;

			// Write a header
			Append(keyword);
			Append(0);

			// Optional name/colour
			var i = 0;
			if (i != args.Length && args[i] is Serialiser.Name name)
			{
				Append(name);
				++i;
			}
			if (i != args.Length && args[i] is Serialiser.Colour colour)
			{
				Append(colour);
				++i;
			}

			// Write section content
			Append(args.Skip(i));

			// The size of the section, excluding the header
			var size = (int)(m_res.Position - ofs - Marshal.SizeOf<SectionHeader>());

			// Update the section size
			m_res.Position = ofs + Marshal.SizeOf<int>();
			Append(size);
			m_res.Position = ofs + Marshal.SizeOf<SectionHeader>() + size;
		}

		/// <inheritdoc/>
		public void Append(params object[] args)
		{
			Append(args.AsEnumerable());
		}
		private void Append(IEnumerable<object> args)
		{
			foreach (var arg in args)
			{
				if (arg == null)
					continue;
				else if (typeof(BinaryWriter).GetMethod(nameof(Append), BindingFlags.Instance | BindingFlags.NonPublic, null, [arg.GetType()], []) is MethodInfo meth)
					meth.Invoke(this, [arg]);
				else if (arg is Action action)
					action();
				else
					throw new Exception($"Unsupported type ({arg.GetType().Name}) for Append");
			}
		}
		private void Append(byte[] bytes)
		{
			m_res.Write(bytes, 0, bytes.Length);
		}
		private void Append(Span<byte> bytes)
		{
#if NET5_0_OR_GREATER
		m_res.Write(bytes);
#else
			Append(bytes.ToArray());
#endif
		}
		private void Append(string s)
		{
			Append(Encoding.UTF8.GetBytes(s));
		}
		private void Append(EKeyword keyword)
		{
			Append(BitConverter.GetBytes((int)keyword));
		}
		private void Append(bool b)
		{
			m_res.WriteByte((byte)(b ? 1 : 0));
		}
		private void Append(int i)
		{
			Append(BitConverter.GetBytes(i));
		}
		private void Append(long i)
		{
			Append(BitConverter.GetBytes(i));
		}
		private void Append(float f)
		{
			Append(BitConverter.GetBytes(f));
		}
		private void Append(double f)
		{
			Append(BitConverter.GetBytes(f));
		}
		private void Append(uint u)
		{
			Append(BitConverter.GetBytes(u));
		}
		private void Append(Colour32 c)
		{
			Append(c.ARGB);
		}
		private void Append(v2 v)
		{
			Append(v.x);
			Append(v.y);
		}
		private void Append(v3 v)
		{
			Append(v.x);
			Append(v.y);
			Append(v.z);
		}
		private void Append(v4 v)
		{
			Append(v.x);
			Append(v.y);
			Append(v.z);
			Append(v.w);
		}
		private void Append(m4x4 m)
		{
			Append(m.x);
			Append(m.y);
			Append(m.z);
			Append(m.w);
		}
		private void Append(View3d.EAddrMode addr)
		{
			Append((int)addr);
		}
		private void Append(View3d.EFilter filter)
		{
			Append((int)filter);
		}
		private void Append(EArrowType type)
		{
			Append((int)type);
		}
		private void Append(EPointStyle style)
		{
			Append((int)style);
		}
		private void Append(Serialiser.VariableInt vint)
		{
			// Variable sized int, write 6 bits at a time: xx444444 33333322 22221111 11000000
			Span<byte> bits = stackalloc byte[5];

			var i = 5;
			for (var val = vint.m_value; val != 0 && i-- != 0; val >>= 6)
				bits[i] = (byte)(0x80 | (val & 0b00111111));

			Append(bits.Slice(i));
		}
		private void Append(Serialiser.StringWithLength str)
		{
			var u8str = Encoding.UTF8.GetBytes(str.m_value);
			Append(new Serialiser.VariableInt(u8str.Length));
			Append(u8str);
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
		private void Append(Serialiser.Scale s)
		{
			if (s.m_scale == 1f) return;
			Write(EKeyword.Scale, s.m_scale);
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
		private void Append(Serialiser.Hidden h)
		{
			if (!h.m_hide) return;
			Write(EKeyword.Hidden);
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
		private void Append(Serialiser.Smooth s)
		{
			if (!s.m_smooth) return;
			Write(EKeyword.Smooth);
		}
		private void Append(Serialiser.LeftHanded lh)
		{
			if (!lh.m_lh) return;
			Write(EKeyword.LeftHanded);
		}
		private void Append(Serialiser.Alpha a)
		{
			if (!a.m_has_alpha) return;
			Write(EKeyword.Alpha);
		}
		private void Append(Serialiser.AxisId a)
		{
			if (a.m_axis == EAxisId.None) return;
			Write(EKeyword.AxisId, (int)a.m_axis.Id);
		}
		private void Append(Serialiser.ArrowType a)
		{
			Write(EKeyword.Style, a.m_type);
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
				if (!Math_.IsAffine(o2w.m_mat))
					Write(EKeyword.NonAffine);
				Write(EKeyword.M4x4, o2w.m_mat);
			});
		}

		/// <summary>Return the script as a binary memory stream</summary>
		public MemoryStream ToBinary()
		{
			m_res.Position = 0;
			return m_res;
		}
	}
}