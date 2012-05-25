//***************************************************************************
//
//	XYZLitPVCTextured
//
//***************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/Effects/XYZLitPVCTextured.h"
#include "PR/Crypt/Crypt.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Viewport.h"
#include "PR/Renderer/DrawListElement.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
bool XYZLitPVCTextured::MidPass(const Viewport& viewport, const DrawListElement& draw_list_element)
{
	Common::SetTransforms(viewport, draw_list_element, m_effect);
	StdTexturing::SetTextures(draw_list_element, m_effect);
	StdLighting::SetLightingParams(viewport.GetRenderer().GetLightingManager().GetLight(0), m_effect);
	return true;
}

//*****
void XYZLitPVCTextured::GetParameterHandles()
{
	Common::GetParameterHandles(m_effect);
	StdLighting::GetParameterHandles(m_effect);
	StdTexturing::GetParameterHandles(m_effect);
}

