//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
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
			v4       m_position;        // Position, only valid for point,spot lights
			v4       m_direction;       // Direction, only valid for directional,spot lights
			ELight   m_type;            // One of ambient, directional, point, spot
			Colour32 m_ambient;         // Ambient light colour
			Colour32 m_diffuse;         // Main light colour
			Colour32 m_specular;        // Specular light colour
			float    m_specular_power;  // Specular power (controls specular spot size)
			float    m_inner_cos_angle; // Spot light inner angle 100% light
			float    m_outer_cos_angle; // Spot light outer angle 0% light
			float    m_range;           // Light range, only valid for point,spot lights
			float    m_falloff;         // Intensity falloff per unit distance
			float    m_cast_shadow;     // Shadow cast range, 0 for off
			bool     m_on;              // True if this light is on

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
