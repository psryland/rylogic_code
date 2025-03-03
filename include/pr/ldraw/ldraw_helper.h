//************************************
// LineDrawer Helper
//  Copyright (c) Rylogic Ltd 2006
//************************************
#pragma once
#include <string>
#include <algorithm>
#include <type_traits>
#include <concepts>
#include <fstream>

#include "pr/common/fmt.h"
#include "pr/common/cast.h"
#include "pr/common/assert.h"
#include "pr/common/scope.h"
#include "pr/container/byte_data.h"
#include "pr/container/vector.h"
#include "pr/gfx/colour.h"
#include "pr/str/to_string.h"
#include "pr/str/string.h"
#include "pr/str/string_util.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/lock_file.h"
#include "pr/maths/maths.h"
#include "pr/maths/conversion.h"
#include "pr/maths/spline.h"
#include "pr/maths/spatial.h"
#include "pr/maths/polynomial.h"
#include "pr/geometry/closest_point.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"

// Plan: Convert this to pr::ldraw namespace and rename to 'ldraw_helper.h'
namespace pr::rdr12::ldraw
{
	using TStr = std::string;
	using TData = pr::byte_data<4>;
	using Scope = pr::Scope<void>;

	// Write the contents of 'ldr' to a file
	inline void Write(std::string_view ldr, std::filesystem::path const& filepath, bool append = false)
	{
		if (ldr.empty()) return;
		filesys::LockFile lock(filepath);
		filesys::BufferToFile(ldr, filepath, EEncoding::utf8, EEncoding::utf8, append);
	}
	inline void Write(std::wstring_view ldr, std::filesystem::path const& filepath, bool append = false)
	{
		if (ldr.empty()) return;
		filesys::LockFile lock(filepath);
		filesys::BufferToFile(ldr, filepath, EEncoding::utf8, EEncoding::utf16_le, append);
	}
	inline void Write(std::span<std::byte const> ldr, std::filesystem::path const& filepath, bool append = false)
	{
		if (ldr.empty()) return;
		filesys::LockFile lock(filepath);
		std::ofstream ofile(filepath, append ? std::ios::app : std::ios::out);
		ofile.write(char_ptr(ldr.data()), ldr.size());
	}

	// Pretty format Ldraw script
	template <typename TStr> TStr FormatScript(TStr const& str)
	{
		TStr out;
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
		return std::move(out);
	}

	// Ldr object fluent helper
	namespace fluent
	{
		struct LdrRawString;
		struct LdrGroup;
		struct LdrPoint;
		struct LdrLine;
		struct LdrLineD;
		struct LdrTriangle;
		struct LdrPlane;
		struct LdrCircle;
		struct LdrSphere;
		struct LdrBox;
		struct LdrCylinder;
		struct LdrSpline;
		struct LdrFrustum;

		struct LdrObj
		{
			using ObjPtr = std::unique_ptr<LdrObj>;
			using ObjCont = std::vector<ObjPtr>;

			ObjCont m_objects;

			LdrObj() = default;
			LdrObj(LdrObj&&) = default;
			LdrObj(LdrObj const&) = delete;
			LdrObj& operator=(LdrObj&&) = default;
			LdrObj& operator=(LdrObj const&) = delete;
			virtual ~LdrObj() = default;

			//template <typename Arg0, typename... Args>
			//LdrObj& Append(Arg0 const& arg0, Args&&... args)
			//{
			//	auto ptr = new LdrRawString(arg0, std::forward<Args>(args)...);
			//	m_objects.emplace_back(ptr);
			//	return *this;
			//}
			LdrGroup& Group(Name name = {}, Col colour = Col());
			LdrPoint& Point(Name name = {}, Col colour = Col());
			LdrLine& Line(Name name = {}, Col colour = Col());
			LdrLineD& LineD(Name name = {}, Col colour = Col());
			LdrTriangle& Triangle(Name name = {}, Col colour = Col());
			LdrPlane& Plane(Name name = {}, Col colour = Col());
			LdrCircle& Circle(Name name = {}, Col colour = Col());
			LdrSphere& Sphere(Name name = {}, Col colour = Col());
			LdrBox& Box(Name name = {}, Col colour = Col());
			LdrCylinder& Cylinder(Name name = {}, Col colour = Col());
			LdrSpline& Spline(Name name = {}, Col colour = Col());
			LdrFrustum& Frustum(Name name = {}, Col colour = Col());

