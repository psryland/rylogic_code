//********************************************************************************************
//
//	Selection Box
//
//********************************************************************************************

#include "stdafx.h"
#include "pr/renderer/renderer.h"
#include "LineDrawer/Objects/SelectionBox.h"
#include "LineDrawer/Source/LineDrawer.h"

//*****
// Create a box to show selections
void SelectionBox::Create(Renderer& renderer)
{
	m_instance.m_base.m_cpt_count = NumComponents;

	rdr::model::Settings settings;
	settings.m_vertex_type = rdr::vf::GetTypeFromGeomType(geom::VC);
	settings.m_Vcount      = 32;
	settings.m_Icount      = 48;
	if( Failed(LineDrawer::Get().CreateModel(settings, m_instance.m_model)) ) { return; }
	
	rdr::model::VLock vlock;
	rdr::vf::iterator vb = m_instance.m_model->LockVBuffer(vlock);
 	vb->set(v4::make(-0.5f, -0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.4f, -0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f, -0.4f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f, -0.5f, -0.4f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f, -0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f, -0.4f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.4f, -0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f, -0.5f, -0.4f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f,  0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.4f,  0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f,  0.4f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f,  0.5f, -0.4f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f,  0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f,  0.4f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.4f,  0.5f, -0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f,  0.5f, -0.4f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f, -0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.4f, -0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f, -0.4f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f, -0.5f,  0.4f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f, -0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f, -0.4f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.4f, -0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f, -0.5f,  0.4f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f,  0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.4f,  0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f,  0.4f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make( 0.5f,  0.5f,  0.4f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f,  0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f,  0.4f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.4f,  0.5f,  0.5f, 1.0f), v4Zero, Colour32White);	++vb;
	vb->set(v4::make(-0.5f,  0.5f,  0.4f, 1.0f), v4Zero, Colour32White);	++vb;

	rdr::model::ILock ilock;
	rdr::Index* ib = m_instance.m_model->LockIBuffer(ilock);
	ib[ 0] =  0; ib[ 1] =  1; ib[ 2] =  0; ib[ 3] =  2; ib[ 4] =  0; ib[ 5] =  3;
	ib[ 6] =  4; ib[ 7] =  5; ib[ 8] =  4; ib[ 9] =  6; ib[10] =  4; ib[11] =  7;
	ib[12] =  8; ib[13] =  9; ib[14] =  8; ib[15] = 10; ib[16] =  8; ib[17] = 11;
	ib[18] = 12; ib[19] = 13; ib[20] = 12; ib[21] = 14; ib[22] = 12; ib[23] = 15;
	ib[24] = 16; ib[25] = 17; ib[26] = 16; ib[27] = 18; ib[28] = 16; ib[29] = 19;
	ib[30] = 20; ib[31] = 21; ib[32] = 20; ib[33] = 22; ib[34] = 20; ib[35] = 23;
	ib[36] = 24; ib[37] = 25; ib[38] = 24; ib[39] = 26; ib[40] = 24; ib[41] = 27;
	ib[42] = 28; ib[43] = 29; ib[44] = 28; ib[45] = 30; ib[46] = 28; ib[47] = 31;

	rdr::Material mat = renderer.m_material_manager.GetDefaultMaterial(geometry::EType_Vertex | geometry::EType_Colour);
	m_instance.m_model->SetMaterial(mat, rdr::model::EPrimitiveType_LineList);
	m_instance.m_model->SetName("Selection Box");
	m_instance.m_instance_to_world.identity();
}

//*****
// Position the selection box
void SelectionBox::SetSelection(const BoundingBox& bbox)
{
	m_instance.m_instance_to_world.identity();
	m_instance.m_instance_to_world[0][0] = bbox.SizeX();
	m_instance.m_instance_to_world[1][1] = bbox.SizeY();
	m_instance.m_instance_to_world[2][2] = bbox.SizeZ();
	m_instance.m_instance_to_world[3]    = bbox.Centre();
}

//*****
// Render the selection box
void SelectionBox::Render(rdr::Viewport& viewport)
{
	viewport.AddInstance(m_instance.m_base);
}

