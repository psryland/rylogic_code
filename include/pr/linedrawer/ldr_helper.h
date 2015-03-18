//************************************
// LineDrawer Helper
//  Copyright (c) Rylogic Ltd 2006
//************************************
#pragma once

#include <string>
#include <algorithm>
#include <windows.h>
#include "pr/common/fmt.h"
#include "pr/common/assert.h"
#include "pr/common/colour.h"
#include "pr/str/tostring.h"
#include "pr/str/prstdstring.h"
#include "pr/maths/maths.h"
#include "pr/maths/conversion.h"

namespace pr
{
	namespace ldr
	{
		template <typename TStr> inline void Write(TStr const& str, char const* filepath, bool append = false)
		{
			if (str.size() == 0) return;
			pr::Handle h = ::CreateFileA(filepath, GENERIC_WRITE, FILE_SHARE_READ, 0, append ? OPEN_ALWAYS : CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
			DWORD bytes_written; ::WriteFile(h, &str[0], (DWORD)str.size(), &bytes_written, 0);
			PR_INFO_IF(PR_DBG, bytes_written != str.size(), PR_LINK "Failed to write ldr string");
		}
		template <typename TStr> inline TStr& Vec3(v4 const& vec, TStr& str)
		{
			return str += FmtS("%f %f %f " ,vec.x ,vec.y ,vec.z);
		}
		template <typename TStr> inline TStr& Vec4(v4 const& vec, TStr& str)
		{
			return str += FmtS("%f %f %f %f " ,vec.x ,vec.y ,vec.z ,vec.w);
		}
		template <typename TStr> inline TStr& Pos(v4 const& vec, TStr& str)
		{
			str += "*pos{"; Vec3(vec, str); str += "}"; return str;
		}
		template <typename TStr> inline TStr& M4x4(m4x4 const& mat, TStr& str)
		{
			str+="*m4x4{"; Vec4(mat.x, str); Vec4(mat.y, str); Vec4(mat.z, str); Vec4(mat.w, str); str+="}"; return str;
		}
		template <typename TStr> inline TStr& Col(unsigned int colour, TStr& str)
		{
			return str += FmtS("%X", colour);
		}
		template <typename TStr> inline TStr& GroupStart(char const* name, unsigned int colour, TStr& str)
		{
			return str += FmtS("*Group %s %08X {\n", name, colour);
		}
		template <typename TStr> inline TStr& GroupStart(char const* name, TStr& str)
		{
			if (name == 0) name = "";
			return str += FmtS("*Group %s {\n", name);
		}
		template <typename TStr> inline TStr& GroupEnd(TStr& str)
		{
			return str += "}\n";
		}
		template <typename TStr> inline TStr& Nest(TStr& str)
		{
			str.resize(str.size() - 2); return str;
		}
		template <typename TStr> inline TStr& UnNest(TStr& str)
		{
			return str += "}\n";
		}
		template <typename TStr> inline TStr& Nest(TStr const& content, TStr& str)
		{
			Nest(str);
			str += content;
			UnNest(str);
			return str;
		}
		template <typename TStr> inline TStr& Position(v4 const& position, TStr& str)
		{
			str+="*o2w{"; Pos(position, str); str+="}"; return str;
		}
		template <typename TStr> inline TStr& Transform(m4x4 const& o2w, TStr& str)
		{
			str+="*o2w{"; M4x4(o2w, str); str+="}"; return str;
		}
		template <typename TStr> inline TStr& Vector(char const* name, unsigned int colour, v4 const& position, v4 const& direction, float point_radius, TStr& str)
		{
			str += FmtS("*Line %s %08X {0 0 0 %f %f %f *Box {%f} " ,name ,colour ,direction[0] ,direction[1] ,direction[2] ,point_radius);
			if (position != pr::v4Origin) Position(position, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Line(char const* name, unsigned int colour, v4 const& start, v4 const& end, TStr& str)
		{
			return str += FmtS("*Line %s %08X {%f %f %f %f %f %f}\n" ,name ,colour ,start[0] ,start[1] ,start[2] ,end[0] ,end[1] ,end[2]);
		}
		template <typename TStr> inline TStr& LineD(char const* name, unsigned int colour, v4 const& start, v4 const& direction, TStr& str)
		{
			return str += FmtS("*LineD %s %08X {%f %f %f %f %f %f}\n" ,name ,colour ,start[0] ,start[1] ,start[2] ,direction[0] ,direction[1] ,direction[2]);
		}
		template <typename TStr> inline TStr& Rect(char const* name, unsigned int colour, int axis, float w, float h, bool solid, m4x4 const& o2w, TStr& str)
		{
			str += FmtS("*Rect %s %08X {%d %f %f %s ", name, colour, axis, w, h, solid ? "*solid" : "");
			if (o2w != pr::m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Rect(char const* name, unsigned int colour, v4 const& TL, v4 const& BL, v4 const& BR, v4 const& TR, TStr& str)
		{
			return str += FmtS(
						"*Rectangle %s %08X "
						"{ "
							"%f %f %f  %f %f %f  %f %f %f  %f %f %f "
						"}\n"
						,name ,colour
						,TL.x ,TL.y ,TL.z
						,BL.x ,BL.y ,BL.z
						,BR.x ,BR.y ,BR.z
						,TR.x ,TR.y ,TR.z
						);
		}
		template <typename TStr> inline TStr& Circle(char const* name, unsigned int colour, v4 const& centre, int axis_id, float radius, TStr& str)
		{
			str += FmtS("*Circle %s %08X {%d %f " ,name ,colour ,axis_id ,radius);
			if (centre != pr::v4Origin) Position(centre, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Spline(char const* name, unsigned int colour, pr::Spline const& spline, TStr& str)
		{
			str += FmtS("*Spline %s %08X {" ,name ,colour);
			Vec3(spline.x, str);
			Vec3(spline.y, str);
			Vec3(spline.z, str);
			Vec3(spline.w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Ellipse(char const* name, unsigned int colour, v4 const& centre, int axis_id, float major, float minor, TStr& str)
		{
			str += FmtS("*Ellipse %s %08X {%d %f %f " ,name ,colour ,axis_id ,major ,minor);
			if (centre != pr::v4Origin) Position(centre, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Sphere(char const* name, unsigned int colour, v4 const& position, float radius, TStr& str)
		{
			str += FmtS("*Sphere %s %08X {%f " ,name ,colour ,radius);
			if (position != pr::v4Origin) Position(position, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Box(char const* name, unsigned int colour, v4 const& position, float dim, TStr& str)
		{
			str += FmtS("*Box %s %08X {%f " ,name ,colour ,dim);
			if (position != pr::v4Origin) Position(position, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Box(char const* name, unsigned int colour, v4 const& position, v4 const& dim, TStr& str)
		{
			str += FmtS("*Box %s %08X {%f %f %f " ,name ,colour ,dim.x ,dim.y ,dim.z);
			if (position != pr::v4Origin) Position(position, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Box(char const* name, unsigned int colour, m4x4 const& o2w, v4 const& dim, TStr& str)
		{
			str += FmtS("*Box %s %08X {%f %f %f " ,name ,colour ,dim.x ,dim.y ,dim.z);
			if (o2w != pr::m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& BoxList(char const* name, unsigned int colour, pr::v4 const& dim, pr::v4 const* positions, int count, TStr& str)
		{
			str += FmtS("*Boxlist %s %08X { %f %f %f " ,name ,colour ,dim.x ,dim.y ,dim.z);
			for (int i = 0; i != count; ++i) Vec3(positions[i], str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& LineBox(char const* name, unsigned int colour, pr::v4 const& position, v4 const& dim, TStr& str)
		{
			str += FmtS("*LineBox %s %08X {%f %f %f ", name, colour, dim.x, dim.y, dim.z);
			if (position != pr::v4Origin) Position(position, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Frustum(char const* name, unsigned int colour, pr::Frustum const& f, m4x4 const& o2w, TStr& str)
		{
			pr::m4x4 f2w = o2w * pr::Translation4x4(0.0f, 0.0f, f.ZDist());
			str += FmtS("*FrustumFA %s %08X { %d %f %f %f %f " ,name ,colour ,-3 ,pr::RadiansToDegrees(f.FovY()) ,f.Aspect() ,0.0f ,f.ZDist());
			Transform(f2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& FrustumFA(char const* name, unsigned int colour, int axis, float fovY, float aspect, float nplane, float fplane, m4x4 const& o2w, TStr& str)
		{
			str += FmtS("*FrustumFA %s %08X { %d %f %f %f %f " ,name ,colour ,axis ,pr::RadiansToDegrees(fovY) ,aspect ,nplane ,fplane);
			if (o2w != pr::m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Cylinder(char const* name, unsigned int colour, m4x4 const& o2w, int axis_id, float height, float radius, TStr& str)
		{
			str += FmtS("*CylinderHR %s %08X { %d %f %f " ,name ,colour ,axis_id ,height ,radius);
			if (o2w != pr::m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& CapsuleHR(char const* name, unsigned int colour, m4x4 const& o2w, int axis_id, float length, float radius, TStr& str)
		{
			str += FmtS("*CapsuleHR %s %08X { %d %f %f " ,name, colour ,axis_id ,length, radius);
			if (o2w != m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Quad(char const* name, unsigned int colour, v4 const& x1, v4 const& x2, v4 const& x3, v4 const& x4, TStr& str)
		{
			return str += FmtS(
						"*Quad %s %08X "
						"{ "
							" %f %f %f "
							" %f %f %f "
							" %f %f %f "
							" %f %f %f "
						"}\n"
						,name ,colour
						,x1.x ,x1.y ,x1.z
						,x2.x ,x2.y ,x2.z
						,x3.x ,x3.y ,x3.z
						,x4.x ,x4.y ,x4.z
						);
		}
		template <typename TStr> inline TStr& Quad(char const* name, unsigned int colour, float width, float height, v4 const& position, v4 const& direction, TStr& str)
		{
			v4 forward = Perpendicular(direction);
			v4 left = Cross3(forward, direction);
			forward *= height / 2.0f;
			left *= width / 2.0f;
			v4 c[4];
			c[0] = -forward - left;
			c[1] = -forward + left;
			c[2] =  forward + left;
			c[3] =  forward - left;
			str += FmtS("*Quad %s %08X {" ,name ,colour);
			Vec3(c[0], str);
			Vec3(c[1], str);
			Vec3(c[2], str);
			Vec3(c[3], str);
			Position(position, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Plane(char const* name, unsigned int colour, pr::Plane const& plane, v4 const& centre, float size, TStr& str)
		{
			str += FmtS("*Plane %s %08X {" ,name ,colour);
			Vec3(ClosestPoint_PointToPlane(centre, plane), str);
			str += " ";
			Vec3(plane::GetDirection(plane::Normalise(plane)), str);
			str += FmtS("%f %f}\n" ,size ,size);
			return str;
		}
		template <typename TStr> inline TStr& Triangle(char const* name, unsigned int colour, m4x4 const& o2w, v4 const& a, v4 const& b, v4 const& c, TStr& str)
		{
			str += FmtS("*Triangle %s %08X { " ,name ,colour);
			Vec3(a, str);
			Vec3(b, str);
			Vec3(c, str);
			if (o2w != m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Triangle(char const* name, unsigned int colour, m4x4 const& o2w, v4 const* verts, int const* faces, int num_faces, TStr& str)
		{
			str += FmtS("*Triangle %s %s\n{\n", name ,colour);
			for (int const* i = faces, *i_end = i + 3*num_faces; i < i_end;)
			{
				Vec3(verts[*i++], str);
				Vec3(verts[*i++], str);
				Vec3(verts[*i++], str);
				str += "\n";
			}
			if (o2w != m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Triangle(char const* name, unsigned int colour, v4 const& a, v4 const& b, v4 const& c, TStr& str)
		{
			return Triangle(name, colour, m4x4Identity, a, b, c, str);
		}
		template <typename TStr> inline TStr& Polytope(char const* name, unsigned int colour, m4x4 const& o2w, const v4* begin, const v4* end, TStr& str)
		{
			str += FmtS("*ConvexHull %s %08X {\n*Verts{\n", name, colour);
			for (v4 const* v = begin; v != end; ++v)
			{
				Vec3(*v, str); str += "\n";
			}
			str += "}\n";
			if (o2w != m4x4Identity) Transform(o2w, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Axis(char const* name, uint colour, m4x4 const& basis, TStr& str)
		{
			str += FmtS(	"*Group %s %08X\n"
						"{\n"
							" *Line X FFFF0000 { 0 0 0 1 0 0 }\n"
							" *Line Y FF00FF00 { 0 0 0 0 1 0 }\n"
							" *Line Z FF0000FF { 0 0 0 0 0 1 }\n"
						,name ,colour);
			Transform(basis, str);
			str += "}\n";
			return str;
		}
		template <typename TStr> inline TStr& Axis(char const* name, uint colour, m3x4 const& basis, TStr& str)
		{
			return Axis(name, colour, pr::m4x4::make(basis, pr::v4Origin), str);
		}
		template <typename TStr, typename VCont, typename ICont> inline TStr& Mesh(char const* name, unsigned int colour, VCont const& verts, ICont const& indices, int indices_per_prim, pr::m4x4 const& o2w, TStr& str)
		{
			auto v = std::begin(verts);
			auto vend = std::end(verts);
			auto i = std::begin(indices);
			auto iend = std::end(indices);
			return MeshFn(name, colour,
				[&](){ return v != vend ? &*v++ : nullptr; },
				[&](){ return i != iend ? &*i++ : nullptr; },
				indices_per_prim, o2w, str);
		}
		template <typename TStr, typename VFunc, typename IFunc> inline TStr& MeshFn(char const* name, unsigned int colour, VFunc verts, IFunc indices, int indices_per_prim, pr::m4x4 const& o2w, TStr& str)
		{
			str += FmtS( "*Mesh %s %08X {\n" ,name ,colour);
			if (o2w != pr::m4x4Identity) Transform(o2w, str);
				str +=      "\t*Verts {";
			for (auto v = verts(); v != nullptr; v = verts())
				str += FmtS("%3.3f %3.3f %3.3f  ",v->x ,v->y ,v->z);
			switch (indices_per_prim) {
			default: throw std::exception("unsupported primitive type");
			case 4: str += "}\n\t*Tetra {"; break;
			case 3: str += "}\n\t*Faces {"; break;
			case 2: str += "}\n\t*Lines {"; break;
			}
			for (auto i = indices(); i != nullptr; i = indices())
				str += FmtS("%d ",*i);
			str += "}\n";
			if (indices_per_prim >= 3)
				str += "\t*GenerateNormals\n";
			str += "}\n";
			return str;
		}

		// A new way....
		struct O2W
		{
			pr::m4x4 m_mat;
			O2W(pr::v4 const& pos) :m_mat(pr::Translation4x4(pos)) {}
			O2W(pr::m4x4 const& mat) :m_mat(mat) {}
		};

		template <typename TStr> inline TStr& Append(TStr& str, char const* s)
		{
			return str.append(s);
		}
		template <typename TStr> inline TStr& Append(TStr& str, long i)
		{
			return str.append(pr::To<std::string>(i));
		}
		template <typename TStr> inline TStr& Append(TStr& str, float f)
		{
			return str.append(pr::To<std::string>(f));
		}
		template <typename TStr> inline TStr& Append(TStr& str, pr::Colour32 c)
		{
			return str.append(pr::To<std::string>(c));
		}
		template <typename TStr> inline TStr& Append(TStr& str, pr::v3 const& v)
		{
			return str.append(pr::To<std::string>(v));
		}
		template <typename TStr> inline TStr& Append(TStr& str, pr::v4 const& v)
		{
			return str.append(pr::To<std::string>(v));
		}
		template <typename TStr> inline TStr& Append(TStr& str, O2W const& o2w)
		{
			if (o2w.m_mat == m4x4Identity)
				return str;
			if (o2w.m_mat.rot == m3x4Identity)
				return str.append("*o2w{*pos{").append(pr::To<std::string>(o2w.m_mat.pos.xyz)).append("}}");
			return str.append("*o2w{*m4x4{").append(pr::To<std::string>(o2w.m_mat)).append("}}");
		}
		template <typename TStr, typename Arg0, typename... Args> inline TStr& Append(TStr& str, Arg0 const& arg0, Args&&... args)
		{
			Append(str, arg0);
			return Append(str, std::forward<Args>(args)...);
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_ldr_ldrhelper)
		{
			std::string str;
			pr::ldr::Append(str,"*Box b ",pr::Colour32Green," {",pr::v3::make(1.0f,2.0f,3.0f)," ",pr::ldr::O2W(pr::m4x4Identity),"}");
			PR_CHECK(str, "*Box b ff00ff00 {1.000000 2.000000 3.000000 }");

			str.resize(0);
			pr::ldr::Append(str,"*Box b ",pr::Colour32Red," {",1.5f," ",pr::ldr::O2W(pr::v4ZAxis.w1()),"}");
			PR_CHECK(str, "*Box b ffff0000 {1.500000 *o2w{*pos{0.000000 0.000000 1.000000}}}");
		}
	}
}
#endif
