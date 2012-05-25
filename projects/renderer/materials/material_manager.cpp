//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/materials/material_manager.h"
#include "pr/renderer/configuration/iallocator.h"
#include "pr/renderer/materials/textures/texture.h"
#include "pr/renderer/materials/textures/texturefilter.h"
#include "pr/renderer/materials/effects/effect.h"
#include "pr/renderer/materials/video/video.h"
#include "pr/renderer/packages/package.h"
#include "pr/renderer/viewport/sortkey.h"
#include "pr/renderer/utility/globalfunctions.h"
#include "pr/renderer/utility/errors.h"
#include <dshow.h> // link to "strmiids.lib" - Don't put this in forward.h because it includes windows headers that break WTL :-/
#include <vmr9.h>
	
using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;
	
namespace pr
{
	namespace rdr
	{
		// Define this as 1 to write files containing the generated shader text
		#define PR_RDR_DUMP_SHADERS 0
		#if PR_RDR_DUMP_SHADERS
		#pragma message (PR_LINK " ************************************************************** PR_RDR_DUMP_SHADERS defined ")
		#endif
			
		// D3DXSHADER_DEBUG
		//  Insert debug filename and line number information during shader compile. 
		// D3DXSHADER_SKIPVALIDATION
		//  Do not validate the generated code against known capabilities and constraints.
		//  This option is recommended only when compiling shaders that are known to work (that is, shaders that have
		//  compiled before, without this option). Shaders are always validated by Microsoft® Direct3D® before they are set to the device. 
		// D3DXSHADER_SKIPOPTIMIZATION
		//  Instructs the compiler to skip optimization steps during code generation. This option is
		//  not recommended unless you are trying to isolate a code problem and you suspect the compiler.
		//  This option is valid only when calling D3DXCompileShader. 
		// D3DXSHADER_USE_LEGACY_D3DX9_31_DLL
		#if PR_DBG_RDR_SHADERS
		DWORD const g_shader_flags = D3DXSHADER_DEBUG | D3DXSHADER_SKIPOPTIMIZATION;
		#else
		DWORD const g_shader_flags = 0;//D3DXSHADER_OPTIMIZATION_LEVEL3;
		#endif
			
		// Create an effect pool
		D3DPtr<ID3DXEffectPool> CreateEffectPool()
		{
			D3DPtr<ID3DXEffectPool> effect_pool;
			Verify(D3DXCreateEffectPool(&effect_pool.m_ptr));
			return effect_pool;
		}
	}
}
	
