//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************

#include "pr/view3d/forward.h"
#include "pr/view3d/render/state_block.h"
#include "pr/view3d/render/renderer.h"

namespace pr::rdr
{
	#pragma region Blend State

	BSBlock::BSBlock()
		:m_bsb()
	{}

	// Returns the description
	BlendStateDesc const& BSBlock::Desc() const
	{
		return m_bsb.m_state;
	}
	BlendStateDesc& BSBlock::Desc()
	{
		return m_bsb.m_state;
	}

	// Clear a field in the state description
	void BSBlock::Clear(EBS field)
	{
		PR_ASSERT(PR_DBG_RDR, field == EBS::AlphaToCoverageEnable || field == EBS::IndependentBlendEnable, "Incorrect field provided");
		m_bsb.Clear(field);
	}
	void BSBlock::Clear(EBS field, int render_target)
	{
		PR_ASSERT(PR_DBG_RDR, field != EBS::AlphaToCoverageEnable && field != EBS::IndependentBlendEnable, "Incorrect field provided");
		m_bsb.Clear(field, render_target);
	}

	// Set the value of a field in the state description
	void BSBlock::Set(EBS field, BOOL value)
	{
		switch (field)
		{
			case EBS::AlphaToCoverageEnable: m_bsb.m_state.AlphaToCoverageEnable = value; break;
			case EBS::IndependentBlendEnable: m_bsb.m_state.IndependentBlendEnable = value; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
		}
		m_bsb.Set(field);
	}
	void BSBlock::Set(EBS field, BOOL value, int render_target)
	{
		PR_ASSERT(PR_DBG_RDR, field == EBS::BlendEnable, "Incorrect field provided");
		m_bsb.m_state.RenderTarget[render_target].BlendEnable = value;
		m_bsb.Set(field, render_target);
	}
	void BSBlock::Set(EBS field, D3D11_BLEND value, int render_target)
	{
		switch (field)
		{
			case EBS::SrcBlend: m_bsb.m_state.RenderTarget[render_target].SrcBlend = value; break;
			case EBS::DestBlend: m_bsb.m_state.RenderTarget[render_target].DestBlend = value; break;
			case EBS::SrcBlendAlpha: m_bsb.m_state.RenderTarget[render_target].SrcBlendAlpha = value; break;
			case EBS::DestBlendAlpha: m_bsb.m_state.RenderTarget[render_target].DestBlendAlpha = value; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
		}
		m_bsb.Set(field, render_target);
	}
	void BSBlock::Set(EBS field, D3D11_BLEND_OP value, int render_target)
	{
		switch (field)
		{
			case EBS::BlendOp: m_bsb.m_state.RenderTarget[render_target].BlendOp = value; break;
			case EBS::BlendOpAlpha: m_bsb.m_state.RenderTarget[render_target].BlendOpAlpha = value; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
		}
		m_bsb.Set(field, render_target);
	}
	void BSBlock::Set(EBS field, UINT8 value, int render_target)
	{
		PR_ASSERT(PR_DBG_RDR, field == EBS::RenderTargetWriteMask, "Incorrect field provided");
		m_bsb.m_state.RenderTarget[render_target].RenderTargetWriteMask = value;
		m_bsb.Set(field, render_target);
	}

