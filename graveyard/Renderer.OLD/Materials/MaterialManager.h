//***********************************************************************
//
//	Material Manager
//
//***********************************************************************
//
//	A class to manage the loading/updating and access to materials/textures
//

#ifndef PR_RDR_MATERIAL_MANAGER_H_H
#define PR_RDR_MATERIAL_MANAGER_H_H

#include "PR/Common/StdList.h"
#include "PR/Common/StdVector.h"
#include "PR/Common/StdMap.h"
#include "PR/Common/D3DPtr.h"
#include "PR/Common/Chain.h"
#include "PR/Geometry/PRGeometry.h"
#include "PR/Renderer/Forward.h"
#include "PR/Renderer/Materials/Material.h"
#include "PR/Renderer/Materials/Texture.h"
#include "PR/Renderer/Effects/EffectBase.h"
#include "PR/Renderer/Materials/IMaterialResolver.h"

namespace pr
{
	namespace rdr
	{
		class MaterialManager
		{
		public:
			MaterialManager(D3DPtr<IDirect3DDevice9> d3d_device, const rdr::TPathList& shader_paths);
			~MaterialManager();

			void Reset();
			void CreateDeviceDependentObjects(D3DPtr<IDirect3DDevice9> d3d_device);
			void ReleaseDeviceDependentObjects();

			// Interface methods
			template <typename EffectClass>
			EResult			LoadEffect			(const char* effect_filename,  effect::Base*& effect_out);
			EResult			LoadTexture			(const char* texture_filename, rdr::Texture*& texture_out);
			bool			AddEffect			(effect::Base* effect);		// Add client owned effects
			bool			AddTexture			(rdr::Texture* texture);	// Add client owned textures
			Material		GetSuitableMaterial	(pr::GeomType geometry_type) const;
			effect::Base*	GetEffectForGeomType(pr::GeomType geometry_type) const;
			EResult			LoadMaterials		(const pr::Material& material, pr::GeomType geom_type, IMaterialResolver& mat_resolver);

		private:
			MaterialManager(const MaterialManager&);
			MaterialManager& operator = (const MaterialManager&);
			bool			ResolveShaderPath	(const std::string& shader_filename, std::string& resolved_shader_path) const;
			Texture*		FindTexture			(const char* texture_filename, uint& texture_hash);
			effect::Base*	FindEffect			(const char* effect_filename, uint& effect_hash);
			void			AddEffectInternal	(effect::Base* effect, uint effect_hash);
			void			AddTextureInternal	(rdr::Texture* texture, uint texture_hash);
			
		private:
			enum EBuiltInEffect
			{
				EBuiltInEffect_XYZ,
				EBuiltInEffect_XYZLit,
				EBuiltInEffect_XYZPVC,
				EBuiltInEffect_XYZLitPVC,
				EBuiltInEffect_XYZTextured,
				EBuiltInEffect_XYZLitTextured,
				EBuiltInEffect_XYZPVCTextured,
				EBuiltInEffect_XYZLitPVCTextured,
				EBuiltInEffect_NumberOf
			};
			typedef std::list<Texture>				TTextureContainer;
			typedef std::list<effect::Base*>		TEffectContainer;
			typedef std::map<uint, Texture*>		THashToTexturePtr;
			typedef std::map<uint, effect::Base*>	THashToEffectPtr;
			typedef pr::chain::head<Texture,      RendererTextureChain>	TTextureChain;
			typedef pr::chain::head<effect::Base, RendererEffectChain>	TEffectChain;

			D3DPtr<IDirect3DDevice9>	m_d3d_device;
			D3DPtr<ID3DXEffectPool>		m_effect_pool;

			THashToTexturePtr		m_texture_lookup;			// A map from texture filename hash to texture pointer
			THashToEffectPtr		m_effect_lookup;			// A map from effect filename hash to effect pointer
			TTextureChain			m_texture;					// A chain of the textures
			TEffectChain			m_effect;					// A chain of effects

			uint16					m_texture_id;				// A rolling count used to represent the texture in the sort key
			uint16					m_effect_id;				// A rolling count used to represent the effect in the sort key

			Texture*				m_default_texture;			// A texture that is guaranteed to be there
			effect::Base*			m_default_effect;			// An effect that is guaranteed to be there
			TTextureContainer		m_texture_storage;			// Storage for textures we've loaded
			TEffectContainer		m_effect_storage;			// Storage for effects we've loaded
			effect::Base*			m_builtin_effect[EBuiltInEffect_NumberOf];
			const rdr::TPathList&	m_shader_paths;
		};
		
		//****************************************************************
		// Implementation
		//*****
		// Add an effect to our internal buffer of effects.
		template <typename EffectClass> 
		EResult MaterialManager::LoadEffect(const char* effect_filename, effect::Base*& effect_out)
		{
			uint effect_hash;
			effect::Base* effect = FindEffect(effect_filename, effect_hash);
			
			// If we do not have the effect already then add it to our container of effects
			if( !effect )
			{
                // Resolve the effect filename into a full path
				std::string shader_path;
				if( !ResolveShaderPath(effect_filename, shader_path) )
				{
					PR_ERROR_STR(PR_DBG_RDR, "Failed to resolve shader path");
					return EResult_ResolveShaderPathFailed;
				}
		
				// Create the effect class
				effect = new EffectClass();
				if( !effect->Create(shader_path.c_str(), m_d3d_device, m_effect_pool) )
				{
					delete effect;
					return EResult_LoadEffectFailed;
				}

				// Add the effect to storage and the effect chain
				m_effect_storage.push_back(effect);
				AddEffectInternal(effect, effect_hash);
			}
			effect_out = effect;
			return EResult_Success;	
		}
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_MATERIAL_MANAGER_H_H
