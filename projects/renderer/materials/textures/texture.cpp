//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
	
#include "renderer/utility/stdafx.h"
#include "pr/renderer/materials/textures/texture.h"
#include "pr/renderer/materials/material_manager.h"
#include "pr/renderer/utility/globalfunctions.h"
	
using namespace pr;
using namespace pr::rdr;
	
// Textures are created and owned by the material manager.
// This is so that pointers (i.e. "handles") to pr::rdr::Texture's can
// be passed out from dlls, to C#, etc.
pr::rdr::Texture::Texture()
:m_t2s(pr::m4x4Identity)
,m_tex(0)
,m_info()
,m_rsb()
,m_filter()
,m_addr_mode()
,m_id()
,m_mat_mgr()
,m_name()
,m_video()
{}
	
// Return a surface in the texture
D3DPtr<IDirect3DSurface9> pr::rdr::Texture::surf(int level) const
{
	PR_ASSERT(PR_DBG_RDR, m_tex != 0, "texture is null, cannot get surface");
	D3DPtr<IDirect3DSurface9> surf = 0;
	pr::Throw(m_tex->GetSurfaceLevel(level, &surf.m_ptr));
	return surf;
}

// Fill a surface within this texture from a file
void pr::rdr::Texture::LoadSurfaceFromFile(char const* filename, int level, RECT const* dst_rect, RECT const* src_rect, DWORD filter, D3DCOLOR colour_key) const
{
	pr::Throw(D3DXLoadSurfaceFromFile(surf(level).m_ptr, 0, dst_rect, filename, src_rect, filter, colour_key, 0));
}

// Handle device lost/restored.
// Most textures will be created as D3DPOOL_DEFAULT in which case we can ignore device lost.
void pr::rdr::Texture::OnEvent(pr::rdr::Evt_DeviceLost const&)
{
	if (m_info.Pool == D3DPOOL_MANAGED) return;
	PR_ASSERT(PR_DBG_RDR, m_tex.RefCount() == 1, "References to this texture still exist");
	m_tex = 0;
}
void pr::rdr::Texture::OnEvent(pr::rdr::Evt_DeviceRestored const& e)
{
	if (m_info.Pool == D3DPOOL_MANAGED) return;
	
	// Recreate the texture, the texture data will be lost however
	Throw(e.m_d3d_device->CreateTexture(m_info.Width, m_info.Height, m_info.MipLevels, m_info.Usage, m_info.Format, m_info.Pool, &m_tex.m_ptr, 0));
}

// Refcounting cleanup function
void pr::rdr::Texture::RefCountZero(pr::RefCount<Texture>* doomed)
{
	pr::rdr::Texture* tex = static_cast<pr::rdr::Texture*>(doomed);
	tex->m_mat_mgr->DeleteTexture(tex);
}

