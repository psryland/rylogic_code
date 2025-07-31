﻿//************************************
// LDraw Builder
//  Copyright (c) Rylogic Ltd 2006
//************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"

namespace pr::rdr12::ldraw
{
	// Write the contents of 'ldr' to a file
	inline void Write(std::string_view ldr, std::filesystem::path const& filepath, bool append = false)
	{
		if (ldr.empty()) return;
		std::ofstream file(filepath, append ? std::ios::app : std::ios::out);
		file << ldr;
		file.close();
	}
	inline void Write(std::wstring_view ldr, std::filesystem::path const& filepath, bool append = false)
	{
		if (ldr.empty()) return;
		std::wofstream file(filepath, append ? std::ios::app : std::ios::out);
		file << ldr;
		file.close();
	}
	inline void Write(std::span<std::byte const> ldr, std::filesystem::path const& filepath, bool append = false)
	{
		if (ldr.empty()) return;
		std::ofstream file(filepath, std::ios::binary | (append ? std::ios::app : std::ios::out));
		file.write(reinterpret_cast<char const*>(ldr.data()), static_cast<std::streamsize>(ldr.size()));
		file.close();
	}

	// Pretty format Ldraw script
	template <typename TStr> requires(requires(TStr t)
	{
		t.reserve(0);
		t.push_back('c');
		{ t.append(0, 'c') } -> std::convertible_to<TStr&>;
	})
	TStr FormatScript(TStr const& str)
	{
		TStr out = {};
		out.reserve(str.size());

		int indent = 0;
		for (auto c : str)
		{
			if (c == '{')
			{
				++indent;
				out.push_back(c);
				out.append(1,'\n').append(indent, '\t');
			}
			else if (c == '}')
			{
				--indent;
				out.append(1,'\n').append(indent, '\t');
				out.push_back(c);
			}
			else
			{
				out.push_back(c);
			}
		}
		return out;
	}

	// Ldr object fluent helper
	namespace fluent
	{
		template <typename T> concept WriterType = requires { true; }; // todo
		static_assert(WriterType<BinaryWriter>);
		static_assert(WriterType<TextWriter>);

		struct LdrPoint;
		struct LdrLine;
		struct LdrLineD;
		struct LdrArrow;
		struct LdrCoordFrame;
		struct LdrTriangle;
		struct LdrPlane;
		struct LdrCircle;
		struct LdrSphere;
		struct LdrBox;
		struct LdrBar;
		struct LdrCylinder;
		struct LdrCone;
		struct LdrSpline;
		struct LdrFrustum;
		struct LdrGroup;
		struct LdrCommands;
		struct LdrBinaryStream;
		struct LdrTextStream;

		struct LdrBuilder
		{
			// Notes:
			//  - You can write ldraw data to a stream using:
			//      stream << builder.ToText() << std::flush; or
			//      stream << builder.ToBinary() << std::flush;
			//  - Use a socket_stream for streaming to LDraw:
			//      pr::network::Winsock winsock;
			//      pr::network::socket_stream ldr("localhost", 1976);
			//      ldr << builder.ToText(false) << std::flush;

			using ObjPtr = std::unique_ptr<LdrBuilder>;
			using ObjCont = std::vector<ObjPtr>;

			ObjCont m_objects;

			LdrBuilder() = default;
			LdrBuilder(LdrBuilder&&) = default;
			LdrBuilder(LdrBuilder const&) = delete;
			LdrBuilder& operator=(LdrBuilder&&) = default;
			LdrBuilder& operator=(LdrBuilder const&) = delete;
			virtual ~LdrBuilder() = default;

			// Reset the builder
			LdrBuilder& Clear(int count = -1)
			{
				if (count >= 0 && count < m_objects.size())
					m_objects.resize(m_objects.size() - count);
				else
					m_objects.clear();

				return *this;
			}

			// Object types
			LdrPoint& Point(Name name = {}, Colour colour = Colour());
			LdrLine& Line(Name name = {}, Colour colour = Colour());
			LdrLineD& LineD(Name name = {}, Colour colour = Colour());
			LdrArrow& Arrow(Name name = {}, Colour colour = Colour());
			LdrCoordFrame& CoordFrame(Name name = {}, Colour colour = Colour());
			LdrTriangle& Triangle(Name name = {}, Colour colour = Colour());
			LdrPlane& Plane(Name name = {}, Colour colour = Colour());
			LdrCircle& Circle(Name name = {}, Colour colour = Colour());
			LdrSphere& Sphere(Name name = {}, Colour colour = Colour());
			LdrBox& Box(Name name = {}, Colour colour = Colour());
			LdrBar& Bar(Name name = {}, Colour colour = Colour());
			LdrCylinder& Cylinder(Name name = {}, Colour colour = Colour());
			LdrCone& Cone(Name name = {}, Colour colour = Colour());
			LdrSpline& Spline(Name name = {}, Colour colour = Colour());
			LdrFrustum& Frustum(Name name = {}, Colour colour = Colour());
			LdrGroup& Group(Name name = {}, Colour colour = Colour());
			LdrCommands& Command(Name name = {}, Colour colour = Colour());

