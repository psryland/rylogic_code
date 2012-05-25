//***************************************
// TetraMesh Editor
//***************************************
#pragma once
#include "TetraMeshEx.h"

struct Selection
{
	Selection();
	bool Empty() const;
	void Clear();
	void Select(TetraMeshEx const& mesh, v4 const& camera_point, v4 const& ray);
	void SelectFeature(TetraMeshEx const& mesh, tetramesh::Face const& face, v4 const& bary);
	void Merge(Selection const& selection);
	void UpdateLdr(TetraMeshEx const& mesh);
	bool OneFace() const;
	bool OneEdge() const;
	bool OneVert() const;
	tetramesh::Face   Face() const;
	tetramesh::VIndex Vert0() const;
	tetramesh::VIndex Vert1() const;
	tetramesh::VIndex Vert() const;

	tetramesh::TVIndices	m_verts;
	tetramesh::TTIndices	m_tetra;
	tetramesh::TFaces		m_faces;
	ldr::ObjectHandle		m_ldr;
	char const*				m_vert_colour;
	char const*				m_face_colour;
};

