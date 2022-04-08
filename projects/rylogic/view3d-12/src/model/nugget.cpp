//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	NuggetData::NuggetData(ETopo topo, EGeom geom, ShaderMap* smap, Range vrange, Range irange)
		:m_topo(topo)
		,m_geom(geom)
		//,m_smap(smap ? *smap : ShaderMap())
		,m_tex_diffuse()
		,m_tint(Colour32White)
		//,m_bsb()
		//,m_dsb()
		//,m_rsb()
		,m_sort_key(ESortGroup::Default)
		,m_relative_reflectivity(1)
		,m_nflags(ENuggetFlag::None)
		,m_vrange(vrange)
		,m_irange(irange)
	{
		//todo
		(void)smap;
	}

	Nugget::Nugget(NuggetData const& ndata, Model* model)
		:NuggetData(ndata)
		,m_model(model)
		,m_prim_count(PrimCount(ndata.m_irange.size(), ndata.m_topo))
		,m_nuggets()
		,m_fill_mode(EFillMode::Default)
		,m_cull_mode(ECullMode::Default)
		,m_alpha_enabled()
		,m_id()
	{
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
		return
			AnySet(m_nflags, ENuggetFlag::GeometryHasAlpha | ENuggetFlag::TintHasAlpha) ||
			(m_tex_diffuse != nullptr ? m_tex_diffuse->m_has_alpha : false);
	}

	// Set the alpha state based on the current has_alpha flags
	void Nugget::UpdateAlphaStates()
	{
		Alpha(RequiresAlpha());
	}

	// Enable/Disable alpha for this nugget
	void Nugget::Alpha(bool enable)
	{
		if (m_alpha_enabled == enable) return;
		m_alpha_enabled = enable;
		#if 0 // todo
		// Clear the alpha blending states 
		m_sort_key.Group(ESortGroup::Default);
		m_bsb.Clear(EBS::BlendEnable, 0);
		m_bsb.Clear(EBS::BlendOp, 0);
		m_bsb.Clear(EBS::SrcBlend, 0);
		m_bsb.Clear(EBS::DestBlend, 0);
		m_bsb.Clear(EBS::BlendOpAlpha, 0);
		m_bsb.Clear(EBS::SrcBlendAlpha, 0);
		m_bsb.Clear(EBS::DestBlendAlpha, 0);
		m_dsb.Clear(EDS::DepthWriteMask);
		m_rsb.Clear(ERS::CullMode);
		#endif
		// Restore the fill and cull modes
		FillMode(FillMode());
		CullMode(CullMode());

		// Find and delete the dependent alpha nugget
		DeleteDependent([](auto& nug) { return nug.m_id == AlphaNuggetId; });

		if (enable)
		{
			#if 0 // todo
			// Set this nugget to do the front faces
			m_sort_key.Group(ESortGroup::AlphaFront);
			m_bsb.Set(EBS::BlendEnable, TRUE, 0);
			m_bsb.Set(EBS::BlendOp, D3D11_BLEND_OP_ADD, 0);
			m_bsb.Set(EBS::SrcBlend, D3D11_BLEND_SRC_ALPHA, 0);
			m_bsb.Set(EBS::DestBlend, D3D11_BLEND_INV_SRC_ALPHA, 0);
			m_bsb.Set(EBS::BlendOpAlpha, D3D11_BLEND_OP_MAX, 0);
			m_bsb.Set(EBS::SrcBlendAlpha, D3D11_BLEND_SRC_ALPHA, 0);
			m_bsb.Set(EBS::DestBlendAlpha, D3D11_BLEND_DEST_ALPHA, 0);
			m_dsb.Set(EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
			m_rsb.Set(ERS::CullMode, D3D11_CULL_BACK);
			m_cull_mode = ECullMode::Back;

			// Create a dependent nugget to do the back faces
			if (m_owner != nullptr)
			{
				auto& nug = *mdl_mgr().CreateNugget(*this, m_model_buffer, nullptr);
				nug.m_sort_key.Group(ESortGroup::AlphaBack);
				nug.m_rsb.Set(ERS::CullMode, D3D11_CULL_FRONT);
				nug.m_cull_mode = ECullMode::Front;
				nug.m_owner = m_owner;
				nug.m_id = AlphaNuggetId;
				m_nuggets.push_back(nug);
			}
			#endif
		}
	}

	// Get/Set the fill mode for this nugget
	EFillMode Nugget::FillMode() const
	{
		return m_fill_mode;
	}
	void Nugget::FillMode(EFillMode fill_mode)
	{
		if (m_fill_mode == fill_mode) return;
		m_fill_mode = fill_mode;
		#if 0 // todo
		switch (m_fill_mode)
		{
		case EFillMode::Default:
		case EFillMode::Solid:     m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		case EFillMode::Wireframe: m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME); break;
		case EFillMode::SolidWire: m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		case EFillMode::Points:    m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		default: throw std::runtime_error(Fmt("Unknown fill mode: %d", fill_mode));
		}
		#endif

		// Apply recursively
		for (auto& nug : m_nuggets)
			nug.FillMode(fill_mode);
	}

	// Get/Set the cull mode for this nugget
	ECullMode Nugget::CullMode() const
	{
		return m_cull_mode;
	}
	void Nugget::CullMode(ECullMode cull_mode)
	{
		if (m_cull_mode == cull_mode) return;
		m_cull_mode = cull_mode;

		#if 0 // todo
		// Alpha rendering nuggets already have the cull mode set.
		if (!m_alpha_enabled && m_id != AlphaNuggetId)
		{
			switch (m_cull_mode)
			{
			case ECullMode::Default:
			case ECullMode::None:  m_rsb.Set(ERS::CullMode, D3D11_CULL_NONE); break;
			case ECullMode::Back:  m_rsb.Set(ERS::CullMode, D3D11_CULL_BACK); break;
			case ECullMode::Front: m_rsb.Set(ERS::CullMode, D3D11_CULL_FRONT); break;
			default: throw std::runtime_error(Fmt("Unknown cull mode: %d", cull_mode));
			}
		}
		#endif

		// Apply recursively
		for (auto& nug : m_nuggets)
			nug.CullMode(m_cull_mode);
	}

	// True if this nugget should be rendered
	bool Nugget::Visible() const
	{
		#if 0 // todo
		// If the object cull mode does not match the render state cull mode then skip.
		// This makes back/front face culling work with Alpha nuggets because render state
		// culling mode has priority over the nugget cull mode.
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
