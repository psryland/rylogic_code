//************************************
// LineDrawerHelper
//  (c)opyright Paul Ryland 2006
//************************************
#ifndef PR_LINE_DRAWER_HELPER_H
#define PR_LINE_DRAWER_HELPER_H

#include <string>
#include "pr/common/fmt.h"
#include "pr/maths/maths.h"
#include "pr/filesys/fileex.h"
#include "pr/geometry/colour.h"
#include "pr/geometry/geometry.h"

namespace pr
{
	namespace ldr
	{
		struct FileOutput
		{
			std::string m_filename;
			pr::Handle	m_handle;
			bool		m_append;

			explicit FileOutput(bool append = false)
			:m_filename("C:/deleteme/temp.txt")
			,m_handle(pr::FileOpen(m_filename.c_str(), append ? pr::EFileOpen_Append : pr::EFileOpen_Writing))
			,m_append(append)
			{}

			explicit FileOutput(char const* filename, bool append = false)
			:m_filename(filename)
			,m_handle(pr::FileOpen(m_filename.c_str(), append ? pr::EFileOpen_Append : pr::EFileOpen_Writing))
			,m_append(append)
			{}
			FileOutput& operator += (std::string const& str)
			{
				pr::FilePrint(m_handle, str.c_str());
				FlushFileBuffers(m_handle);
				return *this;
			}
			void resize(std::size_t new_size)
			{
				SetFilePointer(m_handle, (long)new_size, 0, FILE_BEGIN);
				SetEndOfFile(m_handle);
			}
			void clear()
			{
				SetFilePointer(m_handle, 0, 0, FILE_BEGIN);
				SetEndOfFile(m_handle);
			}
		private:
			FileOutput(FileOutput const&);
			FileOutput operator = (FileOutput const&);
		};

