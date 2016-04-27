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
#include "pr/str/to_string.h"
#include "pr/str/string.h"
#include "pr/filesys/file.h"
#include "pr/maths/maths.h"
#include "pr/maths/conversion.h"

namespace pr
{
	namespace ldr
	{
		struct O2W
		{
			pr::m4x4 m_mat;
			O2W(pr::v4 const& pos) :m_mat(m4x4::Translation(pos)) {}
			O2W(pr::m4x4 const& mat) :m_mat(mat) {}
		};
		union Col
		{
			pr::Colour32 c;
			unsigned int ui;
			Col(pr::Colour32 c_) :c(c_) {}
			Col(unsigned int ui_) :ui(ui_) {}
		};

		// Helper to define string literals as narrow or wide chars
		#define S(str) PR_STRLITERAL(TStr::value_type, str)

		// See unit tests for example.
		// This only works when the overloads of Append have only two parameters.
		// For complex types, either create a struct like O2W above, or a different named function
		template <typename TStr> inline TStr& AppendSpace(TStr& str)
		{
			TStr::value_type ch;
			if (str.empty() || isspace(ch = *(end(str)-1)) || ch == '{' || ch == '(') return str;
			str.append(S(" "));
			return str;
		}

		#pragma region Append
		template <typename TStr, typename Type> inline TStr& Append(TStr& str, Type)
		{
			// Note: if you hit this error, it's probably because Append(str, ???) is being
			// called where ??? is a type not handled by the overloads of Append.
			// Also watch out for the error being a typedef of a common type,
			// e.g. I've seen 'Type=std::ios_base::openmode' as an error, but really it was
			// 'Type=int' that was missing, because of 'typedef int std::ios_base::openmode'
			static_assert(false, "no overload for 'Type'");
		}
		template <typename TStr, typename Arg0, typename... Args> inline TStr& Append(TStr& str, Arg0 const& arg0, Args&&... args)
		{
			Append(str, arg0);
			return Append(str, std::forward<Args>(args)...);
		}
		template <typename TStr> inline TStr& Append(TStr& str, typename TStr::value_type const* s)
		{
			if (*s != '}' && *s != ')') AppendSpace(str);
			if (s != nullptr) str.append(s);
			return str;
		}
		template <typename TStr, typename TStr2, typename = decltype(std::declval<TStr2>().c_str())> inline TStr& Append(TStr& str, TStr2 const& s)
		{
			return Append(str, s.c_str());
		}
		template <typename TStr> inline TStr& Append(TStr& str, int i)
		{
			return AppendSpace(str).append(pr::To<TStr>(i));
		}
		template <typename TStr> inline TStr& Append(TStr& str, long i)
		{
			return AppendSpace(str).append(pr::To<TStr>(i));
		}
		template <typename TStr> inline TStr& Append(TStr& str, float f)
		{
			return AppendSpace(str).append(pr::To<TStr>(f));
		}
		template <typename TStr> inline TStr& Append(TStr& str, Col c)
		{
			return AppendSpace(str).append(pr::To<TStr>(c.c));
		}
		template <typename TStr> inline TStr& Append(TStr& str, pr::Colour32 c)
		{
			return Append(str, Col(c));
		}
		template <typename TStr> inline TStr& Append(TStr& str, pr::v3 const& v)
		{
			return AppendSpace(str).append(pr::To<TStr>(v));
		}
		template <typename TStr> inline TStr& Append(TStr& str, pr::v4 const& v)
		{
			return AppendSpace(str).append(pr::To<TStr>(v));
		}
		template <typename TStr> inline TStr& Append(TStr& str, pr::m4x4 const& m)
		{
			return Append(str, m.x, m.y, m.z, m.w);
		}
		template <typename TStr> inline TStr& Append(TStr& str, O2W const& o2w)
		{
			if (o2w.m_mat == m4x4Identity)
				return str;
			if (o2w.m_mat.rot == m3x4Identity)
				return Append(AppendSpace(str), S("*o2w{*pos{"), o2w.m_mat.pos.xyz, S("}}"));
			else
				return Append(AppendSpace(str), S("*o2w{*m4x4{"), o2w.m_mat, S("}}"));
		}
		#pragma endregion

