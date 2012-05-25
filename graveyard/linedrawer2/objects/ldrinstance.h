//***********************************************************************
//
//	LineDrawer Instance 
//
//***********************************************************************
#ifndef LDR_INSTANCE_H
#define LDR_INSTANCE_H

#include "pr/maths/maths.h"
#include "pr/renderer/renderer.h"

enum
{
	NumComponents = 5,			// Use 5 for most instances except the axes instance which uses 6
	NumComponentsForOverlay = 6,
	MaxComponents = 6
};

// An instance for line drawer to use
PR_RDR_DECLARE_INSTANCE_TYPE6
(
	LdrInstance,
	rdr::Model*,			m_model,				ECpt_ModelPtr,
	m4x4,					m_instance_to_world,	ECpt_I2WTransform,
	rdr::SortkeyOverride,	m_sk_override,			ECpt_SortkeyOverride,
	rdr::rs::Block,			m_render_state,			ECpt_RenderState,
	pr::Colour32,			m_colour,				ECpt_Colour32,
	m4x4,					m_camera_to_screen,		ECpt_C2STransform
);

#endif//LDR_INSTANCE_H
