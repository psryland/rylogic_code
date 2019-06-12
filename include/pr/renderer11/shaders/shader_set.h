//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/util/stock_resources.h"

namespace pr::rdr
{
	// A set of shader pointers
	struct ShaderSet1
	{
		Shader* m_vs;
		Shader* m_ps;
		Shader* m_gs;
		Shader* m_cs;

		ShaderSet1()
			:m_vs()
			,m_ps()
			,m_gs()
			,m_cs()
		{}
		ShaderSet1(Shader* vs, Shader* ps, Shader* gs, Shader* cs)
			:m_vs(vs)
			,m_ps(ps)
			,m_gs(gs)
			,m_cs(cs)
		{}
		std::initializer_list<Shader*> Enumerate() const
		{
			return std::initializer_list<Shader*>(&m_vs, &m_vs + 4);
		}
		ID3D11VertexShader* VS() const
		{
			if (m_vs == nullptr) return nullptr;
			return static_cast<ID3D11VertexShader*>(m_vs->m_dx_shdr.get());
		}
		ID3D11PixelShader* PS() const
		{
			if (m_ps == nullptr) return nullptr;
			return static_cast<ID3D11PixelShader*>(m_ps->m_dx_shdr.get());
		}
		ID3D11GeometryShader* GS() const
		{
			if (m_gs == nullptr) return nullptr;
			return static_cast<ID3D11GeometryShader*>(m_gs->m_dx_shdr.get());
		}
		ID3D11ComputeShader* CS() const
		{ 
			if (m_cs == nullptr) return nullptr;
			return static_cast<ID3D11ComputeShader*>(m_cs->m_dx_shdr.get());
		}

		// Equality
		friend bool operator == (ShaderSet1 const& lhs, ShaderSet1 const& rhs)
		{
			return
				lhs.m_vs == rhs.m_vs &&
				lhs.m_ps == rhs.m_ps &&
				lhs.m_gs == rhs.m_gs &&
				lhs.m_cs == rhs.m_cs;
		}
		friend bool operator != (ShaderSet1 const& lhs, ShaderSet1 const& rhs)
		{
			return !(lhs == rhs);
		}
	};

	// A collection of owned shader instances
	struct ShaderSet0
	{
		ShaderPtr m_vs;
		ShaderPtr m_ps;
		ShaderPtr m_gs;
		ShaderPtr m_cs;

		ShaderSet0()
			:m_vs()
			,m_ps()
			,m_gs()
			,m_cs()
		{}
		ShaderSet0(ShaderPtr vs, ShaderPtr ps, ShaderPtr gs, ShaderPtr cs)
			:m_vs(vs)
			,m_ps(ps)
			,m_gs(gs)
			,m_cs(cs)
		{}
		std::initializer_list<ShaderPtr> Enumerate() const
		{
			return std::initializer_list<ShaderPtr>(&m_vs, &m_vs + 4);
		}
		ID3D11VertexShader*   VS() const { return m_vs != nullptr ? static_cast<ID3D11VertexShader*  >(m_vs->m_dx_shdr.get()) : nullptr; }
		ID3D11PixelShader*    PS() const { return m_ps != nullptr ? static_cast<ID3D11PixelShader*   >(m_ps->m_dx_shdr.get()) : nullptr; }
		ID3D11GeometryShader* GS() const { return m_gs != nullptr ? static_cast<ID3D11GeometryShader*>(m_gs->m_dx_shdr.get()) : nullptr; }
		ID3D11ComputeShader*  CS() const { return m_cs != nullptr ? static_cast<ID3D11ComputeShader* >(m_cs->m_dx_shdr.get()) : nullptr; }

		// Implicit conversion to non-ownership pointers
		operator ShaderSet1() const
		{
			return ShaderSet1(m_vs.get(), m_ps.get(), m_gs.get(), m_cs.get());
		}

		// Equality
		friend bool operator == (ShaderSet0 const& lhs, ShaderSet0 const& rhs)
		{
			return
				lhs.m_vs == rhs.m_vs &&
				lhs.m_ps == rhs.m_ps &&
				lhs.m_gs == rhs.m_gs &&
				lhs.m_cs == rhs.m_cs;
		}
		friend bool operator != (ShaderSet0 const& lhs, ShaderSet0 const& rhs)
		{
			return !(lhs == rhs);
		}
	};

	// A mapping from render step to shader set
	struct ShaderMap
	{
		// An owned set of shaders per render step
		ShaderSet0 m_rstep[Enum<ERenderStep>::NumberOf];

		ShaderMap()
			:m_rstep()
		{}

		// Access to the shaders for a given render step
		ShaderSet0 const& operator [](ERenderStep rstep) const
		{
			assert(int(rstep) >= 0 && int(rstep) < Enum<ERenderStep>::NumberOf);
			return m_rstep[(int)rstep];
		}
		ShaderSet0& operator [](ERenderStep rstep)
		{
			assert(int(rstep) >= 0 && int(rstep) < Enum<ERenderStep>::NumberOf);
			return m_rstep[(int)rstep];
		}

		// Equality
		friend bool operator == (ShaderMap const& lhs, ShaderMap const& rhs)
		{
			for (int rs = 0; rs != Enum<ERenderStep>::NumberOf; ++rs)
			{
				if (lhs[ERenderStep(rs)] == rhs[ERenderStep(rs)]) continue;
				return false;
			}
			return true;
		}
		friend bool operator != (ShaderMap const& lhs, ShaderMap const& rhs)
		{
			return !(lhs == rhs);
		}
	};
}