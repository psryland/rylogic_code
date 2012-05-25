//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

//	This class manages the state of lights in the scene

#pragma once
#ifndef PR_RDR_LIGHTING_MANAGER_H
#define PR_RDR_LIGHTING_MANAGER_H

#include "pr/renderer/lighting/light.h"
#include "pr/renderer/materials/textures/texture.h"

namespace pr
{
	namespace rdr
	{
		class LightingManager
			:public pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,public pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			D3DPtr<IDirect3DDevice9> m_d3d_device;
			
			void OnEvent(pr::rdr::Evt_DeviceLost const&);
			void OnEvent(pr::rdr::Evt_DeviceRestored const&);
			
		public:
			// Data for each individual light
			Light m_light[MaxLights];
			
			// Shadow maps
			D3DPtr<IDirect3DTexture9> m_smap[MaxShadowCasters];
			D3DPtr<IDirect3DSurface9> m_smap_depth;
			
			LightingManager(D3DPtr<IDirect3DDevice9> d3d_device);
			
			// Create the shadow maps for caster index 'idx'
			void CreateSmap(int idx);
			void ReleaseSmaps(int leave_remaining);
		};
	}
}

#endif
