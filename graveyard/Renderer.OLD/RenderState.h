//**********************************************************************************
//
//	Groups a render state with its value
//
//**********************************************************************************
#ifndef RENDERSTATE_H
#define RENDERSTATE_H

#include "PR/Common/PRAssert.h"
#include "PR/Renderer/RendererAssertEnable.h"

namespace pr
{
	namespace rdr
	{
		// A single render state
		struct RenderState
		{
			// No destructor in this struct please. Lists are assumming no destruction is needed
			bool operator ==(const RenderState& other) const { return m_type == other.m_type && m_state == other.m_state; }
			D3DRENDERSTATETYPE	m_type;
			uint				m_state;
		};

		// A collection of render states
		struct RenderStateBlock
		{
			enum { MaxStates = 20 };
			RenderStateBlock() : m_num_states(0)	{}
			void  Reset()							{ m_num_states = 0; }
			void  SetRenderState(D3DRENDERSTATETYPE type, uint state);
			void  ClearRenderState(D3DRENDERSTATETYPE type);
			      RenderState& operator [] (D3DRENDERSTATETYPE type);
			const RenderState& operator [] (D3DRENDERSTATETYPE type) const;

		private:
			friend class RenderStateManager;
			RenderState	m_state[MaxStates];		// The array to store the states in
			uint		m_num_states;				// The number actually used
		};

		//********************************************************************
		// Implementation
		//*****
		// Set a render state
		inline void RenderStateBlock::SetRenderState(D3DRENDERSTATETYPE type, uint state)
		{
			// See if we already have this render state
			for( uint i = 0; i < m_num_states; ++i )
			{
				if( m_state[i].m_type == type ) {  m_state[i].m_state = state; return; }
			}

			// If not, add it
			m_state[m_num_states].m_type			= type;
			m_state[m_num_states].m_state			= state;
			++m_num_states;
			PR_ASSERT_STR(PR_DBG_RDR, m_num_states < MaxStates, "RenderStateBlock. Too many render states used");
		}

		//*****
		// Remove a render state
		inline void RenderStateBlock::ClearRenderState(D3DRENDERSTATETYPE type)
		{
			// Find the state and remove it
			for( uint i = 0; i < m_num_states; ++i )
			{
				if( m_state[i].m_type == type )
				{
					for( ++i; i < m_num_states; ++i )
						m_state[i - 1] = m_state[i];
					--m_num_states;
					return;
				}
			}
			PR_WARN(PR_DBG_RDR, "RenderStateBlock: Render state not found");
		}

		//*****
		// Get a render state
		inline RenderState& RenderStateBlock::operator [] (D3DRENDERSTATETYPE type)
		{
			// See if we already have this render state
			uint i; for( i = 0; i < m_num_states; ++i ) if( m_state[i].m_type == type ) break;
			PR_ASSERT_STR(PR_DBG_RDR, i < m_num_states, "RenderStateBlock: Render state not found");
			return m_state[i];
		}

		//*****
		// Get a render state
		inline const RenderState& RenderStateBlock::operator [] (D3DRENDERSTATETYPE type) const
		{
			// See if we already have this render state
			uint i; for( i = 0; i < m_num_states; ++i ) if( m_state[i].m_type == type ) break;
			PR_ASSERT_STR(PR_DBG_RDR, i < m_num_states, "RenderStateBlock: Render state not found");
			return m_state[i];
		}
	}//namespace rdr
}//namespace pr

#endif//RENDERSTATE_H
