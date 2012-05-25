//**********************************************************************************
//
//	Managers the state of the renderer
//
//**********************************************************************************

#include "Stdafx.h"
#include "PR/Common/D3DHelpers.h"
#include "PR/Renderer/RenderStateManager.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Attribute.h"
#include "PR/Renderer/DrawListElement.h"
#include "PR/Renderer/RenderNugget.h"
#include "PR/Renderer/Models/RenderableBase.h"
#include "PR/Renderer/Instance.h"

//#define _RENDERSTATE_DEBUG
	#ifdef _RENDERSTATE_DEBUG
	#define RENDERSTATE_DEBUG(x) x
	#else//RENDERSTATE_DEBUG
	#define RENDERSTATE_DEBUG(x)
	#endif//RENDERSTATE_DEBUG

const D3DRENDERSTATETYPE INVALID_RENDER_STATE = D3DRS_FORCE_DWORD;

using namespace pr;
using namespace pr::rdr;

//*****
// Constructor
RenderStateManager::RenderStateManager(D3DPtr<IDirect3DDevice9> d3d_device, const vf::Manager* vf_manager, const IRect& client_area)
:m_d3d_device(d3d_device)
,m_vf_manager(vf_manager)
{
	m_stack_frame			.reserve(DefaultStackFrameSize);
	m_viewport_stack		.reserve(DefaultViewportStackSize);
	m_vertex_type_stack		.reserve(DefaultVertexTypeStackSize);
	m_Vstream_stack			.reserve(DefaultVStreamStackSize);
	m_Istream_stack			.reserve(DefaultIStreamStackSize);
	m_render_state_stack	.reserve(DefaultRenderStateStackSize);

	// Default viewport
	m_default_viewport.X		= 0;
	m_default_viewport.Y		= 0;
	m_default_viewport.Width	= client_area.Width();
	m_default_viewport.Height	= client_area.Height();
	m_default_viewport.MinZ		= 0.0f;
	m_default_viewport.MaxZ		= 1.0f;

	// Default vertex type
	m_default_vertex_type		= vf::EType_PosNormDiffTex;

	// Default streams
	m_default_Vstream = 0;
	m_default_Istream = 0;

	// Default render states
	for( int i = 0; i < MaxRenderStates; ++i )
	{
		// Set all to invalid
		m_default_render_state[i].m_type			= static_cast<D3DRENDERSTATETYPE>(i);
		m_default_render_state[i].m_state			= INVALID_RENDER_STATE;
	}
	// Now set the default states
	#define RENDERSTATE(render_state, default_state)	m_default_render_state[render_state].m_state = default_state;
	#include "RenderStatesInc.h"
	#undef RENDERSTATE

	// Use the default render states
	UseDefaultRenderStates();
}

//*****
// Called when resize is called on the renderer
void RenderStateManager::Resize(const IRect& client_area)
{
	m_default_viewport.X		= 0;
	m_default_viewport.Y		= 0;
	m_default_viewport.Width	= client_area.Width();
	m_default_viewport.Height	= client_area.Height();
	m_default_viewport.MinZ		= 0.0f;
	m_default_viewport.MaxZ		= 1.0f;

	UseCurrentRenderStates();
}

//*****
// Set the renderer state to defaults
void RenderStateManager::UseDefaultRenderStates()
{
	// Reset all of the stacks so that the defaults are used
	m_stack_frame			.clear();
	m_viewport_stack		.clear();
	m_vertex_type_stack		.clear();
	m_Vstream_stack			.clear();
	m_Istream_stack			.clear();
	m_render_state_stack	.clear();

	memcpy(m_actual_render_state, m_default_render_state, sizeof(m_actual_render_state));
	m_pending_render_state_changes.Reset();

	UseCurrentRenderStates();
}

//*****
// Set the render state to the current states
void RenderStateManager::UseCurrentRenderStates()
{
	Flush(EFlush_Force);
}

//*****
// push_back the state of a viewport onto the stacks
void RenderStateManager::PushViewport(const Viewport* viewport)
{
	m_viewport_stack.push_back(viewport->m_d3d_viewport);

	for( uint i = 0; i < viewport->m_render_state.m_num_states; ++i )
	{
		RenderStateEx rs;
		rs.m_type				= viewport->m_render_state.m_state[i].m_type;
		rs.m_state				= viewport->m_render_state.m_state[i].m_state;
		rs.m_old_state			= GetCurrentRenderState(rs.m_type);
		m_render_state_stack	.push_back(rs);
		AddPendingRenderState	(rs.m_type, rs.m_state);
	}

	// Record the stack frame
	StackFrame stack_frame;
	stack_frame.m_owner				= viewport;
	stack_frame.m_num_render_states	= viewport->m_render_state.m_num_states;
	m_stack_frame.push_back(stack_frame);

	Flush(EFlush_Diff);
}

