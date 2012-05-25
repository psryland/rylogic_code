//***************************************
// TetraMesh Editor
//***************************************

#include "Stdafx.h"
#include "TetraMeshEx.h"
#include "TetraMeshEditor.h"
#include "pr/common/LineDrawerHelper.h"

using namespace pr::tetramesh;

TetraMeshEx::TetraMeshEx()
:m_ldr(0)
,m_colour("8000FF00")
{}

bool TetraMeshEx::Empty() const
{
	return m_num_verts && m_num_tetra;
}

void TetraMeshEx::Clear()
{
	m_verts_buffer.clear();
	m_tetra_buffer.clear();
	m_num_verts = 0;
	m_num_tetra = 0;
	if( m_ldr ) { ldrUnRegisterObject(m_ldr); m_ldr = 0; }
}

void TetraMeshEx::PushBack(v4 const& vert)
{
	m_verts_buffer.push_back(vert);
	m_verts = &m_verts_buffer[0];
	m_num_verts = m_verts_buffer.size();
}

void TetraMeshEx::PushBack(Tetra const& tetra)
{
	m_tetra_buffer.push_back(tetra);
	m_tetra = &m_tetra_buffer[0];
	m_num_tetra = m_tetra_buffer.size();
}

void TetraMeshEx::Resize(TSize num_verts, TSize num_tetra)
{
	m_verts_buffer.resize(num_verts);
	m_tetra_buffer.resize(num_tetra);
	m_verts = &m_verts_buffer[0];
	m_tetra = &m_tetra_buffer[0];
	m_num_verts = m_verts_buffer.size();
	m_num_tetra = m_tetra_buffer.size();
}

void TetraMeshEx::New(bool single, int dimX, int dimY, int dimZ, float sizeX, float sizeY, float sizeZ)
{
	Clear();
	if( single )
	{
		PushBack(Editor().Snap(v4::make( 0.0f, 0.0f, 0.0f, 1.0f)));
		PushBack(Editor().Snap(v4::make( 0.0f, 0.0f, 1.0f, 1.0f)));
		PushBack(Editor().Snap(v4::make( 0.0f, 1.0f, 0.0f, 1.0f)));
		PushBack(Editor().Snap(v4::make( 1.0f, 0.0f, 0.0f, 1.0f)));
		
		Tetra tetra;
		tetra.m_cnrs[0] = 0;
		tetra.m_cnrs[1] = 1;
		tetra.m_cnrs[2] = 2;
		tetra.m_cnrs[3] = 3;
		tetra.m_nbrs[0] = ExtnFace;
		tetra.m_nbrs[1] = ExtnFace;
		tetra.m_nbrs[2] = ExtnFace;
		tetra.m_nbrs[3] = ExtnFace;
		PushBack(tetra);
	}
	else
	{
		TSize num_verts, num_tetra;
		tetramesh::SizeOfTetramesh(dimX, dimY, dimZ, num_verts, num_tetra);
		Resize(num_verts, num_tetra);
		tetramesh::Generate(*this, dimX, dimY, dimZ, sizeX, sizeY, sizeZ);
	}
	UpdateLdr();
}

void TetraMeshEx::UpdateLdr()
{
	std::string str;
	
	if( m_ldr ) { ldrUnRegisterObject(m_ldr); m_ldr = 0; }
	ldr::GroupStart("tetramesh", str);
	for( Tetra const *t = m_tetra, *t_end = m_tetra + m_num_tetra; t != t_end; ++t )
	{
		ldr::GroupStart("tetra", str);
		v4 const& a = m_verts[t->m_cnrs[0]];
		v4 const& b = m_verts[t->m_cnrs[1]];
		v4 const& c = m_verts[t->m_cnrs[2]];
		v4 const& d = m_verts[t->m_cnrs[3]];
		ldr::Triangle("face", m_colour, a, b, c, str);
		ldr::Triangle("face", m_colour, a, c, d, str);
		ldr::Triangle("face", m_colour, a, d, b, str);
		ldr::Triangle("face", m_colour, d, c, b, str);
		ldr::GroupEnd(str);
	}
	ldr::GroupEnd(str);
	m_ldr = ldrRegisterObject(str.c_str(), str.size());
	ldrRender();
}
