//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "renderer11/directxtex/directxtex.h"

namespace pr
{
	namespace rdr
	{
		Texture2D::Texture2D(TextureManager* mgr, D3DPtr<ID3D11Texture2D>& tex, D3DPtr<ID3D11ShaderResourceView>& srv, SamplerDesc const& sam_desc, SortKeyId sort_id)
			:m_t2s(pr::m4x4Identity)
			,m_tex(tex)
			,m_srv(srv)
			,m_samp()
			,m_id()
			,m_src_id()
			,m_sort_id(sort_id)
			,m_has_alpha(false)
			,m_mgr(mgr)
			,m_name()
		{
			SamDesc(sam_desc);
		}
		Texture2D::Texture2D(TextureManager* mgr, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, ShaderResViewDesc const* srvdesc)
			:m_t2s(pr::m4x4Identity)
			,m_tex()
			,m_srv()
			,m_samp()
			,m_id()
			,m_src_id()
			,m_sort_id(sort_id)
			,m_has_alpha(false)
			,m_mgr(mgr)
			,m_name()
		{
			TexDesc(src, tdesc, srvdesc);
			SamDesc(sdesc);
		}
		Texture2D::Texture2D(TextureManager* mgr, Texture2D const& existing, SortKeyId sort_id)
			:m_t2s(existing.m_t2s)
			,m_tex(existing.m_tex)
			,m_srv(existing.m_srv)
			,m_samp(existing.m_samp)
			,m_id()
			,m_src_id(existing.m_src_id)
			,m_sort_id(sort_id)
			,m_has_alpha(false)
			,m_mgr(mgr)
			,m_name()
		{}

		// Get the description of the current texture pointed to by 'm_tex'
		TextureDesc Texture2D::TexDesc() const
		{
			TextureDesc desc;
			if (m_tex != nullptr) m_tex->GetDesc(&desc);
			return desc;
		}

		// Set a new texture description and re-create/reinitialise the texture and the srv.
		void Texture2D::TexDesc(Image const& src, TextureDesc const& tdesc, ShaderResViewDesc const* srvdesc)
		{
			D3DPtr<ID3D11Texture2D> tex;
			D3DPtr<ID3D11ShaderResourceView> srv;

			// If initialisation data is provided, see if we need to generate mip maps
			if (src.m_pixels != nullptr)
			{
				DirectX::Image img;
				img.width      = src.m_dim.x;
				img.height     = src.m_dim.y;
				img.format     = src.m_format;
				img.rowPitch   = src.m_pitch.x;
				img.slicePitch = src.m_pitch.y;
				img.pixels     = static_cast<uint8_t*>(const_cast<void*>(src.m_pixels));

				DirectX::ScratchImage scratch;
				if (tdesc.MipLevels != 1)
					pr::Throw(DirectX::GenerateMipMaps(img, DirectX::TEX_FILTER_FANT, tdesc.MipLevels, scratch));
				else
					scratch.Initialize2D(img.format, img.width, img.height, tdesc.ArraySize, 1);

				D3DPtr<ID3D11Resource> res;
				pr::Throw(DirectX::CreateTexture(m_mgr->m_device.m_ptr, scratch.GetImages(), scratch.GetImageCount(), scratch.GetMetadata(), &res.m_ptr));
				pr::Throw(DirectX::CreateShaderResourceView(m_mgr->m_device.m_ptr, scratch.GetImages(), scratch.GetImageCount(), scratch.GetMetadata(), &srv.m_ptr));
				pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
			}
			else
			{
				pr::Throw(m_mgr->m_device->CreateTexture2D(&tdesc, nullptr, &tex.m_ptr));
				pr::Throw(m_mgr->m_device->CreateShaderResourceView(tex.m_ptr, srvdesc, &srv.m_ptr));
			}

			// Exception-safely save the pointers
			m_tex = tex;
			m_srv = srv;
		}

		// Returns a description of the current sampler state pointed to by 'm_samp'
		SamplerDesc Texture2D::SamDesc() const
		{
			SamplerDesc desc;
			if (m_samp != nullptr) m_samp->GetDesc(&desc);
			return desc;
		}
		void Texture2D::SamDesc(SamplerDesc const& desc)
		{
			D3DPtr<ID3D11SamplerState> samp_state;
			pr::Throw(m_mgr->m_device->CreateSamplerState(&desc, &samp_state.m_ptr));
			m_samp = samp_state;
		}

		// Refcounting cleanup function
		void Texture2D::RefCountZero(pr::RefCount<Texture2D>* doomed)
		{
			Texture2D* tex = static_cast<Texture2D*>(doomed);
			tex->Delete();
		}
		void Texture2D::Delete()
		{
			m_mgr->Delete(this);
		}
	}
}