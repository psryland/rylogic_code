//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2015
//************************************
#include "lost_at_sea/src/stdafx.h"
#include "lost_at_sea/src/world/skybox.h"

namespace las
{
	Skybox::Skybox(pr::Renderer& rdr, string const& texpath)
:m_inst()
,m_tex()
{
	m_inst.m_model = rdr.m_mdl_mgr.CreateModel(pr::rdr::model::Settings(30,12));
	pr::rdr::model::MLock lock(m_inst.m_model);
	pr::rdr::vf::iterator v = lock.m_vlock.m_ptr;
	v->set(pr::v4::make(-0.5f,  0.5f,  0.5f, 1.0f), pr::v2::make( 0.25f, 0.25f)); ++v; //0
	v->set(pr::v4::make(-0.5f,  0.5f, -0.5f, 1.0f), pr::v2::make( 0.25f, 0.75f)); ++v; //1
	v->set(pr::v4::make( 0.5f,  0.5f, -0.5f, 1.0f), pr::v2::make( 0.75f, 0.75f)); ++v; //2
	v->set(pr::v4::make( 0.5f,  0.5f,  0.5f, 1.0f), pr::v2::make( 0.75f, 0.25f)); ++v; //3
	v->set(pr::v4::make(-0.5f, -0.5f,  0.5f, 1.0f), pr::v2::make(-0.25f, 0.25f)); ++v; //4
	v->set(pr::v4::make(-0.5f, -0.5f, -0.5f, 1.0f), pr::v2::make(-0.25f, 0.75f)); ++v; //5
	v->set(pr::v4::make(-0.5f, -0.5f, -0.5f, 1.0f), pr::v2::make( 0.25f, 1.25f)); ++v; //6
	v->set(pr::v4::make( 0.5f, -0.5f, -0.5f, 1.0f), pr::v2::make( 0.75f, 1.25f)); ++v; //7
	v->set(pr::v4::make( 0.5f, -0.5f, -0.5f, 1.0f), pr::v2::make( 1.25f, 0.75f)); ++v; //8
	v->set(pr::v4::make( 0.5f, -0.5f,  0.5f, 1.0f), pr::v2::make( 1.25f, 0.25f)); ++v; //9
	v->set(pr::v4::make( 0.5f, -0.5f,  0.5f, 1.0f), pr::v2::make( 0.75f,-0.25f)); ++v; //10
	v->set(pr::v4::make(-0.5f, -0.5f,  0.5f, 1.0f), pr::v2::make( 0.25f,-0.25f)); ++v; //11
	
	pr::rdr::Index* i = lock.m_ilock.m_ptr;
	*i++ = 0; *i++ = 1; *i++ = 2;
	*i++ = 0; *i++ = 2; *i++ = 3;
	*i++ = 0; *i++ = 4; *i++ = 5;
	*i++ = 0; *i++ = 5; *i++ = 1;
	*i++ = 1; *i++ = 6; *i++ = 7;
	*i++ = 1; *i++ = 7; *i++ = 2;
	*i++ = 2; *i++ = 8; *i++ = 9;
	*i++ = 2; *i++ = 9; *i++ = 3;
	*i++ = 3; *i++ = 10; *i++ = 11;
	*i++ = 3; *i++ = 11; *i++ = 0;
	
	// Load the skybox texture
	m_tex = rdr.m_mat_mgr.CreateTexture(pr::rdr::AutoId, texpath.c_str());
	m_tex->m_addr_mode.m_addrU = D3DTADDRESS_CLAMP;
	m_tex->m_addr_mode.m_addrV = D3DTADDRESS_CLAMP;
	
	// Add a render nugget
	pr::rdr::Material mat = rdr.m_mat_mgr.GetMaterial(pr::geom::EVT);
	mat.m_diffuse_texture = m_tex;
	mat.m_rsb.SetRenderState(D3DRS_ZENABLE, D3DZB_FALSE);
	mat.m_rsb.SetRenderState(D3DRS_ZWRITEENABLE, FALSE);
	mat.m_rsb.SetRenderState(D3DRS_LIGHTING, FALSE);
	mat.m_rsb.SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
	m_inst.m_model->SetMaterial(mat, pr::rdr::model::EPrimitive::TriangleList, false);
}

// Render the skybox
void las::Skybox::OnEvent(las::Evt_AddToViewport const& e)
{
	m_inst.m_i2w = pr::Scale4x4(100.0f, e.m_cam->CameraToWorld().pos);
	e.m_vp->AddInstance(m_inst);
}


