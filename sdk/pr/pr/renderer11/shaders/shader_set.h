//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/util/stock_resources.h"

namespace pr
{
	namespace rdr
	{
		// A collection of shader instances
		struct ShaderSet
		{
			ShaderPtr m_vs;
			ShaderPtr m_gs;
			ShaderPtr m_ps;

			ShaderSet() :m_vs() ,m_gs() ,m_ps() {}
			std::initializer_list<ShaderPtr> Enumerate() const { return std::initializer_list<ShaderPtr>(&m_vs, &m_ps + 1); }
			D3DPtr<ID3D11VertexShader>   VS() const { return m_vs ? m_vs->m_shdr : nullptr; }
			D3DPtr<ID3D11GeometryShader> GS() const { return m_gs ? m_gs->m_shdr : nullptr; }
			D3DPtr<ID3D11PixelShader>    PS() const { return m_ps ? m_ps->m_shdr : nullptr; }
		};

		// A mapping from render step to shader set
		struct ShaderMap
		{
			// A shader set per render step
			ShaderSet m_rstep[ERenderStep::NumberOf];

			ShaderMap() :m_rstep() {}

			// Access to the shaders for a given render step
			ShaderSet const& operator [](ERenderStep rstep) const { assert(rstep >= 0 && rstep < ERenderStep::NumberOf); return m_rstep[rstep]; }
			ShaderSet&       operator [](ERenderStep rstep)       { assert(rstep >= 0 && rstep < ERenderStep::NumberOf); return m_rstep[rstep]; }
		};

		inline bool operator == (ShaderSet const& lhs, ShaderSet const& rhs)
		{
			return
				lhs.m_vs == rhs.m_vs &&
				lhs.m_ps == rhs.m_ps &&
				lhs.m_gs == rhs.m_gs;
		}
		inline bool operator != (ShaderSet const& lhs, ShaderSet const& rhs) { return !(lhs == rhs); }
		inline bool operator == (ShaderMap const& lhs, ShaderMap const& rhs)
		{
			for (auto rs : ERenderStep::Members())
				if (lhs[rs] != rhs[rs])
					return false;
			return true;
		}
		inline bool operator != (ShaderMap const& lhs, ShaderMap const& rhs) { return !(lhs == rhs); }
	}
}