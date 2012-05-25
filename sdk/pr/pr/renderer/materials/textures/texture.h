//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_TEXTURE_H
#define PR_RDR_TEXTURE_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/iallocator.h"
#include "pr/renderer/renderstates/renderstate.h"
#include "pr/renderer/materials/textures/texturefilter.h"
#include "pr/renderer/materials/video/video.h"

namespace pr
{
	namespace rdr
	{
		static GUID const TexInfoGUID = {0x506e436e, 0x5a4f, 0x4190, 0x98, 0x43, 0x99, 0x7a, 0x19, 0xa8, 0xd8, 0x69}; // {506E436E-5A4F-4190-9843-997A19A8D869}
		
		// Additional data associated with a texture
		struct TexInfo :D3DXIMAGE_INFO
		{
			pr::rdr::RdrId TexFileId; // An id identifying the file this texture was created from
			pr::uint16     SortId;    // Drawlist sort key component
			bool           Alpha;     // True if the texture contains alpha
			uint           Usage;     //
			D3DPOOL        Pool;      // 
			TexInfo() :D3DXIMAGE_INFO() ,SortId() ,Alpha() ,Usage() ,Pool() {}
		};
		
		// A texture instance.
		// Each time MM.CreateTexture is called, a new one of these objects is allocated.
		// However, the resources associated with this texture may be shared with other Textures.
		struct Texture
			:pr::RefCount<Texture>
			,pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			pr::m4x4                  m_t2s;             // Texture to surface transform
			D3DPtr<IDirect3DTexture9> m_tex;             // The texture resource
			TexInfo                   m_info;            // Texture creation info
			rs::Block                 m_rsb;             // Texture specific render states
			TextureFilter             m_filter;          // Mip,Min,Mag filtering to use with this texture
			TextureAddrMode           m_addr_mode;       // The addressing mode to use with this texture
			RdrId                     m_id;              // Id for this texture in the material managers lookup map
			MaterialManager*          m_mat_mgr;         // The material manager that created this texture
			string32                  m_name;            // Human readable id for the texture
			VideoPtr                  m_video;           // Non-null if this texture is the output of a video
			
			// Textures are created and owned by the material manager.
			// This is so that pointers (i.e. "handles") to pr::rdr::Texture's can
			// be passed out from dlls, to C#, etc.
			Texture();
			
			// Return a surface in the texture
			D3DPtr<IDirect3DSurface9> surf(int level) const;
			
			// Fill a surface within this texture from a file
			void LoadSurfaceFromFile(char const* filename, int level, RECT const* dst_rect, RECT const* src_rect, DWORD filter, D3DCOLOR colour_key) const;
			
			void OnEvent(pr::rdr::Evt_DeviceLost const&);
			void OnEvent(pr::rdr::Evt_DeviceRestored const&);
			
			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Texture>* doomed);
			
		private:
			// Altho probably copyable, don't copy them as that'll mess with the material manager
			Texture(Texture const&);
			void operator =(Texture const&);
		};
		
		// Notes about 'm_video':
		//  The reason for having a pointer to a video object in each texture is so that
		//  normal textures and video textures can be treated exactly the same. The user
		//  never sees the Video object and it is cleaned up automatically when the texture
		//  is cleaned up.
		//  The texture has a refptr to the video, which has a standard pointer back to
		//  the texture object. The video will only output to the d3dtexture if it is valid.
		//  The video is not responsible for managing the texture across device lost/restored.
	}
}

#endif


		

