//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/models/model_manager.h"

namespace pr
{
	namespace rdr
	{
		NuggetData::NuggetData(EPrim topo, EGeom geom, ShaderMap* smap, Range vrange, Range irange)
			:m_topo(topo)
			,m_geom(geom)
			,m_smap(smap ? *smap : ShaderMap())
			,m_tex_diffuse()
			,m_bsb()
			,m_dsb()
			,m_rsb()
			,m_sort_key()
			,m_vrange(vrange)
			,m_irange(irange)
		{}

		NuggetProps::NuggetProps(EPrim topo, EGeom geom, ShaderMap* smap, Range vrange, Range irange)
			:NuggetData(topo, geom, smap, vrange, irange)
			,m_range_overlaps(false)
			,m_has_alpha(false)
		{}
		NuggetProps::NuggetProps(NuggetData const& data)
			:NuggetData(data)
			,m_range_overlaps(false)
			,m_has_alpha(false)
		{}

		Nugget::Nugget(NuggetProps const& props, ModelBufferPtr& model_buffer, Model* owner)
			:NuggetData(props)
			,m_model_buffer(model_buffer)
			,m_prim_count(PrimCount(props.m_irange.size(), props.m_topo))
			,m_owner(owner)
			,m_nuggets()
			,m_alpha_enabled(false)
		{
			Alpha(props.m_has_alpha);
		}
		Nugget::~Nugget()
		{
			while (!m_nuggets.empty())
				m_model_buffer->m_mdl_mgr->Delete(&m_nuggets.front());
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

		// Enable/Disable alpha for this nugget
		bool Nugget::Alpha() const
		{
			return m_alpha_enabled;
		}
		void Nugget::Alpha(bool enable)
		{
			if (m_alpha_enabled == enable) return;
			m_alpha_enabled = enable;
			if (enable)
			{
				// Set this nugget to do the front faces
				m_sort_key.Group(ESortGroup::AlphaFront);
				m_bsb.Set(EBS::BlendEnable    ,TRUE                      ,0);
				m_bsb.Set(EBS::BlendOp        ,D3D11_BLEND_OP_ADD        ,0);
				m_bsb.Set(EBS::SrcBlend       ,D3D11_BLEND_SRC_ALPHA     ,0);
				m_bsb.Set(EBS::DestBlend      ,D3D11_BLEND_INV_SRC_ALPHA ,0);
				m_dsb.Set(EDS::DepthWriteMask ,D3D11_DEPTH_WRITE_MASK_ZERO);
				m_rsb.Set(ERS::CullMode       ,D3D11_CULL_BACK);

				// Create a dependent nugget to do the back faces
				NuggetProps props = *this;
				auto& nug = *m_model_buffer->m_mdl_mgr->m_alex_nugget.New(props, m_model_buffer, m_owner);
				nug.m_sort_key.Group(ESortGroup::AlphaBack);
				nug.m_bsb.Set(EBS::BlendEnable    ,TRUE                      ,0);
				nug.m_bsb.Set(EBS::BlendOp        ,D3D11_BLEND_OP_ADD        ,0);
				nug.m_bsb.Set(EBS::SrcBlend       ,D3D11_BLEND_SRC_ALPHA     ,0);
				nug.m_bsb.Set(EBS::DestBlend      ,D3D11_BLEND_INV_SRC_ALPHA ,0);
				nug.m_dsb.Set(EDS::DepthWriteMask ,D3D11_DEPTH_WRITE_MASK_ZERO);
				nug.m_rsb.Set(ERS::CullMode       ,D3D11_CULL_FRONT);
				m_nuggets.push_back(nug);
			}
			else
			{
				// Clear the alpha blending states 
				m_sort_key.Group(ESortGroup::Default);
				m_bsb.Clear(EBS::BlendEnable ,0);
				m_bsb.Clear(EBS::BlendOp     ,0);
				m_bsb.Clear(EBS::SrcBlend    ,0);
				m_bsb.Clear(EBS::DestBlend   ,0);
				m_dsb.Clear(EDS::DepthWriteMask);
				m_rsb.Clear(ERS::CullMode);

				// Find and delete the dependent nugget
				auto iter = pr::find_if(m_nuggets, [](auto& nug){ return nug.m_sort_key.Group() == ESortGroup::AlphaBack; });
				if (iter != m_nuggets.end())
					m_model_buffer->m_mdl_mgr->Delete(&*iter);
			}
		}
	}
}
