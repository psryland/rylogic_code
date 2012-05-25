//************************************
// Lost at Sea
//  Copyright © Rylogic Ltd 2011
//************************************
#ifndef LAS_TERRAIN_H
#define LAS_TERRAIN_H
#pragma once

namespace las
{
	struct Terrain
	{
		PR_RDR_DECLARE_INSTANCE_TYPE2
		(
			Instance
			,pr::rdr::ModelPtr ,m_model ,pr::rdr::instance::ECpt_ModelPtr
			,pr::m4x4          ,m_i2w   ,pr::rdr::instance::ECpt_I2WTransform
		);
		Instance m_inst; // The patch under the camera
		
		Terrain(pr::Renderer& rdr);
		// This will be queried for heights anywhere
	};
}

#endif
