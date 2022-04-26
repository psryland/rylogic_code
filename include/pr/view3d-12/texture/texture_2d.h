//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/texture/texture_base.h"

namespace pr::rdr12
{
	struct Texture2D :TextureBase
	{
		// Notes:
		//   - Texture2D (and derived objects) are lightweight, they are basically reference
		//     counted pointers to D3D resources.
		//   - Textures have value semantics (i.e. copyable)
		//   - Each time CreateTexture is called, a new texture instance is allocated.
		//     However, the resources associated with the texture may be shared with other textures.

		m4x4 m_t2s;       // Texture to surface transform
		bool m_has_alpha; // True if the texture contains alpha pixels

		Texture2D(ResourceManager& mgr, RdrId id, ID3D12Resource* res, RdrId uri, bool has_alpha, char const* name);
		// Texture2D(ResourceManager* mgr, RdrId id, ID3D12Texture2D* tex, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name);
		// Texture2D(ResourceManager* mgr, RdrId id, ID3D12Texture2D* tex, ID3D11ShaderResourceView* srv, SamplerDesc const& sam_desc, SortKeyId sort_id, bool has_alpha, char const* name);
		// Texture2D(ResourceManager* mgr, RdrId id, IUnknown* shared_resource, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name);
		// Texture2D(ResourceManager* mgr, RdrId id, HANDLE shared_handle, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name);
		// Texture2D(ResourceManager* mgr, RdrId id, Image const& src, Texture2DDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name, ShaderResourceViewDesc const* srvdesc = nullptr);
		// Texture2D(ResourceManager* mgr, RdrId id, Texture2D const& existing, char const* name);
	};
}