		template <typename TStr> inline void Write(TStr const& str, wchar_t const* filepath, bool append = false)
		{
			if (str.size() == 0) return;
			pr::BufferToFile(str, filepath, pr::EFileData::Utf8, pr::EFileData::Ucs2, append);
			//pr::Handle h = ::CreateFileW(filepath, GENERIC_WRITE, FILE_SHARE_READ, 0, append ? OPEN_ALWAYS : CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
			//DWORD bytes_written; ::WriteFile(h, &str[0], (DWORD)str.size(), &bytes_written, 0);
			//PR_INFO_IF(PR_DBG, bytes_written != str.size(), PR_LINK "Failed to write ldr string");
		}
		template <typename TStr> inline TStr& GroupStart(TStr& str, typename TStr::value_type const* name, Col colour)
		{
			return Append(str, S("*Group"), name, colour, S("{\n"));
		}
		template <typename TStr> inline TStr& GroupStart(TStr& str, typename TStr::value_type const* name)
		{
			return Append(str, S("*Group"), name, S("{\n"));
		}
		template <typename TStr> inline TStr& GroupEnd(TStr& str)
		{
			return Append(str, S("}\n"));
		}
		template <typename TStr> inline TStr& Nest(TStr& str)
		{
			str.resize(str.size() - 2); return str;
		}
		template <typename TStr> inline TStr& UnNest(TStr& str)
		{
			return str += S("}\n");
		}
		template <typename TStr> inline TStr& Nest(TStr& str, TStr const& content)
		{
			Nest(str);
			str += content;
			UnNest(str);
			return str;
		}
		template <typename TStr> inline TStr& Vector(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& position, v4 const& direction, float point_radius)
		{
			return Append(str,S("*Line"),name,colour,S("{0 0 0 "),direction[0],direction[1],direction[2],S("*Box {"),point_radius,S("}"),O2W(position),S("}\n"));
		}
		template <typename TStr> inline TStr& Line(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& start, v4 const& end, float t0 = 0.0f, float t1 = 1.0f)
		{
			Append(str,S("*Line"),name,colour,S("{"),start[0], start[1], start[2], end[0], end[1], end[2]);
			if (t0 != 0.0f || t1 != 1.0f) Append(str,S("*Param{"),t0,t1,S("}"));
			return Append(str,S("}\n"));
		}
		template <typename TStr> inline TStr& LineD(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& start, v4 const& direction)
		{
			return Append(str, S("*LineD"), name, colour, S("{"), start[0] ,start[1] ,start[2] ,direction[0] ,direction[1] ,direction[2], S("}\n"));
		}
		template <typename TStr> inline TStr& Rect(TStr& str, typename TStr::value_type const* name, Col colour, int axis, float w, float h, bool solid, m4x4 const& o2w)
		{
			return Append(str,S("*Rect"),name,colour,S("{"),axis, w, h, solid?S("*solid"):S(""),O2W(o2w),S("}\n"));
		}
		template <typename TStr> inline TStr& Rect(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& TL, v4 const& BL, v4 const& BR, v4 const& TR)
		{
			return Append(str,S("*Rectangle"),name,colour,
				,S("{")
					,TL.x ,TL.y ,TL.z
					,BL.x ,BL.y ,BL.z
					,BR.x ,BR.y ,BR.z
					,TR.x ,TR.y ,TR.z
				,S("}\n"));
		}
		template <typename TStr> inline TStr& Circle(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& centre, int axis_id, float radius)
		{
			return Append(str,S("*Circle"),name,colour,S("{"),axis_id,radius,O2W(centre),S("}\n"));
		}
		template <typename TStr> inline TStr& Spline(TStr& str, typename TStr::value_type const* name, Col colour, pr::Spline const& spline)
		{
			return Append(str,S("*Spline"),name,colour,S("{"),spline.x.xyz,spline.y.xyz,spline.z.xyz,spline.w.xyz,S("}\n"));
		}
		template <typename TStr> inline TStr& Ellipse(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& centre, int axis_id, float major, float minor)
		{
			return Append(str,S("*Ellipse"),name,colour,S("{"),axis_id,major,minor,O2W(centre),S("}\n"));
		}
		template <typename TStr> inline TStr& Sphere(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& position, float radius)
		{
			return Append(str, S("*Sphere"),name,colour,S("{"),radius,O2W(position),S("}\n"));
		}
		template <typename TStr> inline TStr& Box(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& position, float dim)
		{
			return Append(str,S("*Box"),name,colour,S("{"),dim, O2W(position),S("}\n"));
		}
		template <typename TStr> inline TStr& Box(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& position, v4 const& dim)
		{
			return Append(str,S("*Box"),name,colour,S("{"),dim.xyz,O2W(position),S("}\n"));
		}
		template <typename TStr> inline TStr& Box(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& o2w, v4 const& dim)
		{
			return Append(str,S("*Box"),name,colour,S("{"),dim.xyz,O2W(o2w),S("}\n"));
		}
		template <typename TStr> inline TStr& BoxList(TStr& str, typename TStr::value_type const* name, Col colour, pr::v4 const& dim, pr::v4 const* positions, int count)
		{
			Append(str,S("*BoxList"),name,colour,S("{"),dim.xyz);
			for (int i = 0; i != count; ++i) Append(str, positions[i].xyz);
			return Append(str, S("}\n"));
		}
		template <typename TStr> inline TStr& LineBox(TStr& str, typename TStr::value_type const* name, Col colour, pr::v4 const& position, v4 const& dim)
		{
			return Append(str,S("*LineBox"),name,colour,S("{"),dim.xyz,O2W(position),S("}\n"));
		}
		template <typename TStr> inline TStr& Frustum(TStr& str, typename TStr::value_type const* name, Col colour, pr::Frustum const& f, m4x4 const& o2w)
		{
			return FrustumFA(str, name, colour, -3, f.FovY(), f.Aspect(), 0.0f, f.ZDist(), o2w * pr::Translation4x4(0.0f, 0.0f, f.ZDist()));
		}
		template <typename TStr> inline TStr& FrustumFA(TStr& str, typename TStr::value_type const* name, Col colour, int axis, float fovY, float aspect, float nplane, float fplane, m4x4 const& o2w)
		{
			return Append(str,S("*FrustumFA"),name,colour,S("{"),axis,pr::RadiansToDegrees(fovY),aspect,nplane,fplane,O2W(o2w),S("}\n"));
		}
		template <typename TStr> inline TStr& Cylinder(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& o2w, int axis_id, float height, float radius)
		{
			return Append(str,S("*CylinderHR"),name,colour,S("{"),axis_id,height,radius,O2W(o2w),S("}\n"));
		}
		template <typename TStr> inline TStr& CapsuleHR(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& o2w, int axis_id, float length, float radius)
		{
			return Append(str,S("*CapsuleHR"),name,colour,S("{"),axis_id,length,radius,O2W(o2w),S("}\n"));
		}
		template <typename TStr> inline TStr& Quad(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& x1, v4 const& x2, v4 const& x3, v4 const& x4)
		{
			return Append(str,S("*Quad"),name,colour,S("{"),x1.xyz,x2.xyz,x3.xyz,x4.xyz,S("}\n"));
		}
		template <typename TStr> inline TStr& Quad(TStr& str, typename TStr::value_type const* name, Col colour, float width, float height, v4 const& position, v4 const& direction)
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
			return Append(str,S("*Quad"),name,colour,S("{"),c[0].xyz,c[1].xyz,c[2].xyz,c[3].xyz,O2W(position),S("}\n"));
		}
		template <typename TStr> inline TStr& Plane(TStr& str, typename TStr::value_type const* name, Col colour, pr::Plane const& plane, v4 const& centre, float size)
		{
			Append(str,S("*Plane"),name,colour,S("{"),ClosestPoint_PointToPlane(centre, plane).xyz,plane::GetDirection(plane::Normalise(plane)).xyz,size,size,S("}\n"));
		}
		template <typename TStr> inline TStr& Triangle(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& o2w, v4 const& a, v4 const& b, v4 const& c)
		{
			Append(str,S("*Triangle"),name,colour,S("{"),a.xyz,b.xyz,c.xyz,O2W(o2w),S("}\n"));
		}
		template <typename TStr> inline TStr& Triangle(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& o2w, v4 const* verts, int const* faces, int num_faces)
		{
			Append(str,S("*Triangle"),name,colour,S("{\n"));
			for (int const* i = faces, *i_end = i + 3*num_faces; i < i_end;)
			{
				Append(str, verts[*i++].xyz);
				Append(str, verts[*i++].xyz);
				Append(str, verts[*i++].xyz);
				Append(str,S("\n"));
			}
			return Append(str, O2W(o2w),S("}\n"));
		}
		template <typename TStr> inline TStr& Triangle(TStr& str, typename TStr::value_type const* name, Col colour, v4 const& a, v4 const& b, v4 const& c)
		{
			return Triangle(str, name, colour, m4x4Identity, a, b, c);
		}
		template <typename TStr> inline TStr& ConvexPolygon(TStr& str, typename TStr::value_type const* name, Col colour, v4 const* points, int count)
		{
			Append(str,S("*Triangle"),name,colour,S("{\n"));
			for (int i = 1; i != count; ++i) Append(str, points[0].xyz, points[i].xyz, points[(i+1)%count].xyz,S("\n"));
			Append(str, S("}\n"));
			return str;
		}
		template <typename TStr> inline TStr& Polytope(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& o2w, const v4* begin, const v4* end)
		{
			Append(str,S("*ConvexHull"),name,colour,S("{\n*Verts{\n"));
			for (v4 const* v = begin; v != end; ++v)
			{
				Append(str, v->xyz, S("\n"));
			}
			Append(str,S("}\n"),O2W(o2w),S("}\n"));
			return str;
		}
		template <typename TStr> inline TStr& Axis(TStr& str, typename TStr::value_type const* name, Col colour, m4x4 const& basis)
		{
			return Append(str,S("*Matrix3x3"),name,colour,S("{"),basis.x.xyz,basis.y.xyz,basis.z.xyz,S("}\n"));
		}
		template <typename TStr> inline TStr& Axis(TStr& str, typename TStr::value_type const* name, Col colour, m3x4 const& basis)
		{
			return Axis(str, name, colour, pr::m4x4::make(basis, pr::v4Origin));
		}
		template <typename TStr, typename VCont, typename ICont> inline TStr& Mesh(TStr& str, typename TStr::value_type const* name, Col colour, VCont const& verts, ICont const& indices, int indices_per_prim, pr::m4x4 const& o2w)
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
		template <typename TStr, typename VFunc, typename IFunc> inline TStr& MeshFn(TStr& str, typename TStr::value_type const* name, Col colour, VFunc verts, IFunc indices, int indices_per_prim, pr::m4x4 const& o2w)
		{
			Append(str,S("*Mesh"),name,colour,S("{\n"),O2W(o2w));

			Append(str, S("*Verts {"));
			for (auto v = verts(); v != nullptr; v = verts())
				Append(str, v->xyz);
			Append(str,S("}\n"));

			char const* prim;
			switch (indices_per_prim) {
			default: throw std::exception(S("unsupported primitive type"));
			case 4: prim = S("*Tetra"); break;
			case 3: prim = S("*Faces"); break;
			case 2: prim = S("*Lines"); break;
			}

			Append(str, prim, S("{"));
			for (auto i = indices(); i != nullptr; i = indices())
				Append(str,*i);
			Append(str,S("}\n"));

			Append(str, indices_per_prim >= 3 ? S("*GenerateNormals\n") : S(""));
			return Append(str, S("}\n"));
		}

		#undef S
	}
}

#if PR_UNITTESTS
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_ldr_ldrhelper)
		{
			using namespace pr::ldr;

			std::wstring str;
			Append(str,L"*Box b",pr::Colour32Green,L"{",pr::v3(1.0f,2.0f,3.0f),O2W(pr::m4x4Identity),L"}");
			PR_CHECK(str, L"*Box b ff00ff00 {1.000000 2.000000 3.000000}");

			str.resize(0);
			Append(str,L"*Box b",pr::Colour32Red,L"{",1.5f,O2W(pr::v4ZAxis.w1()),L"}");
			PR_CHECK(str, L"*Box b ffff0000 {1.500000 *o2w{*pos{0.000000 0.000000 1.000000}}}");
		}
	}
}
#endif
