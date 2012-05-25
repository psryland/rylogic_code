//***********************************************************************
//
//	Material Manager
//
//***********************************************************************
//
//	A class to manage the loading/updating and access to materials/textures
//

#include "Stdafx.h"
#include "PR/Common/Fmt.h"
#include "PR/Filesys/Filesys.h"
#include "PR/Crypt/Crypt.h"
#include "PR/Renderer/Materials/MaterialManager.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Effects/XYZ.h"
#include "PR/Renderer/Effects/XYZLit.h"
#include "PR/Renderer/Effects/XYZPVC.h"
#include "PR/Renderer/Effects/XYZLitPVC.h"
#include "PR/Renderer/Effects/XYZTextured.h"
#include "PR/Renderer/Effects/XYZLitTextured.h"
#include "PR/Renderer/Effects/XYZPVCTextured.h"
#include "PR/Renderer/Effects/XYZLitPVCTextured.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
// Constructor
MaterialManager::MaterialManager(D3DPtr<IDirect3DDevice9> d3d_device, const rdr::TPathList&	shader_paths)
:m_d3d_device			(d3d_device)
,m_effect_pool			(0)
,m_texture_lookup		()
,m_effect_lookup		()
,m_texture				()
,m_effect				()
,m_texture_id			(0)
,m_effect_id			(0)
,m_default_texture		(0)
,m_default_effect		(0)
,m_texture_storage		()
,m_effect_storage		()
,m_builtin_effect		()
,m_shader_paths			(shader_paths)
{
	CreateDeviceDependentObjects(m_d3d_device);

	// Create a default texture
	m_texture_storage.push_back(Texture());
	m_default_texture = &m_texture_storage.back();

	// Create the default effects
	if( Failed(LoadEffect<XYZ				>("XYZ.fx"					,m_builtin_effect[EBuiltInEffect_XYZ				])) ||
		Failed(LoadEffect<XYZLit			>("XYZLit.fx"				,m_builtin_effect[EBuiltInEffect_XYZLit				])) ||
		Failed(LoadEffect<XYZPVC			>("XYZPVC.fx"				,m_builtin_effect[EBuiltInEffect_XYZPVC				])) ||
		Failed(LoadEffect<XYZLitPVC			>("XYZLitPVC.fx"			,m_builtin_effect[EBuiltInEffect_XYZLitPVC			])) ||
		Failed(LoadEffect<XYZTextured		>("XYZTextured.fx"			,m_builtin_effect[EBuiltInEffect_XYZTextured		])) ||
		Failed(LoadEffect<XYZLitTextured	>("XYZLitTextured.fx"		,m_builtin_effect[EBuiltInEffect_XYZLitTextured		])) ||
		Failed(LoadEffect<XYZPVCTextured	>("XYZPVCTextured.fx"		,m_builtin_effect[EBuiltInEffect_XYZPVCTextured		])) ||
		Failed(LoadEffect<XYZLitPVCTextured	>("XYZLitPVCTextured.fx"	,m_builtin_effect[EBuiltInEffect_XYZLitPVCTextured	])) )
	{
		throw Exception(EResult_CreateDefaultEffectsFailed, "Failed to create the default effects");
	}
	m_default_effect = m_builtin_effect[EBuiltInEffect_XYZ];
}

//*****
// Destructor
MaterialManager::~MaterialManager()
{
	std::for_each(m_effect_storage.begin(), m_effect_storage.end(), Delete());
}

//*****
// Release and reload all of the textures and effects
void MaterialManager::Reset()
{
	D3DPtr<IDirect3DDevice9> d3d_device = m_d3d_device;
	ReleaseDeviceDependentObjects();
	CreateDeviceDependentObjects(d3d_device);
}

//*****
// Release the device objects.
void MaterialManager::ReleaseDeviceDependentObjects()
{
	std::for_each(m_effect .begin(), m_effect .end(), Release());
	std::for_each(m_texture.begin(), m_texture.end(), Release());
	m_effect_pool = 0;
	m_d3d_device  = 0;
}

