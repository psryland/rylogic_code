//***************************************************************************
//
//	A class to wrap up a texture
//
//***************************************************************************

#include "stdafx.h"
#include "PR/Common/Fmt.h"
#include "PR/Common/PRScript.h"
#include "PR/Common/PRString.h"
#include "PR/Renderer/Materials/Texture.h"
#include "PR/Renderer/Renderer.h"

using namespace pr;
using namespace pr::rdr;

//*****
// Constructor
rdr::Texture::Texture()
:m_texture		(0)
,m_id			(0)
,m_name			("")
,m_properties	(0)
,m_prop			()
{}

//*****
// Create the texture
bool rdr::Texture::ReCreate(D3DPtr<IDirect3DDevice9> d3d_device)
{
	if( m_name.empty() ) return true;

	// Create the texture in DirectX
	if( Failed(D3DXCreateTextureFromFile(d3d_device.m_ptr, m_name.c_str(), &m_texture.m_ptr)) )
	{
		PR_WARN(PR_DBG_RDR, Fmt("Failed	to create texture: %s\n", m_name.c_str()).c_str());
		m_texture = 0;
		m_properties = 0;
		return false;
	}

	// Load the texture info
	LoadTextureInfo();
	return true;
}

//*****
// Release the texture
void rdr::Texture::Release()
{
	m_texture = 0;
	m_properties = 0;
}

//*****
// Load the ".info" for this texture if it exists
void rdr::Texture::LoadTextureInfo()
{
	std::string info_name = m_name + ".info";

	// If we fail to load the info file then assume the default values
	ScriptLoader loader;
	if( !loader.LoadFromFile(info_name.c_str()) ) return;
	
	std::string keyword;
	while( loader.GetKeyword(keyword) )
	{
		// Alpha property
		if( str::EqualsNoCase(keyword, "Alpha") )
		{
			m_properties |= TextureProperty_Alpha;
			loader.ExtractBool(m_prop.m_alpha);
		}
		// Effect property
		else if( str::EqualsNoCase(keyword, "Effect") )
		{
			m_properties |= TextureProperty_Effect;
			loader.ExtractString(m_prop.m_effect_id);
		}
		else
		{
			PR_WARN(PR_DBG_RDR, Fmt("Unknown keyword: '%s' in texture info file: '%s'", keyword.c_str(), info_name.c_str()).c_str());
		}
	}
}
