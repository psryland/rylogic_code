// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct Particle
	{
		v4 pos;
		v4 col;
		v4 vel;
		v3 acc;
		float density;

		inline static constexpr wchar_t const* Layout =
			L"struct PosType "
			L"{ "
			L"	float4 pos; "
			L"	float4 col; "
			L"	float4 vel; "
			L"	float3 accel; "
			L"	float density; "
			L"}";
	};

	// Particle is designed to be compatible with rdr12::Vert so that the
	// same buffer can be used for both particle and vertex data.
	static_assert(sizeof(Particle) == sizeof(rdr12::Vert));
	static_assert(alignof(Particle) == alignof(rdr12::Vert));
	static_assert(offsetof(Particle, pos) == offsetof(rdr12::Vert, m_vert));
	static_assert(offsetof(Particle, col) == offsetof(rdr12::Vert, m_diff));
	static_assert(offsetof(Particle, vel) == offsetof(rdr12::Vert, m_norm));
	static_assert(offsetof(Particle, acc) == offsetof(rdr12::Vert, m_tex0));

}
