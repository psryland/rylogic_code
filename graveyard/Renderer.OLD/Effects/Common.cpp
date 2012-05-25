//***************************************************************************
//
//	Common
//
//***************************************************************************
//	This class contains methods related to the common variable handles

#include "Stdafx.h"
#include "PR/Renderer/Effects/Common.h"
#include "PR/Renderer/Viewport.h"
#include "PR/Renderer/DrawListElement.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
// Constructor
Common::Common()
:m_object_to_world	(0)
,m_object_to_camera	(0)
,m_object_to_screen	(0)
,m_world_to_screen	(0)
,m_world_to_camera	(0)
,m_camera_to_world	(0)
{}

//*****
// Destructor
Common::~Common() {}

//*****
// Set the parameters handles used for std lighting
void Common::GetParameterHandles(D3DPtr<ID3DXEffect> effect)
{
	m_object_to_world	= effect->GetParameterByName(0, "g_object_to_world"	);
	m_object_to_camera	= effect->GetParameterByName(0, "g_object_to_camera");
	m_object_to_screen	= effect->GetParameterByName(0, "g_object_to_screen");
	m_world_to_screen	= effect->GetParameterByName(0, "g_world_to_screen"	);
	m_world_to_camera	= effect->GetParameterByName(0, "g_world_to_camera"	);
	m_camera_to_world	= effect->GetParameterByName(0, "g_camera_to_world"	);
}

//*****
// Set the transforms
void Common::SetTransforms(const Viewport& viewport, const DrawListElement& draw_list_element, D3DPtr<ID3DXEffect> effect)
{
	const m4x4* camera_to_screen = draw_list_element.m_instance->GetCameraToScreen();
	if( !camera_to_screen ) camera_to_screen = &viewport.GetCameraToScreen();
	
	m4x4 inst_to_world		= draw_list_element.m_instance->GetInstanceToWorld();
	m4x4 inst_to_screen		= *camera_to_screen * viewport.GetWorldToCamera() * inst_to_world;
	m4x4 camera_to_world	= viewport.GetWorldToCamera().GetInverseFast();

	Verify(effect->SetMatrix(m_object_to_world,  &d3dm4(inst_to_world)));
	Verify(effect->SetMatrix(m_object_to_screen, &d3dm4(inst_to_screen)));
	Verify(effect->SetMatrix(m_camera_to_world,  &d3dm4(camera_to_world)));
}
