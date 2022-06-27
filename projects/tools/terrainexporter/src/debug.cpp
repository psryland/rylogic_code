//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "terrainexporter/debug.h"
#include "terrainexporter/line2d.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/face.h"

int debug_cpp_symbol;
//#if PR_DBG_TERRAIN
//
//#pragma message(__FILE__(__LINE__) "WARING: ************************************* PR_DBG_TERRAIN is enabled");
//
//
//using namespace pr;
//using namespace pr::terrain;
//
//DebugFn g_db;
//
//// Make an orientation matrix from a direction. Note the rotation around the direction
//// vector is not defined. 'axis' is the axis that 'direction' will become
//MAm4& OrientationFromDirection(MAm4& orientation, pr::v4 const& direction, int axis)
//{
//	pr::v4 direction2(direction[0], direction[2], -direction[1], 0.0f);
//	direction2.getNormal3(direction2);
//
//	orientation[ axis         ]	= direction;
//	orientation[ axis         ] .setW0();
//	orientation[(axis + 1) % 3] .getNormal3(direction.cross(direction2));
//	orientation[(axis + 2) % 3] = orientation[axis].cross(orientation[(axis + 1) % 3]);
//	orientation[3]				.setAsPosition(0,0,0);
//	return orientation;
//}
//
//void DebugFn::DumpFRect(FRect const* rect_ptr, unsigned int colour)
//{
//	FRect const& rect = *rect_ptr;
//	fprintf(Output(),
//		"*Line rect %8.8X {\n"
//		"\t%f 0 %f %f 0 %f\n"
//		"\t%f 0 %f %f 0 %f\n"
//		"\t%f 0 %f %f 0 %f\n"
//		"\t%f 0 %f %f 0 %f\n"
//		"}\n"
//		,colour
//		,rect.m_xmin ,rect.m_zmin ,rect.m_xmax ,rect.m_zmin
//		,rect.m_xmax ,rect.m_zmin ,rect.m_xmax ,rect.m_zmax
//		,rect.m_xmax ,rect.m_zmax ,rect.m_xmin ,rect.m_zmax
//		,rect.m_xmin ,rect.m_zmax ,rect.m_xmin ,rect.m_zmin
//		);
//}
//
//void DebugFn::DumpLine2d(Line2d const* line_ptr, unsigned int colour)
//{
//	Line2d const& line = *line_ptr;
//	Line2d left = line.ccw90();
//	pr::v4 arrow_tip = line.Start() + line.Vector() * 0.95f + left.Normal() * line.Length() * 0.05f;
//
//	fprintf(Output(),
//		"*Line line %8.8X { "
//		"%f 0 %f %f 0 %f "
//		"%f 0 %f %f 0 %f "
//		" }\n"
//		,colour
//		,line.Start()[0] ,line.Start()[2] ,line.End()[0] ,line.End()[2]
//		,line.End()[0] ,line.End()[2], arrow_tip[0], arrow_tip[2]
//		);
//}
//
//void DebugFn::DumpEdge(Edge const* edge_ptr, unsigned int colour)
//{
//	Edge const& edge = *edge_ptr;
//	fprintf(Output(),
//		"*Line edge %8.8X { "
//		"%f %f %f %f %f %f "
//		" }\n"
//		,colour
//		,edge.m_vertex0->m_position[0] ,edge.m_vertex0->m_position[1] ,edge.m_vertex0->m_position[2]
//		,edge.m_vertex1->m_position[0] ,edge.m_vertex1->m_position[1] ,edge.m_vertex1->m_position[2]
//		);
//}
//
//void DebugFn::DumpFace(Face const* face_ptr)
//{
//	Face const& face = *face_ptr;
//	unsigned int colour = 0xA0 << 24 |
//			pr::uint8(face.m_plane_eqn->m_eqn[0] * 0xFF) << 16 |
//			pr::uint8(face.m_plane_eqn->m_eqn[1] * 0xFF) << 8  |
//			pr::uint8(face.m_plane_eqn->m_eqn[2] * 0xFF) << 0;
//
//	fprintf(Output(), "*Triangle face_%d %8.8X {\n", face.m_face_number, colour);
//	fprintf(Output(), "%f %f %f %f %f %f %f %f %f\n"
//		,face.m_original_vertex[0][0]
//		,face.m_original_vertex[0][1]
//		,face.m_original_vertex[0][2]
//		,face.m_original_vertex[1][0]
//		,face.m_original_vertex[1][1]
//		,face.m_original_vertex[1][2]
//		,face.m_original_vertex[2][0]
//		,face.m_original_vertex[2][1]
//		,face.m_original_vertex[2][2]);
//		fprintf(Output(),"*Line face %8.8X {\n"
//			"\t%f %f %f %f %f %f\n"
//			"\t%f %f %f %f %f %f\n"
//			"\t%f %f %f %f %f %f\n"
//			"}\n"
//			,colour
//			,face.m_original_vertex[0][0] ,face.m_original_vertex[0][1] ,face.m_original_vertex[0][2]
//			,face.m_original_vertex[1][0] ,face.m_original_vertex[1][1] ,face.m_original_vertex[1][2]
//			,face.m_original_vertex[1][0] ,face.m_original_vertex[1][1] ,face.m_original_vertex[1][2]
//			,face.m_original_vertex[2][0] ,face.m_original_vertex[2][1] ,face.m_original_vertex[2][2]
//			,face.m_original_vertex[2][0] ,face.m_original_vertex[2][1] ,face.m_original_vertex[2][2]
//			,face.m_original_vertex[0][0] ,face.m_original_vertex[0][1] ,face.m_original_vertex[0][2]
//			);
//		pr::v4 mid = face.MidPoint();
//		fprintf(Output(),"*LineD normal %8.8X {\n"
//			"\t%f %f %f %f %f %f\n"
//			"}\n"
//			,colour
//			,mid[0] ,mid[1] ,mid[2]
//			,face.m_plane_eqn->m_eqn[0] ,face.m_plane_eqn->m_eqn[1] ,face.m_plane_eqn->m_eqn[2]
//			);
//	fprintf(Output(), "}\n");
//}
//
//void DebugFn::DumpPlane(const CellEx* cell_ptr, pr::v4 const& plane_equation, unsigned int colour)
//{
//	PR_ASSERT(PR_DBG_TERRAIN, plane_equation[1] > 0.0f);
//
//	// Project the corners of the cell onto the plane
//	pr::v4 top_left     = cell_ptr->m_bounds.TopLeft();
//	pr::v4 top_right    = cell_ptr->m_bounds.TopRight();
//	pr::v4 bottom_left  = cell_ptr->m_bounds.BottomLeft();
//	pr::v4 bottom_right = cell_ptr->m_bounds.BottomRight();
//	top_left     [1] = (plane_equation[3] - plane_equation[0]*top_left     [0] - plane_equation[2]*top_left     [2]) / plane_equation[1];
//	top_right    [1] = (plane_equation[3] - plane_equation[0]*top_right    [0] - plane_equation[2]*top_right    [2]) / plane_equation[1];
//	bottom_left  [1] = (plane_equation[3] - plane_equation[0]*bottom_left  [0] - plane_equation[2]*bottom_left  [2]) / plane_equation[1];
//	bottom_right [1] = (plane_equation[3] - plane_equation[0]*bottom_right [0] - plane_equation[2]*bottom_right [2]) / plane_equation[1];
//
//	fprintf(Output(), "*Quad plane %8.8X {\n %f %f %f\n %f %f %f\n %f %f %f\n %f %f %f\n}\n"
//		,colour
//		,top_left     [0] ,top_left     [1] ,top_left     [2]
//		,bottom_left  [0] ,bottom_left  [1] ,bottom_left  [2]
//		,bottom_right [0] ,bottom_right [1] ,bottom_right [2]
//		,top_right    [0] ,top_right    [1] ,top_right    [2]
//		);
//}
//
//void DebugFn::DumpMesh(const Mesh* mesh_ptr, unsigned int face_colour, unsigned int mesh_colour, unsigned int edges_colour)
//{
//	const Mesh& mesh = *mesh_ptr;
//	fprintf(Output(), "*Group mesh FFFFFFFF {\n");
//	{
//		fprintf(Output(), "*Triangle face %8.8X {\n", face_colour);
//		for( TFaceVec::const_iterator f = mesh.m_faces.begin(), f_end = mesh.m_faces.end(); f != f_end; ++f )
//		{
//			fprintf(Output(), "%f %f %f %f %f %f %f %f %f\n"
//				,f->m_original_vertex[0][0]
//				,f->m_original_vertex[0][1]
//				,f->m_original_vertex[0][2]
//				,f->m_original_vertex[1][0]
//				,f->m_original_vertex[1][1]
//				,f->m_original_vertex[1][2]
//				,f->m_original_vertex[2][0]
//				,f->m_original_vertex[2][1]
//				,f->m_original_vertex[2][2]);
//		}
//		fprintf(Output(), "}\n");
//
//		fprintf(Output(), "*Line mesh %8.8X {\n", mesh_colour);
//		for( TEdgeSet::const_iterator e = mesh.m_edges.begin(), e_end = mesh.m_edges.end(); e != e_end; ++e )
//		{
//			fprintf(Output(), "%f %f %f %f %f %f\n"
//				,e->m_vertex0->m_position[0]
//				,e->m_vertex0->m_position[1]
//				,e->m_vertex0->m_position[2]
//				,e->m_vertex1->m_position[0]
//				,e->m_vertex1->m_position[1]
//				,e->m_vertex1->m_position[2]);
//		}
//		fprintf(Output(), "}\n");
//
//		fprintf(Output(), "*Line edges %8.8X {\n", edges_colour);
//		for( TEdgeSet::const_iterator e = mesh.m_edges.begin(), e_end = mesh.m_edges.end(); e != e_end; ++e )
//		{
//			if( !e->m_contributes ) continue;
//			fprintf(Output(), "%f %f %f %f %f %f\n"
//				,e->m_vertex0->m_position[0]
//				,e->m_vertex0->m_position[1]
//				,e->m_vertex0->m_position[2]
//				,e->m_vertex1->m_position[0]
//				,e->m_vertex1->m_position[1]
//				,e->m_vertex1->m_position[2]);
//		}
//		fprintf(Output(), "}\n");
//	}
//	fprintf(Output(), "}\n");
//}
//
//void DebugFn::DumpCell(const CellEx* cell_ptr)
//{
//	const CellEx& cell = *cell_ptr;
//	fprintf(Output(), "*Group cell_%d FFFFFFFF {\n", cell.m_cell_number);
//	{	
//		DumpFRect(&cell.m_bounds, 0xA0FF0000);
//		
//		fprintf(Output(), "*Group faces FFFFFFFF {\n");
//		for( TTreeExList::const_iterator t = cell.m_tree.begin(), t_end = cell.m_tree.end(); t != t_end; ++t )
//		{
//			for( TFaceCPtrSet::const_iterator fp = t->m_faces.begin(), fp_end = t->m_faces.end(); fp != fp_end; ++fp )
//			{
//				fprintf(Output(), "*Triangle face 0xA0008000 { %f %f %f %f %f %f %f %f %f }\n"
//					,(*fp)->m_original_vertex[0][0]
//					,(*fp)->m_original_vertex[0][1]
//					,(*fp)->m_original_vertex[0][2]
//					,(*fp)->m_original_vertex[1][0]
//					,(*fp)->m_original_vertex[1][1]
//					,(*fp)->m_original_vertex[1][2]
//					,(*fp)->m_original_vertex[2][0]
//					,(*fp)->m_original_vertex[2][1]
//					,(*fp)->m_original_vertex[2][2]);
//			}
//		}
//		fprintf(Output(), "}\n");
//
//		fprintf(Output(), "*Group edges FFFFFFFF {\n");
//		for( TTreeExList::const_iterator t = cell.m_tree.begin(), t_end = cell.m_tree.end(); t != t_end; ++t )
//		{
//			TreeEx const& tree = *t;
//			for( TEdgeCPtrSet::const_iterator e = tree.m_edges.begin(), e_end = tree.m_edges.end(); e != e_end; ++e )
//			{
//				Edge const& edge = **e;
//				if( !edge.m_contributes ) continue;
//				fprintf(Output(), "*Line edge FFFF0000 { %f %f %f %f %f %f }\n"
//					,edge.m_vertex0->m_position[0]
//					,edge.m_vertex0->m_position[1]
//					,edge.m_vertex0->m_position[2]
//					,edge.m_vertex1->m_position[0]
//					,edge.m_vertex1->m_position[1]
//					,edge.m_vertex1->m_position[2]);
//			}
//		}
//		fprintf(Output(), "}\n");
//	}
//	fprintf(Output(), "}\n");
//}
//
//void DebugFn::DumpTree(BranchEx const* tree_ptr, unsigned int level, char side)
//{
//	BranchEx const& tree = *tree_ptr;
//	fprintf(Output(), "*Line tree_%c_%d_(%d_%d_%d) FFFFFFFF {\n", side, level, tree.m_a, tree.m_b, tree.m_c);
//	{	
//		Line2d const& line = tree.m_line;
//		Line2d left = line.ccw90();
//		pr::v4 arrow_tip = line.Start() + line.Vector() * 0.95f + left.Normal() * line.Length() * 0.05f;
//		fprintf(Output(),
//			"%f 0 %f %f 0 %f %f 0 %f %f 0 %f\n"
//			,line.Start()[0] ,line.Start()[2] ,line.End()[0] ,line.End()[2]
//			,line.End()[0] ,line.End()[2], arrow_tip[0], arrow_tip[2]
//			);
//
//		if( tree.m_Lbranch )	DumpTree(tree.m_Lbranch, level + 1, 'L');
//		else					DumpTree(tree.m_Lleaf, level + 1, 'L');
//		if( tree.m_Rbranch )	DumpTree(tree.m_Rbranch, level + 1, 'R');
//		else					DumpTree(tree.m_Rleaf, level + 1, 'R');
//	}
//	fprintf(Output(), "}\n");
//}
//
//void DebugFn::DumpTree(LeafEx const* leaf_ptr, unsigned int level, char side)
//{
//	LeafEx const& leaf = *leaf_ptr;
//	fprintf(Output(), "*Triangle leaf_%c_%d A0808080 {\n", side, level);
//	{
//		if( leaf.m_face )
//		{
//			Face const& face = *leaf.m_face;
//			fprintf(Output(),
//				"\t%f %f %f\n"
//				"\t%f %f %f\n"
//				"\t%f %f %f\n"
//				,face.m_original_vertex[0][0] ,face.m_original_vertex[0][1] ,face.m_original_vertex[0][2]
//				,face.m_original_vertex[1][0] ,face.m_original_vertex[1][1] ,face.m_original_vertex[1][2]
//				,face.m_original_vertex[2][0] ,face.m_original_vertex[2][1] ,face.m_original_vertex[2][2]
//				);
//		}
//	}
//	fprintf(Output(), "}\n");
//}
//
//void DebugFn::DumpBranches(const TBranchExList::const_iterator& lhs, const TBranchExList::const_iterator& rhs, unsigned int colour)
//{
//	fprintf(Output(), "*Group branch_list FFFFFFFF {\n");
//	for( TBranchExList::const_iterator b = lhs; b != rhs; ++b )
//	{	
//		DumpLine2d(&b->m_line, colour);
//	}
//	fprintf(Output(), "}\n");
//}
//
//void DebugFn::DumpTransform(const MAm4& tx)
//{
//	fprintf(Output(), "*Transform {\n %f %f %f %f\n %f %f %f %f\n %f %f %f %f\n %f %f %f %f\n}\n"
//		,tx[0][0] ,tx[0][1] ,tx[0][2] ,tx[0][3]
//		,tx[1][0] ,tx[1][1] ,tx[1][2] ,tx[1][3]
//		,tx[2][0] ,tx[2][1] ,tx[2][2] ,tx[2][3]
//		,tx[3][0] ,tx[3][1] ,tx[3][2] ,tx[3][3]);
//}
//
//#endif//PR_DBG_TERRAIN
