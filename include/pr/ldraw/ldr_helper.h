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

// NOTE: Deprecated - use "ldraw_helper.h"
#if PR_VIEW3D_12
#error "Should only be used in View3D 11 projects"
#endif

// Plan: Convert this to pr::ldraw namespace and rename to 'ldraw_helper.h'
namespace pr::ldr
{
	using TStr = std::string;
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

	#pragma region Type Wrappers
	enum class EArrowType : uint8_t
	{
		Fwd,
		Back,
		FwdBack,
	};
	enum class EPointStyle : uint8_t
	{
		Square,
		Circle,
		Triangle,
		Star,
		Annulus,
	};

	struct Str
	{
		std::string m_str;
		Str(std::string const& str) :m_str(str) {}
		Str(std::wstring const& str) :m_str(Narrow(str)) {}
	};
	struct Pos
	{
		v4 m_pos;
		Pos(v4 const& pos) :m_pos(pos) {}
		Pos(m4x4 const& mat) :m_pos(mat.pos) {}
	};
	struct O2W
	{
		m4x4 m_mat;
		O2W() :m_mat(m4x4Identity) {}
		O2W(v4 const& pos) :m_mat(m4x4::Translation(pos)) {}
		O2W(m4x4 const& mat) :m_mat(mat) {}
	};
	struct Name
	{
		std::string m_name;
		Name() :m_name() {}
		Name(std::string_view str) :m_name(str) {}
		Name(std::wstring_view str) :m_name(Narrow(str)) {}
		template <int N> Name(char const (&str)[N]) :m_name(str) {}
		template <int N> Name(wchar_t const (&str)[N]) :m_name(Narrow(str)) {}
	};
	struct Col
	{
		union {
		Colour32 m_col;
		unsigned int m_ui;
		};
		Col() :Col(0xFFFFFFFF) {}
		Col(Colour32 c) :m_col(c) {}
		Col(unsigned int ui) :m_ui(ui) {}
	};
	struct Size
	{
		float m_size;
		Size() :m_size(0) {}
		Size(float size) :m_size(size) {}
		Size(int size) :m_size(float(size)) {}
	};
	struct Width
	{
		float m_width;
		Width() :m_width(0) {}
		Width(float w) :m_width(w) {}
		Width(int w) :m_width(float(w)) {}
	};
	struct Wireframe
	{
		bool m_wire;
		Wireframe() :m_wire(false) {}
		Wireframe(bool w) :m_wire(w) {}
	};
	struct Solid
	{
		bool m_solid;
		Solid() :m_solid(false) {}
		Solid(bool s) : m_solid(s) {}
	};
	struct Depth
	{
		bool m_depth;
		Depth() :m_depth(false) {}
		Depth(bool d) : m_depth(d) {}
	};
	struct PointStyle
	{
		EPointStyle m_style;
		PointStyle() :m_style() {}
		PointStyle(EPointStyle s) : m_style(s) {}
	};
	#pragma endregion

	#pragma region Append Text

	// Forward declarations
	template <typename Arg0, typename... Args> TStr& Append(TStr& str, Arg0 const& arg0, Args&&... args);
	TStr& AppendSpace(TStr& str);