// Constructor
pr::rdr::MaterialManager::MaterialManager(IAllocator& allocator, D3DPtr<IDirect3DDevice9> d3d_device, TextureFilter filter)
:pr::events::IRecv<pr::rdr::Evt_DeviceLost>(EDeviceResetPriority::MaterialManager)
,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>(EDeviceResetPriority::MaterialManager)
,m_allocator          (allocator)
,m_d3d_device         (d3d_device)
,m_effect_pool        (CreateEffectPool())
,m_effect_lookup      ()
,m_texture_lookup     ()
,m_texfile_lookup     ()
,m_effect_sortid      (0)
,m_texture_sortid     (0)
,m_smap_effect        ()
{
	// Verify enum values
	#if PR_DBG_RDR
	{
		using namespace pr::hash;
		#define LDR_TEXFILTER(name, hashvalue) PR_ASSERT(PR_DBG_RDR, HashLwr(#name) == hashvalue, pr::FmtS("Hash value for "#name" is incorrect. Should be: 0x%08x\n", HashLwr(#name)));
		#define LDR_TEXADDR(name, hashvalue)   PR_ASSERT(PR_DBG_RDR, HashLwr(#name) == hashvalue, pr::FmtS("Hash value for "#name" is incorrect. Should be: 0x%08x\n", HashLwr(#name)));
		#include "pr/renderer/materials/textures/texturefilter.h"
	}
	#endif
	
	// Require a minimum shader model version
	const int vs_min = 0x0200;
	const int ps_min = 0x0300;
	D3DCAPS9 caps; m_d3d_device->GetDeviceCaps(&caps);
	int vs = caps.VertexShaderVersion & 0xFFFF;
	int ps = caps.PixelShaderVersion & 0xFFFF;
	if (vs < vs_min || ps < ps_min)
	{
		char const* msg = FmtS(
			"This D3D device supports vertex shader model %d.%d and pixel shader model %d.%d\n"
			"Minimum supported version is vertex shader model %d.%d and pixel shader model %d.%d"
			,(vs     >> 8) & 0xFF ,(vs     >> 0) & 0xFF
			,(ps     >> 8) & 0xFF ,(ps     >> 0) & 0xFF
			,(vs_min >> 8) & 0xFF ,(vs_min >> 0) & 0xFF
			,(ps_min >> 8) & 0xFF ,(ps_min >> 0) & 0xFF);
		PR_ASSERT(PR_DBG_RDR, false, msg);
		throw RdrException(EResult::UnsupportedShaderModelVersion ,msg);
	}

	// Set the texture sampling filters based on the texture quality in settings
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_ADDRESSU, D3DTADDRESS_CLAMP));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_ADDRESSV, D3DTADDRESS_CLAMP));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_ADDRESSW, D3DTADDRESS_CLAMP));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_BORDERCOLOR, 0));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MAGFILTER, filter.m_mag));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MIPFILTER, filter.m_mip));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MINFILTER, filter.m_min));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MAXANISOTROPY, 1));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MAXMIPLEVEL, 0));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MIPMAPLODBIAS, 0));
	
	// Create the stock effects
	CreateStockEffects();

	// Create a stock textures
	CreateStockTextures();
}
	
// Return the effects and textures to the allocator
pr::rdr::MaterialManager::~MaterialManager()
{
	m_smap_effect = 0;
	
	// Release any leftover effects
	while (!m_effect_lookup.empty())
	{
		TEffectLookup::iterator iter = m_effect_lookup.begin();
		PR_INFO_EXP(PR_DBG_RDR, iter->second->m_ref_count == 1, pr::FmtS("External references to effect: %d - %s still exist!", iter->second->m_id, iter->second->m_name.c_str()));
		iter->second->Release();
	}
	
	// Release any leftover textures
	while (!m_texture_lookup.empty())
	{
		TTextureLookup::iterator iter = m_texture_lookup.begin();
		PR_INFO_EXP(PR_DBG_RDR, iter->second->m_ref_count == 1, pr::FmtS("External references to texture: %d - %s still exist!", iter->second->m_id, iter->second->m_name.c_str()));
		iter->second->Release();
	}
}
	
// Create the stock effects
void pr::rdr::MaterialManager::CreateStockEffects()
{
	using namespace pr::rdr::effect::frag;
	effect::Desc desc(m_d3d_device);
	const int light_count = 1;
	const int caster_count = 1;
	
	// transform and instance colour (V)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTint, desc)->AddRef();
	
	// transform, instance colour, and per vertex colours (VC)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(PVC(PVC::EStyle_PVC_x_Diff)               );
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTintPvc, desc)->AddRef();
	
	// transform, instance colour, texture (VT)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(Texture2D(0, Texture2D::EStyle_Tex_x_Diff));
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTintTex, desc)->AddRef();
	
	// transform, instance colour, per vertex colour, texture (VCT)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(PVC(PVC::EStyle_PVC_x_Diff)               );
	desc.Add(Texture2D(0, Texture2D::EStyle_Tex_x_Diff));
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTintPvcTex, desc)->AddRef();
	
	// transform, instance colour, single light (VN)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(Lighting(light_count, caster_count, true) );
	desc.Add(EnvMap()                                  );
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTintLitEnv, desc)->AddRef();
	
	// transform, instance colour, per vertex colour, single light (VNC)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(PVC(PVC::EStyle_PVC_x_Diff)               );
	desc.Add(Lighting(light_count, caster_count, true) );
	desc.Add(EnvMap()                                  );
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTintPvcLitEnv, desc)->AddRef();
	
	// transform, instance colour, texture, single light (VNT)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(Texture2D(0, Texture2D::EStyle_Tex_x_Diff));
	desc.Add(Lighting(light_count, caster_count, true) );
	desc.Add(EnvMap()                                  );
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTintTexLitEnv, desc)->AddRef();
	
	// transform, instance colour, single light, per vertex colour, texture (VNCT)
	desc.Reset();
	desc.Add(Txfm()                                    );
	desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
	desc.Add(PVC(PVC::EStyle_PVC_x_Diff)               );
	desc.Add(Texture2D(0, Texture2D::EStyle_Tex_x_Diff));
	desc.Add(Lighting(light_count, caster_count, true) );
	desc.Add(EnvMap()                                  );
	desc.Add(Terminator()                              );
	CreateEffect(EStockEffect::TxTintPvcTexLitEnv, desc)->AddRef();
	
	// Shadow map creater
	desc.Reset();
	desc.Add(SMap()                                    );
	desc.Add(Terminator()                              );
	m_smap_effect = CreateEffect(AutoId, desc);
}
	
