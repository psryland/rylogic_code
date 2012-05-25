//***************************************
// TetraMesh Editor
//***************************************
#pragma once

#include "pr/geometry/TetraMesh.h"
#include "LineDrawer/Plugin/PlugInInterface.h"

struct TetraMeshEx : tetramesh::Mesh
{
	TetraMeshEx();
	bool Empty() const;
	void Clear();
	void PushBack(v4 const& vert);
	void PushBack(tetramesh::Tetra const& tetra);
	void Resize(tetramesh::TSize num_verts, tetramesh::TSize num_tetra);
	void New(bool single, int dimX, int dimY, int dimZ, float sizeX, float sizeY, float sizeZ);
	void UpdateLdr();

	std::vector<v4>					m_verts_buffer;
	std::vector<tetramesh::Tetra>	m_tetra_buffer;
	ldr::ObjectHandle				m_ldr;
	char const*						m_colour;
};