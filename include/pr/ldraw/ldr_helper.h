//************************************
// LineDrawer Helper
//  Copyright (c) Rylogic Ltd 2006
//************************************
#pragma once
#include <string>
#include <algorithm>
#include <type_traits>
#include "pr/common/fmt.h"
#include "pr/common/assert.h"
#include "pr/common/scope.h"
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

namespace pr::ldr
{
	using TStr = std::string;
	using Scope = pr::Scope<std::function<void()>,std::function<void()>>;

	#pragma region Append
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
	enum class EArrowType
	{
		Fwd,
		Back,
		FwdBack,
	};

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
		static_assert(false, "no overload for 'Type'");
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
	inline TStr& Append(TStr& str, typename TStr::value_type const* s)
	{
		if (s == nullptr || *s == '\0') return str;
		if (*s != '}' && *s != ')') AppendSpace(str);
		str.append(s);
		return str;
	}
	inline TStr& Append(TStr& str, std::string const& s)
	{
		return Append(str, s.c_str());
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
	inline TStr& Append(TStr& str, Col c)
	{
		if (c.m_ui == 0xFFFFFFFF) return str;
		return AppendSpace(str).append(To<TStr>(c.m_col));
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
	inline TStr& Append(TStr& str, AxisId id)
	{
		return Append(str, "*AxisId {", int(id), "} ");
	}
	inline TStr& Append(TStr& str, EArrowType ty)
	{
		switch (ty) {
		default: throw std::runtime_error("Unknown arrow type");
		case EArrowType::Fwd:     return Append(str, "Fwd");
		case EArrowType::Back:    return Append(str, "Back");
		case EArrowType::FwdBack: return Append(str, "FwdBack");
		}
	}
	inline TStr& Append(TStr& str, Colour32 c)
	{
		return Append(str, Col(c));
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
	#pragma endregion

	inline void Write(TStr const& str, std::filesystem::path const& filepath, bool append = false)
	{
		if (str.size() == 0) return;
		filesys::LockFile lock(filepath);
		filesys::BufferToFile(str, filepath, EEncoding::utf8, EEncoding::utf16_le, append);
	}
	inline Scope Section(TStr& str, typename TStr::value_type const* keyword)
	{
		assert(keyword[0] == '\0' || keyword[0] == '*');
		std::function<void()> doit = [&] { Append(str, keyword, "{"); };
		std::function<void()> undo = [&] { Append(str, "}\n"); };
		return CreateScope(doit, undo);
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
		std::function<void()> doit = [&]{ GroupStart(str, name, colour); };
		std::function<void()> undo = [&]{ GroupEnd(str, o2w); };
		return CreateScope(doit, undo);
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
		std::function<void()> doit = [&]{ FrameStart(str, name, colour); };
		std::function<void()> undo = [&]{ FrameEnd(str, o2w); };
		return CreateScope(doit, undo);
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
		std::function<void()> doit = [&]{ NestStart(str); };
		std::function<void()> undo = [&]{ NestEnd(str); };
		return CreateScope(doit, undo);
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
		return Append(str,"*Rect",name,colour,"{",axis, w, h, solid?"*solid":"",O2W(o2w),"}\n");
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
		return CoordFrame(str,name,colour,basis.m4x4(),scale);
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
	};
	#pragma endregion

	// Ldr object fluent helper
	namespace fluent
	{
		template <typename> struct LdrObj_;
		struct LdrRawString;
		struct LdrGroup;
		struct LdrLine;
		struct LdrTriangle;
		struct LdrSphere;
		struct LdrBox;
		struct LdrCylinder;
		struct LdrFrustum;
		using LdrObj = LdrObj_<void>;
		using ObjPtr = std::unique_ptr<LdrObj>;
		using ObjCont = std::vector<ObjPtr>;

		template <typename> struct LdrObj_
		{
			ObjCont m_objects;
			virtual ~LdrObj_() {}

			template <typename Arg0, typename... Args>
			LdrObj_& Append(Arg0 const& arg0, Args&&... args)
			{
				auto ptr = new LdrRawString(arg0, std::forward<Args>(args)...);
				m_objects.emplace_back(ptr);
				return *this;
			}
			LdrGroup& Group(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrGroup;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}
			LdrLine& Line(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrLine;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}
			LdrTriangle& Triangle(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrTriangle;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}
			LdrSphere& Sphere(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrSphere;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}
			LdrBox& Box(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrBox;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}
			LdrCylinder& Cylinder(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrCylinder;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}
			LdrFrustum& Frustum(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrFrustum;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}

			// Extension objects
			template <typename LdrCustom, typename = std::enable_if_t<std::is_base_of_v<LdrObj_, LdrCustom>>>
			LdrCustom& Add(std::string_view name = "", Col colour = Col())
			{
				auto ptr = new LdrCustom;
				m_objects.emplace_back(ptr);
				return (*ptr).name(name).col(colour);
			}

			// Serialise the ldr script to a string
			virtual void ToString(std::string& str) const
			{
				for (auto& s : m_objects)
					s->ToString(str);
			}

			// Reset the builder
			LdrObj_& Clear(int count = -1)
			{
				auto size = static_cast<int>(m_objects.size());
				if (count >= 0 && count < size)
					m_objects.resize(size - count);
				else
					m_objects.clear();

				return *this;
			}

			// Write the script to a file
			LdrObj_& Write(std::filesystem::path const& filepath)
			{
				return Write(filepath, false, false);
			}
			LdrObj_& Write(std::filesystem::path const& filepath, bool pretty, bool append)
			{
				std::string str;
				ToString(str);
				if (pretty) str = FormatScript(str);
				ldr::Write(str, filepath, append);
				return *this;
			}
		};
		struct LdrRawString :LdrObj
		{
			std::string m_str;
			template <typename Arg0, typename... Args> 
			LdrRawString(Arg0 const& arg0, Args&&... args)
				:m_str()
			{
				ldr::Append(m_str, arg0, std::forward<Args>(args)...);
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				str.append(m_str);
			}
		};
		template <typename Derived> struct LdrBase :LdrObj
		{
			LdrBase()
				: m_name()
				, m_colour()
				, m_o2w(m4x4Identity)
				, m_wire()
				, m_axis_id(AxisId::PosZ)
			{}

			// Object name
			Derived& name(std::string_view name)
			{
				m_name = name;
				return static_cast<Derived&>(*this);
			}
			std::string m_name;

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
			Derived& pos(v4_cref<> pos)
			{
				return o2w(m4x4::Translation(pos));
			}
			Derived& ori(v4 const& dir, AxisId axis = AxisId::PosZ)
			{
				return ori(m3x4::Rotation(axis.vec(), dir));
			}
			Derived& ori(m3x4 const& rot)
			{
				return o2w(rot.m4x4());
			}
			Derived& scale(float s)
			{
				return scale(s, s, s);
			}
			Derived& scale(float sx, float sy, float sz)
			{
				return ori(m3x4::Scale(sx, sy, sz));
			}
			Derived& o2w(m4x4 const& o2w)
			{
				m_o2w = o2w * m_o2w;
				return static_cast<Derived&>(*this);
			}
			m4x4 m_o2w;

			// Wireframe
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
			virtual void ToString(std::string& str) const
			{
				LdrObj::ToString(str);
				ldr::Append(str, Wireframe(m_wire), O2W(m_o2w));
			}
		};
		struct LdrLine :LdrBase<LdrLine>
		{
			LdrLine()
				:m_strip()
				,m_width()
				,m_points()
			{}

			// Line strip
			LdrLine& strip()
			{
				m_strip = true;
				return *this;
			}
			bool m_strip;

			// Line width
			LdrLine& width(Width w)
			{
				m_width = w;
				return *this;
			}
			Width m_width;

			// Line points
			LdrLine& pt(v4_cref<> a, v4_cref<> b)
			{
				m_points.push_back(a);
				m_points.push_back(b);
				return *this;
			}
			LdrLine& pt(v4 const* verts, int const* lines, int num_lines)
			{
				for (int const* i = lines, *i_end = i + 2*num_lines; i < i_end;)
				{
					m_points.push_back(verts[*i++]);
					m_points.push_back(verts[*i++]);
				}
				return *this;
			}
			template <typename EnumPts> LdrLine& pt(EnumPts points)
			{
				v4 x;
				for (int i = 0; points(i++, x);) m_points.push_back(x);
				return *this;
			}
			std::vector<v4> m_points;

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				auto ty = m_strip ? "*Line" : "*LineStrip";
				auto delim = m_points.size() > 1 ? "\n" : "";
				ldr::Append(str, ty, m_name, m_colour, "{", delim, m_width, delim);
				for (int i = 0, iend = (int)m_points.size(); i != iend; ++i)
				{
					ldr::Append(str, m_points[i].xyz);
					if ((i & 1) == 1) ldr::Append(str, delim);
				}
				LdrBase<LdrLine>::ToString(str);
				ldr::Append(str, "}\n");
			}
		};
		struct LdrTriangle :LdrBase<LdrTriangle>
		{
			LdrTriangle()
				:m_points()
			{}

			LdrTriangle& pt(v4_cref<> a, v4_cref<> b, v4_cref<> c)
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
			std::vector<v4> m_points;

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
				LdrBase<LdrTriangle>::ToString(str);
				ldr::Append(str, "}\n");
			}
		};
		struct LdrSphere :LdrBase<LdrSphere>
		{
			LdrSphere()
				:m_radius()
			{}

			// Radius
			LdrSphere& r(float radius)
			{
				return r(radius, radius, radius);
			}
			LdrSphere& r(float radius_x, float radius_y, float radius_z)
			{
				m_radius = v4{radius_x, radius_y, radius_z, 0};
				return *this;
			}
			v4 m_radius;

			// Create from bounding sphere
			LdrSphere& bsphere(BSphere_cref bsphere)
			{
				if (bsphere == BSphereReset) return *this;
				return r(bsphere.Radius()).pos(bsphere.Centre());
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				if (m_radius.x == m_radius.y && m_radius.x == m_radius.z)
					ldr::Append(str, "*Sphere", m_name, m_colour, "{", m_radius.x);
				else
					ldr::Append(str, "*Sphere", m_name, m_colour, "{", m_radius.x, m_radius.y, m_radius.z);
				LdrBase<LdrSphere>::ToString(str);
				ldr::Append(str, "}\n");
			}
		};
		struct LdrBox :LdrBase<LdrBox>
		{
			LdrBox()
				:m_dim()
			{}

			// Box dimensions
			LdrBox& dim(float dim)
			{
				m_dim = v4{dim, dim, dim, 0};
				return *this;
			}
			LdrBox& dim(v4_cref<> dim)
			{
				m_dim = dim;
				return *this;
			}
			v4 m_dim;

			// Create from bounding box
			LdrBox& bbox(BBox_cref bbox)
			{
				if (bbox == BBoxReset) return *this;
				return dim(2 * bbox.Radius()).pos(bbox.Centre());
			}

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				ldr::Append(str, "*Box", m_name, m_colour, "{", m_dim.xyz);
				LdrBase<LdrBox>::ToString(str);
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
			LdrCylinder& hr(float height, float radius)
			{
				return hr(height, radius, radius);
			}
			LdrCylinder& hr(float height, float radius_x, float radius_y)
			{
				m_height = height;
				m_radius = v2(radius_x, radius_y);
				return *this;
			}
			float m_height;
			v2 m_radius;

			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				ldr::Append(str, "*Cylinder", m_name, m_colour, "{", m_height, m_radius.x, m_radius.y, m_axis_id);
				LdrBase<LdrCylinder>::ToString(str);
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
			LdrFrustum& nf(float n, float f)
			{
				m_nf = v2(n, f);
				return *this;
			}
			LdrFrustum& nf(v2_cref<> nf_)
			{
				return nf(nf_.x, nf_.y);
			}
			v2 m_nf;

			// Frustum dimensions
			LdrFrustum& wh(float w, float h)
			{
				return wh(v2(w, h));
			}
			LdrFrustum& wh(v2_cref<> wh)
			{
				m_fovY = 0;
				m_aspect = 0;
				m_wh = wh;
				return *this;
			}
			v2 m_wh;

			// Frustum angles
			LdrFrustum& fov(float fovY, float aspect)
			{
				m_ortho = false;
				m_wh = v2Zero;
				m_fovY = fovY;
				m_aspect = aspect;
				return *this;
			}
			float m_fovY;
			float m_aspect;

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
					ldr::Append(str, "*Box", m_name, m_colour, "{", m_wh.x, m_wh.y, m_nf.y - m_nf.x, O2W(v4{0, 0, -0.5f * (m_nf.x + m_nf.y), 1}));
				else if (m_wh != v2Zero)
					ldr::Append(str, "*FrustumWH", m_name, m_colour, "{", m_wh.x, m_wh.y, m_nf.x, m_nf.y);
				else
					ldr::Append(str, "*FrustumFA", m_name, m_colour, "{", RadiansToDegrees(m_fovY), m_aspect, m_nf.x, m_nf.y);

				LdrBase<LdrFrustum>::ToString(str);
				ldr::Append(str, "}\n");
			}
		};
		struct LdrGroup :LdrBase<LdrGroup>
		{
			/// <inheritdoc/>
			void ToString(std::string& str) const override
			{
				ldr::Append(str, "*Group", m_name, m_colour, "{\n");
				LdrBase<LdrGroup>::ToString(str);
				for (;!str.empty() && str.back() == '\n';) str.pop_back();
				ldr::Append(str, "\n}\n");
			}
		};
	}

	struct Builder : fluent::LdrObj
	{
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::ldr
{
	PRUnitTest(LdrHelperTests)
	{
		std::string str;

		Append(str,"*Box b",Colour32Green,"{",v3(1.0f,2.0f,3.0f),O2W(m4x4Identity),"}");
		PR_CHECK(str, "*Box b ff00ff00 {1 2 3}");
		str.resize(0);

		Append(str,"*Box b",Colour32Red,"{",1.5f,O2W(v4ZAxis.w1()),"}");
		PR_CHECK(str, "*Box b ffff0000 {1.5 *o2w{*pos{0 0 1}}}");
		str.resize(0);

		Builder L;
		L.Box("b", 0xFF00FF00).dim(1).o2w(m4x4Identity);
		L.Triangle().name("tri").col(0xFFFF0000).pt(v4(0,0,0,1), v4(1,0,0,1), v4(0,1,0,1));
		L.ToString(str);
		PR_CHECK(str, "*Box b ff00ff00 {1 1 1}\n*Triangle tri ffff0000 {0 0 0 1 0 0 0 1 0}\n");
		str.resize(0);
	}
}
#endif