// Create some useful stock textures
void pr::rdr::MaterialManager::CreateStockTextures()
{
	// Calling AddRef so that the textures are not destroyed immediately.
	// This means the only reference to these textures is in the texture lookup map
	{
		uint const data[] = {0};
		CreateTexture(EStockTexture::Black, data, sizeof(data), 1, 1)->AddRef();
	}{
		uint const data[] = {0xFFFFFFFF};
		CreateTexture(EStockTexture::White, data, sizeof(data), 1, 1)->AddRef();
	}{
		uint const data[] = {0xFFFFFFFF, 0, 0xFFFFFFFF, 0, 0, 0xFFFFFFFF, 0, 0xFFFFFFFF, 0xFFFFFFFF, 0, 0xFFFFFFFF, 0, 0, 0xFFFFFFFF, 0, 0xFFFFFFFF};
		CreateTexture(EStockTexture::Checker, data, sizeof(data), 4, 4)->AddRef();
	}
}
	
// Release the device objects.
void pr::rdr::MaterialManager::OnEvent(pr::rdr::Evt_DeviceLost const&)
{
	//for (TTextureLookup::iterator i = m_texture_lookup.begin(), iend = m_texture_lookup.end(); i != iend; ++i)
	//	i->second->m_tex->OnLostDevice();
	
	m_effect_pool = 0;
	m_d3d_device  = 0;
}
	
// Recreate the device objects
void pr::rdr::MaterialManager::OnEvent(pr::rdr::Evt_DeviceRestored const& e)
{
	m_d3d_device = e.m_d3d_device;

	// Recreate the effect pool
	if (Failed(D3DXCreateEffectPool(&m_effect_pool.m_ptr)))
		throw RdrException(EResult::CreateEffectPoolFailed, "Failed to create an effect pool");
}
	
// Effects *************************************************
	
