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
			bool  m_orthographic; // True for orthographic projection
			
			SceneView();
			SceneView(pr::m4x4 const& c2w, float fovY, float aspect, float centre_dist, bool orthographic);
			SceneView(pr::Camera const& cam);
			float NearPlane() const   { return m_centre_dist * 0.01f; }
			float FarPlane() const    { return m_centre_dist * 100.0f; }
			pr::v4 FocusPoint() const { return m_c2w.pos - m_c2w.z * m_centre_dist; }
			void UpdateCameraToScreen();
		};
	}
}

#endif
