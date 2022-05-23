//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/scene_view.h"

namespace pr::rdr
{
	// Construct scene views
	SceneView::SceneView()
		:pr::Camera()
		,m_shadow_zfar(10.0f)
		,m_shadow_max_caster_dist(20.0f)
	{
		PR_ASSERT(PR_DBG_RDR, pr::meta::is_aligned<SceneView>(this), "My alignment is broke");
	}
	SceneView::SceneView(pr::Camera const& cam)
		:pr::Camera(cam)
		,m_shadow_zfar(s_cast<float>(3.0 * cam.FocusDist()))
		,m_shadow_max_caster_dist(s_cast<float>(4.0 * cam.FocusDist()))
	{}
	SceneView::SceneView(m4x4 const& c2w, float fovY, float aspect, float focus_dist, bool orthographic, float near_, float far_)
		:pr::Camera(c2w, fovY, aspect, focus_dist, orthographic, near_, far_)
		,m_shadow_zfar(3.0f * focus_dist)
		,m_shadow_max_caster_dist(4.0f * focus_dist)
	{}

	// Return the scene views for the left and right eye in stereoscopic view
	void SceneView::Stereo(float separation, SceneView (&eye)[Enum<EEye>::NumberOf]) const
	{
		auto sep = 0.5f * separation * m_c2w.x;
		auto focus_point = FocusPoint();
		auto lc2w = m4x4::LookAt(m_c2w.pos - sep, focus_point, m_c2w.y);
		auto rc2w = m4x4::LookAt(m_c2w.pos + sep, focus_point, m_c2w.y);

		eye[(int)EEye::Left ] = SceneView(lc2w, s_cast<float>(m_fovY), s_cast<float>(m_aspect), Length(lc2w.pos - focus_point), m_orthographic);
		eye[(int)EEye::Right] = SceneView(rc2w, s_cast<float>(m_fovY), s_cast<float>(m_aspect), Length(rc2w.pos - focus_point), m_orthographic);
	}
}