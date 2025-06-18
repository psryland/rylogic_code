//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/shaders/shader.h"

namespace pr::rdr
{
	// A geometry shader for showing vertex normals
	struct ShowNormalsGS :ShaderT<ID3D11GeometryShader, ShowNormalsGS>
	{
		using base = ShaderT<ID3D11GeometryShader, ShowNormalsGS>;
		D3DPtr<ID3D11Buffer> m_cbuf;

		ShowNormalsGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader>& shdr);
		void Setup(ID3D11DeviceContext* dc, DeviceState& state) override;
	};
}