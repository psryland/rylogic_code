//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

//	A class to manage the loading/updating and access to materials/textures

#pragma once
#ifndef PR_RDR_MATERIAL_MANAGER2_H
#define PR_RDR_MATERIAL_MANAGER2_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/projectconfiguration.h"
#include "pr/renderer/materials/material.h"

namespace pr
{
	namespace rdr
	{
		// What the Material Manager does:
		// - Manages the memory allocation for Texture/Effect objects
		// - On device reset called "OnDeviceReset()" for each effect
		// - Assigns a 16bit sort id for ordering within the drawlist
		// - Ensures textures from files are loaded into memory only once
		// - Holds the effect pool for shared values between effects
		// - Creates stock textures/effects
		// - Allows others to access loaded textures/effects
		//
		// Notes:
		// - Textures are allocated by calls to CreateTexture and returned as reference counted
		//   pointers. When the last reference to a texture is destroyed the texture is released.
		class MaterialManager
			:public pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,public pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			friend struct pr::rdr::Effect;
			friend struct pr::rdr::Texture;
			
			IAllocator&              m_allocator;         // Renderer object factor
			D3DPtr<IDirect3DDevice9> m_d3d_device;        // D3D device
			D3DPtr<ID3DXEffectPool>  m_effect_pool;       // The effect pool for shared effect variables
			TEffectLookup            m_effect_lookup;     // A map from effect_id to effect pointer
			TTextureLookup           m_texture_lookup;    // A map from texture id to texture data
			TTexFileLookup           m_texfile_lookup;    // A map from hashed filepath to IDirect3DTexture9 pointer
			pr::uint16               m_effect_sortid;     // A rolling count used to represent the effect in the sort key
			pr::uint16               m_texture_sortid;    // A rolling count used to represent the texture in the sort key
			EffectPtr                m_smap_effect;       // The effect used to generate shadow depth maps
			
			MaterialManager(MaterialManager const&); // no copying
			MaterialManager& operator = (MaterialManager const&);
			
			void OnEvent(pr::rdr::Evt_DeviceLost const&);
			void OnEvent(pr::rdr::Evt_DeviceRestored const&);
			
			void CreateStockEffects();
			void CreateStockTextures();
			void DeleteTexture(pr::rdr::Texture const* tex);
			void DeleteEffect (pr::rdr::Effect const* effect);
			
		public:
			MaterialManager(IAllocator& allocator, D3DPtr<IDirect3DDevice9> d3d_device, TextureFilter filter);
			~MaterialManager();
			
			// Get a material suitable for a given geometry type
			pr::rdr::Material GetMaterial(pr::GeomType geom_type) { pr::rdr::Material m = {GetEffect(geom_type)}; return m; }
			
			// Effects ****************************************
			
			EffectPtr GetShadowCastEffect() { return m_smap_effect; }
			
			// Create an effect instance.
			// 'id' is the id to assign to this effect, use AutoId if you don't care.
			// If the id matches an effect that has already been created you will get a pointer to that effect
			// 'data' is data to initialise the texture with. Note: it must have appropriate stride and length.
			// If 'data' or 'data_size' is 0, the texture is left uninitialised.
			// Throws if creation fails. On success returns a pointer to the created texture.
			pr::rdr::EffectPtr pr::rdr::MaterialManager::CreateEffect(RdrId id, pr::rdr::effect::Desc const& desc, pr::rdr::rs::Block const* render_states = 0);
			
			// Get a pointer to a resource or find a resource. 'Find' returns 0 if the resource is not found
			EffectPtr FindEffect(RdrId id) const { TEffectLookup::const_iterator i = m_effect_lookup.find(id); return i != m_effect_lookup.end() ? i->second : 0; }
			
			// Return an effect suitable for the provided geom_type
			EffectPtr GetEffect(pr::GeomType geom_type);
			
			// Textures ****************************************
			
			// Create a texture instance.
			// 'id' is the id to assign to this texture, use AutoId if you don't care.
			// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing texture.
			// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
			//  a new texture instance that points to this d3d texture.
			// 'data' is data to initialise the texture with. Note: it must have appropriate stride and length.
			// If 'data' or 'data_size' is 0, the texture is left uninitialised.
			// Throws if creation fails. On success returns a pointer to the created texture.
			TexturePtr CreateTexture(RdrId id, void const* data, uint data_size, uint width, uint height, uint mips = 0, uint usage = 0, D3DFORMAT format = D3DFMT_A8R8G8B8, D3DPOOL pool = D3DPOOL_MANAGED);
			
