//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/models/nugget.h"
#include "pr/view3d/models/model_manager.h"

namespace pr::rdr
{
	// NuggetData *************************************************

	NuggetData::NuggetData(EPrim topo, EGeom geom, ShaderMap* smap, Range vrange, Range irange)
		:m_topo(topo)
		,m_geom(geom)
		,m_smap(smap ? *smap : ShaderMap())
		,m_tex_diffuse()
		,m_tint(Colour32White)
		,m_bsb()
		,m_dsb()
		,m_rsb()
		,m_sort_key(ESortGroup::Default)
		,m_relative_reflectivity(1)
		,m_flags(ENuggetFlag::None)
		,m_vrange(vrange)
		,m_irange(irange)
	{}

	// NuggetProps ************************************************

	NuggetProps::NuggetProps(EPrim topo, EGeom geom, ShaderMap* smap, Range vrange, Range irange)
		:NuggetData(topo, geom, smap, vrange, irange)
		,m_range_overlaps(false)
	{}
	NuggetProps::NuggetProps(NuggetData const& data)
		:NuggetData(data)
		,m_range_overlaps(false)
	{}

	// Nugget *****************************************************

	Nugget::Nugget(NuggetData const& ndata, ModelBuffer* model_buffer, Model* owner)
		:NuggetData(ndata)
		,m_model_buffer(model_buffer)
		,m_prim_count(PrimCount(ndata.m_irange.size(), ndata.m_topo))
		,m_owner(owner)
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
		while (!m_nuggets.empty())
			mdl_mgr().Delete(&m_nuggets.front());
	}

	// Renderer access
	Renderer& Nugget::rdr() const
	{
		return m_model_buffer->rdr();
	}
	ModelManager& Nugget::mdl_mgr() const
	{
		return m_model_buffer->mdl_mgr();
	}

	// True if this nugget should be rendered
	bool Nugget::Visible() const
	{
		if (CullMode() != ECullMode::None && CullMode() != static_cast<ECullMode>(m_rsb.Desc().CullMode))
			return false;

		return true;
	}

	// Return the sort key composed from the base 'm_sort_key' plus any shaders in 'm_smap'
	SortKey Nugget::SortKey(ERenderStep rstep) const
	{
		auto sk = m_sort_key;

		// Set the texture id part of the key if not set already
		if ((sk & SortKey::TextureIdMask) == 0 && m_tex_diffuse != nullptr)
			sk |= (m_tex_diffuse->m_sort_id << SortKey::TextureIdOfs) & SortKey::TextureIdMask;

		// Set the shader id part of the key if not set already
		if ((sk & SortKey::ShaderIdMask) == 0)
		{
			auto shdr_id = 0;
			for (auto& shdr : m_smap[rstep].Enumerate())
			{
				if (shdr == nullptr) continue;
				shdr_id = shdr_id*13 ^ shdr->m_sort_id; // hash the sort ids together
			}
			sk |= (shdr_id << SortKey::ShaderIdOfs) & SortKey::ShaderIdMask;
		}

		return sk;
	}

	// True if this nugget requires alpha blending
	bool Nugget::RequiresAlpha() const
	{
		return
			AnySet(m_flags, ENuggetFlag::GeometryHasAlpha | ENuggetFlag::TintHasAlpha) ||
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

		// Restore the fill and cull modes
		FillMode(FillMode());
		CullMode(CullMode());

		// Find and delete the dependent alpha nugget
		DeleteDependent([](auto& nug) { return nug.m_id == AlphaNuggetId; });

		if (enable)
		{
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
		switch (m_fill_mode)
		{
		case EFillMode::Default:
		case EFillMode::Solid:     m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		case EFillMode::Wireframe: m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME); break;
		case EFillMode::SolidWire: m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		case EFillMode::Points:    m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		default: throw std::runtime_error(Fmt("Unknown fill mode: %d", fill_mode));
		}

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

		// Apply recursively
		for (auto& nug : m_nuggets)
			nug.CullMode(m_cull_mode);
	}

	// Delete this nugget, removing it from the owning model
	void Nugget::Delete()
	{
		m_owner->mdl_mgr().Delete(this);
	}
}
