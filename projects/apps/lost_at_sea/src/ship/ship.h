//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#pragma once
#include "src/forward.h"

namespace las
{
	struct Ship
	{
		struct Instance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w   , EInstComp::I2WTransform)\
			x(ModelPtr , m_model , EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		// The ship instance
		Instance m_inst;

		Ship(ResourceFactory& factory);
		void AddToScene(Scene& scene);
	};
}