//*****
// pop_back the state of a viewport off the stacks
void RenderStateManager::PopViewport(const Viewport* PR_EXPAND(PR_DBG_RDR, viewport))
{
	StackFrame& stack_frame = m_stack_frame.back();
	PR_ASSERT_STR(PR_DBG_RDR, stack_frame.m_owner == viewport, "RenderStateManager: push_back/pop_back mismatch");

	// Render states
	for( uint i = stack_frame.m_num_render_states; i > 0; --i )
	{
		RenderStateEx& rs = m_render_state_stack.back();
		AddPendingRenderState(rs.m_type, rs.m_old_state);
		m_render_state_stack.pop_back();
	}

	m_viewport_stack		.pop_back();
	m_stack_frame			.pop_back();
}

//*****
// push_back the state of a draw list element onto the stacks
void RenderStateManager::PushDrawListElement(const DrawListElement* element)
{
	const InstanceBase*		instance	= element->m_instance;
	const RenderNugget*		nugget		= element->m_nugget;
	const RenderableBase*	renderable	= element->m_nugget->m_owner;
	m_vertex_type_stack	.push_back(renderable->m_vertex_type);
	m_Vstream_stack     .push_back(renderable->m_vertex_buffer);
	m_Istream_stack     .push_back(renderable->m_index_buffer);
	
	// Add the renderable's render states
	for( uint i = 0; i < renderable->m_render_state.m_num_states; ++i )
	{
		RenderStateEx rs;
		rs.m_type				= renderable->m_render_state.m_state[i].m_type;
		rs.m_state				= renderable->m_render_state.m_state[i].m_state;
		rs.m_old_state			= GetCurrentRenderState(rs.m_type);
		m_render_state_stack	.push_back(rs);
		AddPendingRenderState	(rs.m_type, rs.m_state);
	}

	// Then the nugget's render states
	for( uint i = 0; i < nugget->m_render_state.m_num_states; ++i )
	{
		RenderStateEx rs;
		rs.m_type				= nugget->m_render_state.m_state[i].m_type;
		rs.m_state				= nugget->m_render_state.m_state[i].m_state;
		rs.m_old_state			= GetCurrentRenderState(rs.m_type);
		m_render_state_stack	.push_back(rs);
		AddPendingRenderState	(rs.m_type, rs.m_state);
	}

	// Then the instance's render states
	const RenderStateBlock* render_state = instance->GetRenderStates();
	if( render_state )
	{
		for( uint i = 0; i < render_state->m_num_states; ++i )
		{
			RenderStateEx rs;
			rs.m_type				= render_state->m_state[i].m_type;
			rs.m_state				= render_state->m_state[i].m_state;
			rs.m_old_state			= GetCurrentRenderState(rs.m_type);
			m_render_state_stack	.push_back(rs);
			AddPendingRenderState	(rs.m_type, rs.m_state);
		}
	}

	// Record the stack frame
	StackFrame stack_frame;
	stack_frame.m_owner				=	element;
	stack_frame.m_num_render_states	=	renderable->m_render_state.m_num_states + 
										nugget->m_render_state.m_num_states +
										(render_state != 0 ? render_state->m_num_states : 0);
	m_stack_frame.push_back(stack_frame);

	Flush(EFlush_Diff);
}

//*****
// pop_back the state of a render nugget off the stacks
void RenderStateManager::PopDrawListElement(const DrawListElement* PR_EXPAND(PR_DBG_RDR, element))
{
	StackFrame& stack_frame = m_stack_frame.back();
	PR_ASSERT_STR(PR_DBG_RDR, stack_frame.m_owner == element, "RenderStateManager: push_back/pop_back mismatch");

	// Render states
	for( uint i = stack_frame.m_num_render_states; i > 0; --i )
	{
		RenderStateEx& rs = m_render_state_stack.back();
		AddPendingRenderState(rs.m_type, rs.m_old_state);
		m_render_state_stack.pop_back();
	}

	m_Istream_stack		.pop_back();
	m_Vstream_stack		.pop_back();
	m_vertex_type_stack	.pop_back();
	m_stack_frame		.pop_back();
}

//*****
// Add a block of render states to the render state stack
void RenderStateManager::PushRenderStateBlock(const RenderStateBlock& rsb)
{
	for( uint i = 0; i < rsb.m_num_states; ++i )
	{
		RenderStateEx rs;
		rs.m_type				= rsb.m_state[i].m_type;
		rs.m_state				= rsb.m_state[i].m_state;
		rs.m_old_state			= GetCurrentRenderState(rs.m_type);
		m_render_state_stack	.push_back(rs);
		AddPendingRenderState	(rs.m_type, rs.m_state);
	}

	// Record the stack frame
	StackFrame stack_frame;
	stack_frame.m_owner				= &rsb;
	stack_frame.m_num_render_states = rsb.m_num_states;
	m_stack_frame.push_back(stack_frame);

	Flush(EFlush_Diff);
}

