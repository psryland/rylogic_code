//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d/forward.h"

namespace pr::rdr
{
	struct alignas(16) Light
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
		float    m_cast_shadow;    // Shadow cast range as a fraction of the viewport depth, 0 for off
		bool     m_cam_relative;   // True if the light should move with the camera
		bool     m_on;             // True if this light is on

		Light();
		bool IsValid() const;

		// Returns a light to world transform appropriate for this light type and facing 'centre'
		pr::m4x4 LightToWorld(pr::v4 const& centre, float centre_dist) const;

		// Returns a projection transform appropriate for this light type
		pr::m4x4 Projection(float centre_dist) const;

		// Get/Set light settings
		// throws pr::Exception<HRESULT> if the settings are invalid
		std::wstring Settings() const;
		void Settings(std::wstring_view settings);

		// Operators
		friend bool operator == (Light const& lhs, Light const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(Light)) == 0;
		}
		friend bool operator != (Light const& lhs, Light const& rhs)
		{
			return !(lhs == rhs);
		}
	};
}