		template <typename TStr> inline void Write(TStr const& str, char const* filename, bool append)
		{
			pr::BufferToFile(str, filename, append);
		}
		template <typename TStr> inline void Vec3(v4 const& vec, TStr& str)
		{
			str += Fmt("%f %f %f " ,vec.x ,vec.y ,vec.z);
		}
		template <typename TStr> inline void Vec4(v4 const& vec, TStr& str)
		{
			str += Fmt("%f %f %f %f " ,vec.x ,vec.y ,vec.z ,vec.w);
		}
		template <typename TStr> inline void Pos(v4 const& vec, TStr& str)
		{
			str += "*Position {"; Vec3(vec, str); str += "} ";
		}
		template <typename TStr> inline void Dir(v4 const& vec, TStr& str)
		{
			str += "*Direction {"; Vec3(vec, str); str += "} ";
		}
		template <typename TStr> inline void Txfm(const m4x4& mat, TStr& str)
		{
			str += "*Transform {";
			Vec4(mat.x, str);
			Vec4(mat.y, str);
			Vec4(mat.z, str);
			Vec4(mat.w, str);
			str += "} ";
		}
		template <typename TStr> inline void Col(pr::Colour32 colour, TStr& str)
		{
			str += Fmt("%X", colour.m_aarrggbb);
		}
		template <typename TStr> inline void GroupStart(char const* name, unsigned int colour, TStr& str)
		{
			str += Fmt("*Group %s %08X {\n", name, colour);
		}
		template <typename TStr> inline void GroupStart(char const* name, TStr& str)
		{
			if (name == 0 || *name == 0) name = "unnamed";
			str += Fmt("*Group %s FFFFFFFF {\n", name);
		}
		template <typename TStr> inline void GroupStart(char const* name, int style, float fps, TStr& str)
		{
			// style = 0 = start->end, 1 = end->start, 2 = ping pong
			if (name == 0 || *name == 0) name = "unnamed";
			str += Fmt("*GroupCyclic %s FFFFFFFF {\n %d %f\n", name, style, fps);
		}
		template <typename TStr> inline void GroupEnd(TStr& str)
		{
			str += "}\n";
		}
		template <typename TStr> inline void Nest(TStr& str)
		{
			str.resize(str.size() - 2);
		}
		template <typename TStr> inline void UnNest(TStr& str)
		{
			str += "}\n";
		}
		template <typename TStr> inline void Position(const v4& pos, TStr& str)
		{
			Pos(pos, str);
		}
		template <typename TStr> inline void Transform(const m4x4& mat, TStr& str)
		{
			Txfm(mat, str);
		}
		template <typename TStr> inline void Point(char const* name, unsigned int colour, const v4& position, TStr& str)
		{
			str += Fmt(	"*BoxWHD %s %08X { 0.02 0.02 0.02 *Position {%f %f %f} }\n"
						,name ,colour
						,position[0] ,position[1] ,position[2]);
		}
		template <typename TStr> inline void Vector(char const* name, unsigned int colour, const v4& position, const v4& direction, float point_radius, TStr& str)
		{
			str += Fmt(	"*Line %s %08X "
						"{ "
							"0 0 0 %f %f %f "
							"*BoxWHD %s %08X { %f %f %f } "
							"*Position { %f %f %f } "
						"}\n"
						,name ,colour
						,direction[0] ,direction[1] ,direction[2]
						,name ,colour ,point_radius ,point_radius ,point_radius
						,position[0] ,position[1] ,position[2]
						);
		}
		template <typename TStr> inline void Line(char const* name, unsigned int colour, const v4& start, const v4& end, TStr& str)
		{
			str += Fmt(	"*Line %s %08X "
						"{ "
							"%f %f %f %f %f %f "
						"}\n"
						,name ,colour
						,start[0] ,start[1] ,start[2]
						,end[0] ,end[1] ,end[2]
						);
		}
		template <typename TStr> inline void LineD(char const* name, unsigned int colour, const v4& start, const v4& direction, TStr& str)
		{
			str += Fmt(	"*LineD %s %08X "
						"{ "
							"%f %f %f %f %f %f "
						"}\n"
						,name ,colour
						,start[0] ,start[1] ,start[2]
						,direction[0] ,direction[1] ,direction[2]
						);
		}
		template <typename TStr> inline void Rectangle(char const* name, unsigned int colour, v4 const& TL, v4 const& BL, v4 const& BR, v4 const& TR, TStr& str)
		{
			str += Fmt(	"*Rectangle %s %08X "
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
		template <typename TStr> inline void Circle(char const* name, unsigned int colour, v4 const& centre, float radius, TStr& str)
		{
			str += Fmt(	"*CircleR %s %08X "
						"{ "
							"%f "
							"*Position {%f %f %f} "
						"}\n"
						,name ,colour
						,radius
						,centre.x ,centre.y ,centre.z
						);
		}
		template <typename TStr> inline void Grid(char const* name, unsigned int colour, float dimx, float dimy, int divx, int divy, v4 const& position, TStr& str)
		{
			str += Fmt(	"*GridWH %s %08X "
						"{ "
							"%f %f %d %d "
							"*Position {%f %f %f} "
						"}\n"
						,name
						,colour
						,dimx ,dimy ,divx ,divy
						,position.x ,position.y ,position.z
						);
		}
		template <typename TStr> inline void Ellipse(char const* name, unsigned int colour, v4 const& centre, float major, float minor, TStr& str)
		{
			str += Fmt(	"*CircleRxRyZ %s %08X "
						"{ "
							"%f %f 0 "
							"*Position {%f %f %f} "
						"}\n"
						,name ,colour
						,major ,minor
						,centre.x ,centre.y ,centre.z
						);
		}
		template <typename TStr> inline void Sphere(char const* name, unsigned int colour, const v4& position, float radius, TStr& str)
		{
			str += Fmt(	"*SphereR %s %08X "
						"{ "
							"%f "
							"*Position { %f %f %f } "
						"}\n"
						,name ,colour
						,radius
						,position[0] ,position[1] ,position[2]
						);
		}
		template <typename TStr> inline void Box(char const* name, unsigned int colour, const v4& position, float size, TStr& str)
		{
			str += Fmt(	"*BoxWHD %s %08X "
						"{ "
							"%f %f %f "
							"*Position { %f %f %f } "
						"}\n"
						,name ,colour
						,size/2 ,size/2 ,size/2
						,position[0] ,position[1] ,position[2]
						);
		}
		template <typename TStr> inline void BoxWHD(char const* name, unsigned int colour, const v4& centre, const v4& dim, TStr& str)
		{
			str += Fmt(	"*BoxWHD %s %08X "
						"{ "
							"%f %f %f "
							"*Position { %f %f %f } "
						"}\n"
						,name ,colour
						,dim.x ,dim.y ,dim.z
						,centre.x ,centre.y ,centre.z
						);
		}
		template <typename TStr> inline void BoxWHD(char const* name, unsigned int colour, const m4x4& o2w, const v4& dim, TStr& str)
		{
			str += Fmt(	"*BoxWHD %s %08X { %f %f %f ",name ,colour ,dim.x ,dim.y ,dim.z);
			if (o2w != m4x4Identity) Txfm(o2w, str);
			str += "}\n";
		}
		template <typename TStr> inline void BoxLU(char const* name, unsigned int colour, const v4& lower, const v4& upper, TStr& str)
		{
			str += Fmt(	"*BoxLU %s %08X "
						"{ "
							"%f %f %f "
							"%f %f %f "
						"}\n"
						,name ,colour
						,lower.x ,lower.y ,lower.z
						,upper.x ,upper.y ,upper.z
						);
		}
		template <typename TStr> inline void CylinderHR(char const* name, unsigned int colour, const m4x4& o2w, float radius, float height, TStr& str)
		{
			str += Fmt(	"*CylinderHR %s %08X { %f %f " ,name ,colour ,height ,radius);
			if( o2w != m4x4Identity ) Txfm(o2w, str);
			str += "}\n";
		}
		template <typename TStr> inline void CapsuleHR(char const* name, unsigned int colour, const m4x4& o2w, float radius, float length, TStr& str)
		{
			str += Fmt(	"*CapsuleHR %s %08X { %f %f " ,name, colour ,length, radius);
			if (o2w != m4x4Identity) Txfm(o2w, str);
			str += "}\n";
		}
		template <typename TStr> inline void Quad(char const* name, unsigned int colour, const v4& x1, const v4& x2, const v4& x3, const v4& x4, TStr& str)
		{
			str += Fmt(	"*Quad %s %08X "
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
		template <typename TStr> inline void Quad(char const* name, unsigned int colour, float width, float height, const v4& position, const v4& direction, TStr& str)
		{
			v4 up   = Perpendicular(direction);
			v4 left = Cross3(up, direction);
			up *= height / 2.0f;
			left *= width / 2.0f;
			v4 c[4];
			c[0] = -up - left;
			c[1] =  up - left;
			c[2] =  up + left;
			c[3] = -up + left;
			str += Fmt("*Quad %s %08X { " ,name ,colour);
			Vec3(c[0], str);
			Vec3(c[1], str);
			Vec3(c[2], str);
			Vec3(c[3], str);
			Pos(position, str);
			str += "}\n";
		}
		template <typename TStr> inline void QuadLU(char const* name, unsigned int colour, const v4& lower, const v4& upper, TStr& str)
		{
			str += Fmt("*QuadLU %s %08X "
						"{ "
							" %f %f %f "
							" %f %f %f "
						"}\n"
						,name ,colour
						,lower.x ,lower.y ,lower.z
						,upper.x ,upper.y ,upper.z
						);
		}
		template <typename TStr> inline void Plane(char const* name, unsigned int colour, pr::Plane const& plane, v4 const& centre, float size, TStr& str)
		{
			v4 pos = ClosestPoint_PointToPlane(centre, plane);
			v4 dir = plane::GetDirection(plane::GetNormal(plane));
			Quad(name, colour, size, size, pos, dir, str);
			Nest(str);
			LineD(name, colour, v4Zero, dir, str);
			UnNest(str);
		}
		template <typename TStr> inline void Triangle(char const* name, unsigned int colour, m4x4 const& o2w, const v4& a, const v4& b, const v4& c, TStr& str)
		{
			str += Fmt("*Triangle %s %08X { " ,name ,colour);
			Vec3(a, str);
			Vec3(b, str);
			Vec3(c, str);
			if( o2w != m4x4Identity ) Txfm(o2w, str);
			str += "}\n";
		}
		template <typename TStr> inline void Triangle(char const* name, unsigned int colour, m4x4 const& o2w, v4 const* verts, int const* faces, int num_faces, TStr& str)
		{
			str += Fmt("*Triangle %s %s\n{\n", name ,colour);
			for( int const* i = faces, *i_end = i + 3*num_faces; i < i_end; )
			{
				Vec3(verts[*i++], str);
				Vec3(verts[*i++], str);
				Vec3(verts[*i++], str);
				str += "\n";
			}
			if( o2w != m4x4Identity ) Txfm(o2w, str);
			str += "}\n";
		}
		template <typename TStr> inline void Triangle(char const* name, unsigned int colour, v4 const& a, v4 const& b, v4 const& c, TStr& str)
		{
			Triangle(name, colour, m4x4Identity, a, b, c, str);
		}
		template <typename TStr> inline void Polytope(char const* name, unsigned int colour, const m4x4& o2w, const v4* begin, const v4* end, TStr& str)
		{
			str += Fmt("*Polytope %s %08X {\n", name, colour);
			for( v4 const* v = begin; v != end; ++v )
				Vec3(*v, str), str += "\n";
			if( o2w != m4x4Identity ) Txfm(o2w, str);
			str += "}\n";
		}
		template <typename TStr> inline void Matrix4x4(char const* name, unsigned int colour, const m4x4& tx, float scale, TStr& str)
		{
			v4 x = tx.x * scale;
			v4 y = tx.y * scale;
			v4 z = tx.z * scale;
			v4 w = tx.pos;
			str += Fmt(	"*Matrix4x4 %s %08X "
						"{ "
							"%f %f %f %f "
							"%f %f %f %f "
							"%f %f %f %f "
							"%f %f %f %f "
						"}\n"
						,name ,colour
						,x.x ,x.y ,x.z ,x.w
						,y.x ,y.y ,y.z ,y.w
						,z.x ,z.y ,z.z ,z.w
						,w.x ,w.y ,w.z ,w.w
						);
		}
		template <typename TStr> inline void Axis(char const* name, const m4x4& basis, TStr& str)
		{
			str += Fmt(	"*Group %s FFFFFFFF\n"
						"{\n"
							" *Line X FFFF0000 { 0 0 0 1 0 0 }\n"
							" *Line Y FF00FF00 { 0 0 0 0 1 0 }\n"
							" *Line Z FF0000FF { 0 0 0 0 0 1 }\n"
							"*Transform "
							"{ "
								"%f %f %f %f "
								"%f %f %f %f "
								"%f %f %f %f "
								"%f %f %f %f "
							"}\n"
						"}\n"
						,name
						,basis.x.x ,basis.x.y ,basis.x.z ,basis.x.w
						,basis.y.x ,basis.y.y ,basis.y.z ,basis.y.w
						,basis.z.x ,basis.z.y ,basis.z.z ,basis.z.w
						,basis.w.x ,basis.w.y ,basis.w.z ,basis.w.w
						);
		}
		template <typename TStr> inline void CrossHair(char const* name, unsigned int colour, const v4& position, float size, TStr& str)
		{
			str += Fmt(	"*Line %s %08X "
						"{ "
							"%f 0 0 %f 0 0  0 %f 0 0 %f 0  0 0 %f 0 0 %f "
							"*Position { %f %f %f } "
						"}\n"
						,name ,colour
						,-size/2 ,size/2 ,-size/2 ,size/2 ,-size/2 ,size/2
						,position.x ,position.y ,position.z
						);
		}
		template <typename TStr> inline void BoundingBox(char const* name, unsigned int colour, const pr::BoundingBox& bbox, TStr& str)
		{
			v4 lower = bbox.Lower();
			v4 upper = bbox.Upper();
			str += Fmt(	"*BoxLU %s %08X "
						"{ "
							"%f %f %f "
							"%f %f %f "
						"}\n"
						,name, colour
						,lower.x, lower.y, lower.z
						,upper.x, upper.y, upper.z
						);
		}
		template <typename TStr> inline void OrientedBox(char const* name, unsigned int colour, const pr::OrientedBox& obox, TStr& str)
		{
			str += Fmt("*BoxWHD %s %08X {" ,name ,colour);
			Vec3(2.0f * obox.m_radius, str);
			Txfm(obox.Getm4x4(), str)
			str += "}\n";
		}
		template <typename TStr> inline void PRMesh(char const* name, unsigned int colour, const Mesh& mesh, TStr& str)
		{
			str += Fmt(	"*Mesh %s %s\n"
						"{\n"
							"\t*GenerateNormals\n"
							"\t*Verts\n"
							"\t{\n", name, colour);
			for( TVertexCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v )
			{	str += Fmt(	"\t\t%3.3f %3.3f %3.3f\n",	v->m_vertex.x, v->m_vertex[1], v->m_vertex[2]); }
				str += Fmt(	"\t}\n"
							"\t*Faces\n"
							"\t{\n");
			for( TFaceCont::const_iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f )
			{	str += Fmt(	"\t\t%d %d %d\n",			f->m_vert_index[0], f->m_vert_index[1], f->m_vert_index[2]); }
				str += Fmt(	"\t}\n"
						"}\n");
		}
	}//namespace ldr
}//namespace pr

#endif//PR_LINE_DRAWER_HELPER_H