// Create an effect instance.
// 'id' is the id to assign to this effect, use AutoId if you don't care.
// If the id matches an effect that has already been created you will get a pointer to that effect
// 'data' is data to initialise the texture with. Note: it must have appropriate stride and length.
// If 'data' or 'data_size' is 0, the texture is left uninitialised.
// Throws if creation fails. On success returns a pointer to the created texture.
pr::rdr::EffectPtr pr::rdr::MaterialManager::CreateEffect(RdrId id, pr::rdr::effect::Desc const& desc, pr::rdr::rs::Block const* render_states)
{
	// See if the effect has been created already
	if (id != AutoId)
	{
		pr::rdr::TEffectLookup::const_iterator iter = m_effect_lookup.find(id);
		if (iter != m_effect_lookup.end()) return iter->second;
	}
	
	effect::frag::Header* frags = pr::rdr::effect::frag::Begin((void*)&desc.m_buf[0]);
	pr::GeomType geom_type = GenerateMinGeomType(frags);
	string32     name      = GenerateEffectName(frags);

	// Generate the text for the effect
	ShaderBuffer data;
	desc.GenerateText(data);
	
	// Compile the effect
	D3DPtr<ID3DXEffect> effect;
	D3DPtr<ID3DXBuffer> compile_errors;
	#if PR_DBG_RDR_SHADERS // Create the effect from file when debugging so that PIX shows the HLSL code rather than the asm
	char const* filepath = pr::FmtS("d:/deleteme/%s.fx", inst->m_name.c_str()); pr::BufferToFile(data, filepath);
	HRESULT res = D3DXCreateEffectFromFile(m_d3d_device.m_ptr, filepath, 0, 0, g_shader_flags, m_effect_pool.m_ptr, &effect.m_ptr, &compile_errors.m_ptr);
	#else
	HRESULT res = D3DXCreateEffect(m_d3d_device.m_ptr, data.c_str(), (UINT)data.size(), 0, 0, g_shader_flags, m_effect_pool.m_ptr, &effect.m_ptr, &compile_errors.m_ptr);
	#endif
	if (pr::Failed(res))
	{
		std::string msg = pr::Fmt(
			"Failed to create effect: '%s'\n"
			"Generated Shader Output to: %s.fx\n"
			"HResult: %s\n"
			"Reason: %s\n"
			,name.c_str()
			,name.c_str()
			,pr::Reason().c_str()
			,compile_errors ? static_cast<char const*>(compile_errors->GetBufferPointer()) : "none available");
		PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
		throw RdrException(EResult::LoadEffectFailed, msg);
	}
	#define PR_DBG_RDR_OUTPUTSHADERS 1
	#if PR_DBG_RDR_OUTPUTSHADERS
	{
		std::string fname = "d:/deleteme/dx9shaders/";
		fname += name;
		fname += ".hlsl";
		pr::Handle f = pr::FileOpen(fname.c_str(), pr::EFileOpen::Writing);
		pr::FileWrite(f, data.c_str());
	}
	#endif

	// Get the best technique for this device
	D3DXHANDLE technique;
	if (pr::Failed(effect->FindNextValidTechnique(0, &technique)) || technique == 0)
	{
		char const* msg = FmtS("Failed to find a valid technique in effect: '%s'\n" ,name.c_str());
		PR_ASSERT(PR_DBG_RDR, false, msg);
		throw RdrException(EResult::LoadEffectFailed, msg);
	}
	effect->SetTechnique(technique);
	
	// Set the parameter handles for the fragments
	for (frag::Header* f = frags; f; f = frag::Inc(f))
		f->SetHandles(effect);
	
	// Allocate an effect instance
	pr::rdr::Effect& inst = *m_allocator.AllocEffect();
	
	// Add the effect to the lookup map
	m_effect_lookup[id] = &inst;
	
	inst.m_mat_mgr   = this;
	inst.m_effect    = effect;
	inst.m_buf       = desc.m_buf;
	inst.m_sort_id   = (++m_effect_sortid %= pr::rdr::sort_key::MaxEffectId);
	inst.m_id        = id;
	inst.m_rsb       = render_states ? *render_states : pr::rdr::rs::Block();
	inst.m_geom_type = geom_type;
	inst.m_name      = name;
	return &inst;
}
	
// Delete an effect instance
void pr::rdr::MaterialManager::DeleteEffect(pr::rdr::Effect const* effect)
{
	if (effect == 0) return;
	
	// Remove from the lookup map and deallocate
	TEffectLookup::iterator iter = m_effect_lookup.find(effect->m_id);
	PR_ASSERT(PR_DBG_RDR, iter != m_effect_lookup.end(), "Effect not found");
	m_allocator.DeallocEffect(iter->second);
	m_effect_lookup.erase(iter);
}
	
