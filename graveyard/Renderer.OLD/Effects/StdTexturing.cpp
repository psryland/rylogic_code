//***************************************************************************
//
//	StdTexturing
//
//***************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/Effects/StdTexturing.h"
#include "PR/Renderer/DrawListElement.h"
#include "PR/Renderer/Materials/Material.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
// Constructor
StdTexturing::StdTexturing()
:m_texture0(0)
{}

//*****
// Destructor
StdTexturing::~StdTexturing()
{}

//*****
// Set the parameters handles used for std lighting
void StdTexturing::GetParameterHandles(D3DPtr<ID3DXEffect> effect)
{
	m_texture0 = effect->GetParameterByName(0, "g_texture0");
}

//*****
// Set the transforms
void StdTexturing::SetTextures(const DrawListElement& draw_list_element, D3DPtr<ID3DXEffect> effect)
{
	rdr::Material material = draw_list_element.GetMaterial();
	Verify(effect->SetTexture(m_texture0, material.m_texture->m_texture.m_ptr));
}