			// Extension objects
			template <typename LdrCustom> requires std::is_base_of_v<LdrObj, LdrCustom>
			LdrCustom& Custom(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrCustom;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}

			// Wrap all objects into a group
			LdrObj& WrapAsGroup(Name name = {}, Col colour = Col());

			// Serialise the ldr script to a string
			std::string ToString(bool pretty) const
			{
				std::string str;
				ToString(str);
				if (pretty) str = FormatScript(str);
				return str;
			}
			virtual void ToString(std::string& str) const
			{
				NestedToString(str);
			}

			// Serialise the ldr script to binary
			byte_data<4> ToBinary() const
			{
				byte_data<4> data;
				ToBinary(data);
				return data;
			}
			virtual void ToBinary(byte_data<4>& data) const
			{
				NestedToBinary(data);
			}

			// Write nested objects to 'str'
			virtual void NestedToString(std::string& str) const
			{
				for (auto& obj : m_objects)
					obj->ToString(str);
			}
			virtual void NestedToBinary(byte_data<4>& data) const
			{
				for (auto& obj : m_objects)
					obj->ToBinary(data);
			}

			// Reset the builder
			LdrObj& Clear(int count = -1)
			{
				auto size = static_cast<int>(m_objects.size());
				if (count >= 0 && count < size)
					m_objects.resize(size - count);
				else
					m_objects.clear();

				return *this;
			}

			// Write the script to a file
			LdrObj& Write(std::filesystem::path const& filepath)
			{
				return Write(filepath, false, false);
			}
			LdrObj& Write(std::filesystem::path const& filepath, bool pretty, bool append)
			{
				std::string str;
				ToString(str);
				if (pretty) str = FormatScript(str);
				ldraw::Write(str, filepath, append);
				return *this;
			}
		};
		template <typename Derived>
		struct LdrBase :LdrObj
		{
			LdrBase()
				: m_name()
				, m_colour()
				, m_o2w(m4x4::Identity())
				, m_wire()
				, m_axis_id(AxisId::PosZ)
				, m_solid()
			{}

			// Object name
			Derived& name(Name name)
			{
				m_name = name;
				return static_cast<Derived&>(*this);
			}
			Name m_name;

			// Object colour
			Derived& col(Col colour)
			{
				m_colour = colour;
				return static_cast<Derived&>(*this);
			}
			Col m_colour;

			// Object to world transform
			Derived& pos(float x, float y, float z)
			{
				return o2w(m4x4::Translation(x, y, z));
			}
			Derived& pos(v4_cref pos)
			{
				return o2w(m4x4::Translation(pos));
			}
			Derived& ori(v4 const& dir, AxisId axis = AxisId::PosZ)
			{
				return ori(m3x4::Rotation(axis.vec(), dir));
			}
			Derived& ori(m3x4 const& rot)
			{
				return o2w(rot, v4::Origin());
			}
			Derived& scale(float s)
			{
				return scale(s, s, s);
			}
			Derived& scale(float sx, float sy, float sz)
			{
				return ori(m3x4::Scale(sx, sy, sz));
			}
			Derived& o2w(m3x4 const& rot, v4 const& pos)
			{
				m_o2w.m_mat = m4x4{ rot, pos } * m_o2w.m_mat;
				return static_cast<Derived&>(*this);
			}
			Derived& o2w(m4x4 const& o2w)
			{
				m_o2w.m_mat = o2w * m_o2w.m_mat;
				return static_cast<Derived&>(*this);
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
			Derived& axis(AxisId axis_id)
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

			/// <inheritdoc/>
			void NestedToString(std::string& str) const override
			{
				LdrObj::NestedToString(str);
				TextWriter::Append(str, m_wire, m_solid, m_o2w);
			}
			void NestedToBinary(byte_data<4>& data) const override
			{
				LdrObj::NestedToBinary(data);
				BinaryWriter::Append(data, m_wire, m_solid, m_o2w);
			}
		};

