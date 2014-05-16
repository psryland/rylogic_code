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
			:m_c2w          (m4x4Identity)
			,m_c2s          ()
			,m_fovY         (pr::maths::tau_by_8)
			,m_aspect       (1.0f)
			,m_centre_dist  (1.0f)
			,m_near         (0.01f)
			,m_far          (1.0e8f)
			,m_orthographic (false)
		{
			PR_ASSERT(PR_DBG_RDR, pr::meta::is_aligned<SceneView>(this), "My alignment is broke");

			UpdateCameraToScreen();
			PR_ASSERT(PR_DBG_RDR, pr::IsFinite(m_c2w) && pr::IsFinite(m_c2s) && pr::IsFinite(m_fovY) && pr::IsFinite(m_aspect) && pr::IsFinite(m_centre_dist), "invalid scene view parameters");
		}
		SceneView::SceneView(pr::m4x4 const& c2w, float fovY, float aspect, float centre_dist, bool orthographic)
			:m_c2w         (c2w)
			,m_c2s         ()
			,m_fovY        (fovY)
			,m_aspect      (aspect)
			,m_centre_dist (centre_dist)
			,m_near        (0.01f)
			,m_far         (1.0e8f)
			,m_orthographic(orthographic)
		{
			UpdateCameraToScreen();
			PR_ASSERT(PR_DBG_RDR, pr::IsFinite(m_c2w) && pr::IsFinite(m_c2s) && pr::IsFinite(m_fovY) && pr::IsFinite(m_aspect) && pr::IsFinite(m_centre_dist), "invalid scene view parameters");
		}
		SceneView::SceneView(pr::Camera const& cam)
			:m_c2w         (cam.CameraToWorld())
			,m_c2s         (cam.CameraToScreen())
			,m_fovY        (cam.m_fovY)
			,m_aspect      (cam.m_aspect)
			,m_centre_dist (cam.m_focus_dist)
			,m_near        (cam.Near())
			,m_far         (cam.Far())
			,m_orthographic(cam.m_orthographic)
		{
			PR_ASSERT(PR_DBG_RDR, pr::IsFinite(m_c2w) && pr::IsFinite(m_c2s) && pr::IsFinite(m_fovY) && pr::IsFinite(m_aspect) && pr::IsFinite(m_centre_dist), "invalid scene view parameters");
		}

		// Set the camera to screen transform based on the other view properties
		void SceneView::UpdateCameraToScreen()
		{
			// Note: the aspect ratio is independent of 'm_viewport' in the scene allowing the view to be stretched
			float height = 2.0f * m_centre_dist * pr::Tan(m_fovY * 0.5f);
			m_c2s = m_orthographic
				? ProjectionOrthographic  (height*m_aspect ,height   ,m_near ,m_far ,true)
				: ProjectionPerspectiveFOV(m_fovY          ,m_aspect ,m_near ,m_far ,true);
		}

		// Return the scene views for the left and right eye in stereoscopic view
		void SceneView::Stereo(float separation, SceneView (&eye)[EEye::NumberOf]) const
		{
			v4 sep = 0.5f * separation * m_c2w.x;

			v4 focus_point = FocusPoint();
			m4x4 lc2w = LookAt(m_c2w.pos - sep, focus_point, m_c2w.y);
			m4x4 rc2w = LookAt(m_c2w.pos + sep, focus_point, m_c2w.y);

			eye[EEye::Left ] = SceneView(lc2w, m_fovY, m_aspect, Length3(lc2w.pos - focus_point), m_orthographic);
			eye[EEye::Right] = SceneView(rc2w, m_fovY, m_aspect, Length3(rc2w.pos - focus_point), m_orthographic);
		}
	}
}