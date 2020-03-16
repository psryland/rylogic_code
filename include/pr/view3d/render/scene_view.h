//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"

namespace pr::rdr
{
	// The properties of a camera that looks into the scene
	struct SceneView :Camera
	{
		float m_shadow_zfar;            // The far plane for shadows
		float m_shadow_max_caster_dist; // The maximum distance for objects that cast shadows (used to normalise depth values in the smap)

		SceneView();
		SceneView(Camera const& cam);
		SceneView(m4x4 const& c2w, float fovY = pr::maths::tau_by_8, float aspect = 1.0f, float focus_dist = 1.0f, bool orthographic = false, float near_ = 0.01f, float far_ = 100.0f);

		// Return the view volume in which shadows are cast
		Frustum ShadowFrustum() const
		{
			return ViewFrustum(m_shadow_zfar);
		}

		// Return the scene views for the left and right eye in stereoscopic view
		void Stereo(float separation, SceneView (&eye)[Enum<EEye>::NumberOf]) const;
	};
}
