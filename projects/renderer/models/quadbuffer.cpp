//**********************************
// Quad Buffer
//  Copyright © Rylogic Ltd 2007
//**********************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/Renderer/renderer.h"
#include "pr/renderer/Models/QuadBuffer.h"
#include "pr/renderer/Models/ModelManager.h"
	
using namespace pr;
using namespace pr::rdr;
	
QuadBuffer::QuadBuffer(Renderer& rdr, std::size_t num_quads)
:m_rdr(&rdr)
,m_num_quads(num_quads)
,m_state(EState_Idle)
,m_vlock()
,m_vb()
,m_model(0)
{
	model::Settings settings;
	settings.m_vertex_type = vf::EVertType::PosNormDiffTex;
	settings.m_Vcount      = 4 * m_num_quads;
	settings.m_Icount      = 6 * m_num_quads;
	m_model = rdr.m_mdl_mgr.CreateModel(settings);
	
	{// Fill in the indices
		model::ILock ilock;
		rdr::Index* ib = m_model->LockIBuffer(ilock), i = 0;
		for (std::size_t q = 0; q != num_quads; ++q, i += 4)
		{
			*ib++ = i + 0;
			*ib++ = i + 2;
			*ib++ = i + 3;
			*ib++ = i + 3;
			*ib++ = i + 1;
			*ib++ = i + 0;
		}
	}
}
	
// Called before and after the add calls, saves excessive locking/unlocking of the model buffer
void QuadBuffer::Begin()
{
	PR_ASSERT(PR_DBG_RDR, m_state == EState_Idle, "Begin calls cannot be nested");
	m_vb = m_model->LockVBuffer(m_vlock);
	m_state = EState_Adding;
}
void QuadBuffer::End()
{
	PR_ASSERT(PR_DBG_RDR, m_state == EState_Adding, "Begin has not been called");
	m_vlock.Unlock();
	m_state = EState_Idle;
}
	
// This is a quad whose verts are in world space but always faces the camera
// This methods adds 4 verts at the same position, the shader moves them to the correct positions.
// 'centre' is the centre of the billboard in world space
// 'corner' is an array of 4 vectors pointing to the corners of the billboard in camera space
// 'colour' is an array of 4 vertex colours
// 'tex' is an array of 4 texture coords
// Note: vertex order is: TL, TR, BL, BR... i.e. "Z"
void QuadBuffer::AddBillboard(std::size_t index, v4 const& centre, v4* corner, Colour32* colour, v2* tex)
{
	PR_ASSERT(PR_DBG_RDR, m_state == EState_Adding, "Begin has not been called");
	vf::iterator vb = m_vb + 4 * index;
	(vb++)->set(centre, corner[0], colour[0], tex[0]);
	(vb++)->set(centre, corner[1], colour[1], tex[1]);
	(vb++)->set(centre, corner[2], colour[2], tex[2]);
	(vb++)->set(centre, corner[3], colour[3], tex[3]);
}
void QuadBuffer::AddBillboard(std::size_t index, v4 const& centre, float width, float height)
{
	width  *= 0.5f;
	height *= 0.5f;
	v4       corner[4]  = {{-width, height, 0.0f, 0.0f}, {width, height, 0.0f, 0.0f}, {-width, -height, 0.0f, 0.0f}, {width, -height, 0.0f, 0.0f}};
	v2       tex[4]     = {{0.00f, 0.00f}, {0.99f, 0.00f}, {0.00f, 0.99f}, {0.99f, 0.99f}};
	Colour32 colours[4] = {Colour32White, Colour32White, Colour32White, Colour32White};
	AddBillboard(index, centre, corner, colours, tex);
}
void QuadBuffer::AddBillboard(std::size_t index, v4 const& centre, float width, float height, Colour32 colour)
{
	width  *= 0.5f;
	height *= 0.5f;
	v4       corner[4]  = {{-width, height, 0.0f, 0.0f}, {width, height, 0.0f, 0.0f}, {-width, -height, 0.0f, 0.0f}, {width, -height, 0.0f, 0.0f}};
	v2       tex[4]     = {{0.00f, 0.00f}, {0.99f, 0.00f}, {0.00f, 0.99f}, {0.99f, 0.99f}};
	Colour32 colours[4] = {colour, colour, colour, colour};
	AddBillboard(index, centre, corner, colours, tex);
}
	
// This is a quad whose verts are in screen space
// x,y = [-1, 1], z = [0,1], orthographic projection
void QuadBuffer::AddSprite()
{}

