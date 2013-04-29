//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#include "sol/main/stdafx.h"
#include "sol/objects/astronomical_body.h"
#include "sol/main/asset_manager.h"

using namespace pr;
using namespace sol;

sol::AstronomicalBody::AstronomicalBody(pr::v4 const& position, float radius, float mass, pr::Renderer& rdr, wchar_t const* texture)
	:m_position(position)
	,m_radius(radius)
	,m_mass(mass)
{
	// Load the texture for the body
	pr::rdr::DrawMethod method;
	method.m_shader      = rdr.m_shdr_mgr.FindShaderFor(pr::rdr::VertPCNT::GeomMask);
	method.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(pr::rdr::AutoId, pr::rdr::SamplerDesc::WrapSampler(), AssMgr::DataPath(texture).c_str());

	// Create the model
	m_inst.m_model = pr::rdr::ModelGenerator<>::Geosphere(rdr, m_radius, 4, pr::Colour32White, &method);
	m_inst.m_i2w = pr::Translation(m_position);
}

void sol::AstronomicalBody::OnEvent(pr::rdr::Evt_SceneRender const& e)
{
	//float s = e.m_scene->m_view.m_centre_dist;
	//m_inst.m_i2w = Scale4x4(s,s,s,v4Origin);
	e.m_scene->AddInstance(m_inst);
}