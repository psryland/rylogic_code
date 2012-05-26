//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/draw_method.h"

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
void pr::rdr::Scene::RenderForward(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb)
{
	// Clear the back buffer and depth/stencil
	if (clear_bb)
	{
		D3DPtr<ID3D11RenderTargetView> rtv;
		D3DPtr<ID3D11DepthStencilView> dsv;
		dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
		dc->ClearRenderTargetView(rtv.m_ptr, pr::ColourBlack);
		dc->ClearDepthStencilView(dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);
	}
	
	// Loop over the elements in the draw list
	Drawlist::DLECont::const_iterator dle = m_drawlist.begin(), dle_end = m_drawlist.end();
	for (;dle != dle_end; ++dle)
	{
		BaseInstance const& inst    = *dle->m_instance;
		Nugget const&         nugget  = *dle->m_nugget;
		DrawMethod const&     meth    = nugget.m_draw;
		ModelBuffer const&    mb      = *nugget.m_model->m_model_buffer.m_ptr;
		
		// Bind the vertex buffer to the IA
		ID3D11Buffer* buffers[] = {mb.m_vb.m_ptr};
		UINT          strides[] = {mb.m_vb.m_stride};
		UINT          offsets[] = {(UINT)nugget.m_vrange.m_begin};
		dc->IASetVertexBuffers(0, 1, buffers, strides, offsets);
		
		// Set the input layout for this vertex buffer
		dc->IASetInputLayout(meth.m_shader->m_iplayout.m_ptr);
		
		// Bind the index buffer to the IA
		dc->IASetIndexBuffer(mb.m_ib.m_ptr, mb.m_ib.m_format, (UINT)nugget.m_irange.m_begin);
		
		// Tell the IA would sort of primitives to expect
		dc->IASetPrimitiveTopology(nugget.m_prim_topo);
		
		// Bind the shader to the device
		meth.m_shader->Setup(dc, meth, nugget, inst, m_view);
		
		// Add the nugget to the device context
		dc->DrawIndexed(
			UINT(nugget.m_irange.size()),
			UINT(nugget.m_irange.m_begin),
			UINT(nugget.m_vrange.m_begin));
	}
}
