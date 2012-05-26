//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/textures/texture_manager.h"

using namespace pr::rdr;

pr::rdr::Texture2D::Texture2D()
:m_t2s(pr::m4x4Identity)
,m_tex()
,m_info()
{}

// Refcounting cleanup function
void pr::rdr::Texture2D::RefCountZero(pr::RefCount<Texture2D>* doomed)
{
	pr::rdr::Texture2D* tex = static_cast<pr::rdr::Texture2D*>(doomed);
	tex->m_mgr->Delete(tex);
}