			// Switch data stream modes
			LdrBuilder& BinaryStream();
			LdrBuilder& TextStream();

			// Extension objects. Use: `builder._<LdrCustom>("name", 0xFFFFFFFF)`
			template <typename LdrCustom> requires std::is_base_of_v<LdrBuilder, LdrCustom>
			LdrCustom& _(std::string_view name = "", Colour colour = Colour())
			{
				auto ptr = new LdrCustom;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).colour(colour);
			}

			// Wrap all objects into a group
			LdrBuilder& WrapAsGroup(Name name = {}, Colour colour = Colour());

			// Serialise the ldr script to a string
			textbuf ToText(bool pretty) const
			{
				textbuf out;
				Write(out);
				if (pretty) out = FormatScript(out);
				return out;
			}

			// Serialise the ldr script to binary
			bytebuf ToBinary() const
			{
				bytebuf out;
				Write(out);
				return out;
			}

			// Serialise to 'out'
			virtual void Write(textbuf& out) const
			{
				for (auto& obj : m_objects)
					obj->Write(out);
			}
			virtual void Write(bytebuf& out) const
			{
				for (auto& obj : m_objects)
					obj->Write(out);
			}

			// Write the script to a file
			LdrBuilder& Write(std::filesystem::path const& filepath)
			{
				return Write(filepath, false, false);
			}
			LdrBuilder& Write(std::filesystem::path const& filepath, bool pretty, bool append)
			{
				textbuf out;
				Write(out);
				if (pretty) out = FormatScript(out);
				ldraw::Write(out, filepath, append);
				return *this;
			}
		};

		template <typename Derived>
		struct LdrBase :LdrBuilder
		{
			LdrBase()
				: m_name()
				, m_colour()
				, m_o2w(m4x4::Identity())
				, m_wire()
				, m_axis_id(pr::AxisId::None)
				, m_solid()
			{}

			// Object name
			Derived& name(Name n)
			{
				m_name = n;
				return static_cast<Derived&>(*this);
			}
			Name m_name;

			// Object colour
			Derived& colour(Colour c)
			{
				m_colour = c;
				m_colour.m_kw = EKeyword::Colour;
				return static_cast<Derived&>(*this);
			}
			Colour m_colour;

			// Object colour mask
			Derived& colour_mask(Colour c)
			{
				m_colour_mask = c;
				m_colour_mask.m_kw = EKeyword::ColourMask;
				return static_cast<Derived&>(*this);
			}
			Colour m_colour_mask;

			// Object to world transform
			Derived& o2w(m4x4 const& o2w)
			{
				m_o2w.m_mat = o2w * m_o2w.m_mat;
				return static_cast<Derived&>(*this);
			}
			Derived& o2w(m3x4 const& rot, v4 const& pos)
			{
				m_o2w.m_mat = m4x4{ rot, pos } * m_o2w.m_mat;
				return static_cast<Derived&>(*this);
			}
			Derived& ori(v4 const& dir, pr::AxisId axis = pr::AxisId::PosZ)
			{
				return ori(m3x4::Rotation(axis.vec(), dir));
			}
			Derived& ori(m3x4 const& rot)
			{
				return o2w(rot, v4::Origin());
			}
			Derived& pos(float x, float y, float z)
			{
				return o2w(m4x4::Translation(x, y, z));
			}
			Derived& pos(v4_cref pos)
			{
				return o2w(m4x4::Translation(pos));
			}
			Derived& scale(float s)
			{
				return scale(s, s, s);
			}
			Derived& scale(float sx, float sy, float sz)
			{
				return ori(m3x4::Scale(sx, sy, sz));
			}
			Derived& scale(v4_cref scale)
			{
				return ori(m3x4::Scale(scale.x, scale.y, scale.z));
			}
			Derived& quat(pr::quat const& q)
			{
				return o2w(m4x4::Transform(q, v4::Origin()));
			}
			Derived& euler(float pitch_deg, float yaw_deg, float roll_deg)
			{
				return ori(m3x4::Rotation(
					DegreesToRadians(pitch_deg),
					DegreesToRadians(yaw_deg),
					DegreesToRadians(roll_deg)));
			}
			O2W m_o2w;

			// Wire frame
			Derived& wireframe(bool w = true)
			{
				m_wire.m_wire = w;
				return static_cast<Derived&>(*this);
			}
			Wireframe m_wire;

			// Axis id
			Derived& axis(pr::AxisId axis_id)
			{
				m_axis_id = axis_id;
				return static_cast<Derived&>(*this);
			}
			AxisId m_axis_id;

