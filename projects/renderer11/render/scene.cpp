//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "renderer11/render/draw.h"

using namespace pr::rdr;

// Construct scene views
pr::rdr::SceneView::SceneView()
:m_c2w          (m4x4Identity)
,m_c2s          ()
,m_fovY         (pr::maths::tau_by_8)
,m_aspect       (1.0f)
,m_centre_dist  (1.0f)
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
,m_orthographic(cam.m_orthographic)
{
	PR_ASSERT(PR_DBG_RDR, pr::IsFinite(m_c2w) && pr::IsFinite(m_c2s) && pr::IsFinite(m_fovY) && pr::IsFinite(m_aspect) && pr::IsFinite(m_centre_dist), "invalid scene view parameters");
}

// Set the camera to screen transform based on the other view properties
void pr::rdr::SceneView::UpdateCameraToScreen()
{
	// Note: the aspect ratio is independent of 'm_viewport' in the scene allowing the view to be stretched
	float height = 2.0f * m_centre_dist * pr::Tan(m_fovY * 0.5f);
	if (m_orthographic) ProjectionOrthographic  (m_c2s ,height*m_aspect ,height   ,NearPlane() ,FarPlane() ,true);
	else                ProjectionPerspectiveFOV(m_c2s ,m_fovY          ,m_aspect ,NearPlane() ,FarPlane() ,true);
}

// ***********************************************************

// Make a scene
pr::rdr::Scene::Scene(pr::Renderer& rdr, pr::rdr::ERenderMethod::Type method, SceneView const& view)
:m_rdr(&rdr)
,m_viewport(m_rdr->DisplayRect())
,m_view(view)
,m_drawlist(rdr)
,m_gbuffer()
,m_rdr_method(ERenderMethod::None)
,m_background_colour(pr::ColourBlack)
{
	// Do setup for the choosen render method
	SetRenderMethod(method);
}

// Change the render method for this scene
void pr::rdr::Scene::SetRenderMethod(pr::rdr::ERenderMethod::Type method)
{
	if (m_rdr_method == method) return;
	m_rdr_method = method;
	
	switch (m_rdr_method)
	{
	default: throw pr::Exception<HRESULT>(E_FAIL, "Unknown rendering method");
	case pr::rdr::ERenderMethod::Forward:
		{
			break;
		}
	case pr::rdr::ERenderMethod::Deferred:
		{
			IRect rect = m_rdr->DisplayRect();
			m_gbuffer.Init(m_rdr->Device(),rect.SizeX(), rect.SizeY());
			break;
		}
	}
}

// Render the draw list for this viewport
void pr::rdr::Scene::Render(bool clear_bb)
{
	D3DPtr<ID3D11DeviceContext> ctx = m_rdr->ImmediateDC();
	Render(ctx, clear_bb);
}
void pr::rdr::Scene::Render(D3DPtr<ID3D11DeviceContext>& ctx, bool clear_bb)
{
	// Ensure the drawlist is sorted
	m_drawlist.SortIfNeeded();
	
	// Do the rendering
	switch (m_rdr_method)
	{
	default: throw pr::Exception<HRESULT>(E_FAIL, "Unknown rendering method");
	case pr::rdr::ERenderMethod::Deferred: return RenderDeferred(ctx, clear_bb);
	case pr::rdr::ERenderMethod::Forward:  return RenderForward(ctx, clear_bb);
	}
}

// Render the scene using the deferred rendering technique
void pr::rdr::Scene::RenderDeferred(D3DPtr<ID3D11DeviceContext>& ctx, bool clear_bb)
{
	(void)ctx;
	// Clear the render targets
	if (clear_bb)
	{
		//m_gbuffer.Clear(ctx);
	}
	
	// Loop over the elements in the draw list
	Drawlist::DLECont::const_iterator element     = m_drawlist.begin();
	Drawlist::DLECont::const_iterator element_end = m_drawlist.end();
	while (element != element_end)
	{
		//Material const& mat = element->Material();
		++element;
	}
}

// Render the scene using the standard forward rendering technique
void pr::rdr::Scene::RenderForward(D3DPtr<ID3D11DeviceContext>& ctx, bool clear_bb)
{
	// Create a draw helper
	Draw draw(ctx);
	
	// Clear the back buffer and depth/stencil
	if (clear_bb)
	{
		draw.ClearBB(m_background_colour);
		draw.ClearDB();
	}
	
	// Loop over the elements in the draw list
	Drawlist::DLECont::const_iterator element     = m_drawlist.begin();
	Drawlist::DLECont::const_iterator element_end = m_drawlist.end();
	for (;element != element_end; ++element)
	{
		// Set the device for this nugget
		draw.Setup(*element, m_view);
		
		// Draw the element
		draw.Render(*element->m_nugget);
	}
}