	// See unit tests for example.
	// This only works when the overloads of Append have only two parameters.
	// For complex types, either create a struct like O2W above, or a different named function
	template <typename Type> inline TStr& Append(TStr& str, Type)
	{
		// Note: if you hit this error, it's probably because Append(str, ???) is being
		// called where ??? is a type not handled by the overloads of Append.
		// Also watch out for the error being a typedef of a common type,
		// e.g. I've seen 'Type=std::ios_base::openmode' as an error, but really it was
		// 'Type=int' that was missing, because of 'typedef int std::ios_base::openmode'
		static_assert(dependent_false<Type>, "no overload for 'Type'");
	}
	inline TStr& Append(TStr& str, std::string_view s)
	{
		if (s.empty()) return str;
		if (*s.data() != '}' && *s.data() != ')') AppendSpace(str);
		str.append(s);
		return str;
	}
	inline TStr& Append(TStr& str, char const* s)
	{
		return Append(str, std::string_view(s));
	}
	inline TStr& Append(TStr& str, std::string const& s)
	{
		return Append(str, std::string_view(s));
	}
	inline TStr& Append(TStr& str, std::wstring const& s)
	{
		return Append(str, Narrow(s));
	}
	inline TStr& Append(TStr& str, Str const& s)
	{
		return Append(str, str::Quotes(s.m_str, true));
	}
	inline TStr& Append(TStr& str, int i)
	{
		return AppendSpace(str).append(To<TStr>(i));
	}
	inline TStr& Append(TStr& str, long i)
	{
		return AppendSpace(str).append(To<TStr>(i));
	}
	inline TStr& Append(TStr& str, float f)
	{
		return AppendSpace(str).append(To<TStr>(f));
	}
	inline TStr& Append(TStr& str, double f)
	{
		return AppendSpace(str).append(To<TStr>(f));
	}
	inline TStr& Append(TStr& str, Name n)
	{
		if (n.m_name.empty()) return str;
		return AppendSpace(str).append(n.m_name);
	}
	inline TStr& Append(TStr& str, Col c)
	{
		if (c.m_ui == 0xFFFFFFFF) return str;
		return AppendSpace(str).append(To<TStr>(c.m_col));
	}
	inline TStr& Append(TStr& str, Size s)
	{
		if (s.m_size == 0) return str;
		return Append(str, "*Size {", s.m_size, "} ");
	}
	inline TStr& Append(TStr& str, Depth d)
	{
		if (d.m_depth == false) return str;
		return Append(str, "*Depth ");
	}
	inline TStr& Append(TStr& str, Width w)
	{
		if (w.m_width == 0) return str;
		return Append(str, "*Width {", w.m_width, "} ");
	}
	inline TStr& Append(TStr& str, Wireframe w)
	{
		if (!w.m_wire) return str;
		return Append(str, "*Wireframe");
	}
	inline TStr& Append(TStr& str, Solid s)
	{
		if (!s.m_solid) return str;
		return Append(str, "*Solid");
	}
	inline TStr& Append(TStr& str, AxisId id)
	{
		return Append(str, "*AxisId {", int(id), "} ");
	}
	inline TStr& Append(TStr& str, EArrowType ty)
	{
		switch (ty)
		{
		case EArrowType::Fwd:     return Append(str, "Fwd");
		case EArrowType::Back:    return Append(str, "Back");
		case EArrowType::FwdBack: return Append(str, "FwdBack");
		default: throw std::runtime_error("Unknown arrow type");
		}
	}
	inline TStr& Append(TStr& str, PointStyle style)
	{
		switch (style.m_style)
		{
			case EPointStyle::Square: return str;
			case EPointStyle::Circle: return Append(str, "*Style {Circle}");
			case EPointStyle::Triangle: return Append(str, "*Style {Triangle}");
			case EPointStyle::Star: return Append(str, "*Style {Star}");
			case EPointStyle::Annulus: return Append(str, "*Style {Annulus}");
			default: throw std::runtime_error("Unknown arrow type");
		}
	}
	inline TStr& Append(TStr& str, Colour32 c)
	{
		return Append(str, Col(c));
	}
	inline TStr& Append(TStr& str, v2 const& v)
	{
		return AppendSpace(str).append(To<TStr>(v));
	}
	inline TStr& Append(TStr& str, v3 const& v)
	{
		return AppendSpace(str).append(To<TStr>(v));
	}
	inline TStr& Append(TStr& str, v4 const& v)
	{
		return AppendSpace(str).append(To<TStr>(v));
	}
	inline TStr& Append(TStr& str, m4x4 const& m)
	{
		return Append(str, m.x, m.y, m.z, m.w);
	}
	template <Scalar S> inline TStr& Append(TStr& str, Vec2<S,void> const& v)
	{
		return Append(AppendSpace(str), To<TStr>(v));
	}
	template <Scalar S> inline TStr& Append(TStr& str, Vec3<S,void> const& v)
	{
		return Append(AppendSpace(str), To<TStr>(v));
	}
	template <Scalar S> inline TStr& Append(TStr& str, Vec4<S,void> const& v)
	{
		return Append(AppendSpace(str), To<TStr>(v));
	}
	template <Scalar S> inline TStr& Append(TStr& str, Mat4x4<S,void, void> const& m)
	{
		return Append(str, m.x, m.y, m.z, m.w);
	}
	inline TStr& Append(TStr& str, Pos const& p)
	{
		if (p.m_pos == v4Origin)
			return str;
		else
			return Append(AppendSpace(str), "*o2w{*pos{",p.m_pos.xyz,"}}");
	}
	inline TStr& Append(TStr& str, O2W const& o2w)
	{
		if (o2w.m_mat == m4x4Identity)
			return str;
		if (o2w.m_mat.rot == m3x4Identity && o2w.m_mat.pos.w == 1)
			return Append(AppendSpace(str), "*o2w{*pos{", o2w.m_mat.pos.xyz, "}}");

		auto affine = !IsAffine(o2w.m_mat) ? "*NonAffine": "";
		return Append(AppendSpace(str), "*o2w{", affine, "*m4x4{", o2w.m_mat, "}}");
	}
	template <typename Arg0, typename... Args> inline TStr& Append(TStr& str, Arg0 const& arg0, Args&&... args)
	{
		Append(str, arg0);
		return Append(str, std::forward<Args>(args)...);
	}
	inline TStr& AppendSpace(TStr& str)
	{
		TStr::value_type ch;
		if (str.empty() || isspace(ch = *(end(str)-1)) || ch == '{' || ch == '(') return str;
		str.append(" ");
		return str;
	}
	#pragma endregion

