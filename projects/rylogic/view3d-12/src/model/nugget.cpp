//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/resource/resource_store.h"
#include "pr/view3d-12/resource/resource_factory.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	Nugget::Nugget(NuggetDesc const& ndata, Model* model)
		: NuggetDesc(ndata)
		, m_model(model)
		, m_nuggets()
	{
		// Fixed the initial pipe state overrides
		m_pso.m_fixed = m_pso.count();

		// Enable alpha if the geometry or the diffuse texture map contains alpha
		Alpha(RequiresAlpha());
	}
	Nugget::~Nugget()
	{
		// Clean up dependent nuggets
		ResourceStore::Access store(rdr());
		while (!m_nuggets.empty())
			store.Delete(&m_nuggets.front());
	}

	// Renderer access
	Renderer& Nugget::rdr() const
	{
		return m_model->rdr();
	}

	// The number of primitives in this nugget
	int64_t Nugget::PrimCount() const
	{
		return m_irange.empty()
			? rdr12::PrimCount(m_vrange.size(), m_topo)
			: rdr12::PrimCount(m_irange.size(), m_topo);
	}

	// True if this nugget requires alpha blending
	bool Nugget::RequiresAlpha() const
	{
		return AnySet(m_nflags, ENuggetFlag::GeometryHasAlpha | ENuggetFlag::TintHasAlpha | ENuggetFlag::TexDiffuseHasAlpha);
	}

	// Set the alpha state based on the current has_alpha flags
	void Nugget::UpdateAlphaStates()
	{
		Alpha(RequiresAlpha());
	}

	// Enable/Disable alpha for this nugget
	void Nugget::Alpha(bool enable)
	{
		// Can't set alpha on alpha nuggets
		if (m_id == AlphaNuggetId)
			return;
		
		// See if alpha is already enabled
		auto alpha_enabled = m_sort_key.Group() == ESortGroup::AlphaBack || m_sort_key.Group() == ESortGroup::AlphaFront;
		if (alpha_enabled == enable)
			return;

		// Clear the alpha blending states 
		m_sort_key.Group(ESortGroup::Default);
		m_pso.Clear<EPipeState::CullMode>();
		m_pso.Clear<EPipeState::DepthWriteMask>();
		m_pso.Clear<EPipeState::BlendState0>();

		// Find and delete the dependent alpha nugget
		DeleteDependent([](auto& nug) { return nug.m_id == AlphaNuggetId; });

		if (enable)
		{
			// Set this nugget to do the front faces
			m_sort_key.Group(ESortGroup::AlphaFront);
			m_pso.Set<EPipeState::CullMode>(D3D12_CULL_MODE_BACK);
			m_pso.Set<EPipeState::DepthWriteMask>(D3D12_DEPTH_WRITE_MASK_ZERO);
			m_pso.Set<EPipeState::BlendState0>({
				.BlendEnable = TRUE,
				.LogicOpEnable = FALSE,
				.SrcBlend = D3D12_BLEND_SRC_ALPHA,      // Alpha is always drawn over opaque pixels, so the dest
				.DestBlend = D3D12_BLEND_INV_SRC_ALPHA, // alpha is always 1. Blend the RGB using the src alpha.
				.BlendOp = D3D12_BLEND_OP_ADD,          // And write the dest alpha as one
				.SrcBlendAlpha = D3D12_BLEND_ONE,
				.DestBlendAlpha = D3D12_BLEND_ONE,
				.BlendOpAlpha = D3D12_BLEND_OP_MAX,
				.LogicOp = D3D12_LOGIC_OP_CLEAR,
				.RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL,
			});

			// Create a dependent nugget to do the back faces. Only triangle data needs back faces rendered
			if (m_model != nullptr && TopoGroup(m_topo) == ETopoGroup::Triangles)
			{
				auto nug = NuggetDesc(*this)
					.id(AlphaNuggetId)
					.sort_key(ESortGroup::AlphaBack)
					.pso<EPipeState::CullMode>(D3D12_CULL_MODE_FRONT)
					.pso<EPipeState::DepthWriteMask>(D3D12_DEPTH_WRITE_MASK_ZERO)
					.pso<EPipeState::BlendState0>({
						.BlendEnable = TRUE,
						.LogicOpEnable = FALSE,
						.SrcBlend = D3D12_BLEND_SRC_ALPHA,      // Alpha is always drawn over opaque pixels, so the dest
						.DestBlend = D3D12_BLEND_INV_SRC_ALPHA, // alpha is always 1. Blend the RGB using the src alpha.
						.BlendOp = D3D12_BLEND_OP_ADD,          // And write the dest alpha as one
						.SrcBlendAlpha = D3D12_BLEND_ONE,
						.DestBlendAlpha = D3D12_BLEND_ONE,
						.BlendOpAlpha = D3D12_BLEND_OP_MAX,
						.LogicOp = D3D12_LOGIC_OP_CLEAR,
						.RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL,
					});
		
				ResourceFactory factory(rdr());
				m_nuggets.push_back(*factory.CreateNugget(nug, m_model));
			}
		}
	}

	// Get/Set the fill mode for this nugget
	EFillMode Nugget::FillMode() const
	{
		auto fill_mode = m_pso.Find<EPipeState::FillMode>();
		return fill_mode ? s_cast<EFillMode>(*fill_mode) : EFillMode::Default;
	}
	void Nugget::FillMode(EFillMode fill_mode)
	{
		if (FillMode() == fill_mode)
			return;

		m_pso.Clear<EPipeState::FillMode>();
		if (fill_mode != EFillMode::Default)
			m_pso.Set<EPipeState::FillMode>(s_cast<D3D12_FILL_MODE>(fill_mode));

		// Apply recursively
		for (auto& nug : m_nuggets)
			nug.FillMode(fill_mode);
	}

	// Get/Set the cull mode for this nugget
	ECullMode Nugget::CullMode() const
	{
		auto cull_mode = m_pso.Find<EPipeState::CullMode>();
		return cull_mode ? s_cast<ECullMode>(*cull_mode) : ECullMode::Default;
	}
	void Nugget::CullMode(ECullMode cull_mode)
	{
		// Alpha rendering nuggets already have the cull mode set.
		if (m_id == AlphaNuggetId)
			return;

		if (CullMode() == cull_mode)
			return;

		m_pso.Clear<EPipeState::CullMode>();
		if (cull_mode != ECullMode::Default)
			m_pso.Set<EPipeState::CullMode>(s_cast<D3D12_CULL_MODE>(cull_mode));

		// Apply recursively
		for (auto& nug : m_nuggets)
			nug.CullMode(cull_mode);
	}

	// Delete this nugget, removing it from the owning model
	void Nugget::Delete()
	{
		ResourceStore::Access store(rdr());
		store.Delete(this);
	}
}
