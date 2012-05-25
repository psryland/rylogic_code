//*****************************************
// Test geometry
//*****************************************
#include "test.h"
#include "pr/geometry/geosphere.h"
#include "pr/geometry/patch.h"
#include "pr/geometry/skin.h"
#include "pr/geometry/optimise_mesh.h"
#include "pr/storage/xfile/xfile.h"

using namespace pr;

namespace TestGeometry
{
	void TestGeoSphere();
	void TestPatch();
	void TestSkin();

	void Run()
	{
		TestGeoSphere();
		TestPatch();
		TestSkin();
	}

	// Make an geosphere in an xfile
	void TestGeoSphere()
	{
		//for( int i = 0; i != 6; ++i )
		{
			int i = 2;
			Geometry sphere;
			pr::geometry::GenerateGeosphere(sphere, 10.0f, i);

			//FILE* fp = fopen("C:/DeleteMe/geosphere.pr_script", "w");
			//fprintf(fp, "*Triangle blah FFFFFFFF {\n");
			//TVertexCont const& verts = sphere.m_frame[0].m_mesh.m_vertex;
			//for( TVertexCont::const_iterator i = verts.begin(), i_end = verts.end(); i != i_end; ++i )
			//{
			//	Vertex const& vert = *i;
			//	fprintf(fp, "MAv4::make(%ff, %ff, %ff, 1.0f),\n", vert.m_vertex.x, vert.m_vertex.y, vert.m_vertex.z);
			//}
			TFaceCont& faces = sphere.m_frame[0].m_mesh.m_face;
			for( TFaceCont::iterator i = faces.begin(), i_end = faces.end(); i != i_end; ++i )
			{
				Face& face = *i;
				pr::uint16 swp = face.m_vert_index[1];
				face.m_vert_index[1] = face.m_vert_index[2];
				face.m_vert_index[2] = swp;

				//fprintf(fp, "%d, %d, %d,\n",
				//	face.m_vert_index[0],
				//	face.m_vert_index[1],
				//	face.m_vert_index[2]);
			}		
			//fprintf(fp, "}\n");
			//fclose(fp);

			geometry::OptimiseMesh(sphere.m_frame[0].m_mesh);
			xfile::Save(sphere, "D:/Deleteme/terrain_sphere.x");
		}
		//Texture texture;
		//texture.m_filename = "asteroid_01.tga";
		//sphere.m_frame[0].m_mesh.m_material[0].m_sub_material.Add(texture);

		//XLoader loader;
		//loader.Load("C:\\Temp\\Geosphere.x", geometry);
		//Ldr::StartFile("C:\\temp\\Geosphere.txt");
		//Ldr::Print("rectanglelu tex FFFFFFFF { 0 0 0 1 1 0 }\n");
		//Ldr::Print(Fmt("Line tex_coords FFFFFFFF\n{\n"));

		//Mesh& mesh = geometry.m_frame[0].m_mesh;
		//uint num_verts = mesh.m_vertex.GetCount();
		//uint num_faces = mesh.m_face.GetCount();
		//for( uint f = 0; f < num_faces; ++f )
		//{
		//	Face& face = mesh.m_face[f];
		//	Vertex& v0 = mesh.m_vertex[face.m_vert_index[0]];
		//	Vertex& v1 = mesh.m_vertex[face.m_vert_index[1]];
		//	Vertex& v2 = mesh.m_vertex[face.m_vert_index[2]];
		//	Ldr::Print(Fmt("%0.3f %0.3f 0.0 %0.3f %0.3f 0.0\n", v0.m_tex_vertex[0], v0.m_tex_vertex[1], v1.m_tex_vertex[0], v1.m_tex_vertex[1]));
		//	Ldr::Print(Fmt("%0.3f %0.3f 0.0 %0.3f %0.3f 0.0\n", v1.m_tex_vertex[0], v1.m_tex_vertex[1], v2.m_tex_vertex[0], v2.m_tex_vertex[1]));
		//	Ldr::Print(Fmt("%0.3f %0.3f 0.0 %0.3f %0.3f 0.0\n", v2.m_tex_vertex[0], v2.m_tex_vertex[1], v0.m_tex_vertex[0], v0.m_tex_vertex[1]));
		//}
		//Ldr::Print("\n}\n");

		//Ldr::PRMesh("Geosphere", "FFFFFFFF", mesh);
		//Ldr::EndFile();
	}

	void TestPatch()
	{
		Geometry patch;
		pr::geometry::GeneratePatch(patch, v2::make(1.0f,1.0f), v2::make(5.0f,5.0f), iv2::make(3,3));
	}

	void TestSkin()
	{
		pr::Array<v4> verts(10);
		for (int i = 0; i != 10; ++i) verts[0] = v4Random3(0.5f, 2.0f, 1.0f);
		
		Geometry skin;
		pr::geometry::GenerateSkin(skin, &verts[0], verts.size());
	}

}//namespace TestGeometry

