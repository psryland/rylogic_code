//****************************************************************
//
//	Lighting Manager
//
//****************************************************************
//
//	This class manages the state of lights in the scene
//

#ifndef LIGHTING_MANAGER_H
#define LIGHTING_MANAGER_H

#include "PR/Renderer/Light.h"

namespace pr
{
	namespace rdr
	{
		class LightingManager
		{
		public:
			enum { MAX_LIGHTS = 8 };

			LightingManager()						{}
			      Light& GetLight(uint which)		{ PR_ASSERT(PR_DBG_RDR, which < MAX_LIGHTS); return m_light[which]; }
			const Light& GetLight(uint which) const	{ PR_ASSERT(PR_DBG_RDR, which < MAX_LIGHTS); return m_light[which]; }

		private:
			Light m_light[MAX_LIGHTS];
		};
	}//namespace rdr
}//namespace pr

#endif//LIGHTING_MANAGER_H
