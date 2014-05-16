//*****************************************************************************************
// Sol
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************

#include "sol/main/stdafx.h"
#include "sol/main/main.h"
#include "sol/main/main_gui.h"

using namespace sol;

sol::Main::Main(MainGUI& gui)
	:base(pr::app::DefaultSetup(), gui)
	,m_skybox(m_rdr, AssMgr::DataPath(L"skybox/space1/space??.png"), pr::app::Skybox::SixSidedCube, 100000.0f)
	//,m_skybox(m_rdr, AssMgr::DataPath(L"skybox/galaxy1/galaxy??.dds"), pr::app::Skybox::SixSidedCube)
	//,m_skybox(m_rdr, ResMgr::DataPath(L"skybox/sky1/skybox-clouds-few-noon.dds"), pr::app::Skybox::FiveSidedCube)
	//,m_skybox(m_rdr, AssMgr::DataPath(L"skybox/sky1/sky_spheremap.dds"), pr::app::Skybox::Geosphere)
	,m_gimble(m_rdr)
	//,m_earth(pr::v4::make(0.0f, 0.0f, 0.0f, 1.0f), 1.0f, 1.0f, m_rdr, L"textures/moon2500x1250.dds")
	//,m_earth2(pr::v4::make(-5.0f, 0.0f, -4.0f, 1.0f), 1.0f, 1.0f, m_rdr, L"textures/tycho-equatorial.dds")
	//,m_earth3(pr::v4::make(1.0f, 6.0f, 2.0f, 1.0f), 1.0f, 1.0f, m_rdr, L"textures/space_spherical_map_by_cesium135-d5qay53.dds")
	//,m_test1(m_rdr)
	//,m_test2(m_rdr)
	,m_wireframe(false)
{
	m_cam.ClipPlanes(0.001f, 1e6f, false);
	m_scene.m_global_light.m_diffuse = pr::Colour32White;
	//m_test1.m_inst.m_i2w.pos.y = 0.01f;
	//m_test2.m_inst.m_i2w.pos.y = 0.0f;
}
