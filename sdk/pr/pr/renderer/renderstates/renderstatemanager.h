//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
//	Usage:
//		The default renderstates are applied at initialisation. 
//		Any changes to the state of the render are stored in stacks
//		using PushXXX and PopXXX methods.
//		The state of the render is not guaranteed to be the correct state
//		after a Pop, only after a Flush. This allows unnecessary
//		state changes to be avoided.

#pragma once
#ifndef PR_RDR_RENDER_STATE_MANAGER_H
#define PR_RDR_RENDER_STATE_MANAGER_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/vertexformats/vertexformat.h"
#include "pr/renderer/renderstates/renderstate.h"

namespace pr
{
	namespace rdr
	{
		namespace ERSMFlush
		{
			enum Type
			{
				Diff  = 0,
				Force = 1
			};
		}

		// A class to manager render state changes
		class RenderStateManager
			:public pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,public pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			enum { MaxRenderStates = D3DRS_BLENDOPALPHA + 1 };
			
			D3DPtr<IDirect3DDevice9>   m_d3d_device;
			VertexFormatManager const& m_vf_manager;
			
			// This is the state that the renderer currently is in.
			// The true current state is 'current_state' + 'pending_state'
			rs::DeviceState m_current_device_state;
			rs::State       m_current_render_state[MaxRenderStates];
			
			// This is the state we want the renderer in on the next flush
			rs::DeviceState m_pending_device_state;	
			rs::Block       m_pending_render_state_changes;
			
			// Defaults
			rs::DeviceState m_default_device_state;
			rs::State       m_default_render_state[MaxRenderStates];
			
			RenderStateManager(const RenderStateManager&);
			RenderStateManager& operator = (const RenderStateManager&);
			
			void OnEvent(pr::rdr::Evt_DeviceLost const&);
			void OnEvent(pr::rdr::Evt_DeviceRestored const&);
			
			uint  AddRenderStateBlock    (rs::Block& rsb);
			void  RestoreRenderStateBlock(rs::Block& rsb);
			void  AddPendingRenderState(D3DRENDERSTATETYPE type, uint state);
			void  ApplyPendingRenderStates();
			
		public:
			RenderStateManager(D3DPtr<IDirect3DDevice9> d3d_device, const VertexFormatManager& vf_manager, const IRect& client_area);
			void UseDefaultRenderStates();
			uint GetCurrentRenderState(D3DRENDERSTATETYPE type) const;
			rs::DeviceState const& GetCurrentDeviceState() const { return m_current_device_state; }
			
			void PushViewport        (rs::stack_frame::Viewport& viewport_sf, D3DVIEWPORT9 const& viewport);
			void PopViewport         (rs::stack_frame::Viewport& viewport_sf);
			void PushRenderStateBlock(rs::stack_frame::RSB& rsb_sf, rs::Block const& rsb);
			void PopRenderStateBlock (rs::stack_frame::RSB&	rsb_sf);
			void PushDrawListElement (rs::stack_frame::DLE& dle_sf, DrawListElement const& element);
			void PopDrawListElement  (rs::stack_frame::DLE& dle_sf);
			void PushDLEShadows      (rs::stack_frame::DLEShadows& dle_sf, DrawListElement const& element);
			void PopDLEShadows       (rs::stack_frame::DLEShadows& dle_sf);
			void Flush(ERSMFlush::Type flush_type);
		};
	}
}

#endif
