//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene_view.h"

using namespace pr::rdr;

// Construct scene views
pr::rdr::SceneView::SceneView()
	:m_c2w          (m4x4Identity)
	,m_c2s          ()
	,m_fovY         (pr::maths::tau_by_8)
	,m_aspect       (1.0f)
	,m_centre_dist  (1.0f)
	,m_near         (0.01f)
	,m_far          (1.0e8f)
	,m_orthographic (false)
{
	UpdateCameraToScreen();
	PR_ASSERT(PR_DBG_RDR, pr::IsFinite(m_c2w) && pr::IsFinite(m_c2s) && pr::IsFinite(m_fovY) && pr::IsFinite(m_aspect) && pr::IsFinite(m_centre_dist), "invalid scene view parameters");
}
pr::rdr::SceneView::SceneView(pr::m4x4 const& c2w, float fovY, float aspect, float centre_dist, bool orthographic)
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
pr::rdr::SceneView::SceneView(pr::Camera const& cam)
	:m_c2w         (cam.CameraToWorld())
	,m_c2s         (cam.CameraToScreen())
	,m_fovY        (cam.m_fovY)
	,m_aspect      (cam.m_aspect)
	,m_centre_dist (cam.m_focus_dist)
	,m_near        (0.01f)
	,m_far         (1.0e8f)
	,m_orthographic(cam.m_orthographic)
{
	PR_ASSERT(PR_DBG_RDR, pr::IsFinite(m_c2w) && pr::IsFinite(m_c2s) && pr::IsFinite(m_fovY) && pr::IsFinite(m_aspect) && pr::IsFinite(m_centre_dist), "invalid scene view parameters");
}

// Set the camera to screen transform based on the other view properties
void pr::rdr::SceneView::UpdateCameraToScreen()
{
	// Note: the aspect ratio is independent of 'm_viewport' in the scene allowing the view to be stretched
	float height = 2.0f * m_centre_dist * pr::Tan(m_fovY * 0.5f);
	if (m_orthographic) ProjectionOrthographic  (m_c2s ,height*m_aspect ,height   ,m_near ,m_far ,true);
	else                ProjectionPerspectiveFOV(m_c2s ,m_fovY          ,m_aspect ,m_near ,m_far ,true);
}
