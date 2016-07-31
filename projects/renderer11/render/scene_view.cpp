//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene_view.h"

namespace pr
{
	namespace rdr
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
			,m_shadow_zfar(FocusRelativeDistance(3.0f))
			,m_shadow_max_caster_dist(FocusRelativeDistance(4.0f))
		{}
		SceneView::SceneView(m4x4 const& c2w, float fovY, float aspect, float focus_dist, bool orthographic, float near_, float far_)
			:pr::Camera(c2w, fovY, aspect, focus_dist, orthographic, near_, far_)
			,m_shadow_zfar(FocusRelativeDistance(3.0f))
			,m_shadow_max_caster_dist(FocusRelativeDistance(4.0f))
		{}

		// Return the scene views for the left and right eye in stereoscopic view
		void SceneView::Stereo(float separation, SceneView (&eye)[EEye::NumberOf]) const
		{
			auto sep = 0.5f * separation * m_c2w.x;
			auto focus_point = FocusPoint();
			auto lc2w = m4x4::LookAt(m_c2w.pos - sep, focus_point, m_c2w.y);
			auto rc2w = m4x4::LookAt(m_c2w.pos + sep, focus_point, m_c2w.y);

			eye[EEye::Left ] = SceneView(lc2w, m_fovY, m_aspect, Length3(lc2w.pos - focus_point), m_orthographic);
			eye[EEye::Right] = SceneView(rc2w, m_fovY, m_aspect, Length3(rc2w.pos - focus_point), m_orthographic);
		}
	}
}