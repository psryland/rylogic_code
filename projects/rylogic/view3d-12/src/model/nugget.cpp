//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	// NuggetData ctor
	NuggetData::NuggetData(ETopo topo, EGeom geom, Range vrange, Range irange)
		:m_topo(topo)
		,m_geom(geom)
		,m_shaders()
		,m_pso()
		,m_tex_diffuse()
		,m_sam_diffuse()
		,m_tint(Colour32White)
		,m_sort_key(ESortGroup::Default)
		,m_relative_reflectivity(1)
		,m_nflags(ENuggetFlag::None)
		,m_vrange(vrange)
		,m_irange(irange)
	{}

	// Nugget ctor
	Nugget::Nugget(NuggetData const& ndata, Model* model, RdrId id)
		:NuggetData(ndata)
		,m_model(model)
		,m_prim_count(PrimCount(ndata.m_irange.size(), ndata.m_topo))
		,m_nuggets()
		,m_id(id)
	{
		// Fixed the initial pipe state overrides
		m_pso.m_fixed = m_pso.count();

		// Enable alpha if the geometry or the diffuse texture map contains alpha
		Alpha(RequiresAlpha());
	}
	Nugget::~Nugget()
	{
		// Clean up dependent nuggets
		while (!m_nuggets.empty())
			res_mgr().Delete(&m_nuggets.front());
	}

	// Renderer access
	Renderer& Nugget::rdr() const
	{
		return m_model->rdr();
	}
	ResourceManager& Nugget::res_mgr() const
	{
		return m_model->res_mgr();
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
		m_pso.Clear<EPipeState::BlendState0>();
		m_pso.Clear<EPipeState::DepthWriteMask>();
		m_pso.Clear<EPipeState::CullMode>();

		// Find and delete the dependent alpha nugget
		DeleteDependent([](auto& nug) { return nug.m_id == AlphaNuggetId; });

		if (enable)
		{
			// Set this nugget to do the front faces
			m_sort_key.Group(ESortGroup::AlphaFront);
			m_pso.Set<EPipeState::BlendState0>({ // D3D12_RENDER_TARGET_BLEND_DESC 
				.BlendEnable = TRUE,
				.LogicOpEnable = FALSE,
				.SrcBlend = D3D12_BLEND_SRC_ALPHA,
				.DestBlend = D3D12_BLEND_INV_SRC_ALPHA,
				.BlendOp = D3D12_BLEND_OP_ADD,
				.SrcBlendAlpha = D3D12_BLEND_SRC_ALPHA,
				.DestBlendAlpha = D3D12_BLEND_DEST_ALPHA,
				.BlendOpAlpha = D3D12_BLEND_OP_MAX,
				.LogicOp = D3D12_LOGIC_OP_CLEAR,
				.RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL,
			});
			m_pso.Set<EPipeState::DepthWriteMask>(D3D12_DEPTH_WRITE_MASK_ZERO);
			m_pso.Set<EPipeState::CullMode>(D3D12_CULL_MODE_BACK);

			// Create a dependent nugget to do the back faces
			if (m_model != nullptr)
			{
				auto& nug = *m_model->res_mgr().CreateNugget(*this, m_model, AlphaNuggetId);
				nug.m_sort_key.Group(ESortGroup::AlphaBack);
				nug.m_pso.Set<EPipeState::CullMode>(D3D12_CULL_MODE_FRONT);
				m_nuggets.push_back(nug);
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

	// True if this nugget should be rendered
	bool Nugget::Visible() const
	{
		// If the object cull mode does not match the pipe state cull mode then skip.
		// This makes back/front face culling work with Alpha nuggets because render state
		// culling mode has priority over the nugget cull mode.
		#if 0 // todo - need a better way, rather than storing cull mode twice in the nugget. Maybe pass in the cull mode for the alpha pass
		if (CullMode() != ECullMode::None &&
			CullMode() != ECullMode::Default &&
			CullMode() != static_cast<ECullMode>(m_rsb.Desc().CullMode))
			return false;
		#endif

		return true;
	}

	// Delete this nugget, removing it from the owning model
	void Nugget::Delete()
	{
		res_mgr().Delete(this);
	}
}