			// Solid
			Derived& solid(bool s = true)
			{
				m_solid.m_solid = s;
				return static_cast<Derived&>(*this);
			}
			Solid m_solid;

			// Copy all modifiers from another object
			template <typename D> Derived& modifiers(LdrBase<D> const& rhs)
			{
				m_name = rhs.m_name;
				m_colour = rhs.m_colour;
				m_o2w = rhs.m_o2w;
				m_wire = rhs.m_wire;
				m_axis_id = rhs.m_axis_id;
				return static_cast<Derived&>(*this);
			}

			// Write nested objects to 'out'
			virtual void Write(textbuf& out) const override
			{
				auto const& derived = *static_cast<Derived const*>(this);
				derived.Derived::WriteTo<TextWriter>(out);
			}
			virtual void Write(bytebuf& out) const override 
			{
				auto const& derived = *static_cast<Derived const*>(this);
				derived.Derived::WriteTo<BinaryWriter>(out);
			}

			// Write ldraw script to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Append(out, m_axis_id, m_wire, m_solid, m_colour_mask, m_o2w);
				LdrBuilder::Write(out);
			}
		};

		// Modifiers
		struct LdrTexture
		{
			std::filesystem::path m_filepath;
			EAddrMode m_addr[2];
			EFilter m_filter;
			Alpha m_has_alpha;
			O2W m_t2s;

			LdrTexture()
				: m_filepath()
				, m_addr()
				, m_filter(EFilter::Linear)
				, m_t2s()
				, m_has_alpha()
			{
			}

			// Texture filepath
			LdrTexture& path(std::filesystem::path filepath)
			{
				m_filepath = filepath;
				return *this;
			}

			// Addressing mode
			LdrTexture& addr(EAddrMode addrU, EAddrMode addrV)
			{
				m_addr[0] = addrU;
				m_addr[1] = addrV;
				return *this;
			}

			// Filtering mode
			LdrTexture& filter(EFilter filter)
			{
				m_filter = filter;
				return *this;
			}

			// Texture to surface transform
			LdrTexture& t2s(O2W t2s)
			{
				m_t2s = t2s;
				return *this;
			}

			// Has alpha flag
			LdrTexture& alpha(Alpha has_alpha)
			{
				m_has_alpha = has_alpha;
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				if (m_filepath.empty()) return;
				Writer::Write(out, EKeyword::Texture, [&]
				{
					Writer::Write(out, EKeyword::FilePath, "\"", m_filepath.string(), "\"");
					Writer::Write(out, EKeyword::Addr, m_addr[0], m_addr[1]);
					Writer::Write(out, EKeyword::Filter, m_filter);
					Writer::Append(out, m_has_alpha);
					Writer::Append(out, m_t2s);
				});
			}
		};

		// Object types
		struct LdrPoint :LdrBase<LdrPoint>
		{
			struct Point { v4 pt; Colour col; };
			std::vector<Point> m_points;
			Size2 m_size;
			Depth m_depth;
			EPointStyle m_style;
			PerItemColour m_per_item_colour;

			LdrPoint()
				: m_points()
				, m_size()
				, m_style()
				, m_per_item_colour()
			{}

			// Points
			LdrPoint& pt(v4_cref point, Colour colour)
			{
				pt(point);
				m_points.back().col = colour;
				m_per_item_colour = true;
				return *this;
			}
			LdrPoint& pt(v4_cref point)
			{
				m_points.push_back({ point, {} });
				return *this;
			}

			// Point size (in pixels if depth == false, in world space if depth == true)
			LdrPoint& size(float s)
			{
				m_size = v2(s);
				return *this;
			}
			LdrPoint& size(v2 s)
			{
				m_size = s;
				return *this;
			}

			// Points have depth
			LdrPoint& depth(bool d)
			{
				m_depth = d;
				return *this;
			}

			// Point style
			LdrPoint& style(EPointStyle s)
			{
				m_style = s;
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Point, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Style, m_style);
					Writer::Append(out, m_size, m_depth, m_per_item_colour);
					Writer::Write(out, EKeyword::Data, [&]
					{
						for (auto& point : m_points)
						{
							Writer::Append(out, point.pt.xyz);
							if (m_per_item_colour)
								Writer::Append(out, point.col);
						}
					});
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrLine :LdrBase<LdrLine>
		{
			struct Line { v4 a, b; Colour col; };
			pr::vector<Line> m_lines;
			Smooth m_smooth;
			Width m_width;
			bool m_strip;
			PerItemColour m_per_item_colour;

			LdrLine()
				:m_lines()
				,m_smooth()
				,m_width()
				,m_strip()
				,m_per_item_colour()
			{}

			// Smooth line
			LdrLine& smooth(bool smooth = true)
			{
				m_smooth = smooth;
				return *this;
			}

			// Line width
			LdrLine& width(Width w)
			{
				m_width = w;
				return *this;
			}

			// Lines
			LdrLine& line(v4_cref a, v4_cref b, Colour colour)
			{
				line(a, b);
				m_lines.back().col = colour;
				m_per_item_colour = true;
				return *this;
			}
			LdrLine& line(v4_cref a, v4_cref b)
			{
				m_lines.push_back({ a, b, {} });
				return *this;
			}
			LdrLine& lines(std::span<v4 const> verts, std::span<int const> indices)
			{
				assert((isize(indices) % 2) == 0);
				for (int const* i = indices.data(), *iend = i + indices.size(); i < iend; i += 2)
					line(verts[*(i+0)], verts[*(i+1)]);

				return *this;
			}

			// Add points by callback function
			template <std::invocable<void(int, v4&, v4&)> EnumLines>
			LdrLine& lines(EnumLines lines)
			{
				v4 a = {}, b = {};
				for (int i = 0; lines(i++, a, b);)
					line(a, b);

				return *this;
			}
			template <std::invocable<void(int, v4&, v4&, Colour&)> EnumLines>
			LdrLine& lines(EnumLines lines)
			{
				v4 a = {}, b = {}; Colour c = {};
				for (int i = 0; lines(i++, a, b, c);)
					line(a, b, c);

				return *this;
			}

			// Line strip
			LdrLine& strip(v4_cref start)
			{
				line(start, start);
				m_strip = true;
				return *this;
			}
			LdrLine& line_to(v4_cref pt)
			{
				assert(m_strip);
				line(pt, pt);
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, m_strip ? EKeyword::LineStrip : EKeyword::Line, m_name, m_colour, [&]
				{
					Writer::Append(out, m_smooth, m_width, m_per_item_colour);
					Writer::Write(out, EKeyword::Data, [&]
					{
						for (auto& line : m_lines)
						{
							Writer::Append(out, line.a.xyz, line.b.xyz);
							if (m_per_item_colour)
								Writer::Append(out, line.col);
						}
					});
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrLineD :LdrBase<LdrLineD>
		{
			struct Line { v4 pt, dir; Colour32 col; };
			pr::vector<Line> m_lines;
			PerItemColour m_per_item_colour;
			Width m_width;

			LdrLineD()
				: m_lines()
				, m_per_item_colour()
				, m_width()
			{}

			// Line width
			LdrLineD& width(Width w)
			{
				m_width = w;
				return *this;
			}

			// Line points
			LdrLineD& line(v4_cref pt, v4_cref dir, Colour32 colour)
			{
				line(pt, dir);
				m_lines.back().col = colour;
				m_per_item_colour = true;
				return *this;
			}
			LdrLineD& line(v4_cref pt, v4_cref dir)
			{
				m_lines.push_back({ pt, dir });
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::LineD, m_name, m_colour, [&]
				{
					Writer::Append(out, m_width, m_per_item_colour);
					Writer::Write(out, EKeyword::Data, [&]
					{
						for (auto& line : m_lines)
						{
							Writer::Append(out, line.pt.xyz, line.dir.xyz);
							if (m_per_item_colour)
								Writer::Append(out, line.col);
						}
					});
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrArrow :LdrBase<LdrArrow>
		{
			struct Pt { v4 p; Colour col; };
			pr::vector<Pt> m_pts;
			EArrowType m_style;
			bool m_smooth;
			Width m_width;
			PerItemColour m_per_item_colour;

			LdrArrow()
				:m_pts()
				,m_style(EArrowType::Fwd)
				,m_smooth()
				,m_width()
				,m_per_item_colour()
			{}

			// Arrow style
			LdrArrow& style(EArrowType style)
			{
				m_style = style;
				return *this;
			}

			// Spline arrow
			LdrArrow& smooth(bool smooth = true)
			{
				m_smooth = smooth;
				return *this;
			}

			// Line width
			LdrArrow& width(Width w)
			{
				m_width = w;
				return *this;
			}

			// Line strip parts
			LdrArrow& start(v4_cref p, Colour colour)
			{
				start(p);
				m_pts.back().col = colour;
				m_per_item_colour = true;
				return *this;
			}
			LdrArrow& start(v4_cref p)
			{
				assert(m_pts.empty() && "Arrows can only have one start point");
				m_pts.push_back({ p, {} });
				return *this;
			}
			LdrArrow& line_to(v4_cref p, Colour colour)
			{
				line_to(p);
				m_pts.back().col = colour;
				m_per_item_colour = true;
				return *this;
			}
			LdrArrow& line_to(v4_cref p)
			{
				assert(!m_pts.empty() && "Arrows require a start point first");
				m_pts.push_back({ p, {} });
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Arrow, m_name, m_colour, [&]
				{
					Writer::Append(out, m_style, m_width, m_per_item_colour);
					Writer::Write(out, EKeyword::Data, [&]
					{
						for (auto& pt : m_pts)
						{
							Writer::Append(out, pt.p.xyz);
							if (m_per_item_colour)
								Writer::Append(out, pt.col);
						}
					});
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrCoordFrame :LdrBase<LdrCoordFrame>
		{
			Scale m_scale = {};
			LeftHanded m_lh = {};

			// Scale size
			LdrCoordFrame& scale(float s)
			{
				m_scale = s;
				return *this;
			}

			// Left handed axis
			LdrCoordFrame& left_handed(bool lh = true)
			{
				m_lh = lh;
				return *this;
			}

			// Write to 'outp
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::CoordFrame, m_name, m_colour, [&]
				{
					Writer::Append(out, m_scale, m_lh);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrTriangle :LdrBase<LdrTriangle>
		{
			struct Tri { v4 a, b, c; Colour32 col; };
			std::vector<Tri> m_tris;
			PerItemColour m_per_item_colour;

			LdrTriangle()
				: m_tris()
				, m_per_item_colour()
			{}

			LdrTriangle& tri(v4_cref a, v4_cref b, v4_cref c, Colour32 colour)
			{
				tri(a, b, c);
				m_tris.back().col = colour;
				m_per_item_colour = true;
				return *this;
			}
			LdrTriangle& tri(v4_cref a, v4_cref b, v4_cref c)
			{
				m_tris.push_back({ a, b, c });
				return *this;
			}
			LdrTriangle& tris(v4 const* verts, std::span<int const> faces)
			{
				assert((isize(faces) % 3) == 0);
				for (int const* i = faces.data(), *iend = i + faces.size(); i < iend; i += 3)
					tri(verts[*(i+0)], verts[*(i+1)], verts[*(i+2)]);

				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Triangle, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, [&]
					{
						for (auto& tri : m_tris)
						{
							Writer::Append(out, tri.a.xyz, tri.b.xyz, tri.c.xyz);
							if (m_per_item_colour)
								Writer::Append(out, tri.col);
						}
					});
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrPlane :LdrBase<LdrPlane>
		{
			v2 m_wh;
			LdrTexture m_tex;

			LdrPlane()
				: m_wh(1,1)
				, m_tex()
			{}

			LdrPlane& plane(v4_cref p)
			{
				pos((p.xyz * -p.w).w1());
				ori(Normalise(p.xyz.w0()), pr::AxisId::PosZ);
				return *this;
			}
			LdrPlane& wh(float width, float height)
			{
				m_wh = { width, height };
				return *this;
			}
			LdrPlane& wh(v2_cref wh)
			{
				m_wh = wh;
				return *this;
			}

			LdrTexture& texture()
			{
				return m_tex;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Plane, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, m_wh);
					m_tex.WriteTo<Writer>(out);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrCircle :LdrBase<LdrCircle>
		{
			float m_radius;

			LdrCircle()
				:m_radius(1.0f)
			{}

			LdrCircle& radius(float r)
			{
				m_radius = r;
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Circle, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, m_radius);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrSphere :LdrBase<LdrSphere>
		{
			v4 m_radius;

			LdrSphere()
				:m_radius()
			{}

			// Radius
			LdrSphere& radius(float r)
			{
				return radius({ r, r, r, 0 });
			}
			LdrSphere& radius(v4 r)
			{
				m_radius = r;
				return *this;
			}

			// Create from bounding sphere
			LdrSphere& bsphere(BSphere_cref bsphere)
			{
				if (bsphere == BSphere::Reset()) return *this;
				return radius(bsphere.Radius()).pos(bsphere.Centre());
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Sphere, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, m_radius.xyz);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrBox :LdrBase<LdrBox>
		{
			v4 m_dim;

			LdrBox()
				:m_dim()
			{}

			// Box dimensions
			LdrBox& radii(float radii)
			{
				return dim(radii * 2);
			}
			LdrBox& radii(v4_cref radii)
			{
				return dim(radii * 2);
			}
			LdrBox& dim(float dim)
			{
				m_dim = v4{dim, dim, dim, 0};
				return *this;
			}
			LdrBox& dim(v4_cref dim)
			{
				m_dim = v4(dim.x, dim.y, dim.z, 0);
				return *this;
			}
			LdrBox& dim(float sx, float sy, float sz)
			{
				m_dim = v4(sx, sy, sz, 0);
				return *this;
			}

			// Create from bounding box
			LdrBox& bbox(BBox_cref bbox)
			{
				if (bbox == BBox::Reset()) return *this;
				return dim(2 * bbox.Radius()).pos(bbox.Centre());
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Box, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, m_dim.xyz);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrBar :LdrBase<LdrBar>
		{
			v4 m_p0, m_p1;
			v2 m_wh;

			LdrBar()
				:m_p0()
				,m_p1()
				,m_wh(1.f)
			{}

			// Box dimensions
			LdrBar& bar(v4_cref p0, v4_cref p1)
			{
				m_p0 = p0;
				m_p1 = p1;
				return *this;
			}
			LdrBar& wh(v2 const& wh)
			{
				m_wh = wh;
				return *this;
			}
			LdrBar& wh(float w, float h)
			{
				return wh({ w, h });
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Bar, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, m_p0.xyz, m_p1.xyz, m_wh);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrCylinder :LdrBase<LdrCylinder>
		{
			v2 m_radius; // x = base, y = tip
			Scale2 m_scale;
			float m_height;

			LdrCylinder()
				: m_radius(0.5f)
				, m_scale()
				, m_height(1)
			{}

			// Height/Radius
			LdrCylinder& cylinder(float height, float radius)
			{
				return cylinder(height, radius, radius);
			}
			LdrCylinder& cylinder(float height, float radius_base, float radius_tip)
			{
				m_height = height;
				m_radius = v2{ radius_base, radius_tip };
				return *this;
			}

			// Scale
			LdrCylinder& scale(Scale2 scale)
			{
				m_scale = scale;
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Cylinder, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, m_height, m_radius.x, m_radius.y);
					Writer::Append(out, m_scale);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrCone :LdrBase<LdrCone>
		{
			v2 m_distance; // x = tip-to-top face, y = tip-to-base
			Scale2 m_scale;
			float m_angle;

			LdrCone()
				: m_distance(0, 1)
				, m_scale()
				, m_angle(45.0f)
			{}

			// Height/Radius
			LdrCone& angle(float solid_angle_deg)
			{
				m_angle = solid_angle_deg;
				return *this;
			}
			LdrCone& height(float height)
			{
				m_distance = v2{ m_distance.x, m_distance.x + height };
				return *this;
			}
			LdrCone& dist(float dist0, float dist1)
			{
				m_distance = v2{ dist0, dist1 };
				return *this;
			}

			// Scale
			LdrCone& scale(Scale2 scale)
			{
				m_scale = scale;
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Cone, m_name, m_colour, [&]
				{
					Writer::Write(out, EKeyword::Data, m_angle, m_distance.x, m_distance.y);
					Writer::Append(out, m_scale);
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrSpline :LdrBase<LdrSpline>
		{
			struct Bezier
			{
				v4 pt0, pt1, pt2, pt3;
				Colour col;
			};

			pr::vector<Bezier> m_splines;
			Width m_width;
			PerItemColour m_per_item_colour;
			
			LdrSpline()
				:m_splines()
				,m_width()
				,m_per_item_colour()
			{}

			// Spline width
			LdrSpline& width(Width w)
			{
				m_width = w;
				return *this;
			}

			// Add a spline piece
			LdrSpline& spline(v4 pt0, v4 pt1, v4 pt2, v4 pt3, Colour colour)
			{
				spline(pt0, pt1, pt2, pt3);
				m_splines.back().col = colour;
				m_per_item_colour = true;
				return *this;
			}
			LdrSpline& spline(v4 pt0, v4 pt1, v4 pt2, v4 pt3)
			{
				assert(pt0.w == 1 && pt1.w == 1 && pt2.w == 1 && pt3.w == 1);
				m_splines.push_back(Bezier{ pt0, pt1, pt2, pt3, {} });
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Spline, m_name, m_colour, [&]
				{
					Writer::Append(out, m_width, m_per_item_colour);
					Writer::Write(out, EKeyword::Data, [&]
					{
						for (auto& bez : m_splines)
						{
							Writer::Append(out, bez.pt0.xyz, bez.pt1.xyz, bez.pt2.xyz, bez.pt3.xyz);
							if (m_per_item_colour)
								Writer::Append(out, bez.col);
						}
					});
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrFrustum :LdrBase<LdrFrustum>
		{
			v2 m_wh;
			v2 m_nf;
			float m_fovY;
			float m_aspect;
			bool m_ortho;

			LdrFrustum()
				: m_ortho()
				, m_nf()
				, m_wh()
				, m_fovY()
				, m_aspect()
			{}

			// Orthographic
			LdrFrustum& ortho(bool ortho = true)
			{
				m_ortho = ortho;
				return *this;
			}

			// Near/Far
			LdrFrustum& nf(float n, float f)
			{
				m_nf = v2{ n, f };
				return *this;
			}
			LdrFrustum& nf(v2_cref nf_)
			{
				return nf(nf_.x, nf_.y);
			}

			// Frustum dimensions
			LdrFrustum& wh(float w, float h)
			{
				m_wh = v2{ w, h };
				m_fovY = 0;
				m_aspect = 0;
				return *this;
			}
			LdrFrustum& wh(v2_cref sz)
			{
				return wh(sz.x, sz.y);
			}

			// Frustum angles
			LdrFrustum& fov(float fovY, float aspect)
			{
				m_ortho = false;
				m_wh = v2::Zero();
				m_fovY = fovY;
				m_aspect = aspect;
				return *this;
			}

			// From maths frustum
			LdrFrustum& frustum(pr::Frustum const& f)
			{
				return nf(0, f.zfar()).fov(f.fovY(), f.aspect());
			}

			// From projection matrix
			LdrFrustum& proj(m4x4 const& c2s)
			{
				if (c2s.w.w == 1) // If orthographic
				{
					auto rh = -Sign(c2s.z.z);
					auto zn = Div(c2s.w.z, c2s.z.z, 0.0f);
					auto zf = Div(zn * (c2s.w.z - rh), c2s.w.z, 1.0f);
					auto w = 2.0f / c2s.x.x;
					auto h = 2.0f / c2s.y.y;
					return ortho(true).nf(zn, zf).wh(w,h);
				}
				else // Otherwise perspective
				{
					auto rh = -Sign(c2s.z.w);
					auto zn = rh * c2s.w.z / c2s.z.z;
					auto zf = Div(zn * c2s.z.z, (rh + c2s.z.z), zn * 1000.0f);
					auto w = 2.0f * zn / c2s.x.x;
					auto h = 2.0f * zn / c2s.y.y;
					return ortho(false).nf(zn, zf).wh(w, h);
				}
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				if (m_ortho)
				{
					Writer::Write(out, EKeyword::Box, m_name, m_colour, [&]
					{
						Writer::Write(out, EKeyword::Data, m_wh.x, m_wh.y, m_nf.y - m_nf.x);
						Writer::Append(out, O2W{ v4{ 0, 0, -0.5f * s_cast<float>(m_nf.x + m_nf.y), 1 } });
						LdrBase::WriteTo<Writer>(out);
					});
				}
				else if (m_wh != v2::Zero())
				{
					Writer::Write(out, EKeyword::FrustumWH, m_name, m_colour, [&]
					{
						Writer::Write(out, EKeyword::Data, m_wh.x, m_wh.y, m_nf.x, m_nf.y);
						LdrBase::WriteTo<Writer>(out);
					});
				}
				else
				{
					Writer::Write(out, EKeyword::FrustumFA, m_name, m_colour, [&]
					{
						Writer::Write(out, EKeyword::Data, RadiansToDegrees(m_fovY), m_aspect, m_nf.x, m_nf.y);
						LdrBase::WriteTo<Writer>(out);
					});
				}
			}
		};
		struct LdrGroup :LdrBase<LdrGroup>
		{
			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Group, m_name, m_colour, [&]
				{
					LdrBase::WriteTo<Writer>(out);
				});
			}
		};
		struct LdrCommands :LdrBase<LdrCommands>
		{
			using nstr_t = StringWithLength;
			using param_t = union param_t
			{
				// Don't need type descrimination, the command implies the parameter types
				m4x4 mat4;
				v4 vec4;
				v2 vec2;
				nstr_t nstr;
				float f;
				int i;
				bool b;
			};
			using params_t = pr::vector<param_t, 4>;
			using cmd_t = struct Cmd
			{
				ECommandId m_id;
				params_t m_params;
			};

			pr::vector<cmd_t> m_cmds;

			// Add objects created by this script to scene 'scene_id'
			LdrCommands& add_to_scene(int scene_id)
			{
				m_cmds.push_back({ ECommandId::AddToScene, {{.i = scene_id}} });
				return *this;
			}

			// Apply a transform to an object with the given name
			LdrCommands& object_transform(std::string_view object_name, m4x4 const& o2w)
			{
				m_cmds.push_back({ ECommandId::ObjectToWorld, {{.nstr = object_name}, {.mat4 = o2w}} });
				return *this;
			}

			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::Commands, [&]
				{
					for (auto& cmd : m_cmds)
					{
						Writer::Write(out, EKeyword::Data, [&]
						{
							Writer::Append(out, (int)cmd.m_id);
							switch (cmd.m_id)
							{
								case ECommandId::AddToScene:
								{
									Writer::Append(out, cmd.m_params[0].i);
									break;
								}
								case ECommandId::ObjectToWorld:
								{
									Writer::Append(out, cmd.m_params[0].nstr);
									Writer::Append(out, cmd.m_params[1].mat4);
									break;
								}
								default:
								{
									throw std::runtime_error("Unknown command id");
								}
							}
						});
					}
				});
			}
		};
		struct LdrBinaryStream :LdrBase<LdrBinaryStream>
		{
			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::BinaryStream);
			}
		};
		struct LdrTextStream :LdrBase<LdrTextStream>
		{
			// Write to 'out'
			template <WriterType Writer, typename TOut>
			void WriteTo(TOut& out) const
			{
				Writer::Write(out, EKeyword::TextStream);
			}
		};

		#pragma region LdrBuilder::Implementation
		inline LdrPoint& LdrBuilder::Point(Name name, Colour colour)
		{
			auto ptr = new LdrPoint;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrLine& LdrBuilder::Line(Name name, Colour colour)
		{
			auto ptr = new LdrLine;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrLineD& LdrBuilder::LineD(Name name, Colour colour)
		{
			auto ptr = new LdrLineD;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrArrow& LdrBuilder::Arrow(Name name, Colour colour)
		{
			auto ptr = new LdrArrow;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrCoordFrame& LdrBuilder::CoordFrame(Name name, Colour colour)
		{
			auto ptr = new LdrCoordFrame;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrTriangle& LdrBuilder::Triangle(Name name, Colour colour)
		{
			auto ptr = new LdrTriangle;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrPlane& LdrBuilder::Plane(Name name, Colour colour)
		{
			auto ptr = new LdrPlane;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrCircle& LdrBuilder::Circle(Name name, Colour colour)
		{
			auto ptr = new LdrCircle;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrSphere& LdrBuilder::Sphere(Name name, Colour colour)
		{
			auto ptr = new LdrSphere;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrBox& LdrBuilder::Box(Name name, Colour colour)
		{
			auto ptr = new LdrBox;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrBar& LdrBuilder::Bar(Name name, Colour colour)
		{
			auto ptr = new LdrBar;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrCylinder& LdrBuilder::Cylinder(Name name, Colour colour)
		{
			auto ptr = new LdrCylinder;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrCone& LdrBuilder::Cone(Name name, Colour colour)
		{
			auto ptr = new LdrCone;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrSpline& LdrBuilder::Spline(Name name, Colour colour)
		{
			auto ptr = new LdrSpline;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrFrustum& LdrBuilder::Frustum(Name name, Colour colour)
		{
			auto ptr = new LdrFrustum;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrGroup& LdrBuilder::Group(Name name, Colour colour)
		{
			auto ptr = new LdrGroup;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrCommands& LdrBuilder::Command(Name name, Colour colour)
		{
			auto ptr = new LdrCommands;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).colour(colour);
		}
		inline LdrBuilder& LdrBuilder::WrapAsGroup(Name name, Colour colour)
		{
			auto ptr = new LdrGroup;
			swap(m_objects, ptr->m_objects);
			m_objects.emplace_back(ptr);
			(*ptr).name(name).colour(colour);
			return *this;
		}
		inline LdrBuilder& LdrBuilder::BinaryStream()
		{
			auto ptr = new LdrBinaryStream;
			m_objects.emplace_back(ptr);
			return *this;
		}
		inline LdrBuilder& LdrBuilder::TextStream()
		{
			auto ptr = new LdrTextStream;
			m_objects.emplace_back(ptr);
			return *this;
		}
		#pragma endregion
	}

	// Fluent Ldraw script builder
	using Builder = fluent::LdrBuilder;
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::rdr12::ldraw
{
	PRUnitTest(LdrHelperTextTests)
	{
		{
			Builder L;
			L.Box("b", 0xFF00FF00).dim(1).o2w(m4x4::Identity());
			auto str = L.ToText(false);
			PR_EXPECT(str::Equal(str, "*Box b FF00FF00 {*Data {1 1 1}}"));
		}
		{
			Builder L;
			L.Triangle().name("tri").colour(0xFFFF0000).tri(v4{ 0,0,0,1 }, v4{ 1, 0, 0, 1 }, v4{ 0, 1, 0, 1 });
			auto str = L.ToText(false);
			PR_EXPECT(str::Equal(str, "*Triangle tri FFFF0000 {*Data {0 0 0 1 0 0 0 1 0}}"));
		}
		{
			Builder L;
			L.LineD().name("lined").colour(0xFF00FF00).line(v4{ 0,0,0,1 }, v4{ 1, 0, 0, 1 }).line(v4{ 0, 0, 0, 1 }, v4{ 0, 0, 1, 1 });
			auto str = L.ToText(true);
			PR_EXPECT(str::Equal(str, "*LineD lined FF00FF00 {\n\t*Data {\n\t\t0 0 0 1 0 0 0 0 0 0 0 1\n\t}\n}"));
		}
		{
			Builder L;
			L.Box("box", 0xFF00FF00).dim(1, 2, 3);
			
			std::stringstream ss;
			ss << L.ToText(false) << std::flush;
			PR_EXPECT(str::Equal(ss.str(), "*Box box FF00FF00 {*Data {1 2 3}}"));
		}
	}
	PRUnitTest(LdrHelperBinaryTests)
	{
		Builder builder;
		auto& group = builder.Group("TestGroup", 0xFF112233);
		auto& points = group.Point("TestPoints", 0xFF00FF00).size(5.0f);
		for (int i = 0; i != 10; ++i)
		{
			auto f = i * 1.f;
			points.pt(v4{ f, 0, 0.0f, 1.0f });
			points.pt(v4{ f, f, 0.0f, 1.0f });
			points.pt(v4{ 0, f, 0.0f, 1.0f });
		}

		auto ldr = builder.ToBinary();
	}
}
#endif
