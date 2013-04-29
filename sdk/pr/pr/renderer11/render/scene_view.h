//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_SCENE_VIEW_H
#define PR_RDR_RENDER_SCENE_VIEW_H

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
			pr::v4 FocusPoint() const { return m_c2w.pos - m_c2w.z * m_centre_dist; }
			void UpdateCameraToScreen();

			// Return the scene views for the left and right eye in stereoscopic view
			void Stereo(float separation, SceneView (&eye)[EEye::NumberOf]) const
			{
				v4 sep = 0.5f * separation * m_c2w.x;

				v4 focus_point = FocusPoint();
				m4x4 lc2w = LookAt(m_c2w.pos - sep, focus_point, m_c2w.y);
				m4x4 rc2w = LookAt(m_c2w.pos + sep, focus_point, m_c2w.y);

				eye[EEye::Left ] = SceneView(lc2w, m_fovY, m_aspect, Length3(lc2w.pos - focus_point), m_orthographic);
				eye[EEye::Right] = SceneView(rc2w, m_fovY, m_aspect, Length3(rc2w.pos - focus_point), m_orthographic);
			}
		};
	}
}

#endif