	// Combine two states into one. 'rhs' has priority over 'this'
	BSBlock& BSBlock::operator |= (BSBlock const& rhs)
	{
		m_bsb.Merge(rhs.m_bsb, [this](EBS field, int i, BlendStateDesc const& r)
		{
			switch (field)
			{
				case EBS::AlphaToCoverageEnable: Set(EBS::AlphaToCoverageEnable, r.AlphaToCoverageEnable); break;
				case EBS::IndependentBlendEnable: Set(EBS::IndependentBlendEnable, r.IndependentBlendEnable); break;
				case EBS::BlendEnable: Set(EBS::BlendEnable, r.RenderTarget[i].BlendEnable, i); break;
				case EBS::SrcBlend: Set(EBS::SrcBlend, r.RenderTarget[i].SrcBlend, i); break;
				case EBS::DestBlend: Set(EBS::DestBlend, r.RenderTarget[i].DestBlend, i); break;
				case EBS::BlendOp: Set(EBS::BlendOp, r.RenderTarget[i].BlendOp, i); break;
				case EBS::SrcBlendAlpha: Set(EBS::SrcBlendAlpha, r.RenderTarget[i].SrcBlendAlpha, i); break;
				case EBS::DestBlendAlpha: Set(EBS::DestBlendAlpha, r.RenderTarget[i].DestBlendAlpha, i); break;
				case EBS::BlendOpAlpha: Set(EBS::BlendOpAlpha, r.RenderTarget[i].BlendOpAlpha, i); break;
				case EBS::RenderTargetWriteMask: Set(EBS::RenderTargetWriteMask, r.RenderTarget[i].RenderTargetWriteMask, i); break;
				default: PR_ASSERT(PR_DBG_RDR, false, "Unknown blend state field"); break;
			}
		});
		return *this;
	}

	BlendStateManager::BlendStateManager(Renderer& rdr)
		:base(rdr)
	{}

	// Get/Create a state object for 'desc'
	D3DPtr<ID3D11BlendState> BlendStateManager::State(pr::rdr::BSBlock const& desc)
	{
		Renderer::Lock lock(*m_rdr);
		return GetState(desc, [&](pr::rdr::BlendStateDesc const& d)
		{
			ID3D11BlendState* bs;
			pr::Throw(lock.D3DDevice()->CreateBlendState(&d, &bs));
			return bs;
		});
	}

	#pragma endregion

	#pragma region Depth State

	DSBlock::DSBlock()
		:m_dsb()
	{}

	// Returns the description
	DepthStateDesc const& DSBlock::Desc() const
	{
		return m_dsb.m_state;
	}
	DepthStateDesc& DSBlock::Desc()
	{
		return m_dsb.m_state;
	}

	// Clear a field in the state description
	void DSBlock::Clear(EDS field)
	{
		PR_ASSERT(PR_DBG_RDR,
			field == EDS::DepthEnable ||
			field == EDS::DepthWriteMask ||
			field == EDS::DepthFunc ||
			field == EDS::StencilEnable ||
			field == EDS::StencilReadMask ||
			field == EDS::StencilWriteMask
			, "Incorrect field provided");
		m_dsb.Clear(field);
	}
	void DSBlock::Clear(EDS field, bool back_face)
	{
		PR_ASSERT(PR_DBG_RDR,
			field == EDS::StencilFunc ||
			field == EDS::StencilDepthFailOp ||
			field == EDS::StencilPassOp ||
			field == EDS::StencilFailOp
			, "Incorrect field provided");
		m_dsb.Clear(field, (int)back_face);
	}

	// Set the value of a field in the state description
	void DSBlock::Set(EDS field, BOOL value)
	{
		switch (field)
		{
			case EDS::DepthEnable: m_dsb.m_state.DepthEnable = value; break;
			case EDS::StencilEnable: m_dsb.m_state.StencilEnable = value; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
		}
		m_dsb.Set(field);
	}
	void DSBlock::Set(EDS field, D3D11_DEPTH_WRITE_MASK value)
	{
		PR_ASSERT(PR_DBG_RDR, field == EDS::DepthWriteMask, "Incorrect field provided");
		m_dsb.m_state.DepthWriteMask = value;
		m_dsb.Set(field);
	}
	void DSBlock::Set(EDS field, D3D11_COMPARISON_FUNC value)
	{
		PR_ASSERT(PR_DBG_RDR, field == EDS::DepthFunc, "Incorrect field provided");
		m_dsb.m_state.DepthFunc = value;
		m_dsb.Set(field);
	}
	void DSBlock::Set(EDS field, UINT8 value)
	{
		switch (field)
		{
			case EDS::StencilReadMask: m_dsb.m_state.StencilReadMask = value; break;
			case EDS::StencilWriteMask: m_dsb.m_state.StencilWriteMask = value; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
		}
		m_dsb.Set(field);
	}
	void DSBlock::Set(EDS field, D3D11_COMPARISON_FUNC value, bool back_face)
	{
		PR_ASSERT(PR_DBG_RDR, field == EDS::StencilFunc, "Incorrect field provided");
		(back_face ? m_dsb.m_state.BackFace : m_dsb.m_state.FrontFace).StencilFunc = value;
		m_dsb.Set(field, (int)back_face);
	}
	void DSBlock::Set(EDS field, D3D11_STENCIL_OP value, bool back_face)
	{
		auto& face = back_face ? m_dsb.m_state.BackFace : m_dsb.m_state.FrontFace;
		switch (field)
		{
			case EDS::StencilDepthFailOp: face.StencilDepthFailOp = value; break;
			case EDS::StencilPassOp: face.StencilPassOp = value; break;
			case EDS::StencilFailOp: face.StencilFailOp = value; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
		}
		m_dsb.Set(field, (int)back_face);
	}

