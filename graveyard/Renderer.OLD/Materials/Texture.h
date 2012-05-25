//**********************************************************************************************
//
// Texture
//
//**********************************************************************************************
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

#ifndef PR_RDR_TEXTURE_H
#define PR_RDR_TEXTURE_H

#include "PR/Common/StdString.h"
#include "PR/Common/Chain.h"
#include "PR/Common/D3DPtr.h"
#include "PR/Renderer/Forward.h"
#include "PR/Renderer/Materials/TextureProperty.h"

namespace pr
{
	namespace rdr
	{
		class Texture : public pr::chain::link<Texture, RendererTextureChain>
		{
		public:
			Texture();
			bool Create(const char* filename, D3DPtr<IDirect3DDevice9> d3d_device) { m_name = filename; return ReCreate(d3d_device); }
			bool ReCreate(D3DPtr<IDirect3DDevice9> d3d_device);
			void Release();

			const char*		GetName() const							{ return m_name.c_str(); }
			bool			HasProperty(TextureProperty prop) const	{ return (m_properties & prop) != 0; }
			bool&			Alpha()									{ return m_prop.m_alpha; }
			std::string&	EffectId() 								{ return m_prop.m_effect_id; }

			// Public members
			D3DPtr<IDirect3DTexture9>	m_texture;
			uint16						m_id;

		private:
			void LoadTextureInfo();
			std::string	m_name;

			// The texture properties
			uint m_properties;
			struct Prop
			{
				Prop()
				:m_alpha(false)
				,m_effect_id("")
				{}
				bool		m_alpha;
				std::string	m_effect_id;
			} m_prop;
		};
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_TEXTURE_H
