//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// The properties of a camera that looks into the scene
		struct SceneView
		{
			m4x4  m_c2w;          // Camera to world, or InvView transform
			m4x4  m_c2s;          // Camera to Screen, or Projection transform
			float m_fovY;         // FOV
			float m_aspect;       // Aspect ratio = width / height
			float m_centre_dist;  // Distance to the centre of the frustum
			float m_near;         // The distance to the near plane
			float m_far;          // The distance to the far plane
			bool  m_orthographic; // True for orthographic projection

			SceneView();
			SceneView(pr::m4x4 const& c2w, float fovY, float aspect, float centre_dist, bool orthographic);
			SceneView(pr::Camera const& cam);

			// Return the view frustum in camera space
			pr::Frustum Frustum(float zfar) const { return Frustum::makeFA(m_fovY, m_aspect, zfar); }
			pr::Frustum Frustum() const           { return Frustum(m_far); }

			// The world space position of the camera focus point
			pr::v4 FocusPoint() const { return m_c2w.pos - m_c2w.z * m_centre_dist; }

			// Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
			pr::v2 ViewArea(float dist) const
			{
				auto h = 2.0f * pr::Tan(m_fovY * 0.5f);
				return m_orthographic
					? pr::v2::make(h * m_aspect, h)
					: pr::v2::make(dist * h * m_aspect, dist * h);
			}

			// Update the camera to screen projection transform
			void UpdateCameraToScreen();

			// Return the scene views for the left and right eye in stereoscopic view
			void Stereo(float separation, SceneView (&eye)[EEye::NumberOf]) const;
		};
	}
}