	#pragma region Deprecated Ldr Functions
	#if 1 // todo - remove these
	inline Scope Section(TStr& str, typename TStr::value_type const* keyword)
	{
		assert(keyword[0] == '\0' || keyword[0] == '*');
		return Scope(
			[&] { Append(str, keyword, "{"); },
			[&] { Append(str, "}\n"); });
	}
	inline TStr& GroupStart(TStr& str, typename TStr::value_type const* name, Col colour = 0xFFFFFFFF)
	{
		return Append(str, "*Group", name, colour, "{\n");
	}
	inline TStr& GroupEnd(TStr& str, O2W const& o2w = m4x4Identity)
	{
		return Append(str, o2w, "\n}\n");
	}
	inline Scope Group(TStr& str, typename TStr::value_type const* name, Col colour = 0xFFFFFFFF, O2W const& o2w = m4x4Identity)
	{
		return Scope(
			[&]{ GroupStart(str, name, colour); },
			[&]{ GroupEnd(str, o2w); });
	}
	inline TStr& FrameStart(TStr& str, typename TStr::value_type const* name, Col colour = 0xFFFFFFFF)
	{
		return Append(str, "*CoordFrame", name, Col(colour), "{\n");
	}
	inline TStr& FrameEnd(TStr& str, O2W const& o2w = m4x4Identity)
	{
		return Append(str, O2W(o2w), "\n}\n");
	}
	inline Scope Frame(TStr& str, typename TStr::value_type const* name, Col colour = 0xFFFFFFFF, O2W const& o2w = m4x4Identity)
	{
		return Scope(
			[&]{ FrameStart(str, name, colour); },
			[&]{ FrameEnd(str, o2w); });
	}
	inline TStr& NestStart(TStr& str)
	{
		for (; !str.empty() && str.back() != '}'; str.resize(str.size() - 1)){}
		if (!str.empty()) str.resize(str.size() - 1);
		return Append(str, "\n");
	}
	inline TStr& NestEnd(TStr& str)
	{
		return Append(str, "}\n");
	}
	inline Scope Nest(TStr& str)
	{
		return Scope(
			[&]{ NestStart(str); },
			[&]{ NestEnd(str); });
	}
	inline TStr& Nest(TStr& str, TStr const& content)
	{
		auto n = Nest(str);
		str += content;
		return str;
	}
	inline TStr& Arrow(TStr& str, typename TStr::value_type const* name, Col colour, EArrowType type, v4 const& position, v4 const& direction, Width width)
	{
		return Append(str,"*Arrow",name,colour,"{",type,position.xyz,(position+direction).xyz,width,"}\n");
	}
	inline TStr& Vector(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& position, v4 const& direction, float point_radius)
	{
		return Arrow(str,name,colour,EArrowType::Fwd,position,direction,point_radius);
	}
	inline TStr& Line(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& start, v4 const& end, float t0 = 0.0f, float t1 = 1.0f)
	{
		Append(str,"*Line",name,colour,"{",start[0], start[1], start[2], end[0], end[1], end[2]);
		if (t0 != 0.0f || t1 != 1.0f) Append(str,"*Param{",t0,t1,"}");
		return Append(str,"}\n");
	}
	inline TStr& LineD(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& start, v4 const& direction, float t0 = 0.0f, float t1 = 1.0f)
	{
		Append(str, "*LineD", name, colour, "{", start.xyz ,direction.xyz);
		if (t0 != 0.0f || t1 != 1.0f) Append(str,"*Param{",t0,t1,"}");
		return Append(str, "}\n");
	}
	inline TStr& LineStrip(TStr& str, typename TStr::value_type const* name, Col colour, Width width, int count, v4 const* points)
	{
		Append(str,"*LineStrip",name,colour,"{",width);
		for (int i = 0; i != count; ++i) Append(str, points[i].xyz);
		return Append(str, "}\n");
	}
	inline TStr& Rect(TStr& str, typename TStr::value_type const* name, Col colour, AxisId axis, float w, float h, bool solid, m4x4 const& o2w)
	{
		return Append(str,"*Rect",name,colour,"{",axis, w, h, Solid(solid),O2W(o2w),"}\n");
	}
	inline TStr& Rect(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& TL, v4 const& BL, v4 const& BR, v4 const& TR)
	{
		return Append(str,"*Rectangle",name,colour
			,"{"
				,TL.x ,TL.y ,TL.z
				,BL.x ,BL.y ,BL.z
				,BR.x ,BR.y ,BR.z
				,TR.x ,TR.y ,TR.z
			,"}\n");
	}
	inline TStr& Circle(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& centre, AxisId axis_id, float radius)
	{
		return Append(str,"*Circle",name,colour,"{",radius,axis_id,O2W(centre),"}\n");
	}
	inline TStr& Spline(TStr& str, typename TStr::value_type const* name, Col colour, pr::Spline const& spline)
	{
		return Append(str,"*Spline",name,colour,"{",spline.x.xyz,spline.y.xyz,spline.z.xyz,spline.w.xyz,"}\n");
	}
	inline TStr& Curve(TStr& str, typename TStr::value_type const* name, Col colour, maths::Quadratic const& curve, float x0, float x1, int steps, O2W const& o2w)
	{
		Append(str,"*LineStrip",name,colour,"{");

		auto x = x0;
		auto dx = (x1 - x0) / steps;
		for (auto i = 0; i != steps; ++i, x += dx)
			Append(str, x, curve.F(x), 0);

		return Append(str, o2w, "}\n");
	}
	inline TStr& Curve(TStr& str, typename TStr::value_type const* name, Col colour, maths::Quadratic const& curve, float x0, float x1, int steps)
	{
		return Curve(str, name, colour, curve, x0, x1, steps, m4x4Identity);
	}
	inline TStr& Ellipse(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& centre, AxisId axis_id, float major, float minor)
	{
		return Append(str,"*Ellipse",name,colour,"{",major,minor,axis_id,O2W(centre),"}\n");
	}
	inline TStr& Sphere(TStr& str, typename TStr::value_type const* name, Col colour, float radius, Pos const& position)
	{
		return Append(str, "*Sphere",name,colour,"{",radius,position,"}\n");
	}
	inline TStr& Box(TStr& str, typename TStr::value_type const* name, Col colour, float dim, Pos const& position)
	{
		return Append(str,"*Box",name,colour,"{",dim,position,"}\n");
	}
	inline TStr& Box(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& dim, O2W const& o2w)
	{
		return Append(str,"*Box",name,colour,"{",dim.xyz,o2w,"}\n");
	}
	inline TStr& BoxList(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& dim, v4 const* positions, int count)
	{
		Append(str,"*BoxList",name,colour,"{",dim.xyz);
		for (int i = 0; i != count; ++i) Append(str, positions[i].xyz);
		return Append(str, "}\n");
	}
	inline TStr& LineBox(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& position, v4 const& dim)
	{
		return Append(str,"*LineBox",name,colour,"{",dim.xyz,O2W(position),"}\n");
	}
	inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, AxisId axis, float fovY, float aspect, float nplane, float fplane, O2W const& o2w)
	{
		return Append(str, "*FrustumFA", name, colour, "{", axis, RadiansToDegrees(fovY), aspect, nplane, fplane, o2w, "}\n");
	}
	inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, float dist, float width, float height, float nplane, float fplane)
	{
		// tan(fovY/2) = (height/2)/dist
		auto aspect = width / height;
		auto fovY = 2.0f * atan(0.5f * height / dist);
		return Frustum(str, name, colour, AxisId::NegZ, fovY, aspect, nplane, fplane, m4x4Identity);
	}
	inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, pr::Frustum const& f, float nplane, float fplane, O2W const& o2w)
	{
		return Frustum(str, name, colour, AxisId::NegZ, f.fovY(), f.aspect(), nplane, fplane, o2w);
	}
	inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, pr::Frustum const& f, float nplane, float fplane)
	{
		return Frustum(str, name, colour, f, nplane, fplane, m4x4Identity);
	}
	inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, pr::Frustum const& f, O2W const& o2w)
	{
		return Frustum(str, name, colour, f, 0.0f, f.zfar(), o2w);
	}
	inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, pr::Frustum const& f)
	{
		return Frustum(str, name, colour, f, m4x4Identity);
	}
	inline TStr& Cylinder(TStr& str, typename TStr::value_type const* name, Col colour, AxisId axis_id, float height, float radius, O2W const& o2w)
	{
		return Append(str,"*Cylinder",name,colour,"{",height,radius,axis_id,o2w,"}\n");
	}
	inline TStr& CapsuleHR(TStr& str, typename TStr::value_type const* name, Col colour, AxisId axis_id, float length, float radius, O2W const& o2w)
	{
		return Append(str,"*CapsuleHR",name,colour,"{",length,radius,axis_id,o2w,"}\n");
	}
	inline TStr& Quad(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& x1, v4 const& x2, v4 const& x3, v4 const& x4)
	{
		return Append(str,"*Quad",name,colour,"{",x1.xyz,x2.xyz,x3.xyz,x4.xyz,"}\n");
	}
	inline TStr& Quad(TStr& str, typename TStr::value_type const* name, Col colour, float width, float height, v4 const& position, v4 const& direction)
	{
		auto forward = Perpendicular(direction);
		auto left = Cross3(forward, direction);
		forward *= height / 2.0f;
		left *= width / 2.0f;
		v4 c[4];
		c[0] = -forward - left;
		c[1] = -forward + left;
		c[2] =  forward + left;
		c[3] =  forward - left;
		return Append(str,"*Quad",name,colour,"{",c[0].xyz,c[1].xyz,c[2].xyz,c[3].xyz,O2W(position),"}\n");
	}
	inline TStr& Plane(TStr& str, typename TStr::value_type const* name, Col colour, pr::Plane const& plane, v4 const& centre, float size)
	{
		return Append(str,"*Plane",name,colour,"{",ClosestPoint_PointToPlane(centre, plane).xyz,plane::Direction(plane::Normalise(plane)).xyz,size,size,"}\n");
	}
	inline TStr& Triangle(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& a, v4 const& b, v4 const& c, O2W const& o2w)
	{
		return Append(str,"*Triangle",name,colour,"{",a.xyz,b.xyz,c.xyz,o2w,"}\n");
	}
	inline TStr& Triangle(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& a, v4 const& b, v4 const& c)
	{
		return Triangle(str, name, colour, a, b, c, m4x4Identity);
	}
	inline TStr& Triangle(TStr& str, typename TStr::value_type const* name, Col colour, v4 const* verts, int const* faces, int num_faces, O2W const& o2w)
	{
		Append(str,"*Triangle",name,colour,"{\n");
		for (int const* i = faces, *i_end = i + 3*num_faces; i < i_end;)
		{
			Append(str, verts[*i++].xyz);
			Append(str, verts[*i++].xyz);
			Append(str, verts[*i++].xyz);
			Append(str,"\n");
		}
		return Append(str, o2w,"}\n");
	}
	inline TStr& ConvexPolygon(TStr& str, typename TStr::value_type const* name, Col colour, v4 const* points, int count)
	{
		Append(str,"*Triangle",name,colour,"{\n");
		for (int i = 1; i != count; ++i) Append(str, points[0].xyz, points[i].xyz, points[(i+1)%count].xyz,"\n");
		Append(str, "}\n");
		return str;
	}
	inline TStr& Polytope(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& o2w, const v4* begin, const v4* end)
	{
		Append(str,"*ConvexHull",name,colour,"{\n*Verts{\n");
		for (v4 const* v = begin; v != end; ++v)
		{
			Append(str, v->xyz, "\n");
		}
		Append(str,"}\n",O2W(o2w),"}\n");
		return str;
	}
	inline TStr& Axis(TStr& str, typename TStr::value_type const* name, Col colour, m3x4 const& basis)
	{
		return Append(str,"*Matrix3x3",name,colour,"{",basis.x.xyz,basis.y.xyz,basis.z.xyz,"}\n");
	}
	inline TStr& Axis(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& basis)
	{
		return Axis(str, name, colour, basis.rot);
	}
	inline TStr& CoordFrame(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& frame, float scale = 1.0f)
	{
		return Append(str,"*CoordFrame",name,colour,"{",scale,O2W(frame),"}\n");
	}
	inline TStr& CoordFrame(TStr& str, typename TStr::value_type const* name, Col colour, m3x4 const& basis, float scale = 1.0f)
	{
		return CoordFrame(str,name,colour,m4x4(basis, v4::Origin()),scale);
	}
	inline TStr& SpatialVector(TStr& str, typename TStr::value_type const* name, Col colour, v8 const& vec, v4 const& pos, float point_radius = 0)
	{
		auto g = Group(str, name, colour);
		auto c = Lerp(colour.m_col, Colour32Black, 0.5f);
		LineD(str, "Ang", c, pos, vec.ang);
		LineD(str, "Lin", colour, pos, vec.lin);
		if (point_radius > 0) Box(str, "", colour, point_radius, pos);
		return str;
	}
	inline TStr& VectorField(TStr& str, typename TStr::value_type const* name, Col colour, v8 const& vec, v4 const& pos, float scale = 1.0f, float step = 0.1f)
	{
		Append(str,"*Line",name,colour,"{");
		auto fwd = vec.AngAt(v4{});
		auto ori = fwd != v4{} ? OriFromDir(fwd, AxisId::PosZ) : m3x4Identity;
		for (float y = -scale; y <= scale; y += step)
		for (float x = -scale; x <= scale; x += step)
		{
			auto pt = ori.x * x + ori.y * y;
			auto vf = vec.lin + Cross(vec.ang, pt);
			Append(str, pt.xyz, (pt+vf).xyz);
		}
		Append(str, O2W(pos), "}\n");
		return str;
	}

	template <typename VCont, typename ICont> inline TStr& Mesh(TStr& str, typename TStr::value_type const* name, Col colour, VCont const& verts, ICont const& indices, int indices_per_prim, O2W const& o2w)
	{
		auto v    = std::begin(verts);
		auto vend = std::end(verts);
		auto i    = std::begin(indices);
		auto iend = std::end(indices);
		return MeshFn(str, name, colour,
			[&](){ return v != vend ? &*v++ : nullptr; },
			[&](){ return i != iend ? &*i++ : nullptr; },
			indices_per_prim, o2w);
	}
	template <typename VFunc, typename IFunc> inline TStr& MeshFn(TStr& str, typename TStr::value_type const* name, Col colour, VFunc verts, IFunc indices, int indices_per_prim, O2W const& o2w)
	{
		Append(str,"*Mesh",name,colour,"{\n",o2w);

		Append(str, "*Verts {");
		for (auto v = verts(); v != nullptr; v = verts())
			Append(str, v->xyz);
		Append(str,"}\n");

		char const* prim;
		switch (indices_per_prim) {
		default: throw std::exception("unsupported primitive type");
		case 4: prim = "*Tetra"; break;
		case 3: prim = "*Faces"; break;
		case 2: prim = "*Lines"; break;
		}

		Append(str, prim, "{");
		for (auto i = indices(); i != nullptr; i = indices())
			Append(str,*i);
		Append(str,"}\n");

		Append(str, indices_per_prim >= 3 ? "*GenerateNormals\n" : "");
		return Append(str, "}\n");
	}
	#endif
	#pragma endregion

	// Pretty format Ldraw script
	template <typename TStr>
	TStr FormatScript(TStr const& str)
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

			template <typename Arg0, typename... Args>
			LdrObj& Append(Arg0 const& arg0, Args&&... args)
			{
				auto ptr = new LdrRawString(arg0, std::forward<Args>(args)...);
				m_objects.emplace_back(ptr);
				return *this;
			}
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
			template <typename LdrCustom, typename = std::enable_if_t<std::is_base_of_v<LdrObj, LdrCustom>>>
			LdrCustom& Custom(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrCustom;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}

			// Wrap all objects into a group
			LdrObj& WrapAsGroup(Name name = {}, Col colour = Col());

			// Serialise the ldr script to a string
			std::string ToString() const
			{
				std::string str;
				ToString(str);
				return str;
			}
			virtual void ToString(std::string& str) const
			{
				NestedToString(str);
			}

			// Write nested objects to 'str'
			virtual void NestedToString(std::string& str) const
			{
				for (auto& obj : m_objects)
					obj->ToString(str);
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
				ldr::Write(str, filepath, append);
				return *this;
			}
		};
		template <typename Derived>
		struct LdrBase :LdrObj
		{
			LdrBase()
				: m_name()
				, m_colour()
				, m_o2w(m4x4Identity)
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
				m_o2w = m4x4{ rot, pos } * m_o2w;
				return static_cast<Derived&>(*this);
			}
			Derived& o2w(m4x4 const& o2w)
			{
				m_o2w = o2w * m_o2w;
				return static_cast<Derived&>(*this);
			}
			m4x4 m_o2w;

			// Wire frame
			Derived& wireframe(bool w = true)
			{
				m_wire = w;
				return static_cast<Derived&>(*this);
			}
			bool m_wire;

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
				m_solid = s;
				return static_cast<Derived&>(*this);
			}
			bool m_solid;

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
				ldr::Append(str, Wireframe(m_wire), Solid(m_solid), O2W(m_o2w));
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
				auto delim = m_points.size() > 1 ? "\n" : "";
				ldr::Append(str, "*Point", m_name, m_colour, "{", delim, m_size, m_style, m_depth, delim);
				for (auto& pt : m_points)
				{
					ldr::Append(str, pt.point.xyz);
					if (m_has_colours) ldr::Append(str, pt.colour);
					ldr::Append(str, delim);
				}
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				auto delim = m_lines.size() > 1 ? "\n" : "";

				ldr::Append(str, m_strip ? "*LineStrip" : "*Line", m_name, m_colour, "{", delim, m_width, delim);
				for (auto const& line : m_lines)
				{
					ldr::Append(str, line.a.xyz);
					if (!m_strip)
						ldr::Append(str, line.b.xyz);

					ldr::Append(str, delim);
				}
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				assert((m_lines.size() & 1) == 0);
				auto const delim = m_lines.size() > 1 ? "\n" : "";
				ldr::Append(str, "*LineD", m_name, m_colour, "{", delim, m_width, delim);
				for (int i = 0, iend = isize(m_lines); i != iend; i += 2)
				{
					ldr::Append(str
						,m_lines[i + 0].xyz
						,m_lines[i + 1].xyz
						, delim);
				}
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				auto delim = m_points.size() > 3 ? "\n" : "";
				ldr::Append(str, "*Triangle", m_name, m_colour, "{", delim);
				for (int i = 0, iend = (int)m_points.size(); i != iend; ++i)
				{
					ldr::Append(str, m_points[i].xyz);
					if ((i & 3) == 3) ldr::Append(str, delim);
				}
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				ldr::Append(str, "*Plane", m_name, m_colour, "{", m_position.xyz, m_direction.xyz, m_wh);
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				ldr::Append(str, "*Circle", m_name, m_colour, "{", m_radius, m_axis_id);
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				if (m_radius.x == m_radius.y && m_radius.x == m_radius.z)
					ldr::Append(str, "*Sphere", m_name, m_colour, "{", m_radius.x);
				else
					ldr::Append(str, "*Sphere", m_name, m_colour, "{", m_radius.x, m_radius.y, m_radius.z);
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				ldr::Append(str, "*Box", m_name, m_colour, "{", m_dim.xyz);
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				ldr::Append(str, "*Cylinder", m_name, m_colour, "{", m_height, m_radius.x, m_radius.y, m_axis_id);
				NestedToString(str);
				ldr::Append(str, "}\n");
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
				auto delim = m_splines.size() > 1 ? "\n" : "";
				ldr::Append(str, "*Spline", m_name, m_colour, "{", delim, m_width, delim);
				for (auto& bez : m_splines)
				{
					ldr::Append(str, bez.pt0.xyz, bez.pt1.xyz, bez.pt2.xyz, bez.pt3.xyz);
					if (m_has_colour) ldr::Append(str, bez.col);
					ldr::Append(str, delim);
				}
				NestedToString(str);
				ldr::Append(str, "}\n");
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
					ldr::Append(str, "*Box", m_name, m_colour, "{", m_wh.x, m_wh.y, m_nf.y - m_nf.x, O2W(v4{0, 0, -0.5f * s_cast<float>(m_nf.x + m_nf.y), 1}));
				else if (m_wh != Vec2d<void>::Zero())
					ldr::Append(str, "*FrustumWH", m_name, m_colour, "{", m_wh.x, m_wh.y, m_nf.x, m_nf.y);
				else
					ldr::Append(str, "*FrustumFA", m_name, m_colour, "{", RadiansToDegrees(m_fovY), m_aspect, m_nf.x, m_nf.y);

				NestedToString(str);
				ldr::Append(str, "}\n");
			}
		};
		struct LdrGroup :LdrBase<LdrGroup>
		{
			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				ldr::Append(str, "*Group", m_name, m_colour, "{\n");
				NestedToString(str);
				for (;!str.empty() && str.back() == '\n';) str.pop_back();
				ldr::Append(str, "\n}\n");
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
namespace pr::ldr
{
	PRUnitTest(LdrHelperTextTests)
	{
		std::string str;

		Append(str,"*Box b",Colour32Green,"{",v3(1.0f,2.0f,3.0f),O2W(m4x4::Identity()),"}");
		PR_CHECK(str, "*Box b ff00ff00 {1 2 3}");
		str.resize(0);

		Append(str,"*Box b",Colour32Red,"{",1.5f,O2W(v4ZAxis.w1()),"}");
		PR_CHECK(str, "*Box b ffff0000 {1.5 *o2w{*pos{0 0 1}}}");
		str.resize(0);

		{
			Builder L;
			L.Box("b", 0xFF00FF00).dim(1).o2w(m4x4::Identity());
			L.ToString(str);

			PR_CHECK(str, "*Box b ff00ff00 {1 1 1}\n");
			str.resize(0);
		}
		{
			Builder L;
			L.Triangle().name("tri").col(0xFFFF0000).pt(v4(0,0,0,1), v4(1,0,0,1), v4(0,1,0,1));
			L.ToString(str);
			
			PR_CHECK(str, "*Triangle tri ffff0000 {0 0 0 1 0 0 0 1 0}\n");
			str.resize(0);
		}
		{
			Builder L;
			L.LineD().name("lined").col(0xFF00FF00).add(v4(0,0,0,1), v4(1,0,0,1)).add(v4(0,0,0,1), v4(0,0,1,1));
			L.ToString(str);
			
			PR_CHECK(str, "*LineD lined ff00ff00 {\n\n0 0 0 1 0 0 \n0 0 0 0 0 1 \n}\n");
			str.resize(0);
		}
	}
}
#endif
