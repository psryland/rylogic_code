//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_RENDER_STATE_STACK_FRAME_H
#define PR_RDR_RENDER_STATE_STACK_FRAME_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/vertexformats/vertexformat.h"
#include "pr/renderer/renderstates/renderstatemanager.h"

namespace pr
{
	namespace rdr
	{
		namespace rs
		{
			// These objects contain the information necessary to restore the state of the renderer.
			namespace stack_frame
			{
				struct Viewport
				{
					RenderStateManager* m_rsm;                    // The manager
					D3DVIEWPORT9        m_viewport;               // The viewport's "viewport"
					Viewport(RenderStateManager& rsm, D3DVIEWPORT9 const& viewport) :m_rsm(&rsm) { m_rsm->PushViewport(*this, viewport); }
					~Viewport()                                                                  { m_rsm->PopViewport(*this); }
				};
				struct RSB
				{
					RenderStateManager* m_rsm;                    // The manager
					rs::Block           m_render_state_changes;   // The render state changes added
					RSB(RenderStateManager& rsm, rs::Block const& rsb) :m_rsm(&rsm) { m_rsm->PushRenderStateBlock(*this, rsb); }
					~RSB()                                                          { if (m_rsm) m_rsm->PopRenderStateBlock(*this); }
				};
				struct DLE
				{
					RenderStateManager* m_rsm;                    // The manager
					vf::Type            m_vertex_type;            // The vertex type of the draw list element
					D3DPtr<IDirect3DVertexBuffer9> m_Vstream;     // The Vbuffer of the draw list element
					D3DPtr<IDirect3DIndexBuffer9>  m_Istream;     // The Ibuffer of the draw list element
					rs::Block m_texture_render_state_changes;     // The render state changes added by the texture
					rs::Block m_effect_render_state_changes;      // The render state changes added by the effect
					rs::Block m_material_render_state_changes;    // The render state changes added by the material
					rs::Block m_instance_render_state_changes;    // The render state changes added by the instance

					DLE(RenderStateManager& rsm, DrawListElement const& dle) :m_rsm(&rsm)  { m_rsm->PushDrawListElement(*this, dle); }
					~DLE()                                                                 { m_rsm->PopDrawListElement(*this); }
				};
				struct DLEShadows
				{
					RenderStateManager* m_rsm;                    // The manager
					vf::Type            m_vertex_type;            // The vertex type of the draw list element
					D3DPtr<IDirect3DVertexBuffer9> m_Vstream;     // The Vbuffer of the draw list element
					D3DPtr<IDirect3DIndexBuffer9>  m_Istream;     // The Ibuffer of the draw list element
					DLEShadows(RenderStateManager& rsm, DrawListElement const& elem) :m_rsm(&rsm) { m_rsm->PushDLEShadows(*this, elem); }
					~DLEShadows()                                                                 { m_rsm->PopDLEShadows(*this); }
				};
			}
		}
	}
}

#endif
