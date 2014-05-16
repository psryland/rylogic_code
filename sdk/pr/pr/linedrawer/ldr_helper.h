//************************************
// LineDrawer Helper
//  Copyright © Rylogic Ltd 2006
//************************************
#ifndef PR_LINE_DRAWER_HELPER_H
#define PR_LINE_DRAWER_HELPER_H

#include <string>
#include <algorithm>
#include <windows.h>
#include "pr/common/fmt.h"
#include "pr/common/assert.h"
#include "pr/str/prstdstring.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace ldr
	{
		template <typename TStr> inline void Write(TStr const& str, char const* filepath, bool append = false)
		{
			if (str.size() == 0) return;
			pr::Handle h = ::CreateFileA(filepath, GENERIC_WRITE, FILE_SHARE_READ, 0, append ? OPEN_ALWAYS : CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
			DWORD bytes_written; ::WriteFile(h, &str[0], (DWORD)str.size(), &bytes_written, 0);
			PR_INFO_EXP(PR_DBG, bytes_written == str.size(), PR_LINK "Failed to write ldr string");
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
		//template <typename TStr> inline void Dir(v4 vec, TStr& str)
		//{
		//	str += "*direction {"; Vec3(vec, str); str += "} ";
		//}
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
			if (name == 0 || *name == 0) name = "unnamed";
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
		template <typename TStr> inline TStr& Rectangle(char const* name, unsigned int colour, v4 const& TL, v4 const& BL, v4 const& BR, v4 const& TR, TStr& str)
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
		//template <typename TStr> inline void Grid(char const* name, unsigned int colour, float dimx, float dimy, int divx, int divy, v4 position, TStr& str)
		//{
		//	str += Fmt(	"*GridWH %s %08X "
		//				"{ "
		//					"%f %f %d %d "
		//					"*Position {%f %f %f} "
		//				"}\n"
		//				,name
		//				,colour
		//				,dimx ,dimy ,divx ,divy
		//				,position.x ,position.y ,position.z
		//				);
		//}
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
		//template <typename TStr> inline void BoxLU(char const* name, unsigned int colour, v4 lower, v4 upper, TStr& str)
		//{
		//	str += Fmt(	"*BoxLU %s %08X {%f %f %f %f %f %f}\n" ,name ,colour ,lower.x ,lower.y ,lower.z ,upper.x ,upper.y ,upper.z);
		//}

		//template <typename TStr> inline void BoxLine(char const* name, unsigned int colour, v4 s, v4 e, float size, TStr& str)
		//{
		//	size *= 0.5f;
		//	v4 d = e - s;
		//	str += Fmt(	"*Box %s %08X {%f %f %f *Line ray %08X {0 0 0 %f %f %f} " ,name ,colour ,size ,size ,size ,colour ,d.x ,d.y ,d.z);
		//	if (s != pr::v4Origin) Position(s, str);
		//	str += "}\n";
		//}
		//template <typename TStr> inline void Box2(char const* name, unsigned int colour, v4 position, float size, TStr& str)
		//{
		//	size *= 0.5f;
		//	str += Fmt(	"*Box %s %08X {%f %f %f " ,name ,colour ,size ,size ,size);
		//	if (position != pr::v4Origin) str += Fmt("*Position {%f %f %f}" ,position.x ,position.y ,position.z);
		//	str += "}\n";
		//}

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
		//template <typename TStr> inline void QuadLU(char const* name, unsigned int colour, v4 lower, v4 upper, TStr& str)
		//{
		//	str += Fmt("*QuadLU %s %08X "
		//				"{ "
		//					" %f %f %f "
		//					" %f %f %f "
		//				"}\n"
		//				,name ,colour
		//				,lower.x ,lower.y ,lower.z
		//				,upper.x ,upper.y ,upper.z
		//				);
		//}
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
		//template <typename TStr> inline void Matrix4x4(char const* name, unsigned int colour, m4x4 tx, float scale, TStr& str)
		//{
		//	v4 x = tx.x * scale;
		//	v4 y = tx.y * scale;
		//	v4 z = tx.z * scale;
		//	v4 w = tx.pos;
		//	str += FmtS("*Matrix4x4 %s %08X "
		//				"{ "
		//					"%f %f %f %f "
		//					"%f %f %f %f "
		//					"%f %f %f %f "
		//					"%f %f %f %f "
		//				"}\n"
		//				,name ,colour
		//				,x.x ,x.y ,x.z ,x.w
		//				,y.x ,y.y ,y.z ,y.w
		//				,z.x ,z.y ,z.z ,z.w
		//				,w.x ,w.y ,w.z ,w.w
		//				);
		//}
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
		//template <typename TStr> inline void CrossHair(char const* name, unsigned int colour, v4 position, float size, TStr& str)
		//{
		//	str += FmtS("*Line %s %08X "
		//				"{ "
		//					"%f 0 0 %f 0 0  0 %f 0 0 %f 0  0 0 %f 0 0 %f "
		//					"*Position { %f %f %f } "
		//				"}\n"
		//				,name ,colour
		//				,-size/2 ,size/2 ,-size/2 ,size/2 ,-size/2 ,size/2
		//				,position.x ,position.y ,position.z
		//				);
		//}
		//template <typename TStr> inline void BBox(char const* name, unsigned int colour, const pr::BBox& bbox, TStr& str)
		//{
		//	v4 lower = bbox.Lower();
		//	v4 upper = bbox.Upper();
		//	str += FmtS("*BoxLU %s %08X "
		//				"{ "
		//					"%f %f %f "
		//					"%f %f %f "
		//				"}\n"
		//				,name, colour
		//				,lower.x, lower.y, lower.z
		//				,upper.x, upper.y, upper.z
		//				);
		//}
		//template <typename TStr> inline void OrientedBox(char const* name, unsigned int colour, const pr::OrientedBox& obox, TStr& str)
		//{
		//	str += FmtS("*Box %s %08X {" ,name ,colour);
		//	Vec3(2.0f * obox.m_radius, str);
		//	Txfm(obox.Getm4x4(), str)
		//	str += "}\n";
		//}

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

		//template <typename TStr> inline void Mesh(char const* name, unsigned int colour, pr::Mesh const& mesh, TStr& str)
		//{
		//	if (mesh.m_vertex.empty() || mesh.m_face.empty()) return;
		//	bool gen_norms = IsZero3(mesh.m_vertex[0].m_normal);

		//	str += FmtS("*Mesh %s %08X\n{\n" ,name ,colour);
		//	str +=         "\t*Verts {\n";
		//	for (TVertCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v)
		//	{	str += FmtS("\t%f %f %f\n" ,v->m_vertex.x ,v->m_vertex.y ,v->m_vertex.z); }
		//	str +=         "\t}\n";
		//	str +=         "\t*Faces {\n";
		//	for (TFaceCont::const_iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f)
		//	{	str += FmtS("\t%d %d %d\n" ,f->m_vert_index[0] ,f->m_vert_index[1] ,f->m_vert_index[2]); }
		//	str +=         "\t}\n";
		//	if (gen_norms)
		//	{	str +=     "\t*GenerateNormals\n"; }
		//	else {
		//	str +=         "\t*Normals {\n";
		//	for (TVertCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v)
		//	{	str += FmtS("\t%f %f %f\n" ,v->m_normal.x ,v->m_normal.y ,v->m_normal.z); }
		//	str +=         "\t}\n";
		//	str +=     "}\n";
		//	}
		//}
	}
}

#endif
