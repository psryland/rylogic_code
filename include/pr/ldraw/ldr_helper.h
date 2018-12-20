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
#include "pr/common/colour.h"
#include "pr/common/scope.h"
#include "pr/str/to_string.h"
#include "pr/str/string.h"
#include "pr/filesys/file.h"
#include "pr/filesys/fileex.h"
#include "pr/maths/maths.h"
#include "pr/maths/conversion.h"
#include "pr/maths/spatial.h"
#include "pr/geometry/closest_point.h"

namespace pr
{
	namespace ldr
	{
		using TStr = std::string;
		using Scope = pr::Scope<std::function<void()>,std::function<void()>>;

		struct Pos
		{
			v4 m_pos;
			Pos(v4 const& pos) :m_pos(pos) {}
			Pos(m4x4 const& mat) :m_pos(mat.pos) {}
		};
		struct O2W
		{
			m4x4 m_mat;
			O2W(v4 const& pos) :m_mat(m4x4::Translation(pos)) {}
			O2W(m4x4 const& mat) :m_mat(mat) {}
		};
		union Col
		{
			Colour32 c;
			unsigned int ui;
			Col(Colour32 c_) :c(c_) {}
			Col(unsigned int ui_) :ui(ui_) {}
		};
		struct Width
		{
			float m_width;
			Width(float w) :m_width(w) {}
			Width(int w) :m_width(float(w)) {}
		};
		enum class EArrowType
		{
			Fwd,
			Back,
			FwdBack,
		};

		#pragma region Append
		// See unit tests for example.
		// This only works when the overloads of Append have only two parameters.
		// For complex types, either create a struct like O2W above, or a different named function
		inline TStr& AppendSpace(TStr& str)
		{
			TStr::value_type ch;
			if (str.empty() || isspace(ch = *(end(str)-1)) || ch == '{' || ch == '(') return str;
			str.append(" ");
			return str;
		}
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
		inline TStr& Append(TStr& str, typename TStr::value_type const* s)
		{
			if (*s != '}' && *s != ')') AppendSpace(str);
			if (s != nullptr) str.append(s);
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
			if (c.ui == 0xFFFFFFFF) return str;
			return AppendSpace(str).append(To<TStr>(c.c));
		}
		inline TStr& Append(TStr& str, Width w)
		{
			if (w.m_width != 0) Append(str, "*Width {",w.m_width,"} ");
			return str;
		}
		inline TStr& Append(TStr& str, AxisId id)
		{
			return AppendSpace(str).append(To<TStr>(int(id)));
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
			if (o2w.m_mat.rot == m3x4Identity)
				return Append(AppendSpace(str), "*o2w{*pos{", o2w.m_mat.pos.xyz, "}}");
			else
				return Append(AppendSpace(str), "*o2w{*m4x4{", o2w.m_mat, "}}");
		}
		#pragma endregion

		inline void Write(TStr const& str, wchar_t const* filepath, bool append = false)
		{
			if (str.size() == 0) return;
			LockFile lock(filepath);
			BufferToFile(str, filepath, EFileData::Utf8, EFileData::Ucs2, append);
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
		inline TStr& Circle(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& centre, int axis_id, float radius)
		{
			return Append(str,"*Circle",name,colour,"{",axis_id,radius,O2W(centre),"}\n");
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
		inline TStr& Ellipse(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& centre, int axis_id, float major, float minor)
		{
			return Append(str,"*Ellipse",name,colour,"{",axis_id,major,minor,O2W(centre),"}\n");
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
			return Frustum(str, name, colour, AxisId::NegZ, f.FovY(), f.Aspect(), nplane, fplane, o2w);
		}
		inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, pr::Frustum const& f, float nplane, float fplane)
		{
			return Frustum(str, name, colour, f, nplane, fplane, m4x4Identity);
		}
		inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, pr::Frustum const& f)
		{
			return Frustum(str, name, colour, f, 0.0f, f.ZDist(), m4x4Identity);
		}
		inline TStr& Cylinder(TStr& str, typename TStr::value_type const* name, Col colour, int axis_id, float height, float radius, O2W const& o2w)
		{
			return Append(str,"*CylinderHR",name,colour,"{",axis_id,height,radius,o2w,"}\n");
		}
		inline TStr& CapsuleHR(TStr& str, typename TStr::value_type const* name, Col colour, int axis_id, float length, float radius, O2W const& o2w)
		{
			return Append(str,"*CapsuleHR",name,colour,"{",axis_id,length,radius,o2w,"}\n");
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
			Append(str,"*Plane",name,colour,"{",ClosestPoint_PointToPlane(centre, plane).xyz,plane::Direction(plane::Normalise(plane)).xyz,size,size,"}\n");
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
			LineD(str, "Ang", Lerp(colour.c,Colour32Black,0.5f), pos, vec.ang);
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

		struct LdrBuilder
		{
			TStr m_sb;

			LdrBuilder()
				:m_sb()
			{}

			template <typename EnumPts> void Line(std::string name, Col colour, int width, EnumPts points)
			{
				auto w = width != 0 ? FmtS("*Width {%d}", width) : "";
				Append(m_sb, "*LineStrip ", name, " ", colour, " {", w);
				int i = 0; v4 x; for (; points(i++, x);) Append(m_sb, x.xyz);
				Append(m_sb, "}\n");
			}
			void Triangle(std::string name, Col colour, v4 const& a, v4 const& b, v4 const& c)
			{
				ldr::Triangle(m_sb, name.c_str(), colour, a, b, c);
			}
			void Triangle(std::string name, Col colour, v4 const& a, v4 const& b, v4 const& c, m4x4 const& o2w)
			{
				ldr::Triangle(m_sb, name.c_str(), colour, a, b, c, o2w);
			}
			void Box(std::string name, Col colour, float dim, v4 const& position)
			{
				ldr::Box(m_sb, name.c_str(), colour, dim, position);
			}
		
			void ToFile(wchar_t const* filepath, bool append = false)
			{
				Write(m_sb, filepath, append);
			}
		};
	}
}

#if PR_UNITTESTS
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
	}
}
#endif
