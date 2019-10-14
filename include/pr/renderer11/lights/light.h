//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"

namespace pr::rdr
{
	struct Light :pr::AlignTo<16>
	{
		v4       m_position;       // Position, only valid for point,spot lights
		v4       m_direction;      // Direction, only valid for directional,spot lights
		ELight   m_type;           // One of ambient, directional, point, spot
		Colour32 m_ambient;        // Ambient light colour
		Colour32 m_diffuse;        // Main light colour
		Colour32 m_specular;       // Specular light colour
		float    m_specular_power; // Specular power (controls specular spot size)
		float    m_range;          // Light range
		float    m_falloff;        // Intensity falloff per unit distance
		float    m_inner_angle;    // Spot light inner angle 100% light (in radians)
		float    m_outer_angle;    // Spot light outer angle 0% light (in radians)
		float    m_cast_shadow;    // Shadow cast range, 0 for off
		bool     m_on;             // True if this light is on
		bool     m_cam_relative;   // True if the light should move with the camera

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