//*****
// Recreate the device objects
void MaterialManager::CreateDeviceDependentObjects(D3DPtr<IDirect3DDevice9> d3d_device)
{
	m_d3d_device = d3d_device;

	// Create the effect pool
	if( Failed(D3DXCreateEffectPool(&m_effect_pool.m_ptr)) ) { throw Exception(EResult_CreateEffectPoolFailed, "Failed to create an effect pool"); }
	
	// Re-Create the textures
	for( TTextureChain::iterator t = m_texture.begin(), t_end = m_texture.end(); t != t_end; ++t )
	{
		t->ReCreate(m_d3d_device);
	}

	// Re-Create the effects
	for( TEffectChain::iterator e = m_effect.begin(), e_end = m_effect.end(); e != e_end; ++e )
	{
		e->ReCreate(m_d3d_device, m_effect_pool);
	}
}

//*****
// Add a texture to our internal buffer of textures.
EResult MaterialManager::LoadTexture(const char* texture_filename, rdr::Texture*& texture_out)
{
	uint texture_hash;
	rdr::Texture* texture = FindTexture(texture_filename, texture_hash);

	// If we do not have the texture already then add it to our container of textures
	if( !texture )
	{
		m_texture_storage.push_back(Texture());
		texture = &m_texture_storage.back();
		
		if( !texture->Create(texture_filename, m_d3d_device) )
		{
			m_texture_storage.pop_back();
			return EResult_LoadTextureFailed;
		}

		// Add the texture to the chain
		AddTextureInternal(texture, texture_hash);
	}
	texture_out = texture;
	return EResult_Success;	
}

//*****
// Add an effect to our effect chain
bool MaterialManager::AddEffect(effect::Base* effect)
{
	uint effect_hash;
	if( FindEffect(effect->GetFilename(), effect_hash) ) return false;
	AddEffectInternal(effect, effect_hash);
	return true;
}

//*****
// Add a texture to our texture chain. Returns true if the texture was added
bool MaterialManager::AddTexture(rdr::Texture* texture)
{
	uint texture_hash;
	if( FindTexture(texture->GetName(), texture_hash) ) return false;
	AddTextureInternal(texture, texture_hash);
	return true;
}

//*****
// Return the full path for a shader filename
bool MaterialManager::ResolveShaderPath(const std::string& shader_filename, std::string& resolved_shader_path) const
{
	// Search in the local directory first
	if( file_sys::DoesFileExist(shader_filename.c_str()) )
	{
		resolved_shader_path = shader_filename;
		return true;
	}

	// Search for the file in the shader paths
	for( TPathList::const_iterator iter = m_shader_paths.begin(), iter_end = m_shader_paths.end(); iter != iter_end; ++iter )
	{
		if( file_sys::DoesFileExist((*iter) + "/" + shader_filename) )
		{
			resolved_shader_path = (*iter) + "/" + shader_filename;
			return true;
		}
	}
	return false;
}

//*****
// Look for an existing texture with this name. On return 'texture_hash' is the hash of the filename
rdr::Texture* MaterialManager::FindTexture(const char* texture_filename, uint& texture_hash)
{
	texture_hash = crypt::Crc(texture_filename, (uint)strlen(texture_filename));

	// See if we have this texture already.
	THashToTexturePtr::iterator iter = m_texture_lookup.find(texture_hash);
	if( iter != m_texture_lookup.end() ) return iter->second;
	return 0;
}

//*****
// Look for an existing effect with this name. On return 'effect_hash' is the hash of the filename
effect::Base* MaterialManager::FindEffect(const char* effect_filename, uint& effect_hash)
{
	effect_hash = crypt::Crc(effect_filename, (uint)strlen(effect_filename));

	// See if we have this effect already.
	THashToEffectPtr::iterator iter = m_effect_lookup.find(effect_hash);
	if( iter != m_effect_lookup.end() ) return iter->second;
	return 0;
}

