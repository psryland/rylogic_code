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
			ShaderPtr m_cs;

			ShaderSet() :m_vs() ,m_gs() ,m_ps() ,m_cs() {}
			ShaderSet(ShaderPtr vs, ShaderPtr gs, ShaderPtr ps, ShaderPtr cs) :m_vs(vs) ,m_gs(gs) ,m_ps(ps) ,m_cs(cs) {}
			std::initializer_list<ShaderPtr> Enumerate() const { return std::initializer_list<ShaderPtr>(&m_vs, &m_cs + 1); }
			D3DPtr<ID3D11VertexShader>   VS() const { return m_vs ? m_vs->m_dx_shdr : nullptr; }
			D3DPtr<ID3D11GeometryShader> GS() const { return m_gs ? m_gs->m_dx_shdr : nullptr; }
			D3DPtr<ID3D11PixelShader>    PS() const { return m_ps ? m_ps->m_dx_shdr : nullptr; }
			D3DPtr<ID3D11ComputeShader>  CS() const { return m_cs ? m_cs->m_dx_shdr : nullptr; }
		};
		inline bool operator == (ShaderSet const& lhs, ShaderSet const& rhs)
		{
			return
				lhs.m_vs == rhs.m_vs &&
				lhs.m_ps == rhs.m_ps &&
				lhs.m_gs == rhs.m_gs &&
				lhs.m_cs == rhs.m_cs;
		}
		inline bool operator != (ShaderSet const& lhs, ShaderSet const& rhs)
		{
			return !(lhs == rhs);
		}

		// A mapping from render step to shader set
		struct ShaderMap
		{
			// A shader set per render step
			ShaderSet m_rstep[Enum<ERenderStep>::NumberOf];

			ShaderMap()
				:m_rstep()
			{}

			// Access to the shaders for a given render step
			ShaderSet const& operator [](ERenderStep rstep) const
			{
				assert(int(rstep) >= 0 && int(rstep) < Enum<ERenderStep>::NumberOf);
				return m_rstep[(int)rstep];
			}
			ShaderSet& operator [](ERenderStep rstep)
			{
				assert(int(rstep) >= 0 && int(rstep) < Enum<ERenderStep>::NumberOf);
				return m_rstep[(int)rstep];
			}
		};
		inline bool operator == (ShaderMap const& lhs, ShaderMap const& rhs)
		{
			for (int rs = 0; rs != Enum<ERenderStep>::NumberOf; ++rs)
				if (lhs[ERenderStep(rs)] != rhs[ERenderStep(rs)])
					return false;
			return true;
		}
		inline bool operator != (ShaderMap const& lhs, ShaderMap const& rhs)
		{
			return !(lhs == rhs);
		}
	}
}