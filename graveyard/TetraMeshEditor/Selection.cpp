//***************************************
// TetraMesh Editor
//***************************************
#include "Stdafx.h"
#include "pr/common/StdAlgorithm.h"
#include "pr/common/LineDrawerHelper.h"
#include "Selection.h"

using namespace pr::tetramesh;

Selection::Selection()
:m_ldr(0)
,m_vert_colour("FFFFFF00")
,m_face_colour("FFFFFF00")
{}

bool Selection::Empty() const
{
	return m_verts.empty() && m_faces.empty();
}

void Selection::Clear()
{
	m_verts.resize(0);
	m_tetra.resize(0);
	m_faces.resize(0);
}

void Selection::Select(TetraMeshEx const& mesh, v4 const& point, v4 const& ray)
{
	Clear();
	if( mesh.Empty() ) return;

	v4 const* verts = &mesh.m_verts[0];

	bool hit = false;
	float min_t = 1.0f;
	v4 min_bary = v4Zero;
	tetramesh::Face face;
	for( Tetra const *tetra = mesh.m_tetra, *t_end = mesh.m_tetra + mesh.m_num_tetra; tetra != t_end; ++tetra )
	{
		for( int i = 0; i != 4; ++i )
		{
			v4 a = verts[tetra->m_cnrs[FaceIndex[i][0]]];
			v4 b = verts[tetra->m_cnrs[FaceIndex[i][1]]];
			v4 c = verts[tetra->m_cnrs[FaceIndex[i][2]]];
			v4 ctr = (a + b + c) / 3.0f;
			a = ctr + (a - ctr) * 1.05f;
			b = ctr + (b - ctr) * 1.05f;
			c = ctr + (c - ctr) * 1.05f;

			float t;
			v4 bary;
			if( tetra->m_nbrs[i] == ExtnFace &&
				Intersect_InfiniteLineToTriangle(point, point + ray, a, b, c, bary, t) && t < min_t )
			{
				hit				= true;
				face.m_tetra0	= static_cast<TIndex>(tetra - mesh.m_tetra);
				face.m_i[0]		= tetra->m_cnrs[FaceIndex[i][0]];
				face.m_i[1]		= tetra->m_cnrs[FaceIndex[i][1]];
				face.m_i[2]		= tetra->m_cnrs[FaceIndex[i][2]];
				min_t			= t;
				min_bary		= bary;
			}
		}
	}

	if( hit )
	{
		SelectFeature(mesh, face, min_bary);
	}
}

void Selection::SelectFeature(TetraMeshEx const& mesh, tetramesh::Face const& face, v4 const& bary)
{
	int largest  = bary.LargestElement3();
	int smallest = bary.SmallestElement3();

	// If one bary coord is close to 1.0 then it's a vertex selection
	if( bary[largest] > 0.9f )
	{
		VIndex selected_vert = face.m_i[largest];
		m_verts.push_back(selected_vert);

		// Select any external faces connected to the vertex
		TIndex t_idx = 0;
		for( Tetra const *tetra = mesh.m_tetra, *t_end = mesh.m_tetra + mesh.m_num_tetra; tetra != t_end; ++tetra, ++t_idx )
		{
			for( int i = 0; i != 4; ++i )
			{
				if( tetra->m_nbrs[i] == ExtnFace )
				{
					tetramesh::Face face;				
					face.m_i[0] = tetra->m_cnrs[FaceIndex[i][0]];
					face.m_i[1] = tetra->m_cnrs[FaceIndex[i][1]];
					face.m_i[2] = tetra->m_cnrs[FaceIndex[i][2]];
					if( face.m_i[0] == selected_vert || face.m_i[1] == selected_vert || face.m_i[2] == selected_vert )
					{
						face.m_tetra0 = t_idx;
						face.m_order  = GetFaceIndexOrder(face);
						m_faces.push_back(face);
					}
				}
				if( tetra->m_cnrs[i] == selected_vert )
				{
					TTIndices::iterator iter = std::lower_bound(m_tetra.begin(), m_tetra.end(), t_idx);
					if( iter == m_tetra.end() || *iter != face.m_tetra0 ) m_tetra.insert(iter, t_idx);
				}
			}
		}
	}

	// If one bary coord is close to 0.0 then it's an edge selection
	else if( bary[smallest] < 0.05f )
	{
		VIndex v0 = face.m_i[(smallest + 1) % 3];
		VIndex v1 = face.m_i[(smallest + 2) % 3];
		m_verts.push_back(v0);
		m_verts.push_back(v1);

		// Select the faces that are connected to this edge
		TIndex t_idx = 0;
		for( Tetra const *tetra = mesh.m_tetra, *t_end = mesh.m_tetra + mesh.m_num_tetra; tetra != t_end; ++tetra, ++t_idx )
		{
			for( int i = 0; i != 4; ++i )
			{
				if( tetra->m_nbrs[i] == ExtnFace )
				{
					tetramesh::Face face;				
					face.m_i[0] = tetra->m_cnrs[FaceIndex[i][0]];
					face.m_i[1] = tetra->m_cnrs[FaceIndex[i][1]];
					face.m_i[2] = tetra->m_cnrs[FaceIndex[i][2]];
					bool matched_v0 = face.m_i[0] == v0 || face.m_i[1] == v0 || face.m_i[2] == v0;
					bool matched_v1 = face.m_i[0] == v1 || face.m_i[1] == v1 || face.m_i[2] == v1;
					if( matched_v0 && matched_v1 )
					{
						face.m_tetra0 = t_idx;
						face.m_order  = GetFaceIndexOrder(face);
						m_faces.push_back(face);
					}
				}
			}
			
			bool matched_v0 = tetra->m_cnrs[0] == v0 || tetra->m_cnrs[1] == v0 || tetra->m_cnrs[2] == v0 || tetra->m_cnrs[3] == v0;
			bool matched_v1 = tetra->m_cnrs[0] == v1 || tetra->m_cnrs[1] == v1 || tetra->m_cnrs[2] == v1 || tetra->m_cnrs[3] == v1;
			if( matched_v0 && matched_v1 )
			{
				TTIndices::iterator iter = std::lower_bound(m_tetra.begin(), m_tetra.end(), t_idx);
				if( iter == m_tetra.end() || *iter != face.m_tetra0 ) m_tetra.insert(iter, t_idx);
			}
		}
	}

	// Otherwise, it's a face selection
	else
	{
		m_faces.push_back(face);
		m_tetra.push_back(face.m_tetra0);
	}
}

