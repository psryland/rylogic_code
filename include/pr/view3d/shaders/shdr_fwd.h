//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/shaders/shader.h"

namespace pr::rdr
{
	// Forward rendering vertex shader
	struct FwdShaderVS :ShaderT<ID3D11VertexShader, FwdShaderVS>
	{
		using base = ShaderT<ID3D11VertexShader, FwdShaderVS>;
		FwdShaderVS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11VertexShader> const& shdr);
	};

	// Forward rendering pixel shader
	struct FwdShaderPS :ShaderT<ID3D11PixelShader, FwdShaderPS>
	{
		using base = ShaderT<ID3D11PixelShader, FwdShaderPS>;
		FwdShaderPS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11PixelShader> const& shdr);
	};

	// Forward rendering pixel shader with radial fading
	struct FwdRadialFadePS :ShaderT<ID3D11PixelShader, FwdRadialFadePS>
	{
		using base = ShaderT<ID3D11PixelShader, FwdRadialFadePS>;

		D3DPtr<ID3D11Buffer> m_cbuf;
		v4 m_fade_centre;
		v2 m_fade_radius;
		ERadial m_fade_type;
		bool m_focus_relative;

		FwdRadialFadePS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11PixelShader> const& shdr);
		void Setup(ID3D11DeviceContext* dc, DeviceState& state);
	};
}