// Return an effect suitable for the provided geom_type
EffectPtr pr::rdr::MaterialManager::GetEffect(pr::GeomType geom_type)
{
	Effect* close = 0;
	uint close_bits = 0;
	for (TEffectLookup::iterator i = m_effect_lookup.begin(), iend = m_effect_lookup.end(); i != iend; ++i)
	{
		Effect& effect = *i->second;
		
		// Look for an exact match
		if (effect.m_geom_type == geom_type)
			return &effect;
		
		// Otherwise find the closest match
		if ((geom_type & effect.m_geom_type) == effect.m_geom_type && CountBits(effect.m_geom_type) > close_bits)
		{
			close_bits = CountBits(effect.m_geom_type);
			close = &effect;
		}
	}
	if (close_bits == 0)
	{
		std::string msg = FmtS(
			"No effect found that supports geometry type: %X\n"
			"Available Effects:\n" ,geom_type);
		for (TEffectLookup::const_iterator i = m_effect_lookup.begin(), iend = m_effect_lookup.end(); i != iend; ++i)
			msg += FmtS("   %s - geometry type: %X\n" ,i->second->m_name.c_str() ,i->second->m_geom_type);
		
		PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
		throw RdrException(EResult::EffectNotFound, msg);
	}
	return close;
}
	
// Textures *************************************************
	
// Create a texture instance.
// 'id' is the id to assign to this texture, use AutoId if you don't care.
// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing texture.
// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
//  a new texture instance that points to this d3d texture.
// 'data' is data to initialise the texture with. Note: it must have appropriate stride and length.
// If 'data' or 'data_size' is 0, the texture is left uninitialised.
// Throws if creation fails. On success returns a pointer to the created texture.
TexturePtr pr::rdr::MaterialManager::CreateTexture(RdrId id, void const* data, uint data_size, uint width, uint height, uint mips, uint usage, D3DFORMAT format, D3DPOOL pool)
{
	// See if 'id' already exists
	if (id != AutoId)
	{
		pr::rdr::TTextureLookup::const_iterator iter = m_texture_lookup.find(id);
		if (iter != m_texture_lookup.end())
		{
			PR_ASSERT(PR_DBG_RDR, data == 0 && data_size == 0, "data provided for an existing texture");
			
			// Duplicate the texture instance, but reuse the d3d texture
			pr::rdr::Texture& inst = *m_allocator.AllocTexture();
			inst.m_tex     = iter->second->m_tex;
			inst.m_info    = iter->second->m_info;
			inst.m_id      = GetId(&inst);
			inst.m_mat_mgr = this;
			PR_ASSERT(PR_DBG_RDR, m_texture_lookup.find(inst.m_id) == m_texture_lookup.end(), "overwriting an existing texture id");
			m_texture_lookup[inst.m_id] = &inst;
			return &inst;
		}
	}
	
	// 'id' doesn't exist (or is Auto), allocate the d3d texture resource
	D3DPtr<IDirect3DTexture9> tex;
	if (pr::Failed(m_d3d_device->CreateTexture(width, height, mips, usage, format, pool, &tex.m_ptr, 0)))
	{
		char const* msg = FmtS("Failed to create texture: %s\n" ,pr::Reason().c_str());
		PR_ASSERT(PR_DBG_RDR, false, msg);
		throw RdrException(EResult::LoadTextureFailed, msg);
	}
	
	// Save the texture creation info with the d3d texture
	TexInfo info;
	info.Width           = width;
	info.Height          = height;
	info.Depth           = 1;
	info.MipLevels       = mips;
	info.Format          = format;
	info.ImageFileFormat = D3DXIFF_FORCE_DWORD;
	info.ResourceType    = D3DRTYPE_TEXTURE;
	info.TexFileId       = 0;
	info.SortId          = (++m_texture_sortid %= pr::rdr::sort_key::MaxTextureId);
	info.Alpha           = false;
	info.Usage           = usage;
	info.Pool            = pool;
	pr::Throw(tex->SetPrivateData(TexInfoGUID, &info, sizeof(info), 0)); // Attach info to the texture, d3d cleans this up

	// If data is provided, copy it to the texture
	if (data && data_size != 0)
	{
		D3DLOCKED_RECT rect;
		if (pr::Succeeded(tex->LockRect(0, &rect, 0, 0)))
		{
			PR_ASSERT(PR_DBG_RDR, pr::rdr::BytesPerPixel(format) * width == (uint)rect.Pitch, "Texture pitch mismatch");
			PR_ASSERT(PR_DBG_RDR, pr::rdr::BytesPerPixel(format) * width * height <= data_size, "Insufficient data provided for texture initialisation");
			memcpy(rect.pBits, data, rect.Pitch * height);
			pr::Throw(tex->UnlockRect(0));
		}
	}
	
	// Allocate the texture instance and save texture creation info
	pr::rdr::Texture& inst = *m_allocator.AllocTexture();
	inst.m_tex     = tex;
	inst.m_info    = info;
	inst.m_id      = id == AutoId ? GetId(&inst) : id;
	inst.m_mat_mgr = this;
	inst.m_name    = "";
	PR_ASSERT(PR_DBG_RDR, m_texture_lookup.find(inst.m_id) == m_texture_lookup.end(), "overwriting an existing texture id");
	m_texture_lookup[inst.m_id] = &inst;
	return &inst;
}
	