	// Combine two states into one. 'rhs' has priority over 'this'
	DSBlock& DSBlock::operator |= (DSBlock const& rhs)
	{
		m_dsb.Merge(rhs.m_dsb, [this](EDS field, int i, DepthStateDesc const& r)
		{
			switch (field)
			{
				case EDS::DepthEnable: Set(EDS::DepthEnable, r.DepthEnable); break;
				case EDS::DepthWriteMask: Set(EDS::DepthWriteMask, r.DepthWriteMask); break;
				case EDS::DepthFunc: Set(EDS::DepthFunc, r.DepthFunc); break;
				case EDS::StencilEnable: Set(EDS::StencilEnable, r.StencilEnable); break;
				case EDS::StencilReadMask: Set(EDS::StencilReadMask, r.StencilReadMask); break;
				case EDS::StencilWriteMask: Set(EDS::StencilWriteMask, r.StencilWriteMask); break;
				case EDS::StencilFunc: Set(EDS::StencilFunc, (i == 0 ? r.FrontFace : r.BackFace).StencilFunc, i != 0); break;
				case EDS::StencilDepthFailOp: Set(EDS::StencilDepthFailOp, (i == 0 ? r.FrontFace : r.BackFace).StencilDepthFailOp, i != 0); break;
				case EDS::StencilPassOp: Set(EDS::StencilPassOp, (i == 0 ? r.FrontFace : r.BackFace).StencilPassOp, i != 0); break;
				case EDS::StencilFailOp: Set(EDS::StencilFailOp, (i == 0 ? r.FrontFace : r.BackFace).StencilFailOp, i != 0); break;
				default: PR_ASSERT(PR_DBG_RDR, false, "Unknown depth state field"); break;
			}
		});
		return *this;
	}

	DepthStateManager::DepthStateManager(Renderer& rdr)
		:base(rdr)
	{}

	// Get/Create a state object for 'desc'
	D3DPtr<ID3D11DepthStencilState> DepthStateManager::State(DSBlock const& desc)
	{
		Renderer::Lock lock(*m_rdr);
		return GetState(desc, [&](DepthStateDesc const& d)
		{
			ID3D11DepthStencilState* ds;
			pr::Throw(lock.D3DDevice()->CreateDepthStencilState(&d, &ds));
			return ds;
		});
	}

	#pragma endregion

	#pragma region Raster State

	RSBlock::RSBlock()
		:m_rsb()
	{}
	RSBlock::RSBlock(D3D11_FILL_MODE fill, D3D11_CULL_MODE cull)
		:m_rsb()
	{
		Set(ERS::FillMode, fill);
		Set(ERS::CullMode, cull);
	}
	
	// Returns the description
	RasterStateDesc const& RSBlock::Desc() const
	{
		return m_rsb.m_state;
	}
	RasterStateDesc& RSBlock::Desc()
	{
		return m_rsb.m_state;
	}

