//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#include "lost_at_sea/src/forward.h"
#include "lost_at_sea/src/world/terrain.h"
//#include "pr/geometry/hex_patch.h"
//#include "pr/linedrawer/ldr_helper.h"
//
//using namespace las;
//
////namespace pr { namespace rdr { namespace model
////{
////	pr::rdr::ModelPtr HexPatch(pr::Renderer& rdr)
////	{
////		pr::rdr::ModelPtr model = rdr.m_mdl_mgr.CreateModel(pr::rdr::model::Settings(1,1));
////		pr::rdr::model::MLock lock(model);
////		pr::rdr::vf::iterator v = lock.m_vlock.m_ptr;
////
////		return model;
////	}
////}}}
//
//las::Terrain::Terrain(pr::Renderer& rdr)
//:m_inst()
//{
//	(void)rdr;
//	pr::Geometry geom;
//	pr::geometry::GenerateHexPatch(geom);
//	
//	std::string s;
//	pr::ldr::Mesh("hex_patch", 0xFFFFFFFF, geom.m_frame[0].m_mesh, s);
//	pr::ldr::Write(s, "hex_patch.ldr");
//
//	//m_inst.m_model = pr::rdr::model::HexPatch();
//}
