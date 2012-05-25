//***************************************************************************
//
//	XYZTextured
//
//***************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/Effects/XYZTextured.h"

#include "PR/Crypt/Crypt.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Viewport.h"
#include "PR/Renderer/DrawListElement.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
bool XYZTextured::MidPass(const Viewport& viewport, const DrawListElement& draw_list_element)
{
	Common::SetTransforms(viewport, draw_list_element, m_effect);
	StdTexturing::SetTextures(draw_list_element, m_effect);

	// Update the lighting
	const Light& light = viewport.GetRenderer().GetLightingManager().GetLight(0);
	Verify(m_effect->SetFloatArray(m_light_ambient, reinterpret_cast<const float*>(&light.m_ambient), 4));
	return true;
}

//*****
void XYZTextured::GetParameterHandles()
{
	Common::GetParameterHandles(m_effect);
	StdTexturing::GetParameterHandles(m_effect);
}

