//***************************************
// Physics Testbed Test Code
//***************************************

#include "PhysicsTestbed/Stdafx.h"
//#include "PhysicsTestbed/DefrmMesh.h"
//#include "PhysicsTestbed/PhysicsEngine.h"
//
//#define PHYSICS_TESTBED_DUMP_POLYTOPES 1
//#if PHYSICS_TESTBED_DUMP_POLYTOPES
//	#include "pr/LineDrawerHelper/LineDrawerHelper.h"
//#endif//PHYSICS_TESTBED_DUMP_POLYTOPES
//
//using namespace pr;
//
//DeformableMesh::DeformableMesh(parse::Deformable const& deformable, pr::Colour32 colour)
//:m_deform()
//,m_model()
//,m_strength(deformable.m_strength)
//,m_convex_tolerance(deformable.m_convex_tolerance)
//,m_colour(colour)
//{
//	PhysicsEngine::CreateDeformableModel(deformable, m_deform);
//}
//
//// Create a collision model from the deformable mesh
//void DeformableMesh::GenerateCollisionModel()
//{
//	PhysicsEngine::Decompose(m_deform, m_col_model, m_convex_tolerance);
//
//	// test the new polybuilder helper
//#if USE_REFLECTIONS_ENGINE
//	PHcollisionModel guard;			memset(&guard, 0xCC, sizeof(guard));
//	PHcollisionModel buffer[102];	memset(&buffer[0], 0xCC, sizeof(buffer));
//	PolytopeBuilder poly_builder(&buffer[1], sizeof(buffer) - 2*sizeof(buffer[0]));
//	PhysicsEngine::Decompose(m_deform, poly_builder, m_convex_tolerance);
//	PR_ASSERT(1, memcmp(&buffer[0]  , &guard, sizeof(buffer[0])) == 0);
//	PR_ASSERT(1, memcmp(&buffer[101], &guard, sizeof(buffer[0])) == 0);
//#endif	
//	
//	PR_EXPAND(PHYSICS_TESTBED_DUMP_POLYTOPES, ClearFile("C:/Deleteme/tetramesh_polytopes.pr_script");)
//	m_model.m_prim.resize(0);
//	PhysicsEngine::Decompose(m_deform, *this, m_convex_tolerance);
//}
//
//void DeformableMesh::BeginPolytope()
//{
//	parse::Prim prim;
//	prim.m_type   = parse::Prim::EType_PolytopeExplicit;
//	prim.m_colour = m_colour;
//	m_model.m_prim.push_back(prim);
//}
//void DeformableMesh::AddPolytopeVert(VertType const& vertex)
//{
//	parse::Prim& prim = m_model.m_prim.back();
//	prim.m_vertex.push_back(conv(vertex));
//}
//void DeformableMesh::AddPolytopeFace(VIndex a, VIndex b, VIndex c)
//{
//	parse::Prim& prim = m_model.m_prim.back();
//	prim.m_face.push_back(a);
//	prim.m_face.push_back(b);
//	prim.m_face.push_back(c);
//}
//void DeformableMesh::EndPolytope()
//{
//	// Write the polytope into "tetramesh_polytopes.pr_script"
//	#if PHYSICS_TESTBED_DUMP_POLYTOPES
//	#pragma message("q:/Paul/PhysicsTestbed/PropDeformable.cpp(45): *********************************** PHYSICS_TESTBED_DUMP_POLYTOPES is defined")
//	char const* colour[] = {"FF990000", "FF009900", "FF000099", "FF999900", "FF009999", "FF990099", "FF999999", "FF550000", "FF005500", "FF000055", "FF555500", "FF005555", "FF550055", "FF555555", "FF995500", "FF009955", "FF990055", "FF995555", "FF559900", "FF005599", "FF550099", "FF559955", "FF555599"};
//	uint num_colours = sizeof(colour)/sizeof(colour[0]);
//
//	uint poly_id = (uint)m_model.m_prim.size();
//	parse::Prim& prim = m_model.m_prim.back();
//
//	static float scale = 1.0f;
//	v4 centre = v4Zero;
//	for( parse::TIndices::iterator f = prim.m_face.begin(), f_end = prim.m_face.end(); f != f_end; ++f )
//		centre += prim.m_vertex[*f];
//	centre /= float(prim.m_face.size());
//
//	AppendFile("C:/Deleteme/tetramesh_polytopes.pr_script");
//	ldr::GroupStart(FmtS("polytope_%d", poly_id));
//	for( parse::TIndices::iterator f = prim.m_face.begin(), f_end = prim.m_face.end(); f != f_end; f += 3 )
//	{
//		v4 a = scale * (prim.m_vertex[*(f+0)] - centre) + centre;
//		v4 b = scale * (prim.m_vertex[*(f+1)] - centre) + centre;
//		v4 c = scale * (prim.m_vertex[*(f+2)] - centre) + centre;
//		ldr::Triangle("tri", colour[poly_id%num_colours], a, b, c);
//	}
//	ldr::GroupEnd();
//	EndFile();
//	#endif//PHYSICS_TESTBED_DUMP_POLYTOPES
//}
