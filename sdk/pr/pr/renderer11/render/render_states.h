//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_RENDER_STATES_H
#define PR_RDR_RENDER_RENDER_STATES_H

#include "pr/renderer11/forward.h"
//#include "pr/renderer/models/rendernugget.h"
//#include "pr/renderer/materials/material.h"
//#include "pr/renderer/materials/textures/texture.h"
//#include "pr/renderer/materials/effects/effect.h"

namespace pr
{
	namespace rdr
	{
		namespace rs
		{
			//// A single render state
			//struct State
			//{
			//	D3DRENDERSTATETYPE  m_type;
			//	uint                m_state;
			//	uint                m_prev_state;   // Used by the render state manager
			//};
			//inline bool operator == (State const& lhs, State const& rhs)        { return lhs.m_type == rhs.m_type && lhs.m_state == rhs.m_state; }
			//inline bool operator == (State const& lhs, D3DRENDERSTATETYPE rhs)  { return lhs.m_type == rhs; }
			//
			//// A collection of render states
			//struct Block
			//{
			//	enum { MaxStates = 10 };
			//	typedef pr::pod_array<State, MaxStates> TStateBlock;
			//	TStateBlock m_state;
			//	
			//	bool  IsFull() const     { return m_state.size() == m_state.max_size(); }
			//	void  Reset()            { m_state.clear(); }
			//	void  SetRenderState(D3DRENDERSTATETYPE type, uint state);
			//	void  ClearRenderState(D3DRENDERSTATETYPE type);
			//	State const* Find(D3DRENDERSTATETYPE type) const;
			//	State*       Find(D3DRENDERSTATETYPE type);
			//	State const& operator [](D3DRENDERSTATETYPE type) const;
			//	State&       operator [](D3DRENDERSTATETYPE type);
			//};
			//
			//// A cross section of the render state manager
			//struct DeviceState
			//{
			//	D3DVIEWPORT9                    m_viewport;
			//	vf::Type                        m_vertex_type;
			//	D3DPtr<IDirect3DVertexBuffer9>  m_Vstream;
			//	D3DPtr<IDirect3DIndexBuffer9>   m_Istream;
			//};
			//
			//// Implementation *****************************************************
			//
			//// Set a render state
			//inline void Block::SetRenderState(D3DRENDERSTATETYPE type, uint state)
			//{
			//	// See if we already have this render state
			//	TStateBlock::iterator state_iter = std::find(m_state.begin(), m_state.end(), type);
			//	if (state_iter != m_state.end()) { state_iter->m_state = state; return; }
			//	
			//	// If not, add it
			//	State s = {type, state};
			//	m_state.push_back(s);
			//}
			//
			//// Remove a render state
			//inline void Block::ClearRenderState(D3DRENDERSTATETYPE type)
			//{
			//	// See if we have this render state
			//	TStateBlock::iterator state_iter = std::find(m_state.begin(), m_state.end(), type);
			//	if (state_iter == m_state.end()) { return; }
			//	m_state.erase(state_iter);
			//}
			//
			//// Search for a state in the block and return a pointer to it if found
			//inline State const* Block::Find(D3DRENDERSTATETYPE type) const
			//{
			//	TStateBlock::const_iterator iter = std::find(m_state.begin(), m_state.end(), type);
			//	return iter != m_state.end() ? &*iter : 0;
			//}
			//inline State* Block::Find(D3DRENDERSTATETYPE type)
			//{
			//	TStateBlock::iterator iter = std::find(m_state.begin(), m_state.end(), type);
			//	return iter != m_state.end() ? &*iter : 0;
			//}
			//
			//// Get a render state
			//inline State const& Block::operator [](D3DRENDERSTATETYPE type) const
			//{
			//	PR_ASSERT(1, std::find(m_state.begin(), m_state.end(), type) != m_state.end(), "Block: Render state not found");
			//	return *std::find(m_state.begin(), m_state.end(), type);
			//}
			//inline State& Block::operator [](D3DRENDERSTATETYPE type)
			//{
			//	PR_ASSERT(1, std::find(m_state.begin(), m_state.end(), type) != m_state.end(), "Block: Render state not found");
			//	return *std::find(m_state.begin(), m_state.end(), type);
			//}
			//
			//struct Block
			//{
			//};
		}
	}
}

#endif
