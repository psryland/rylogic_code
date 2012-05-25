//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/RenderStates/RenderStateManager.h"
#include "pr/renderer/RenderStates/StackFrames.h"
#include "pr/renderer/Renderer/renderer.h"
#include "pr/renderer/Viewport/DrawListElement.h"
#include "pr/renderer/Viewport/Viewport.h"
#include "pr/renderer/Models/RenderNugget.h"
#include "pr/renderer/Models/ModelBuffer.h"
#include "pr/renderer/Instances/Instance.h"

const D3DRENDERSTATETYPE INVALID_RENDER_STATE = D3DRS_FORCE_DWORD;

using namespace pr;
using namespace pr::rdr;

inline DWORD FtoDW(float f)                                     { return reinterpret_cast<DWORD&>(f); }
template <typename T> inline bool Equal(T const& a, T const& b) { return memcmp(&a, &b, sizeof(T)) == 0; }

// Constructor
RenderStateManager::RenderStateManager(D3DPtr<IDirect3DDevice9> d3d_device, const VertexFormatManager& vf_manager, const IRect& client_area)
:pr::events::IRecv<pr::rdr::Evt_DeviceLost>(EDeviceResetPriority::RenderStateManager)
,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>(EDeviceResetPriority::RenderStateManager)
,m_d3d_device(d3d_device)
,m_vf_manager(vf_manager)
{
	// Default viewport
	m_default_device_state.m_viewport.X      = client_area.m_min.x;
	m_default_device_state.m_viewport.Y      = client_area.m_min.y;
	m_default_device_state.m_viewport.Width  = client_area.SizeX();
	m_default_device_state.m_viewport.Height = client_area.SizeY();
	m_default_device_state.m_viewport.MinZ   = 0.0f;
	m_default_device_state.m_viewport.MaxZ   = 1.0f;

	// Default vertex type
	m_default_device_state.m_vertex_type = vf::EVertType::PosNormDiffTex;

	// Default streams
	m_default_device_state.m_Vstream = 0;
	m_default_device_state.m_Istream = 0;

	// Default render states
	for (int i = 0; i < MaxRenderStates; ++i)
	{
		// Set all to invalid
		m_default_render_state[i].m_type  = static_cast<D3DRENDERSTATETYPE>(i);
		m_default_render_state[i].m_state = INVALID_RENDER_STATE;
	}
	// Now set the default states
	#define RENDERSTATE(render_state, default_state) m_default_render_state[render_state].m_state = default_state;
	#include "renderstatesinc.h"
	#undef RENDERSTATE

	// Use the default render states
	UseDefaultRenderStates();
}
	
// Release our pointer to the d3d device
void RenderStateManager::OnEvent(pr::rdr::Evt_DeviceLost const&)
{
	m_pending_device_state = m_default_device_state;
	m_d3d_device = 0;
}
	
// Assign the new device and default viewport area
void RenderStateManager::OnEvent(pr::rdr::Evt_DeviceRestored const& e)
{
	m_d3d_device = e.m_d3d_device;

	m_default_device_state.m_viewport.X      = e.m_client_area.m_min.x;
	m_default_device_state.m_viewport.Y      = e.m_client_area.m_min.y;
	m_default_device_state.m_viewport.Width  = e.m_client_area.SizeX();
	m_default_device_state.m_viewport.Height = e.m_client_area.SizeY();
	m_default_device_state.m_viewport.MinZ   = 0.0f;
	m_default_device_state.m_viewport.MaxZ   = 1.0f;
	
	m_pending_device_state.m_viewport = m_default_device_state.m_viewport;
	m_pending_device_state.m_Vstream  = m_default_device_state.m_Vstream;
	m_pending_device_state.m_Istream  = m_default_device_state.m_Istream;

	Flush(ERSMFlush::Force);
}
	
// Set the renderer state to defaults
void RenderStateManager::UseDefaultRenderStates()
{
	m_current_device_state = m_default_device_state;
	m_pending_device_state = m_default_device_state;

	memcpy(m_current_render_state, m_default_render_state, sizeof(m_current_render_state));
	m_pending_render_state_changes.Reset();

	Flush(ERSMFlush::Force);
}
	
// Return what should be the current value of a render state
uint RenderStateManager::GetCurrentRenderState(D3DRENDERSTATETYPE type) const
{
	rs::Block::TStateBlock const& pending = m_pending_render_state_changes.m_state;
	rs::Block::TStateBlock::const_iterator iter = std::find(pending.begin(), pending.end(), type);
	if (iter != pending.end()) return iter->m_state;
	else                       return m_current_render_state[type].m_state;
}

