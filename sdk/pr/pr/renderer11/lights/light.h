//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************

#pragma once
#ifndef PR_RDR_LIGHT_H
#define PR_RDR_LIGHT_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		struct Light
		{
			v4           m_position;
			v4           m_direction;
			ELight       m_type;
			Colour32     m_ambient;
			Colour32     m_diffuse;
			Colour32     m_specular;
			float        m_specular_power;
			float        m_inner_cos_angle;
			float        m_outer_cos_angle;
			float        m_range;
			float        m_falloff;
			bool         m_cast_shadows;
			bool         m_on;

			Light();
			bool IsValid() const;

			// Returns a light to world transform appropriate for this light type and facing 'centre'
			pr::m4x4 LightToWorld(pr::v4 const& centre, float centre_dist) const;

			// Returns a projection transform appropriate for this light type
			pr::m4x4 Projection(float centre_dist) const;

			// Get/Set light settings
			// throws pr::Exception<HRESULT> if the settings are invalid
			std::string Settings() const;
			void Settings(char const* settings);
		};

		inline bool operator == (Light const& lhs, Light const& rhs) { return memcmp(&lhs, &rhs, sizeof(Light)) == 0; }
		inline bool operator != (Light const& lhs, Light const& rhs) { return !(lhs == rhs); }
	}
}

#endif