// Create a texture instance from file
// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing texture.
// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
//  a new texture instance that points to this d3d texture.
// If width/height are 0 the dimensions of the image file are used as the texture size.
// Throws if creation fails. On success returns a pointer to the created texture.
TexturePtr pr::rdr::MaterialManager::CreateTexture(RdrId id, char const* filepath, uint width, uint height, uint mips, D3DCOLOR colour_key, DWORD filter, DWORD mip_filter, D3DFORMAT format, uint usage, D3DPOOL pool)
{
	// Accept stock texture strings: #black, #white, #checker, etc
	// and id's given via string
	if (filepath && filepath[0] == '#')
	{
		if (::isdigit(filepath[1]))
		{
			id = pr::str::as<RdrId>(filepath + 1, 0, 10);
		}
		else
		{
			id = pr::rdr::EStockTexture::Parse(filepath + 1);
			if (id == pr::rdr::EStockTexture::NumberOf)
			{
				char const* msg = FmtS("Failed to create stock texture: %s\n" ,filepath);
				PR_INFO(PR_DBG_RDR, msg);
				throw RdrException(EResult::LoadTextureFailed, msg);
			}
		}
	}
	
	// See if 'id' already exists
	if (id != AutoId)
	{
		pr::rdr::TTextureLookup::const_iterator iter = m_texture_lookup.find(id);
		if (iter != m_texture_lookup.end())
		{
			// Duplicate the texture instance, but reuse the d3d texture
			pr::rdr::Texture& inst = *m_allocator.AllocTexture();
			inst.m_tex     = iter->second->m_tex;
			inst.m_info    = iter->second->m_info;
			inst.m_id      = GetId(&inst);
			inst.m_mat_mgr = this;
			inst.m_name    = filepath;
			PR_ASSERT(PR_DBG_RDR, m_texture_lookup.find(inst.m_id) == m_texture_lookup.end(), "overwriting an existing texture id");
			m_texture_lookup[inst.m_id] = &inst;
			return &inst;
		}
	}
	
	D3DPtr<IDirect3DTexture9> tex;
	TexInfo info;
	
	// Look for an existing d3d texture corresponding to 'filepath'
	RdrId texfile_id = GetId(pr::filesys::StandardiseC<std::string>(filepath).c_str());
	pr::rdr::TTexFileLookup::const_iterator iter = m_texfile_lookup.find(texfile_id);
	if (iter != m_texfile_lookup.end())
	{
		tex = iter->second;
		DWORD info_size = sizeof(info);
		tex->GetPrivateData(TexInfoGUID, &info, &info_size);
	}
	else // Otherwise, if not loaded already, load now
	{
		// If not allocate the texture and populate from the file
		if (pr::Failed(D3DXCreateTextureFromFileEx(m_d3d_device.m_ptr, filepath, width, height, mips, usage, format, pool, filter, mip_filter, colour_key, &info, 0, &tex.m_ptr)))
		{
			char const* msg = FmtS("Failed to create texture: %s - %s\n" ,filepath ,pr::Reason().c_str());
			PR_INFO(PR_DBG_RDR, msg);
			throw RdrException(EResult::LoadTextureFailed, msg);
		}
		info.TexFileId = texfile_id;
		info.SortId    = (++m_texture_sortid %= pr::rdr::sort_key::MaxTextureId);
		info.Alpha     = false;
		info.Usage     = usage;
		info.Pool      = pool;
		pr::Throw(tex->SetPrivateData(TexInfoGUID, &info, sizeof(info), 0)); // Attach info to the texture, d3d cleans this up
		m_texfile_lookup[texfile_id] = tex.m_ptr;
	}
	
	// Allocate the texture instance
	pr::rdr::Texture& inst = *m_allocator.AllocTexture();
	inst.m_tex     = tex;
	inst.m_info    = info;
	inst.m_id      = id == AutoId ? GetId(&inst) : id;
	inst.m_mat_mgr = this;
	inst.m_name    = filepath;
	PR_ASSERT(PR_DBG_RDR, m_texture_lookup.find(inst.m_id) == m_texture_lookup.end(), "overwriting an existing texture id");
	m_texture_lookup[inst.m_id] = &inst;
	return &inst;
}
	