		struct LdrPoint :LdrBase<LdrPoint>
		{
			struct Point { v4 point; Col colour; };

			std::vector<Point> m_points;
			Size m_size;
			Depth m_depth;
			PointStyle m_style;
			bool m_has_colours;

			LdrPoint()
				:m_points()
				,m_size()
				,m_style()
				,m_has_colours()
			{}

			// Points
			LdrPoint& pt(v4_cref point, Col colour)
			{
				pt(point);
				m_points.back().colour = colour;
				m_has_colours = true;
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
			LdrPoint& style(PointStyle s)
			{
				m_style = s;
				return *this;
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Point, m_name, m_colour, [&]
				{
					TextWriter::Append(str, m_size, m_style, m_depth);
					TextWriter::Write(str, EKeyword::Data, [&]
					{
						for (auto& pt : m_points)
						{
							TextWriter::Append(str, pt.point.xyz);
							if (m_has_colours) TextWriter::Append(str, pt.colour);
						}
						NestedToString(str);
					});
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Point, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour, m_size, m_style, m_depth);
					BinaryWriter::Write(data, EKeyword::Data, [&]
					{
						for (auto& pt : m_points)
						{
							BinaryWriter::Append(data, pt.point.xyz);
							if (m_has_colours) BinaryWriter::Append(data, pt.colour);
						}
					});
					NestedToBinary(data);
				});
			}
		};
		struct LdrLine :LdrBase<LdrLine>
		{
			struct Line { v4 a, b; Col colour; };

			pr::vector<Line> m_lines;
			Width m_width;
			bool m_strip;
			bool m_has_colours;

			LdrLine()
				:m_lines()
				,m_width()
				,m_strip()
				,m_has_colours()
			{}

			// Line width
			LdrLine& width(Width w)
			{
				m_width = w;
				return *this;
			}

			// Lines
			LdrLine& line(v4_cref a, v4_cref b, Col colour)
			{
				line(a, b);
				m_lines.back().colour = colour;
				m_has_colours = true;
				return *this;
			}
			LdrLine& line(v4_cref a, v4_cref b)
			{
				m_lines.push_back({ a, b, {} });
				return *this;
			}
			LdrLine& lines(std::span<v4 const> verts, std::span<int const> indices)
			{
				assert((isize(indices) & 1) == 0);
				for (int i = 0, iend = isize(indices); i != iend; i += 2)
					line(verts[indices[i + 0]], verts[indices[i + 1]]);

				return *this;
			}

