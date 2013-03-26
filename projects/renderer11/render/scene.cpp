//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/draw_method.h"
#include "pr/renderer11/util/lock.h"
#include "renderer11/shaders/cbuffer.h"

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

// Scene ***********************************************************

// Make a scene
pr::rdr::Scene::Scene(pr::Renderer& rdr, SceneView const& view)
:m_rdr(&rdr)
,m_viewport(m_rdr->DisplayArea())
,m_view(view)
,m_drawlist(rdr)
,m_background_colour(pr::ColourBlack)
,m_cbuf_frame()
,m_rs(rdr.m_rs_mgr.RasterState(ERasterState::SolidCullBack))
{
	CBufFrame scene_constants = {};
	scene_constants.m_c2w = view.m_c2w;
	scene_constants.m_w2c = pr::GetInverse(view.m_c2w);
	SubResourceData init(scene_constants);
	CBufferDesc cbdesc(sizeof(CBufFrame));
	pr::Throw(rdr.Device()->CreateBuffer(&cbdesc, &init, &m_cbuf_frame.m_ptr));
	PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_frame, "CBufFrame"));
}

// Resize the viewport on back buffer resize
void pr::rdr::Scene::OnEvent(pr::rdr::Evt_Resize const& evt)
{
	if (evt.m_done)
		m_viewport = evt.m_area;
}

// Render the draw list for this viewport
void pr::rdr::Scene::Render(bool clear_bb) const
{
	D3DPtr<ID3D11DeviceContext> dc = m_rdr->ImmediateDC();
	Render(dc, clear_bb);
}
void pr::rdr::Scene::Render(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb) const
{
	dc->RSSetViewports(1, &m_viewport);
	
	{// Set the frame constant variables
		CBufFrame scene_constants;
		scene_constants.m_c2w = m_view.m_c2w;
		scene_constants.m_w2c = pr::GetInverse(m_view.m_c2w);
		*Lock(dc, m_cbuf_frame, 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufFrame>() = scene_constants;
		
		// Bind the frame constants to the shader
		dc->VSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);
	}
	
	DoRender(dc, clear_bb);
}

// SceneForward ***********************************************************

pr::rdr::SceneForward::SceneForward(pr::Renderer& rdr, SceneView const& view)
:Scene(rdr, view)
{}

// Render the scene using the standard forward rendering technique
void pr::rdr::SceneForward::DoRender(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb) const
{
	// Clear the back buffer and depth/stencil
	if (clear_bb)
	{
		D3DPtr<ID3D11RenderTargetView> rtv;
		D3DPtr<ID3D11DepthStencilView> dsv;
		dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
		dc->ClearRenderTargetView(rtv.m_ptr, m_background_colour);
		dc->ClearDepthStencilView(dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);
	}

	// Loop over the elements in the draw list
	Drawlist::DLECont::const_iterator dle = m_drawlist.begin(), dle_end = m_drawlist.end();
	for (;dle != dle_end; ++dle)
	{
		Nugget const&       nugget = *dle->m_nugget;
		BaseInstance const& inst   = *dle->m_instance;
		ShaderPtr const&    shader = nugget.m_draw.m_shader;

		// Bind the shader to the device
		shader->Setup(dc, nugget, inst, *this);

		// Add the nugget to the device context
		dc->DrawIndexed(
			UINT(nugget.m_irange.size()),
			UINT(nugget.m_irange.m_begin),
			UINT(nugget.m_vrange.m_begin));
	}
}

// SceneDeferred ***********************************************************

pr::rdr::SceneDeferred::SceneDeferred(pr::Renderer& rdr, SceneView const& view)
:Scene(rdr, view)
,m_gbuffer()
{
	pr::iv2 area = m_rdr->DisplayArea();
	m_gbuffer.Init(m_rdr->Device(),area.x, area.y);
}

// Render the scene using the deferred rendering technique
void pr::rdr::SceneDeferred::DoRender(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb) const
{
	(void)dc;
	(void)clear_bb;
	//// Clear the render targets
	//if (clear_bb)
	//{
	//	//m_gbuffer.Clear(dc);
	//}
	//
	//// Loop over the elements in the draw list
	//Drawlist::DLECont::const_iterator element     = m_drawlist.begin();
	//Drawlist::DLECont::const_iterator element_end = m_drawlist.end();
	//while (element != element_end)
	//{
	//	//Material const& mat = element->Material();
	//	++element;
	//}
	
	// Make Scene a base class, and derive ForwardRenderedScene and DeferredRenderedScene

	// Use a light volumes drawlist, that contains instances of light volume models
	// i.e. sphere = point light, full screen quad = directional light, cone = spot light, etc
	// Each light volume is a triangle list containing position and light index.
	// Light index is a lookup into a constants buffer describing light type, colour, position, direction, etc
	// Draw the light volumes drawlist with back face culling (only front faces drawn).
	// Output pixel is GBuffer pixel colour plus lighting for the light type
	// Do directional, ambient occlusion/reflection, depth of field, etc as a single pass using a full screen quad
	// after the other lights

	// GBuffer Pass:
	//  Build a render target containing position info for every visible pixel
	//  Draw things in the drawlist from [0, first_alpha)
	
	// Lighting Pass:
	//  Set the GBuffer as a source texture
	//  Draw the light volume draw list
	
	// Full Screen Pass:
	//  Draw a full screen quad and do final post-process pass
}

