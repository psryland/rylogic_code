//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d/forward.h"
#include "pr/view3d/render/state_block.h"
#include "pr/view3d/shaders/shader_set.h"

namespace pr::rdr
{
	struct DeviceState
	{
		RenderStep const* m_rstep;
		ShadowMap const* m_rstep_smap;
		DrawListElement const* m_dle;
		ModelBuffer* m_mb;
		ETopo m_topo;
		DSBlock m_dsb;
		RSBlock m_rsb;
		BSBlock m_bsb;
		ShaderSet1 m_shdrs;
		Texture2D* m_tex_diffuse;
		TextureCube* m_tex_envmap;

		DeviceState()
			:m_rstep()
			,m_rstep_smap()
			,m_dle()
			,m_mb()
			,m_topo(ETopo::PointList)
			,m_dsb()
			,m_rsb()
			,m_bsb()
			,m_shdrs()
			,m_tex_diffuse()
			,m_tex_envmap()
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
			virtual ~Frame() { m_ss.m_pending = m_restore; }
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
		struct RTFrame :Frame
		{
			// Note, this is not a type frame, it applies changes immediately rather than waiting for Commit.
			UINT m_count;
			ID3D11RenderTargetView* m_rtv[D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT];
			ID3D11DepthStencilView* m_dsv;

			RTFrame(StateStack& ss, ID3D11RenderTargetView* rtv, ID3D11DepthStencilView* dsv);
			RTFrame(StateStack& ss, UINT count, ID3D11RenderTargetView*const* rtv, ID3D11DepthStencilView* dsv);
			RTFrame(RTFrame const&) = delete;
			~RTFrame();
		};
		struct UAVFrame :Frame
		{
			// Note, this is not a type frame, it applies changes immediately rather than waiting for Commit.
			UINT m_first;
			UINT m_count;
			ID3D11UnorderedAccessView* m_uav[D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT];
			UINT m_initial_counts[D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT];
				
			UAVFrame(StateStack& ss, UINT first, ID3D11UnorderedAccessView* uav, UINT initial_count);
			UAVFrame(StateStack& ss, UINT first, UINT count, ID3D11UnorderedAccessView*const* uav, UINT const* initial_counts = nullptr);
			UAVFrame(UAVFrame const&) = delete;
			~UAVFrame();
		};
		struct SOFrame :Frame
		{
			// Note, this is not a type frame, it applies changes immediately rather than waiting for Commit.
			SOFrame(StateStack& ss, ID3D11Buffer* target, UINT offset);
			SOFrame(StateStack& ss, UINT num_buffers, ID3D11Buffer*const* targets, UINT const* offsets);
			SOFrame(SOFrame const&) = delete;
			~SOFrame();
		};

		ID3D11DeviceContext1* m_dc;
		Scene&               m_scene;
		DeviceState          m_init_state;
		DeviceState          m_pending;
		DeviceState          m_current;
		Texture2DPtr         m_def_texture0;
		D3DPtr<ID3D11SamplerState> m_def_sampler;
		D3DPtr<ID3D11SamplerState> m_def_sampler_comp;
		D3DPtr<ID3DUserDefinedAnnotation> m_dbg; // nullptr unless PR_DBG_RDR is 1

		StateStack(ID3D11DeviceContext1* dc, Scene& scene);
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