//*****
// pop_back a block of render states from the render state stack
void RenderStateManager::PopRenderStateBlock(const RenderStateBlock& PR_EXPAND(PR_DBG_RDR, rsb))
{
	StackFrame& stack_frame = m_stack_frame.back();
	PR_ASSERT_STR(PR_DBG_RDR, stack_frame.m_owner == &rsb, "RenderStateManager: push_back/pop_back mismatch");

	// Render states
	for( uint i = stack_frame.m_num_render_states; i > 0; --i )
	{
		RenderStateEx& rs = m_render_state_stack.back();
		AddPendingRenderState(rs.m_type, rs.m_old_state);
		m_render_state_stack.pop_back();
	}
	m_stack_frame.pop_back();
}

//*****
// Flushes the current logical renderer state down to the d3d device
void RenderStateManager::Flush(EFlushType flush_type)
{
	// Get the state that the renderer should be in
	D3DVIEWPORT9&					current_viewport	= (m_viewport_stack.size()    == 0) ? (m_default_viewport)    : (m_viewport_stack	.back());
	vf::Type&						current_vertex_type	= (m_vertex_type_stack.size() == 0) ? (m_default_vertex_type) : (m_vertex_type_stack.back());
	D3DPtr<IDirect3DVertexBuffer9>&	current_Vstream		= (m_Vstream_stack.size()     == 0) ? (m_default_Vstream)     : (m_Vstream_stack	.back());
	D3DPtr<IDirect3DIndexBuffer9>&	current_Istream		= (m_Istream_stack.size()     == 0) ? (m_default_Istream)     : (m_Istream_stack	.back());

	switch( flush_type )
	{
	// Apply the current state
	case EFlush_Force:
		Verify(m_d3d_device->SetViewport			(&current_viewport)												);
		Verify(m_d3d_device->SetVertexDeclaration	(m_vf_manager->GetVertexDeclaration(current_vertex_type).m_ptr)	);
		Verify(m_d3d_device->SetStreamSource		(0, current_Vstream.m_ptr, 0, vf::GetSize(current_vertex_type))	);
		Verify(m_d3d_device->SetIndices				(current_Istream.m_ptr)											);

		// Set the render states to the current states
		for( uint i = 0; i < MaxRenderStates; ++i )
		{
			if( m_actual_render_state[i].m_state != INVALID_RENDER_STATE )
				Verify(m_d3d_device->SetRenderState(m_actual_render_state[i].m_type, m_actual_render_state[i].m_state));
		}

		ApplyPendingRenderStates();

		m_current_state.m_viewport			= current_viewport;
		m_current_state.m_vertex_type		= current_vertex_type;
		m_current_state.m_Vstream			= current_Vstream;
		m_current_state.m_Istream			= current_Istream;
		break;

	// Apply the differences to the current state only
	case EFlush_Diff:
		if( !EqualViewport(current_viewport, m_current_state.m_viewport) )
		{
			Verify(m_d3d_device->SetViewport(&current_viewport));
			m_current_state.m_viewport = current_viewport;
		}

		if( current_vertex_type != m_current_state.m_vertex_type )
		{
			Verify(m_d3d_device->SetVertexDeclaration(m_vf_manager->GetVertexDeclaration(current_vertex_type).m_ptr));
			m_current_state.m_vertex_type = current_vertex_type;
		}

		if( current_Vstream != m_current_state.m_Vstream )
		{
			Verify(m_d3d_device->SetStreamSource(0, current_Vstream.m_ptr, 0, vf::GetSize(current_vertex_type)));
			m_current_state.m_Vstream = current_Vstream;
		}
		
		if( current_Istream != m_current_state.m_Istream )
		{
			Verify(m_d3d_device->SetIndices(current_Istream.m_ptr));
			m_current_state.m_Istream = current_Istream;
		}

		ApplyPendingRenderStates();
		break;

	default:
		PR_ERROR_STR(PR_DBG_RDR, "Unknown flush type");
	}
}

//*****
// Flush the pending render states
void RenderStateManager::ApplyPendingRenderStates()
{
	// Apply the pending render state differences
	for( uint i = 0; i < m_pending_render_state_changes.m_num_states; ++i )
	{
		RenderState& current_rs	= m_pending_render_state_changes.m_state[i];
		RenderState& actual_rs	= m_actual_render_state[current_rs.m_type];
		if( actual_rs.m_state != current_rs.m_state )
		{
			Verify(m_d3d_device->SetRenderState(current_rs.m_type, current_rs.m_state));
			actual_rs.m_state = current_rs.m_state;
		}
	}
	m_pending_render_state_changes.Reset();
}