// Push the state of a viewport
void RenderStateManager::PushViewport(rs::stack_frame::Viewport& viewport_sf, D3DVIEWPORT9 const& viewport)
{
	// Add the viewport's viewport
	viewport_sf.m_viewport = m_current_device_state.m_viewport;
	m_pending_device_state.m_viewport = viewport;

	//// Add the viewport render states
	//viewport_sf.m_render_state_changes = viewport.RenderStateBlock();
	//AddRenderStateBlock(viewport_sf.m_render_state_changes);

	//Flush(EFlush_Diff);
}
void RenderStateManager::PopViewport(rs::stack_frame::Viewport& viewport_sf)
{
	//// Restore render states
	//RestoreRenderStateBlock(viewport_sf.m_render_state_changes);

	m_pending_device_state.m_viewport = viewport_sf.m_viewport;
}
	
// Add a block of render states to the render state stack
void RenderStateManager::PushRenderStateBlock(rs::stack_frame::RSB& rsb_sf, rs::Block const& rsb)
{
	// Add the render states
	rsb_sf.m_render_state_changes = rsb;
	AddRenderStateBlock(rsb_sf.m_render_state_changes);

	//Flush(EFlush_Diff);
}
void RenderStateManager::PopRenderStateBlock(rs::stack_frame::RSB& rsb_sf)
{
	RestoreRenderStateBlock(rsb_sf.m_render_state_changes);
}
	
// Push the state of a draw list element 
void RenderStateManager::PushDrawListElement(rs::stack_frame::DLE& dle_sf, DrawListElement const& element)
{
	Material const&       material     = element.m_nugget->m_material;
	ModelBufferPtr const& model_buffer = element.m_nugget->m_model_buffer;

	// Add the vertex type
	dle_sf.m_vertex_type = m_current_device_state.m_vertex_type;
	m_pending_device_state.m_vertex_type = model_buffer->m_vertex_type;

	// Add the Vstream
	dle_sf.m_Vstream = m_current_device_state.m_Vstream;
	m_pending_device_state.m_Vstream = model_buffer->m_Vbuffer;

	// Add the Istream
	dle_sf.m_Istream = m_current_device_state.m_Istream;
	m_pending_device_state.m_Istream = model_buffer->m_Ibuffer;
	
	// Add the render states
	if (rs::Block const* instance_render_states = instance::FindCpt<rs::Block>(*element.m_instance, instance::ECpt_RenderState))
	{
		dle_sf.m_instance_render_state_changes = *instance_render_states;
		AddRenderStateBlock(dle_sf.m_instance_render_state_changes);
	}
	if (material.m_diffuse_texture)
	{
		dle_sf.m_texture_render_state_changes = material.m_diffuse_texture->m_rsb;
		AddRenderStateBlock(dle_sf.m_texture_render_state_changes);
	}
	if (material.m_effect)
	{
		dle_sf.m_effect_render_state_changes = material.m_effect->m_rsb;
		AddRenderStateBlock(dle_sf.m_effect_render_state_changes);
	}
	
	dle_sf.m_material_render_state_changes = material.m_rsb;
	AddRenderStateBlock(dle_sf.m_material_render_state_changes);

	//Flush(EFlush_Diff);
}
void RenderStateManager::PopDrawListElement(rs::stack_frame::DLE& dle_sf)
{
	RestoreRenderStateBlock(dle_sf.m_material_render_state_changes);
	RestoreRenderStateBlock(dle_sf.m_effect_render_state_changes);
	RestoreRenderStateBlock(dle_sf.m_texture_render_state_changes);
	RestoreRenderStateBlock(dle_sf.m_instance_render_state_changes);

	m_pending_device_state.m_Istream = dle_sf.m_Istream;
	m_pending_device_state.m_Vstream = dle_sf.m_Vstream;
	m_pending_device_state.m_vertex_type = dle_sf.m_vertex_type;
}
	
// Push dle info needed for shadow map rendering only
void RenderStateManager::PushDLEShadows(rs::stack_frame::DLEShadows& dle_sf, DrawListElement const& element)
{
	ModelBufferPtr const& model_buffer = element.m_nugget->m_model_buffer;

	// Add the vertex type
	dle_sf.m_vertex_type = m_current_device_state.m_vertex_type;
	m_pending_device_state.m_vertex_type = model_buffer->m_vertex_type;

	// Add the Vstream
	dle_sf.m_Vstream = m_current_device_state.m_Vstream;
	m_pending_device_state.m_Vstream = model_buffer->m_Vbuffer;

	// Add the Istream
	dle_sf.m_Istream = m_current_device_state.m_Istream;
	m_pending_device_state.m_Istream = model_buffer->m_Ibuffer;
	
	//Flush(EFlush_Diff);
}
void RenderStateManager::PopDLEShadows(rs::stack_frame::DLEShadows& dle_sf)
{
	m_pending_device_state.m_Istream     = dle_sf.m_Istream;
	m_pending_device_state.m_Vstream     = dle_sf.m_Vstream;
	m_pending_device_state.m_vertex_type = dle_sf.m_vertex_type;
}
	