//// Constructor
//pr::rdr::Texture::Texture()
//:m_texture      (0)
//,m_texture_id   (0)
//,m_sort_id      (0)
//,m_render_state ()
//,m_t2s          (pr::m3x3Identity)
//,m_filter       ()
//,m_addr_mode    ()
//,m_info         ()
//,m_properties   (0)
//,m_prop         ()
//,m_name         ()
//{}
//	
//// Create this texture from raw texture data
//// if 'data' is null or data_size == 0 or width*height == 0 then the texture is left uninitialised
//// Note: data is expected to have the correct stride
//void pr::rdr::Texture::Create(D3DPtr<IDirect3DDevice9> d3d_device, RdrId texture_id, void const* data, uint data_size, uint width, uint height, uint mips, uint usage, D3DFORMAT format, D3DPOOL pool)
//{
//	// Release the old texture
//	Release();
//	
//	// Create the texture
//	HRESULT res = d3d_device->CreateTexture(width, height, mips, usage, format, pool, &m_texture.m_ptr, 0);
//	if (Failed(res))
//	{
//		m_texture = 0;
//		m_properties = 0;
//
//		char const* msg = FmtS(
//			"Failed to create texture id: %8.8x\n"
//			"HResult: %s\n"
//			,texture_id
//			,pr::HrMsg(res).c_str());
//		PR_ASSERT(PR_DBG_RDR, false, msg);
//		throw RdrException(EResult::LoadTextureFailed, msg);
//	}
//
//	// If data is provided, copy it to the texture
//	if (data && data_size > 0)
//	{
//		D3DLOCKED_RECT rect;
//		if (Succeeded(m_texture->LockRect(0, &rect, 0, 0)))
//		{
//			PR_ASSERT(PR_DBG_RDR, pr::rdr::BytesPerPixel(format) * width == (uint)rect.Pitch, "Texture pitch mismatch");
//			PR_ASSERT(PR_DBG_RDR, pr::rdr::BytesPerPixel(format) * width * height <= data_size, "Insufficient data provided for texture initialisation");
//			memcpy(rect.pBits, data, rect.Pitch * height);
//			//uint src_pitch = pr::rdr::BytesPerPixel(format) * width;
//			//if (src_pitch == (uint)rect.Pitch)
//			//{
//			//}
//			//else
//			//{
//			//	pr::uint8 const* src = static_cast<pr::uint8 const*>(data);
//			//	pr::uint8*       dst = static_cast<pr::uint8*>      (rect.pBits);
//			//	for (uint i = 0; i != height; ++i, src += src_pitch, dst += rect.Pitch)
//			//		memcpy(dst, src, rect.Pitch);
//			//}
//			m_texture->UnlockRect(0);
//		}
//	}
//	m_texture_id = texture_id;
//	m_info.Width = width;
//	m_info.Height = height;
//	m_info.Depth = 1;
//	m_info.MipLevels = mips;
//	m_info.Format = format;
//	m_info.ImageFileFormat = D3DXIFF_FORCE_DWORD;
//	m_info.ResourceType = D3DRTYPE_TEXTURE;
//}
//	
//// Create this texture from file specifying extra parameters
//bool pr::rdr::Texture::CreateFromFile(D3DPtr<IDirect3DDevice9> d3d_device, RdrId texture_id, const char* filename, uint width, uint height, uint mips, D3DCOLOR colour_key, uint usage, D3DFORMAT format, D3DPOOL pool, DWORD filter, DWORD mip_filter)
//{
//	// Release the old texture
//	Release();
//	
//	if (Failed(D3DXCreateTextureFromFileEx(d3d_device.m_ptr, filename, width, height, mips, usage, format, pool, filter, mip_filter, colour_key, &m_info, 0, &m_texture.m_ptr)))
//	{
//		PR_INFO(PR_DBG_RDR, FmtS("Failed to load texture: %s\n", filename));
//		m_texture = 0;
//		m_properties = 0;
//		return false;
//	}
//	LoadTextureInfo(filename);
//	m_texture_id = texture_id;
//	return true;
//}
//	
//// Release the texture
//void pr::rdr::Texture::Release()
//{
//	m_texture = 0;
//	m_texture_id = 0;
//	m_properties = 0;
//}
//	
//// Fill a surface within this texture from a file
//bool pr::rdr::Texture::LoadSurfaceFromFile(char const* filename, int level, RECT const* dst_rect, RECT const* src_rect, DWORD filter, D3DCOLOR colour_key) const
//{
//	if (Failed(D3DXLoadSurfaceFromFile(Surface(level).m_ptr, 0, dst_rect, filename, src_rect, filter, colour_key, 0)))
//	{
//		PR_INFO(PR_DBG_RDR, FmtS("Failed to load texture surface: %s\n", filename));
//		return false;
//	}
//	return true;
//}
//	
//// Return a surface in the texture
//D3DPtr<IDirect3DSurface9> pr::rdr::Texture::Surface(int level) const
//{
//	D3DPtr<IDirect3DSurface9> surf;
//	Throw(m_texture->GetSurfaceLevel(level, &surf.m_ptr));
//	return surf;
//}
//	
//// Load the info for this texture if it exists
//void pr::rdr::Texture::LoadTextureInfo(const char* filename)
//{
//	// Look for extra texture info
//	std::string info_name = std::string(filename) + ".info";
//	if (pr::filesys::FileExists(info_name))
//	{
//		pr::script::Reader  reader;
//		pr::script::FileSrc src(info_name.c_str());
//		reader.AddSource(src);
//		
//		std::string keyword;
//		while (reader.NextKeywordS(keyword))
//		{
//			// Alpha property
//			if (pr::str::EqualI(keyword, "Alpha"))
//			{
//				m_properties |= ETextureProperty_Alpha;
//				reader.ExtractBool(m_prop.m_alpha);
//			}
//			else
//			{
//				PR_INFO(PR_DBG_RDR, Fmt("Unknown keyword: '%s' in texture info file: '%s'", keyword.c_str(), info_name.c_str()).c_str());
//			}
//		}
//	}
//}
//	
//// Set this texture as additive alpha blend
//void pr::rdr::Texture::SetAsAdditiveAlpha(bool on)
//{
//	m_properties |= ETextureProperty_Alpha;
//	m_prop.m_alpha = on;
//	SetAlphaRenderStates(m_render_state, on);
//}
//	