// Create a video texture from file
TexturePtr pr::rdr::MaterialManager::CreateVideoTexture(RdrId id, char const* filepath, uint width, uint height)
{
	// Create the video object first to check that it can be loaded successfully
	// and so that we can get the native size of the video
	VideoPtr video = new Video();
	video->CreateFromFile(m_d3d_device, filepath);
	
	// Get the native video resolution so we can create an appropriate size texture
	pr::iv2 res = video->GetNativeResolution();
	if (res == pr::iv2Zero) return 0;
	if (width  == 0) width  = uint(res.x);
	if (height == 0) height = uint(res.y);
		
	// Create a compatible render target texture
	TexturePtr tex = CreateTexture(id, (void const*)0, 0, width, height, 1, D3DUSAGE_RENDERTARGET, D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT);
	tex->m_name    = pr::filesys::GetFilename<string32>(filepath);
	tex->m_video   = video;
	video->m_tex = tex.m_ptr;
	return tex;
}

// Delete a texture instance.
void pr::rdr::MaterialManager::DeleteTexture(pr::rdr::Texture const* tex)
{
	if (tex == 0) return;
	
	// Find our reference to 'tex' (non-const version)
	TTextureLookup::iterator iter = m_texture_lookup.find(tex->m_id);
	PR_ASSERT(PR_DBG_RDR, iter != m_texture_lookup.end(), "Texture not found");
	
	// If this is the last reference to the d3d texture, remove it from the texfile lookup map (if there)
	if (iter->second->m_tex.RefCount() == 1)
	{
		TTexFileLookup::iterator jter = m_texfile_lookup.find(tex->m_info.TexFileId);
		if (jter != m_texfile_lookup.end()) m_texfile_lookup.erase(jter);
	}
	
	// Remove from the lookup map and deallocate
	m_allocator.DeallocTexture(iter->second);
	m_texture_lookup.erase(iter);
}
	
// Return info about a texture from its file
EResult::Type pr::rdr::MaterialManager::TextureInfo(char const* tex_filepath, D3DXIMAGE_INFO& info)
{
	return pr::Failed(D3DXGetImageInfoFromFile(tex_filepath, &info)) ? EResult::LoadTextureFailed : EResult::Success;
}
	

//	
//// Set the texture filter do a quality level based on what the hardware can support.
//// Note: the device will have to be restored before the changes take affect
//void pr::rdr::MaterialManager::SetTextureFiltering(EQuality::Type quality)
//{
//	D3DCAPS9 caps;
//	Verify(m_d3d_device->GetDeviceCaps(&caps));
//	SetTextureFilter(m_texture_filter, caps, quality);
//}
////
//template <typename T> inline void DumpHex(T const& t)
//{
//	unsigned char const* ptr = reinterpret_cast<unsigned char const*>(&t);
//	for (int i = 0; i != sizeof(T); ++i, ++ptr)
//		printf("%2X%c" ,(int)*ptr ,(i%16)==15?'\n':' ');
//}