			// Add points by callback function
			template <std::invocable<void(int, v4&, v4&)> EnumLines>
			LdrLine& lines(EnumLines lines)
			{
				v4 a, b;
				for (int i = 0; lines(i++, a, b);)
					line(a, b);

				return *this;
			}
			template <std::invocable<void(int, v4&, v4&, Col&)> EnumLines>
			LdrLine& lines(EnumLines lines)
			{
				v4 a, b; Col c;
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

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, m_strip ? EKeyword::LineStrip : EKeyword::Line, m_name, m_colour, [&]
				{
					TextWriter::Append(str, m_width);
					TextWriter::Write(str, EKeyword::Data, [&]
					{
						for (auto const& line : m_lines)
						{
							TextWriter::Append(str, line.a.xyz);
							if (!m_strip) TextWriter::Append(str, line.b.xyz);
						}
					});
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, m_strip ? EKeyword::LineStrip : EKeyword::Line, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour, m_width);
					BinaryWriter::Write(data, EKeyword::Data, [&]
					{
						for (auto const& line : m_lines)
						{
							BinaryWriter::Append(data, line.a.xyz);
							if (!m_strip) BinaryWriter::Append(data, line.b.xyz);
						}
						NestedToBinary(data);
					});
				});
			}
		};
		struct LdrLineD :LdrBase<LdrLineD>
		{
			pr::vector<v4> m_lines;
			Width m_width;

			LdrLineD()
				:m_width()
				,m_lines()
			{}

			// Line width
			LdrLineD& width(Width w)
			{
				m_width = w;
				return *this;
			}

			// Line points
			LdrLineD& add(v4_cref pt, v4_cref dir)
			{
				m_lines.push_back(pt);
				m_lines.push_back(dir);
				return *this;
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::LineD, m_name, m_colour, [&]
				{
					TextWriter::Append(str, m_width);
					TextWriter::Write(str, EKeyword::Data, [&]
					{
						for (int i = 0, iend = isize(m_lines); i != iend; i += 2)
						{
							TextWriter::Append(str, m_lines[i + 0].xyz, m_lines[i + 1].xyz);
							//if (pretty) TextWriter::Append(str, "\n");
						}
					});
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::LineD, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour, m_width);
					BinaryWriter::Write(data, EKeyword::Data, [&]
					{
						for (int i = 0, iend = isize(m_lines); i != iend; i += 2)
							BinaryWriter::Append(data, m_lines[i + 0].xyz, m_lines[i + 1].xyz);
					});
					NestedToBinary(data);
				});
			}
		};
		struct LdrTriangle :LdrBase<LdrTriangle>
		{
			std::vector<v4> m_points;

			LdrTriangle()
				:m_points()
			{}

			LdrTriangle& pt(v4_cref a, v4_cref b, v4_cref c)
			{
				m_points.push_back(a);
				m_points.push_back(b);
				m_points.push_back(c);
				return *this;
			}
			LdrTriangle& pt(v4 const* verts, int const* faces, int num_faces)
			{
				for (int const* i = faces, *i_end = i + 3*num_faces; i < i_end;)
				{
					m_points.push_back(verts[*i++]);
					m_points.push_back(verts[*i++]);
					m_points.push_back(verts[*i++]);
				}
				return *this;
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Triangle, m_name, m_colour, [&]
				{
					TextWriter::Write(str, EKeyword::Data, [&]
					{
						for (int i = 0, iend = isize(m_points); i != iend; ++i)
						{
							TextWriter::Append(str, m_points[i].xyz);
							//if (pretty && (i & 3) == 3) TextWriter::Append(str, "\n");
						}
					});
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Triangle, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour);
					BinaryWriter::Write(data, EKeyword::Data, [&]
					{
						for (int i = 0, iend = isize(m_points); i != iend; ++i)
							BinaryWriter::Append(data, m_points[i].xyz);
					});
					NestedToBinary(data);
				});
			}
		};
		struct LdrPlane :LdrBase<LdrPlane>
		{
			v4 m_position;
			v4 m_direction;
			v2 m_wh;

			LdrPlane()
				:m_position(v4::Origin())
				,m_direction(v4::ZAxis())
				,m_wh(1,1)
			{}

			LdrPlane& plane(v4_cref p)
			{
				m_position = (p.xyz * -p.w).w1();
				m_direction = Normalise(p.xyz.w0());
				return *this;
			}
			LdrPlane& pos(v4_cref position)
			{
				m_position = position;
				return *this;
			}
			LdrPlane& dir(v4_cref direction)
			{
				m_direction = direction;
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

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Plane, m_name, m_colour, [&]
				{
					TextWriter::Write(str, EKeyword::Data, m_position.xyz, m_direction.xyz, m_wh);
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Plane, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour);
					BinaryWriter::Write(data, EKeyword::Data, m_position.xyz, m_direction.xyz, m_wh);
					NestedToBinary(data);
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

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Circle, m_name, m_colour, [&]
				{
					TextWriter::Write(str, EKeyword::Data, m_radius, m_axis_id);
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Circle, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour);
					BinaryWriter::Write(data, EKeyword::Data, m_radius, m_axis_id);
					NestedToBinary(data);
				});
			}
		};
		struct LdrSphere :LdrBase<LdrSphere>
		{
			LdrSphere()
				:m_radius()
			{}

			// Radius
			LdrSphere& r(double radius)
			{
				return r(radius, radius, radius);
			}
			LdrSphere& r(double radius_x, double radius_y, double radius_z)
			{
				m_radius = Vec4d<void>{radius_x, radius_y, radius_z, 0};
				return *this;
			}
			Vec4d<void> m_radius;

			// Create from bounding sphere
			LdrSphere& bsphere(BSphere_cref bsphere)
			{
				if (bsphere == BSphere::Reset()) return *this;
				return r(bsphere.Radius()).pos(bsphere.Centre());
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Sphere, m_name, m_colour, [&]
				{
					TextWriter::Write(str, EKeyword::Data, m_radius.xyz);
					NestedToString(str);
				});
				
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Sphere, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour);
					BinaryWriter::Write(data, EKeyword::Data, m_radius.xyz);
					NestedToBinary(data);
				});
			}
		};
		struct LdrBox :LdrBase<LdrBox>
		{
			Vec4d<void> m_dim;

			LdrBox()
				:m_dim()
			{}

			// Box dimensions
			LdrBox& radii(double radii)
			{
				return dim(radii * 2);
			}
			LdrBox& radii(v4_cref radii)
			{
				return dim(radii * 2);
			}
			LdrBox& dim(double dim)
			{
				m_dim = Vec4d<void>{dim, dim, dim, 0};
				return *this;
			}
			LdrBox& dim(v4_cref dim)
			{
				m_dim = Vec4d<void>(dim.x, dim.y, dim.z, 0);
				return *this;
			}
			LdrBox& dim(double sx, double sy, double sz)
			{
				m_dim = Vec4d<void>(sx, sy, sz, 0);
				return *this;
			}

			// Create from bounding box
			LdrBox& bbox(BBox_cref bbox)
			{
				if (bbox == BBox::Reset()) return *this;
				return dim(2 * bbox.Radius()).pos(bbox.Centre());
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Box, m_name, m_colour, [&]
				{
					TextWriter::Write(str, EKeyword::Data, m_dim.xyz);
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Box, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour);
					BinaryWriter::Write(data, EKeyword::Data, m_dim.xyz);
					NestedToBinary(data);
				});
			}
		};
		struct LdrCylinder :LdrBase<LdrCylinder>
		{
			LdrCylinder()
				:m_height()
				,m_radius()
			{}

			// Height/Radius
			LdrCylinder& hr(double height, double radius)
			{
				return hr(height, radius, radius);
			}
			LdrCylinder& hr(double height, double radius_x, double radius_y)
			{
				m_height = height;
				m_radius = Vec2d<void>(radius_x, radius_y);
				return *this;
			}
			double m_height;
			Vec2d<void> m_radius;

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Cylinder, m_name, m_colour, [&]
				{
					TextWriter::Write(str, EKeyword::Data, m_height, m_radius.x, m_radius.y, m_axis_id);
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Cylinder, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour);
					BinaryWriter::Write(data, EKeyword::Data, m_height, m_radius, m_axis_id);
					NestedToBinary(data);
				});
			}
		};
		struct LdrSpline :LdrBase<LdrSpline>
		{
			struct Bezier {
				v4 pt0, pt1, pt2, pt3;
				Col col;
			};
			pr::vector<Bezier> m_splines;
			Width m_width;
			bool m_has_colour;
			
			LdrSpline()
				:m_splines()
				,m_width()
				,m_has_colour()
			{}

			// Spline width
			LdrSpline& width(Width w)
			{
				m_width = w;
				return *this;
			}

			// Add a spline piece
			LdrSpline& spline(v4 pt0, v4 pt1, v4 pt2, v4 pt3, Col colour)
			{
				spline(pt0, pt1, pt2, pt3);
				m_splines.back().col = colour;
				m_has_colour = true;
				return *this;
			}
			LdrSpline& spline(v4 pt0, v4 pt1, v4 pt2, v4 pt3)
			{
				assert(pt0.w == 1 && pt1.w == 1 && pt2.w == 1 && pt3.w == 1);
				m_splines.push_back(Bezier{ pt0, pt1, pt2, pt3, {} });
				return *this;
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Spline, m_name, m_colour, [&]
				{
					TextWriter::Append(str, m_width);
					TextWriter::Write(str, EKeyword::Data, [&]
					{
						for (auto& bez : m_splines)
						{
							TextWriter::Append(str, bez.pt0.xyz, bez.pt1.xyz, bez.pt2.xyz, bez.pt3.xyz);
							if (m_has_colour) TextWriter::Append(str, bez.col);
						}
					});
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Spline, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour, m_width);
					BinaryWriter::Write(data, EKeyword::Data, [&]
					{
						for (auto& bez : m_splines)
						{
							BinaryWriter::Append(data, bez.pt0.xyz, bez.pt1.xyz, bez.pt2.xyz, bez.pt3.xyz);
							if (m_has_colour) BinaryWriter::Append(data, bez.col);
						}
					});
					NestedToBinary(data);
				});
			}
		};
		struct LdrFrustum :LdrBase<LdrFrustum>
		{
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
			bool m_ortho;

			// Near/Far
			LdrFrustum& nf(double n, double f)
			{
				m_nf = Vec2d<void>(n, f);
				return *this;
			}
			LdrFrustum& nf(v2_cref nf_)
			{
				return nf(nf_.x, nf_.y);
			}
			Vec2d<void> m_nf;

			// Frustum dimensions
			LdrFrustum& wh(double w, double h)
			{
				m_wh = Vec2d<void>(w, h);
				m_fovY = 0;
				m_aspect = 0;
				return *this;
			}
			LdrFrustum& wh(v2_cref sz)
			{
				return wh(sz.x, sz.y);
			}
			Vec2d<void> m_wh;

			// Frustum angles
			LdrFrustum& fov(double fovY, double aspect)
			{
				m_ortho = false;
				m_wh = Vec2d<void>::Zero();
				m_fovY = fovY;
				m_aspect = aspect;
				return *this;
			}
			double m_fovY;
			double m_aspect;

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

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				if (m_ortho)
				{
					TextWriter::Write(str, EKeyword::Box, m_name, m_colour, [&]
					{
						TextWriter::Write(str, EKeyword::Data, m_wh.x, m_wh.y, m_nf.y - m_nf.x);
						TextWriter::Append(str, O2W{ v4{ 0, 0, -0.5f * s_cast<float>(m_nf.x + m_nf.y), 1 } });
						NestedToString(str);
					});
				}
				else if (m_wh != Vec2d<void>::Zero())
				{
					TextWriter::Write(str, EKeyword::FrustumWH, m_name, m_colour, [&]
					{
						TextWriter::Write(str, EKeyword::Data, m_wh.x, m_wh.y, m_nf.x, m_nf.y);
						NestedToString(str);
					});
				}
				else
				{
					TextWriter::Write(str, EKeyword::FrustumFA, m_name, m_colour, [&]
					{
						TextWriter::Write(str, EKeyword::Data, RadiansToDegrees(m_fovY), m_aspect, m_nf.x, m_nf.y);
						NestedToString(str);
					});
				}
			}
			void ToBinary(byte_data<4>& data) const override
			{
				if (m_ortho)
				{
					BinaryWriter::Write(data, EKeyword::Box, [&]
					{
						BinaryWriter::Append(data, m_name, m_colour);
						BinaryWriter::Write(data, EKeyword::Data, m_wh.x, m_wh.y, m_nf.y - m_nf.x);
						BinaryWriter::Append(data, O2W(v4{ 0, 0, -0.5f * s_cast<float>(m_nf.x + m_nf.y), 1 }));
						NestedToBinary(data);
					});
				}
				else if (m_wh != Vec2d<void>::Zero())
				{
					BinaryWriter::Write(data, EKeyword::FrustumWH, [&]
					{
						BinaryWriter::Append(data, m_name, m_colour);
						BinaryWriter::Write(data, EKeyword::Data, m_wh.x, m_wh.y, m_nf.x, m_nf.y);
						NestedToBinary(data);
					});
				}
				else
				{
					BinaryWriter::Write(data, EKeyword::FrustumFA, [&]
					{
						BinaryWriter::Append(data, m_name, m_colour);
						BinaryWriter::Write(data, EKeyword::Data, RadiansToDegrees(m_fovY), m_aspect, m_nf.x, m_nf.y);
						NestedToBinary(data);
					});
				}
			}
		};
		struct LdrGroup :LdrBase<LdrGroup>
		{
			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				TextWriter::Write(str, EKeyword::Group, m_name, m_colour, [&]
				{
					NestedToString(str);
				});
			}
			void ToBinary(byte_data<4>& data) const override
			{
				BinaryWriter::Write(data, EKeyword::Group, [&]
				{
					BinaryWriter::Append(data, m_name, m_colour);
					NestedToBinary(data);
				});
			}
		};

		#pragma region LdrObj::Implementation
		inline LdrGroup& LdrObj::Group(Name name, Col colour)
		{
			auto ptr = new LdrGroup;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrPoint& LdrObj::Point(Name name, Col colour)
		{
			auto ptr = new LdrPoint;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrLine& LdrObj::Line(Name name, Col colour)
		{
			auto ptr = new LdrLine;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrLineD& LdrObj::LineD(Name name, Col colour)
		{
			auto ptr = new LdrLineD;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrTriangle& LdrObj::Triangle(Name name, Col colour)
		{
			auto ptr = new LdrTriangle;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrPlane& LdrObj::Plane(Name name, Col colour)
		{
			auto ptr = new LdrPlane;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrCircle& LdrObj::Circle(Name name, Col colour)
		{
			auto ptr = new LdrCircle;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrSphere& LdrObj::Sphere(Name name, Col colour)
		{
			auto ptr = new LdrSphere;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrBox& LdrObj::Box(Name name, Col colour)
		{
			auto ptr = new LdrBox;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrCylinder& LdrObj::Cylinder(Name name, Col colour)
		{
			auto ptr = new LdrCylinder;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrSpline& LdrObj::Spline(Name name, Col colour)
		{
			auto ptr = new LdrSpline;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrFrustum& LdrObj::Frustum(Name name, Col colour)
		{
			auto ptr = new LdrFrustum;
			m_objects.emplace_back(ptr);
			return (*ptr).name(name).col(colour);
		}
		inline LdrObj& LdrObj::WrapAsGroup(Name name, Col colour)
		{
			auto ptr = new LdrGroup;
			swap(m_objects, ptr->m_objects);
			m_objects.emplace_back(ptr);
			(*ptr).name(name).col(colour);
			return *this;
		}
		#pragma endregion
	}

	// Fluent Ldraw script builder
	using Builder = fluent::LdrObj;
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
			auto str = L.ToString(false);
			PR_EXPECT(str::Equal(str, "*Box b ff00ff00 {*Data {1 1 1}}"));
		}
		{
			Builder L;
			L.Triangle().name("tri").col(0xFFFF0000).pt(v4(0,0,0,1), v4(1,0,0,1), v4(0,1,0,1));
			auto str = L.ToString(false);
			PR_EXPECT(str::Equal(str, "*Triangle tri ffff0000 {*Data {0 0 0 1 0 0 0 1 0}}"));
		}
		{
			Builder L;
			L.LineD().name("lined").col(0xFF00FF00).add(v4(0,0,0,1), v4(1,0,0,1)).add(v4(0,0,0,1), v4(0,0,1,1));
			auto str = L.ToString(true);
			PR_EXPECT(str::Equal(str, "*LineD lined ff00ff00 {\n\t*Data {\n\t\t0 0 0 1 0 0 0 0 0 0 0 1\n\t}\n}"));
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
