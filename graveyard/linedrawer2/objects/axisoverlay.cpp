//********************************************************************************************
//
//	Axis
//
//********************************************************************************************

#include "Stdafx.h"
#include "pr/renderer/renderer.h"
#include "LineDrawer/Objects/AxisOverlay.h"
#include "LineDrawer/Source/LineDrawer.h"

//*****
// Create an axis
void AxisOverlay::Create(Renderer& renderer, const Colour32& Xcolour, const Colour32& Ycolour, const Colour32& Zcolour)
{
	m_instance.m_base.m_cpt_count = NumComponentsForOverlay;

	rdr::model::Settings settings;
	settings.m_vertex_type		= rdr::vf::GetTypeFromGeomType(geometry::EType_Vertex | geometry::EType_Colour);
	settings.m_Vcount			= 6;
	settings.m_Icount			= 6;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }

	rdr::model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
	vb->set(v4::make( 0.0f,  0.0f,  0.0f, 1.0f), v4Zero, Xcolour);	++vb;
	vb->set(v4::make( 1.0f,  0.0f,  0.0f, 1.0f), v4Zero, Xcolour);	++vb;
	vb->set(v4::make( 0.0f,  0.0f,  0.0f, 1.0f), v4Zero, Ycolour);	++vb;
	vb->set(v4::make( 0.0f,  1.0f,  0.0f, 1.0f), v4Zero, Ycolour);	++vb;
	vb->set(v4::make( 0.0f,  0.0f,  0.0f, 1.0f), v4Zero, Zcolour);	++vb;
	vb->set(v4::make( 0.0f,  0.0f,  1.0f, 1.0f), v4Zero, Zcolour);	++vb;

	rdr::model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	ib[0] = 0; ib[1] = 1; ib[2] = 2; ib[3] = 3; ib[4] = 4; ib[5] = 5;

	rdr::Material mat = renderer.m_material_manager.GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Colour);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_LineList);
	m_instance.m_model->SetName("Axis Overlay");
	m_instance.m_sk_override.Set(EDrawOrder_Mask, EDrawOrder_Overlay);
	m_instance.m_render_state.SetRenderState(D3DRS_ZENABLE, D3DZB_FALSE);
	m_instance.m_instance_to_world.identity();
}

//*****
// Position the axis
void AxisOverlay::SetPositionAndScale(const v4& position, float scale)
{
	m_instance.m_instance_to_world[0][0] = scale;
	m_instance.m_instance_to_world[1][1] = scale;
	m_instance.m_instance_to_world[2][2] = scale;
	m_instance.m_instance_to_world[3]    = position;	
}

//*****
void AxisOverlay::SetTransform(const m4x4& txfm)
{
	m_instance.m_instance_to_world = txfm;
}

//*****
void AxisOverlay::SetProjection(float width, float height, float Near, float Far, bool righthanded)
{
	m_instance.m_camera_to_screen = ProjectionOrthographic(width, height, Near, Far, righthanded);
}

//*****
// Render the axis
void AxisOverlay::Render(rdr::Viewport& viewport)
{
	viewport.AddInstance(m_instance.m_base);
}

