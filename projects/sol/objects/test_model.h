//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_OBJECTS_TEST_MODEL_H
#define PR_SOL_OBJECTS_TEST_MODEL_H

#include "sol/main/forward.h"
#include "sol/main/asset_manager.h"

namespace sol
{
	struct TestModel
		:pr::events::IRecv<pr::rdr::Evt_UpdateScene>
	{
		// A renderer instance type for the body
		#define PR_RDR_INST(x)\
			x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
			x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr    )
		PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST);
		#undef PR_RDR_INST

		Instance m_inst;     // The renderer instance

		TestModel(pr::Renderer& rdr)
		{
			pr::Array<pr::v4> points;
			for (int i = 0; i != 20; ++i)
				points.push_back(pr::Random4(pr::v4Origin, 5.0f));

			pr::rdr::NuggetProps mat;
			mat.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(pr::rdr::AutoId, pr::rdr::SamplerDesc::LinearClamp(), AssMgr::DataPath(L"textures\\smiling gekko.dds").c_str());
			m_inst.m_model    = pr::rdr::ModelGenerator<>::Quad(rdr, 1.0f, 1.0f, pr::iv2Zero, pr::Colour32White, &mat);
			m_inst.m_i2w      = pr::Translation4x4(0,0,0);
		}

		// Add to a viewport
		void OnEvent(pr::rdr::Evt_UpdateScene const& e) override
		{
			float s = e.m_scene.m_view.m_centre_dist;
			m_inst.m_i2w = pr::Scale4x4(s,s,s, m_inst.m_i2w.pos);
			e.m_scene.AddInstance(m_inst);
		}
	};
}

#endif