// Add render states to the pending render states
uint RenderStateManager::AddRenderStateBlock(rs::Block& rsb)
{
	for (rs::Block::TStateBlock::iterator i = rsb.m_state.begin(), i_end = rsb.m_state.end(); i != i_end; ++i)
	{
		i->m_prev_state = GetCurrentRenderState(i->m_type);
		AddPendingRenderState(i->m_type, i->m_state);
	}
	return (uint)rsb.m_state.size();
}
	
// Restore render states
void RenderStateManager::RestoreRenderStateBlock(rs::Block& rsb)
{
	for (rs::Block::TStateBlock::iterator i = rsb.m_state.begin(), i_end = rsb.m_state.end(); i != i_end; ++i)
		AddPendingRenderState(i->m_type, i->m_prev_state);
}
	
// Flushes the current logical renderer state down to the d3d device
void RenderStateManager::Flush(ERSMFlush::Type flush_type)
{
	switch (flush_type)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "Unknown flush type"); break;

	// Apply the current state
	case ERSMFlush::Force:
		Verify(m_d3d_device->SetViewport          (&m_pending_device_state.m_viewport));
		Verify(m_d3d_device->SetVertexDeclaration (m_vf_manager.GetVertexDeclaration(m_pending_device_state.m_vertex_type).m_ptr));
		Verify(m_d3d_device->SetStreamSource      (0, m_pending_device_state.m_Vstream.m_ptr, 0, (UINT)vf::GetSize(m_pending_device_state.m_vertex_type)));
		Verify(m_d3d_device->SetIndices           (m_pending_device_state.m_Istream.m_ptr));

		// Set the render states to the current states
		for (uint i = 0; i < MaxRenderStates; ++i)
		{
			if (m_current_render_state[i].m_state != INVALID_RENDER_STATE)
				Verify(m_d3d_device->SetRenderState(m_current_render_state[i].m_type, m_current_render_state[i].m_state));
		}
		ApplyPendingRenderStates();

		m_current_device_state = m_pending_device_state;
		break;

	// Apply the differences to the current state only
	case ERSMFlush::Diff:
		if (!Equal(m_pending_device_state.m_viewport, m_current_device_state.m_viewport))
		{
			Verify(m_d3d_device->SetViewport(&m_pending_device_state.m_viewport));
			m_current_device_state.m_viewport = m_pending_device_state.m_viewport;
		}
		if (m_pending_device_state.m_vertex_type != m_current_device_state.m_vertex_type)
		{
			Verify(m_d3d_device->SetVertexDeclaration(m_vf_manager.GetVertexDeclaration(m_pending_device_state.m_vertex_type).m_ptr));
			m_current_device_state.m_vertex_type = m_pending_device_state.m_vertex_type;
		}
		if (m_pending_device_state.m_Vstream != m_current_device_state.m_Vstream)
		{
			Verify(m_d3d_device->SetStreamSource(0, m_pending_device_state.m_Vstream.m_ptr, 0, (UINT)vf::GetSize(m_pending_device_state.m_vertex_type)));
			m_current_device_state.m_Vstream = m_pending_device_state.m_Vstream;
		}
		if (m_pending_device_state.m_Istream != m_current_device_state.m_Istream)
		{
			Verify(m_d3d_device->SetIndices(m_pending_device_state.m_Istream.m_ptr));
			m_current_device_state.m_Istream = m_pending_device_state.m_Istream;
		}
		ApplyPendingRenderStates();
		break;
	}
}
	
// Add a state to the pending render state changes
void RenderStateManager::AddPendingRenderState(D3DRENDERSTATETYPE type, uint state)
{
	m_pending_render_state_changes.SetRenderState(type, state);
	if (!m_pending_render_state_changes.IsFull()) return;
	Flush(ERSMFlush::Diff);
}

// Flush the pending render states
void RenderStateManager::ApplyPendingRenderStates()
{
	// Apply the pending render state differences
	rs::Block::TStateBlock& pending = m_pending_render_state_changes.m_state;
	for (rs::Block::TStateBlock::const_iterator i = pending.begin(), i_end = pending.end(); i != i_end; ++i)
	{
		if( i->m_state != m_current_render_state[i->m_type].m_state )
		{
			Verify(m_d3d_device->SetRenderState(i->m_type, i->m_state));
			m_current_render_state[i->m_type].m_state = i->m_state;
		}
	}
	m_pending_render_state_changes.Reset();
}
