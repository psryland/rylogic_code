//***********************************************************************************
//
// Viewport - A place where rendererables get drawn
//
//***********************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/Viewport.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/DrawListElement.h"

using namespace pr;
using namespace pr::rdr;

//*****
// Construction
Viewport::Viewport(const VPSettings& settings)
:m_settings			(settings)
,m_d3d_viewport		()
,m_render_state		()
,m_drawlist			()
{
	// Normalise the settings
	SetViewportRect(m_settings.m_viewport_rect);
	m_settings.UpdateProjectionMatrix();
	
	if( m_settings.m_righthanded )	SetRenderState(D3DRS_CULLMODE, D3DCULL_CCW);
	else							SetRenderState(D3DRS_CULLMODE, D3DCULL_CW);

	// Register with the renderer
	m_settings.m_renderer->RegisterViewport(*this);

	// Create the element vertex/index buffers
	CreateDeviceDependentObjects();
}

//*****
// Release everything
Viewport::~Viewport()
{
	m_drawlist.Clear();
	ReleaseDeviceDependentObjects();
	m_settings.m_renderer->UnregisterViewport(*this);
}

//***********************************************************************************
// Viewport Privates
//*****
// Create the device dependent objects
HRESULT Viewport::CreateDeviceDependentObjects()
{
	// The size of the main window may have changed so we need to adjust our viewport
	SetViewportRect(m_settings.m_viewport_rect);
	return S_OK;
}

//*****
// Release the device dependent objects
void Viewport::ReleaseDeviceDependentObjects()
{
	// Empty the draw list. We mustn't draw anything from the old d3d device
	m_drawlist.Clear();
}

//*****
// Update the viewport area
void Viewport::SetViewportRect(const FRect& viewport_rect)
{
	m_settings.m_viewport_rect = viewport_rect;
	PR_ASSERT(PR_DBG_RDR, m_settings.m_viewport_rect.Area() > 0.0f);

	IRect client_area = m_settings.m_renderer->GetClientArea();

	m_d3d_viewport.X		= Clamp<DWORD>((DWORD)(m_settings.m_viewport_rect.m_left	* client_area.Width ()) ,0 ,client_area.Width () - 1);
	m_d3d_viewport.Y		= Clamp<DWORD>((DWORD)(m_settings.m_viewport_rect.m_top		* client_area.Height()) ,0 ,client_area.Height() - 1);
	m_d3d_viewport.Width	= Clamp<DWORD>((DWORD)(m_settings.m_viewport_rect.Width()	* client_area.Width ()) ,1 ,client_area.Width ());
	m_d3d_viewport.Height	= Clamp<DWORD>((DWORD)(m_settings.m_viewport_rect.Height()	* client_area.Height()) ,1 ,client_area.Height());
	m_d3d_viewport.MinZ		= 0.0f;
	m_d3d_viewport.MaxZ		= 1.0f;

	m_settings.UpdateProjectionMatrix();
}

//*****
// Draw the nuggets for this viewport
void Viewport::Render()
{
	// Set the state of the renderer ready for this viewport
	m_settings.m_renderer->m_render_state_manager.PushViewport(this);
	
	// Loop over the elements in the draw list
	const DrawListElement* element		= m_drawlist.Begin();
	const DrawListElement* element_end	= m_drawlist.End();
	while( element != element_end )
	{
		// Get the material with which to renderer this element
		Material material = element->GetMaterial();
		
		// Get the effect to set itself up
		material.m_effect->PrePass();

		// Begin the effect
		uint num_passes;
		Verify(material.m_effect->Begin(&num_passes, 0));
		const DrawListElement* pass_start = element;
		
		// Begin the pass
		for( uint p = 0; p != num_passes; ++p )
		{
			element = pass_start;           
			Verify(material.m_effect->BeginPass(p));

			// Loop over the draw list elements that are using this effect
			do
			{
				// Set effect properties specific to this draw list element
				if( material.m_effect->MidPass(*this, *element) )
				{
					Verify(material.m_effect->CommitChanges());
				}

				// Draw the element
				RenderDrawListElement(*element);
				element = element->m_drawlist_next;
			}
			while( element != element_end && element->GetMaterial().m_effect == material.m_effect );
			
			// End this pass
			material.m_effect->EndPass();
		}

		// End the effect
		material.m_effect->PostPass();
		material.m_effect->End();
	}

	// Reset the draw list
	m_drawlist.Clear();

	// Remove the viewports render states
	m_settings.m_renderer->m_render_state_manager.PopViewport(this);
}

//*****
// Interpret a draw list element and render it
void Viewport::RenderDrawListElement(const DrawListElement& element)
{
	// Set the state of the renderer ready for this element
	m_settings.m_renderer->m_render_state_manager.PushDrawListElement(&element);

	// Draw the element
	if( element.m_nugget->m_owner->m_primitive_type != D3DPT_POINTLIST )
	{
		Verify(m_settings.m_renderer->GetD3DDevice()->DrawIndexedPrimitive(
			element.m_nugget->m_owner->m_primitive_type,
			0,
			element.m_nugget->m_vertex_byte_offset / vf::GetSize(element.m_nugget->m_owner->m_vertex_type),
			element.m_nugget->m_vertex_length,
			element.m_nugget->m_index_byte_offset / sizeof(Index),
			element.m_nugget->m_number_of_primitives));
	}
	else
	{
		Verify(m_settings.m_renderer->GetD3DDevice()->DrawPrimitive(
			element.m_nugget->m_owner->m_primitive_type,
			element.m_nugget->m_vertex_byte_offset / vf::GetSize(element.m_nugget->m_owner->m_vertex_type),
			element.m_nugget->m_number_of_primitives));
	}

	// Undo the render states for this element
	m_settings.m_renderer->m_render_state_manager.PopDrawListElement(&element);
}