//*****
// Add an effect to our effect chain
void MaterialManager::AddEffectInternal(effect::Base* effect, uint effect_hash)
{
	// Assign the effect an id
	effect->m_id = m_effect_id++;

	// Add the effect to the effect chain
	m_effect.push_back(*effect);
	m_effect_lookup[effect_hash] = effect;
}

//*****
// Add a texture to our texture chain
void MaterialManager::AddTextureInternal(rdr::Texture* texture, uint texture_hash)
{
	// Assign the texture an id
	texture->m_id = m_texture_id++;

	// Add the texture to the chain
	m_texture.push_back(*texture);
	m_texture_lookup[texture_hash] = texture;
}

//*****
// Return a material that is suitable for the provided geometry type
rdr::Material MaterialManager::GetSuitableMaterial(pr::GeomType geometry_type) const
{
	rdr::Material mat = {m_default_texture, GetEffectForGeomType(geometry_type)};
	return mat;
}

//*****
// Return a builtin effect to use for a geometry type
effect::Base* MaterialManager::GetEffectForGeomType(pr::GeomType geometry_type) const
{
	using namespace geometry;
	switch( geometry_type )
	{
	case (EType_Vertex                                              ):		return m_builtin_effect[EBuiltInEffect_XYZ];
	case (EType_Vertex | EType_Normal                               ):		return m_builtin_effect[EBuiltInEffect_XYZLit];
	case (EType_Vertex                | EType_Colour                ):		return m_builtin_effect[EBuiltInEffect_XYZPVC];
	case (EType_Vertex | EType_Normal | EType_Colour                ):		return m_builtin_effect[EBuiltInEffect_XYZLitPVC];
	case (EType_Vertex                               | EType_Texture):		return m_builtin_effect[EBuiltInEffect_XYZTextured];
	case (EType_Vertex | EType_Normal                | EType_Texture):		return m_builtin_effect[EBuiltInEffect_XYZLitTextured];
	case (EType_Vertex                | EType_Colour | EType_Texture):		return m_builtin_effect[EBuiltInEffect_XYZPVCTextured];
	case (EType_Vertex | EType_Normal | EType_Colour | EType_Texture):		return m_builtin_effect[EBuiltInEffect_XYZLitPVCTextured];
	default: PR_ERROR_STR(PR_DBG_RDR, "Unknown geometry type combination");	return m_builtin_effect[EBuiltInEffect_XYZ];
	}
}

//*****
// Turn a pr::Material into a collection of renderer materials corresponding to the textures in the pr::Material.
EResult	MaterialManager::LoadMaterials(const pr::Material& material, pr::GeomType geom_type, IMaterialResolver& mat_resolver)
{
	// Select an effect appropriate for the vertex format being used. Any effect in a texture may overwrite this
	effect::Base* default_effect = GetEffectForGeomType(geom_type);
	
	// Load the textures of this material
	uint material_count = 0;
	for( TTextureCont::const_iterator t = material.m_sub_material.begin(), t_end = material.m_sub_material.end(); t != t_end; ++t, ++material_count )
	{
		Material material = {m_default_texture, default_effect};
		
		if( !mat_resolver.LoadTexture(t->m_filename.c_str(), material.m_texture) )
		{
			material.m_texture = m_default_texture;
		}

		// If this texture references an effect load that too
		if( material.m_texture && material.m_texture->HasProperty(TextureProperty_Effect) )
		{
			if( !mat_resolver.LoadEffect(material.m_texture->EffectId().c_str(), material.m_effect) )
			{
				material.m_effect = default_effect;
			}
		}
		if( !mat_resolver.AddMaterial(material_count, material) ) return EResult_EnumateTerminated;
	}
	return EResult_Success;
}