	// Clear a field in the state description
	void RSBlock::Clear(ERS field)
	{
		m_rsb.Clear(field);
	}

	// Set the value of a field in the state description
	void RSBlock::Set(ERS field, D3D11_FILL_MODE value)
	{
		PR_ASSERT(PR_DBG_RDR, field == ERS::FillMode, "Incorrect field provided");
		m_rsb.m_state.FillMode = value;
		m_rsb.Set(field);
	}
	void RSBlock::Set(ERS field, D3D11_CULL_MODE value)
	{
		PR_ASSERT(PR_DBG_RDR, field == ERS::CullMode, "Incorrect field provided");
		m_rsb.m_state.CullMode = value;
		m_rsb.Set(field);
	}
	void RSBlock::Set(ERS field, int value)
	{
		switch (field)
		{
			case ERS::DepthClipEnable: m_rsb.m_state.FrontCounterClockwise = value != 0; break;
			case ERS::FrontCCW: m_rsb.m_state.DepthBias = value; break;
			case ERS::MultisampleEnable: m_rsb.m_state.MultisampleEnable = value != 0; break;
			case ERS::ScissorEnable: m_rsb.m_state.DepthClipEnable = value != 0; break;
			case ERS::DepthBias: m_rsb.m_state.ScissorEnable = value != 0; break;
			case ERS::SlopeScaledDepthBias: m_rsb.m_state.AntialiasedLineEnable = value != 0; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided");
		}
		m_rsb.Set(field);
	}
	void RSBlock::Set(ERS field, float value)
	{
		switch (field)
		{
			case ERS::AntialiasedLineEnable: m_rsb.m_state.SlopeScaledDepthBias = value; break;
			case ERS::DepthBias_clamp: m_rsb.m_state.DepthBiasClamp = value; break;
			default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided");
		}
		m_rsb.Set(field);
	}

	// Combine two states into one. 'rhs' has priority over 'this'
	RSBlock& RSBlock::operator |= (RSBlock const& rhs)
	{
		m_rsb.Merge(rhs.m_rsb, [this](ERS field, int, RasterStateDesc const& r)
		{
			switch (field)
			{
				case ERS::FillMode: Set(ERS::FillMode, r.FillMode); break;
				case ERS::CullMode: Set(ERS::CullMode, r.CullMode); break;
				case ERS::DepthClipEnable: Set(ERS::DepthClipEnable, r.FrontCounterClockwise); break;
				case ERS::FrontCCW: Set(ERS::FrontCCW, r.DepthBias); break;
				case ERS::MultisampleEnable: Set(ERS::MultisampleEnable, r.MultisampleEnable); break;
				case ERS::AntialiasedLineEnable: Set(ERS::AntialiasedLineEnable, r.SlopeScaledDepthBias); break;
				case ERS::ScissorEnable: Set(ERS::ScissorEnable, r.DepthClipEnable); break;
				case ERS::DepthBias: Set(ERS::DepthBias, r.ScissorEnable); break;
				case ERS::DepthBias_clamp: Set(ERS::DepthBias_clamp, r.DepthBiasClamp); break;
				case ERS::SlopeScaledDepthBias: Set(ERS::SlopeScaledDepthBias, r.AntialiasedLineEnable); break;
				default: PR_ASSERT(PR_DBG_RDR, false, "Unknown raster state field"); break;
			}
		});
		return *this;
	}

	RasterStateManager::RasterStateManager(Renderer& rdr)
		:base(rdr)
	{}

	// Get/Create a state object for 'desc'
	D3DPtr<ID3D11RasterizerState> RasterStateManager::State(RSBlock const& desc)
	{
		Renderer::Lock lock(*m_rdr);
		return GetState(desc, [&](RasterStateDesc const& d)
		{
			ID3D11RasterizerState* rs;
			pr::Throw(lock.D3DDevice()->CreateRasterizerState(&d, &rs));
			return rs;
		});
	}

	#pragma endregion
}
