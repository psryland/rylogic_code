//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/textures/texture_cube.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/util/util.h"
#include "renderer11/directxtex/directxtex.h"

namespace pr::rdr
{
	// Initialise 'tex.m_srv' based on the texture description
	void InitSRV(TextureCube& tex)
	{
		if (tex.m_srv != nullptr)
			return;

		Texture2DDesc tdesc;
		tex.dx_tex()->GetDesc(&tdesc);

		// If the texture can be a shader resource, create a shader resource view
		if ((tdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE) != 0)
		{
			ShaderResourceViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURECUBE);
			srvdesc.TextureCube.MostDetailedMip = 0U;
			srvdesc.TextureCube.MipLevels = ~0U;

			Renderer::Lock lock(tex.m_mgr->rdr());
			pr::Throw(lock.D3DDevice()->CreateShaderResourceView(tex.m_res.get(), &srvdesc, &tex.m_srv.m_ptr));
		}
	}

	TextureCube::TextureCube(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, ID3D11ShaderResourceView* srv, SamplerDesc const& sdesc, char const* name)
		:TextureBase(mgr, id, tex, srv, nullptr, 0, name)
		,m_cube2w(m4x4Identity)
	{
		InitSRV(*this);
		SamDesc(sdesc);
	}
}