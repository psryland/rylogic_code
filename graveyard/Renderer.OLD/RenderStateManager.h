//**********************************************************************************
//
//	Managers the state of the renderer
//
//**********************************************************************************
//
//	Usage:
//		The default renderstates are applied at initialisation. 
//		Any changes to the state of the render are stored in stacks
//		using PushXXX and PopXXX methods.
//		The state of the render is not guaranteed to be the correct state
//		after a Pop, only after a Flush. This allows unnecessary
//		state changes to be avoided.
//
#ifndef RENDERSTATEMANAGER_H
#define RENDERSTATEMANAGER_H

#include "PR/Common/D3DPtr.h"
#include "PR/Renderer/VertexFormat.h"
#include "PR/Renderer/RenderState.h"
//#include "PR/Renderer/Material.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
		struct RendererState
		{
			D3DVIEWPORT9					m_viewport;
			vf::Type						m_vertex_type;
			D3DPtr<IDirect3DVertexBuffer9>	m_Vstream;
			D3DPtr<IDirect3DIndexBuffer9>	m_Istream;
		};

		//*****
		// A class to manager render state changes
		class RenderStateManager
		{
		public:
			enum
			{
				MaxRenderStates					= D3DRS_BLENDOPALPHA + 1,
				DefaultStackFrameSize			= 10,
				DefaultViewportStackSize		= 3,
				DefaultVertexTypeStackSize		= 10,
				DefaultVStreamStackSize			= 10,
				DefaultIStreamStackSize			= 10,
				DefaultRenderStateStackSize		= 100
			};
			enum EFlushType
			{
				EFlush_Diff							= 0,
				EFlush_Force						= 1
			};

			RenderStateManager(D3DPtr<IDirect3DDevice9> d3d_device, const vf::Manager* vf_manager, const IRect& client_area);
			void Resize(const IRect& client_area);
			void ReleaseDeviceDependentObjects()									{ m_d3d_device = 0; }
			void CreateDeviceDependentObjects(D3DPtr<IDirect3DDevice9> d3d_device)	{ m_d3d_device = d3d_device; }
			void UseDefaultRenderStates();
			void UseCurrentRenderStates();
			uint GetCurrentRenderState(D3DRENDERSTATETYPE type) const;
			const RendererState& GetCurrentState() const					{ return m_current_state; }
			
			void PushViewport(const Viewport* viewport);
			void PopViewport(const Viewport* viewport);
			void PushDrawListElement(const DrawListElement* element);
			void PopDrawListElement(const DrawListElement* element);
			void PushRenderStateBlock(const RenderStateBlock& rsb);
			void PopRenderStateBlock(const RenderStateBlock& rsb);
			void Flush(EFlushType flush_type);

		private:
			DWORD FtoDW(float f)	{ return reinterpret_cast<DWORD&>(f); }
			void AddPendingRenderState(D3DRENDERSTATETYPE type, uint state);
			void ApplyPendingRenderStates();
			bool EqualViewport(const D3DVIEWPORT9& a, const D3DVIEWPORT9& b) const;
			bool EqualTransform(const m4x4& a, const m4x4& b) const;

		private:
			struct RenderStateEx : public RenderState { uint m_old_state; };
			struct StackFrame
			{
				const void*	m_owner;				// The pointer passed in in PushXXX
				uint		m_num_render_states;	// Number of render states in this stack frame
			};

		private:
			D3DPtr<IDirect3DDevice9> m_d3d_device;
			const vf::Manager*		 m_vf_manager;

			// This is the state that the renderer currently is in.
			RendererState		m_current_state;
			RenderStateBlock	m_pending_render_state_changes;
			
			// This is the render states as d3d sees them, the current render state is 
			// 'm_actual_render_state' + the pending render state changes
			RenderState			m_actual_render_state[MaxRenderStates];

			// Stacks
			std::vector<StackFrame>							m_stack_frame;
			std::vector<D3DVIEWPORT9>						m_viewport_stack;
			std::vector<vf::Type>							m_vertex_type_stack;
			std::vector<D3DPtr<IDirect3DVertexBuffer9> >	m_Vstream_stack;
			std::vector<D3DPtr<IDirect3DIndexBuffer9> >		m_Istream_stack;
			std::vector<RenderStateEx>						m_render_state_stack;

			// Defaults
			D3DVIEWPORT9					m_default_viewport;
			vf::Type						m_default_vertex_type;
			D3DPtr<IDirect3DVertexBuffer9>	m_default_Vstream;
			D3DPtr<IDirect3DIndexBuffer9>	m_default_Istream;
			RenderState						m_default_render_state[MaxRenderStates];
		};

		//*******************************************************************
		// Implementation
		//*****
		// Return what should be the current value of a render state
		inline uint	RenderStateManager::GetCurrentRenderState(D3DRENDERSTATETYPE type) const
		{
			uint state = m_actual_render_state[type].m_state;
			for( uint i = 0; i < m_pending_render_state_changes.m_num_states; ++i )
			{
				if( m_pending_render_state_changes.m_state[i].m_type == type )
				{
					state = m_pending_render_state_changes.m_state[i].m_state;
					break;
				}
			}
			return state;
		}

		//*****
		// Adds a state to the block of states that need to be changed with the next flush.
		inline void	RenderStateManager::AddPendingRenderState(D3DRENDERSTATETYPE type, uint state)
		{
			m_pending_render_state_changes.SetRenderState(type, state);
			if( m_pending_render_state_changes.m_num_states == RenderStateBlock::MaxStates )
			{
				Flush(EFlush_Diff);
			}
		}

		//*****
		// Returns true if two viewports are equal
		inline bool RenderStateManager::EqualViewport(const D3DVIEWPORT9& a, const D3DVIEWPORT9& b) const
		{
			return	a.X			== b.X		&&
					a.Y			== b.Y		&&
					a.Width		== b.Width	&&
					a.Height	== b.Height	&&
					FEql(a.MinZ, b.MinZ)	&&
					FEql(a.MaxZ, b.MaxZ);
		}

		//*****
		// Returns true if two matrices are equal. sort of
		inline bool RenderStateManager::EqualTransform(const m4x4& a, const m4x4& b) const
		{
			return	FEql(a[0][0], b[0][0]) &&
					FEql(a[1][1], b[1][1]) &&
					FEql(a[2][2], b[2][2]) &&
					FEql(a[3][0], b[3][0]) &&
					FEql(a[3][1], b[3][1]) &&
					FEql(a[3][2], b[3][2]);
		}
	
	}//namespace rdr
}//namespace pr

#endif//RENDERSTATEMANAGER_H
