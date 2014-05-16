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
		Texture2D::Texture2D(TextureManager* mgr, D3DPtr<ID3D11Texture2D> tex, D3DPtr<ID3D11ShaderResourceView> srv, SamplerDesc const& sam_desc, SortKeyId sort_id)
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
			if (m_srv == nullptr)
			{
				TextureDesc tdesc;
				tex->GetDesc(&tdesc);
				ShaderResViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
				srvdesc.Texture2D.MipLevels = tdesc.MipLevels;
				pr::Throw(m_mgr->m_device->CreateShaderResourceView(tex.m_ptr, &srvdesc, &srv.m_ptr));
			}
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
			TexDesc(src, tdesc, false, false, srvdesc);
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
		// 'all_instances' - if true, all Texture2D objects that refer to the same underlying
		//  dx texture get updated as well. If false, then this texture becomes a unique instance
		//  and 'm_id' is changed.
		// 'perserve' - if true, the content of the current texture is stretch blted to the new texture
		//  if possible. If not possible, an exception is thrown
		// 'srvdesc' - if not null, causes the new shader resourve view to be created using this description
		void Texture2D::TexDesc(Image const& src, TextureDesc const& tdesc, bool all_instances, bool preserve, ShaderResViewDesc const* srvdesc)
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
				{
					pr::Throw(DirectX::GenerateMipMaps(img, DirectX::TEX_FILTER_FANT, tdesc.MipLevels, scratch));
				}
				else
				{
					scratch.InitializeFromImage(img, false);
				}

				D3DPtr<ID3D11Resource> res;
				pr::Throw(DirectX::CreateTexture(m_mgr->m_device.m_ptr, scratch.GetImages(), scratch.GetImageCount(), scratch.GetMetadata(), &res.m_ptr));
				pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
				
				if (srvdesc)
					pr::Throw(m_mgr->m_device->CreateShaderResourceView(tex.m_ptr, srvdesc, &srv.m_ptr));
				else
					pr::Throw(DirectX::CreateShaderResourceView(m_mgr->m_device.m_ptr, scratch.GetImages(), scratch.GetImageCount(), scratch.GetMetadata(), &srv.m_ptr));
			}
			else
			{
				pr::Throw(m_mgr->m_device->CreateTexture2D(&tdesc, nullptr, &tex.m_ptr));
				pr::Throw(m_mgr->m_device->CreateShaderResourceView(tex.m_ptr, srvdesc, &srv.m_ptr));
			}

			// Copy the surface data from the existing texture
			if (preserve && m_tex != nullptr)
			{
				// Note: it might be tempting to do this but the effect would look the same
				// as just using the old texture, i.e. stretched. There is no stretch copy in
				// dx10+. If you do decide to support this, this is the process that's needed:
				//  - Make a render target texture of the same format as the new texture,
				//  - Use a shader set up for rendering one texture into another,
				//  - Push render states,
				//  - call dc->Draw()
				//  - Restore render states
				TextureDesc existing_desc; m_tex->GetDesc(&existing_desc);
				if (existing_desc.Width != tdesc.Width || existing_desc.Height != tdesc.Height)
					throw std::exception("Cannot preserve content of resized textures");

				// Copy unscaled content from 
				D3DPtr<ID3D11DeviceContext> dc;
				m_mgr->m_device->GetImmediateContext(&dc.m_ptr);
				if (existing_desc.SampleDesc == tdesc.SampleDesc)
				{
					dc->CopyResource(tex.m_ptr, m_tex.m_ptr);
				}
				else
				{
					throw std::exception("not implemented");
					//for (UINT slice = 0; slice != existing_desc.MipLevels; ++slice)
					//	for (UINT arr = 0; arr != existing_desc.ArraySize; ++arr)
					//		dc->ResolveSubresource(tex.m_ptr, D3D11CalcSubresource(slice, arr, existing_desc.MipLevels);
					
				}
			}

			// Update the texture and optionally all instances of the same dx texture
			m_mgr->ReplaceTexture(*this, tex, srv, all_instances);
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

		// Resize this texture to 'size' optionally applying the resize to all instances of this
		// texture and optionally preserving the current content of the texture
		void Texture2D::Resize(size_t width, size_t height, bool all_instances, bool preserve)
		{
			TextureDesc tdesc;
			m_tex->GetDesc(&tdesc);
			tdesc.Width = checked_cast<UINT>(width);
			tdesc.Height = checked_cast<UINT>(height);
			TexDesc(Image(), tdesc, all_instances, preserve);
		}

		// Get the GDI dc from the surface
		HDC Texture2D::GetDC()
		{
			HDC dc;
			D3DPtr<IDXGISurface1> surf;
			pr::Throw(m_tex->QueryInterface(__uuidof(IDXGISurface1), (void **)&surf.m_ptr));
			pr::Throw(surf->GetDC(TRUE, &dc), "GetDC can only be called for textures that were created with the D3D11_RESOURCE_MISC_GDI_COMPATIBLE flag");
			return dc;
		}

		// Release the GDI dc from the surface
		void Texture2D::ReleaseDC()
		{
			D3DPtr<IDXGISurface1> surf;
			pr::Throw(m_tex->QueryInterface(__uuidof(IDXGISurface1), (void **)&surf.m_ptr));
			pr::Throw(surf->ReleaseDC(nullptr));
			// Note: the main RT must be restored once all ReleaseDC's have been called
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