//// Load a texture from raw texture data in memory
//void pr::rdr::MaterialManager::CreateTexture(RdrId texture_id, void const* data, uint data_size, uint width, uint height, rdr::Texture const*& texture_out, uint mips, uint usage, D3DFORMAT format, D3DPOOL pool)
//{
//	PR_ASSERT(PR_DBG_RDR, texture_id != 0, "RdrId 0 is reserved");
//	
//	if (texture_id != pr::rdr::AutoId)
//	{
//		texture_out = FindTexture(texture_id);
//		if (texture_out) return;
//	}
//	
//	Texture texture;
//	texture.Create(m_d3d_device, texture_id, data, data_size, width, height, mips, usage, format, pool);
//	
//	// Allocate a texture, copy the one we've created, and add it to the chain
//	Texture& tex = *m_allocator.AllocTexture(); tex = texture;
//	AddTextureInternal(tex);
//	texture_out = &tex;
//}
//	
//// Release a texture
//void pr::rdr::MaterialManager::DeleteTexture(rdr::Texture const* texture)
//{
//	PR_ASSERT(PR_DBG_RDR, texture != 0, "Deleting an texture null pointer");
//	TTextureLookup::iterator i = m_texture_lookup.find(texture->m_texture_id);
//	if (i != m_texture_lookup.end())
//	{
//		i->second->Release();
//		m_allocator.DeallocTexture(i->second);
//		m_texture_lookup.erase(i);
//	}
//}
//	
//// Load a texture from a file
//EResult::Type pr::rdr::MaterialManager::LoadTexture(const char* texture_filename, rdr::Texture const*& texture_out)
//{
//	// See if the texture has already been loaded
//	RdrId texture_id = GetId(texture_filename);
//	texture_out = FindTexture(texture_id);
//	if (texture_out) return EResult::SuccessAlreadyLoaded;
//	
//	// If not, create the texture
//	Texture texture;
//	if (!texture.CreateFromFile(m_d3d_device, texture_id, texture_filename))
//		return EResult::LoadTextureFailed;
//	
//	// Allocate a texture, copy the one we've created, and add it to the chain
//	Texture& tex = *m_allocator.AllocTexture(); tex = texture;
//	AddTextureInternal(tex);
//	texture_out = &tex;
//	return EResult::Success;
//}
//	
//// Load a texture from a file
//EResult::Type pr::rdr::MaterialManager::LoadTexture(const char* texture_filename, rdr::Texture const*& texture_out, uint width, uint height, uint mips, D3DCOLOR colour_key, uint usage, D3DFORMAT format, D3DPOOL pool, DWORD filter, DWORD mip_filter)
//{
//	// See if the texture has already been loaded
//	RdrId texture_id = GetId(texture_filename);
//	texture_out = FindTexture(texture_id);
//	if (texture_out) return EResult::SuccessAlreadyLoaded;
//
//	// If not, create the texture
//	Texture texture;
//	if (!texture.CreateFromFile(m_d3d_device, texture_id, texture_filename, width, height, mips, colour_key, usage, format, pool, filter, mip_filter))
//		return EResult::LoadTextureFailed;
//
//	// Allocate a texture, copy the one we've created, and add it to the chain
//	Texture& tex = *m_allocator.AllocTexture(); tex = texture;
//	AddTextureInternal(tex);
//	texture_out = &tex;
//	return EResult::Success;
//}
//
//// Create an effect from a list of fragments
//// Throws if the effect cannot be created.
//void pr::rdr::MaterialManager::CreateEffect(effect::Desc const& effect_desc, rdr::Effect2 const*& effect_out, rs::Block const* render_states)
//{
//	// See if the effect has already been loaded
//	effect_out = FindEffect(effect_desc.m_effect_id);
//	if (effect_out) return;
//	
//	// If not, create the effect
//	Effect2 effect;
//	effect.Create(m_d3d_device, m_effect_pool, effect_desc);
//	
//	// Allocate an effect, copy the one we've created, and add it to the chain
//	Effect2& eff = *m_allocator.AllocEffect(); eff = effect;
//	if (render_states) eff.m_render_state = *render_states;
//	AddEffectInternal(eff);
//	effect_out = &eff;
//}