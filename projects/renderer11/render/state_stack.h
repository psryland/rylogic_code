//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/state_block.h"
#include "pr/renderer11/shaders/shader_set.h"
#include <d3d11_1.h>

namespace pr
{
	namespace rdr
	{
		struct DeviceState
		{
			RenderStep const* m_rstep;
			ShadowMap const* m_rstep_smap;
			DrawListElement const* m_dle;
			ModelBuffer* m_mb;
			EPrim m_topo;
			DSBlock m_dsb;
			RSBlock m_rsb;
			BSBlock m_bsb;
			ShaderSet m_shdrs;
			Texture2D* m_tex_diffuse;

			DeviceState()
				:m_rstep()
				,m_rstep_smap()
				,m_dle()
				,m_mb()
				,m_topo(EPrim::PointList)
				,m_dsb()
				,m_rsb()
				,m_bsb()
				,m_shdrs()
				,m_tex_diffuse()
			{}
		};

		// Maintains a history of the device state restoring it on destruction
		struct StateStack
		{
			struct Frame
			{
				StateStack& m_ss;
				DeviceState m_restore;
				Frame(StateStack& ss) :m_ss(ss) ,m_restore(m_ss.m_pending) {}
				~Frame() { m_ss.m_pending = m_restore; }
				Frame(Frame const&) = delete;
			};
			struct RSFrame :Frame
			{
				RSFrame(StateStack& ss, RenderStep const& rstep);
				RSFrame(RSFrame const&) = delete;
			};
			struct DleFrame :Frame
			{
				DleFrame(StateStack& ss, DrawListElement const& dle);
				DleFrame(DleFrame const&) = delete;
			};
			struct SmapFrame :Frame
			{
				SmapFrame(StateStack& ss, ShadowMap const* rstep);
				SmapFrame(SmapFrame const&) = delete;
			};

			ID3D11DeviceContext* m_dc;
			Scene&               m_scene;
			DeviceState          m_init_state;
			DeviceState          m_pending;
			DeviceState          m_current;
			Texture2DPtr         m_tex_default;  // A default texture to use in shaders that expect a texture/sampler but have no texture/sampler bound
			D3DPtr<ID3DUserDefinedAnnotation> m_dbg; // nullptr unless PR_DBG_RDR is 1

			StateStack(ID3D11DeviceContext* dc, Scene& scene);
			StateStack(StateStack const&) = delete;
			~StateStack();

			// Apply the current state to the device
			void Commit();
		
		private:

			void ApplyState(DeviceState& current, DeviceState& pending, bool force);
			void SetupIA(DeviceState& current, DeviceState& pending, bool force);
			void SetupRS(DeviceState& current, DeviceState& pending, bool force);
			void SetupShdrs(DeviceState& current, DeviceState& pending, bool force);
			void SetupTextures(DeviceState& current, DeviceState& pending, bool force);
		};
	}
}