			// Create a texture instance from file
			// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
			// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing texture.
			// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
			//  a new texture instance that points to this d3d texture.
			// If width/height are 0 the dimensions of the image file are used as the texture size.
			// Throws if creation fails. On success returns a pointer to the created texture.
			TexturePtr CreateTexture(RdrId id, char const* filepath, uint width = 0, uint height = 0, uint mips = 0, D3DCOLOR colour_key = 0, DWORD filter = D3DX_DEFAULT, DWORD mip_filter = D3DX_DEFAULT, D3DFORMAT format = D3DFMT_UNKNOWN, uint usage = 0, D3DPOOL pool = D3DPOOL_MANAGED);
			
			// Create a video texture from file
			TexturePtr CreateVideoTexture(RdrId id, char const* filepath, uint width = 0, uint height = 0);
			
			// Return info about a texture from its file
			EResult::Type TextureInfo(char const* tex_filepath, D3DXIMAGE_INFO& info);
			
			// Return a pointer to an existing texture
			TexturePtr FindTexture(RdrId id) const { TTextureLookup::const_iterator i = m_texture_lookup.find(id); return i != m_texture_lookup.end() ? i->second : 0; }
		};
	}
}

#endif


//TODO: pr::rdr::Texture contains render state, filter states, and addressing modes for the texture
// This means clients may want to load multiple copies of the same texture file, but have different states for it.
// We don't want to load the texture file multiple times, but we want to allow multiple copies of the pr::rdr::Texture
// objects.
//IDEA: get rid of the ability for users to choose the RdrId assigned to each texture, the material manager can assign
// them. The texture lookup table should be for IDirect3DTexture9 objects not pr::rdr::Textures

			//// Create effects - throws if creation fails
			//void CreateEffect(effect::Desc const& effect_desc, rdr::Effect2 const*& effect_out, rs::Block const* render_states = 0);
			//void CreateEffect(effect::Desc const& effect_desc) { rdr::Effect2 const* effect_out; return CreateEffect(effect_desc, effect_out); }
			//void DeleteEffect(rdr::Effect2 const* effect);

			//// Create textures - throws if creation fails
			//void           CreateTexture(                  void const* data, uint data_size, uint width, uint height, Texture const*& texture_out, uint mips = 0, uint usage = 0, D3DFORMAT format = D3DFMT_A8R8G8B8, D3DPOOL pool = D3DPOOL_MANAGED) { return CreateTexture(pr::rdr::AutoId, data, data_size, width, height, texture_out, mips, usage, format, pool); }
			//void           CreateTexture(RdrId texture_id, void const* data, uint data_size, uint width, uint height,                              uint mips = 0, uint usage = 0, D3DFORMAT format = D3DFMT_A8R8G8B8, D3DPOOL pool = D3DPOOL_MANAGED) { Texture const* texture_out; return CreateTexture(texture_id, data, data_size, width, height, texture_out, mips, usage, format, pool); }
			//void           CreateTexture(RdrId texture_id, void const* data, uint data_size, uint width, uint height, Texture const*& texture_out, uint mips = 0, uint usage = 0, D3DFORMAT format = D3DFMT_A8R8G8B8, D3DPOOL pool = D3DPOOL_MANAGED);
			//void           DeleteTexture(rdr::Texture const* texture);
			//
			//// Load textures from files
			//EResult::Type  LoadTexture(char const* texture_filename, Texture const*& texture_out);
			//EResult::Type  LoadTexture(char const* texture_filename) { Texture const* texture_out; return LoadTexture(texture_filename, texture_out); }
			//EResult::Type  LoadTexture(const char* texture_filename, Texture const*& texture_out, uint width, uint height, uint mips = 0, D3DCOLOR colour_key = 0, uint usage = 0, D3DFORMAT format = D3DFMT_UNKNOWN, D3DPOOL pool = D3DPOOL_MANAGED, DWORD filter = D3DX_DEFAULT, DWORD mip_filter = D3DX_DEFAULT);
			//