/*
//**********************************************************
//
//	Test program for geometry
//
//**********************************************************

#include <conio.h>
#include "pr/macros/link.h"
#include "pr/geometry/geometry.h"
#include "pr/geometry/Primitive.h"
#include "pr/geometry/Manipulator/Manipulator.h"
#include "pr/storage/xfile/XFile.h"

using namespace pr;
using namespace pr::geometry;

inline void Print(const v4& point, const v4& norm, const v2& tex)
{
	printf("{{%6.6ff, %6.6ff, %6.6ff, %6.6ff}, {%6.6ff, %6.6ff, %6.6ff, %6.6ff}, {0x%8.8X}, {%6.6ff, %6.6ff}},\n",
		point.x, point.y, point.z, point.w,
		norm.x, norm.y, norm.z, norm.w,
		Colour32White,
		tex.x, tex.y);
}

inline void Print(int i0, int i1, int i2)
{
	printf("%d, %d, %d,\n", i0, i1, i2);
}
int main(int, char*[])
{
	namespace test = unit_cylinder;
	Geometry geom;

	geom.m_name = "C";
	geom.m_frame.push_back(Frame());
	Frame& frame = geom.m_frame.back();

	frame.m_name		= geom.m_name;
	frame.m_transform	= m4x4Identity;
	for( const Vertex *v = test::vertices, *v_end = test::vertices + test::num_vertices; v != v_end; ++v )
	{
		frame.m_mesh.m_vertex.push_back(*v);
	}
	for( const Index *i = test::indices, *i_end = test::indices + test::num_indices; i != i_end; )
	{
		PR_ASSERT(1, *i < frame.m_mesh.m_vertex.size());
		Face face;
		face.m_vert_index[0] = *i++;
		face.m_vert_index[1] = *i++;
		face.m_vert_index[2] = *i++;
		frame.m_mesh.m_face.push_back(face);
	}
	Verify(xfile::Save(geom, "C:/DeleteMe/UnitCylinder.x"));



	uint m_wedges = 20;
	uint m_layers = 1;
	float height		= 1.0f;
	float xradius		= 1.0f;
	float yradius		= 1.0f;
	uint num_vertices	= 2 + (m_wedges + 1) * (m_layers + 3);


	uint16 w, layer, wedges = (uint16)m_wedges;
	float z = -height / 2.0f, dz = height / m_layers;
	float da = 2.0f * maths::pi / m_wedges;
	
	// Bottom face
	v4 point = {0, 0, z, 1.0f};
	Print(point, v4::make(0.0f, 0.0f, -1.0f, 0.0f), v2::make(0.0f, 0.0f));
	for( w = 0; w <= m_wedges; ++w )
	{
		point.Set(Cos(w * da) * xradius, Sin(w * da) * yradius, z, 1.0f);
		Print(point, v4::make(0.0f, 0.0f, -1.0f, 0.0f), v2::make(w / (float)m_wedges, 0.0f));		
	}

	// The walls
	v4 norm;
	for( layer = 0; layer <= m_layers; ++layer )
	{
		for( w = 0; w <= m_wedges; ++w )
		{
			point.Set(Cos(w * da) * xradius, Sin(w * da) * yradius, z,    1.0f);
			norm .Set(Cos(w * da) / xradius, Sin(w * da) / yradius, 0.0f, 0.0f);
			norm.Normalise3();

			Print(point, norm, v2::make(w / (float)m_wedges, z + 0.5f));
		}
		z += dz;
	}

	// Top face
	z = height / 2.0f;
	for( w = 0; w <= m_wedges; ++w )
	{
		point.Set(Cos(w * da) * xradius, Sin(w * da) * yradius, z, 1.0f);
		Print(point, v4::make(0.0f, 0.0f, 1.0f, 0.0f), v2::make(w / (float)m_wedges, z + 0.5f));
	}
	point.Set(0, 0, z, 1.0f);
	Print(point, v4::make(0.0f, 0.0f, 1.0f, 0.0f), v2::make(0.0f, 1.0f));

	// Create the bottom face
	for( w = 0; w <= wedges; ++w )
	{
		Print(0, w + 1, w + 2);
	}
	
	// Create the walls
	for( w = 0; w <= wedges; ++w )
	{
		Print(w + 2 + (wedges + 1), w + 1 +     (wedges + 1), w + 1 + 2 * (wedges + 1));
		Print(w + 2 + (wedges + 1), w + 1 + 2 * (wedges + 1), w + 2 + 2 * (wedges + 1));
	}
	
	// Create the top face
	uint16 last = (uint16)(num_vertices - 1);
	for( w = 0; w <= wedges; ++w )
	{
		Print(last, last - 1 - wedges + w, last - 2 - wedges + w);
	}

	_getch();
	return 0;
}
*/