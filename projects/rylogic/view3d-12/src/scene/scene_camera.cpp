//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/scene/scene_camera.h"

namespace pr::rdr12
{
	// Construct scene views
	SceneCamera::SceneCamera()
		:Camera()
		,m_shadow_zfar(10.0f)
		,m_shadow_max_caster_dist(20.0f)
	{
		PR_ASSERT(PR_DBG_RDR, pr::meta::is_aligned<SceneCamera>(this), "My alignment is broke");
	}
	SceneCamera::SceneCamera(pr::Camera const& cam)
		:Camera(cam)
		,m_shadow_zfar(3.0f * cam.FocusDist())
		,m_shadow_max_caster_dist(4.0f * cam.FocusDist())
	{}
	SceneCamera::SceneCamera(m4x4 const& c2w, float fovY, float aspect, float focus_dist, bool orthographic, float near_, float far_)
		:pr::Camera(c2w, fovY, aspect, focus_dist, orthographic, near_, far_)
		,m_shadow_zfar(3.0f * focus_dist)
		,m_shadow_max_caster_dist(4.0f * focus_dist)
	{}

	// Return the scene views for the left and right eye in stereoscopic view
	void SceneCamera::Stereo(float separation, SceneCamera (&eye)[Enum<EEye>::NumberOf]) const
	{
		auto sep = 0.5f * separation * m_c2w.x;
		auto focus_point = FocusPoint();
		auto lc2w = m4x4::LookAt(m_c2w.pos - sep, focus_point, m_c2w.y);
		auto rc2w = m4x4::LookAt(m_c2w.pos + sep, focus_point, m_c2w.y);

		eye[(int)EEye::Left ] = SceneCamera(lc2w, m_fovY, m_aspect, Length(lc2w.pos - focus_point), m_orthographic);
		eye[(int)EEye::Right] = SceneCamera(rc2w, m_fovY, m_aspect, Length(rc2w.pos - focus_point), m_orthographic);
	}
}