void Selection::Merge(Selection const& selection)
{
	m_verts.insert(m_verts.end(), selection.m_verts.begin(), selection.m_verts.end());
	m_tetra.insert(m_tetra.end(), selection.m_tetra.begin(), selection.m_tetra.end());
	m_faces.insert(m_faces.end(), selection.m_faces.begin(), selection.m_faces.end());
	std::sort(m_verts.begin(), m_verts.end());
	std::sort(m_tetra.begin(), m_tetra.end());
	std::sort(m_faces.begin(), m_faces.end());
	m_verts.resize(std::unique(m_verts.begin(), m_verts.end()) - m_verts.begin());
	m_tetra.resize(std::unique(m_tetra.begin(), m_tetra.end()) - m_tetra.begin());
	m_faces.resize(std::unique(m_faces.begin(), m_faces.end()) - m_faces.begin());
}

bool Selection::OneFace() const
{
	return m_faces.size() == 1;
}

bool Selection::OneEdge() const
{
	return m_verts.size() == 2;
}

bool Selection::OneVert() const
{
	return m_verts.size() == 1;
}

tetramesh::Face Selection::Face() const
{
	PR_ASSERT(PR_DBG_COMMON, OneFace());
	return m_faces[0];
}

tetramesh::VIndex Selection::Vert0() const
{
	PR_ASSERT(PR_DBG_COMMON, OneEdge());
	return m_verts[0];
}

tetramesh::VIndex Selection::Vert1() const
{
	PR_ASSERT(PR_DBG_COMMON, OneEdge());
	return m_verts[1];
}

tetramesh::VIndex Selection::Vert() const
{
	PR_ASSERT(PR_DBG_COMMON, OneVert());
	return m_verts[0];
}

void Selection::UpdateLdr(TetraMeshEx const& mesh)
{
	if( m_ldr ) { ldrUnRegisterObject(m_ldr); m_ldr = 0; }
	if( !Empty() )
	{
		std::string str;
		v4 const* verts = &mesh.m_verts[0];

		ldr::GroupStart("selection", str);
		for( tetramesh::TVIndices::const_iterator i = m_verts.begin(), i_end = m_verts.end(); i != i_end; ++i )
			ldr::Box("vert", m_vert_colour, verts[*i], 0.05f, str);
		for( tetramesh::TFaces::const_iterator f = m_faces.begin(), f_end = m_faces.end(); f != f_end; ++f )
			ldr::Triangle("face", m_face_colour, verts[f->m_i[0]], verts[f->m_i[1]], verts[f->m_i[2]], str);
		ldr::GroupEnd(str);
		m_ldr = ldrRegisterObject(str.c_str(), str.size());
	}
	ldrRender();
}