// All textures can have an optional ".info" file.  A texture info file contains renderer
// specific properties for a texture. The filename for a texture info file is the texture
// name with ".info" on the end
//     e.g. MyTexture.tga.info
//
//	Texture info files are in PRScript
//	The parameters are:
//		# 0 = doesn't have any alpha, 1 = does have alpha
//		*Alpha 0
//
//		# Effect "some_effect_id" <- This is provided in a call back function to the client
//		*Effect "MyEffect.fx"
//
//		# EnvironmentMap "environment_map_id" <-
//		# NormalMap "normal_map_id" <-
//		
		//// This is becoming more like a texture instance

		//struct Texture : public pr::chain::link<Texture, texture::RdrTextureChain>
		//{
		//	// Public members
		//	D3DPtr<IDirect3DTexture9> m_texture;
		//	RdrId                     m_texture_id;
		//	pr::uint16                m_sort_id;
		//	rs::Block                 m_render_state;    // Texture specific render states
		//	// Todo: these shouldn't be mutable, see note in material manager
		//	mutable pr::m3x3          m_t2s;             // Texture to surface transform
		//	mutable TextureFilter     m_filter;          // Mip,Min,Mag filtering to use with this texture
		//	mutable TextureAddrMode   m_addr_mode;       // The addressing mode to use with this texture
		//	D3DXIMAGE_INFO            m_info;            // Info about the texture
		//	pr::uint                  m_properties;
		//	TextureProperty           m_prop;
		//	string32                  m_name;
		//	
		//	Texture();
		//	
		//	// Create this texture from raw texture data
		//	// if 'data' is null then the texture is left uninitialised
		//	void Create(D3DPtr<IDirect3DDevice9> d3d_device, RdrId texture_id, void const* data, uint data_size, uint width, uint height, uint mips = 0, uint usage = 0, D3DFORMAT format = D3DFMT_A8R8G8B8, D3DPOOL pool = D3DPOOL_MANAGED);
		//	
		//	// Create this texture from an external file
		//	// In the extended version of this function, setting width/height to zero will use the dimensions of the texture
		//	bool CreateFromFile(D3DPtr<IDirect3DDevice9> d3d_device, RdrId texture_id, const char* filename, uint width, uint height, uint mips = 0, D3DCOLOR colour_key = 0, uint usage = 0, D3DFORMAT format = D3DFMT_UNKNOWN, D3DPOOL pool = D3DPOOL_MANAGED, DWORD filter = D3DX_DEFAULT, DWORD mip_filter = D3DX_DEFAULT);
		//	bool CreateFromFile(D3DPtr<IDirect3DDevice9> d3d_device, RdrId texture_id, const char* filename) { return CreateFromFile(d3d_device, texture_id, filename, 0, 0); }
		//	
		//	// Release the texture resources.
		//	// Note: typically clients shouldn't call this, they would normally create the texture via the material manager and
		//	// so should delete it via the material manager. If they create it themselves though then they should have non-const
		//	// reference to the texture which they can call Release() on.
		//	void Release();
		//	
		//	// Fill a surface within this texture from a file
		//	// This method is const because we allow owners of a Texture const* to call it. Technically this should be called
		//	// through the material manager which can get a non-const pointer to the texture to modify it, but that's a hassle and inefficient.
		//	bool LoadSurfaceFromFile(char const* filename, int level = 0, RECT const* dst_rect = 0, RECT const* src_rect = 0, DWORD filter = D3DX_DEFAULT, D3DCOLOR colour_key = 0) const;
		//	
		//	// Return a surface in the texture
		//	D3DPtr<IDirect3DSurface9> Surface(int level) const;
		//	
		//	// Boolean test for a valid texture
		//	operator bool() const { return m_texture != 0; }
		//	
		//private:
		//	void LoadTextureInfo(const char* filename);
		//	bool HasProperty(ETextureProperty prop) const { return (m_properties & prop) != 0; }
		//	void SetAsAdditiveAlpha(bool on);
		//};

					
			//// Set this texture as additive alpha blend
			//void pr::rdr::Texture::SetAsAdditiveAlpha(bool on)
			//{
			//	m_properties |= ETextureProperty_Alpha;
			//	m_prop.m_alpha = on;
			//	SetAlphaRenderStates(m_render_state, on);
			//}
			// Fill a surface within this texture from a file
		//	// This method is const because we allow owners of a Texture const* to call it. Technically this should be called
		//	// through the material manager which can get a non-const pointer to the texture to modify it, but that's a hassle and inefficient.
		//	bool LoadSurfaceFromFile(char const* filename, int level = 0, RECT const* dst_rect = 0, RECT const* src_rect = 0, DWORD filter = D3DX_DEFAULT, D3DCOLOR colour_key = 0) const;
		
		//	
		//	
		//	void LoadTextureInfo(const char* filename);
		//	bool HasProperty(ETextureProperty prop) const { return (m_properties & prop) != 0; }
		//	void SetAsAdditiveAlpha(bool on);
