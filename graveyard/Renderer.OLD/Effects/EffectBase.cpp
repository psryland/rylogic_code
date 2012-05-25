//***************************************************************************
//
//	A class to wrap up an effect
//
//***************************************************************************

#include "stdafx.h"
#include "PR/Common/Fmt.h"
#include "PR/Renderer/Effects/EffectBase.h"
#include "PR/Renderer/Effects/BuiltInShaders.h"
#include "PR/Renderer/Renderer.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
// Constructor
Base::Base()
:m_name				()
,m_effect			(0)
,m_compile_errors	(0)
,m_technique		()
{}

//*****
// Get the techniques that are valid for this device
bool Base::GetValidTechniques()
{
	PR_ASSERT(PR_DBG_RDR, m_effect);

	D3DXHANDLE prev = 0, technique;
	while( Succeeded(m_effect->FindNextValidTechnique(prev, &technique)) && technique != 0 )
	{
		m_technique.push_back(technique);
		prev = technique;
	}
	return !m_technique.empty();
}

//*****
// Create an effect from a filename
bool Base::Create(const char* filename, D3DPtr<IDirect3DDevice9> d3d_device, D3DPtr<ID3DXEffectPool> effect_pool)
{
	m_name = filename;
	return ReCreate(d3d_device, effect_pool);
}

//*****
// Create this effect. If the effect file exists, load the effect from the disc.
// If not, look in the built in effects
bool Base::ReCreate(D3DPtr<IDirect3DDevice9> d3d_device, D3DPtr<ID3DXEffectPool> effect_pool)
{
	// D3DXSHADER_DEBUG
	//	Insert debug filename and line number information during shader compile. 
	// D3DXSHADER_SKIPVALIDATION
	//	Do not validate the generated code against known capabilities and constraints.
	//	This option is recommended only when compiling shaders that are known to work (that is, shaders that have
	//	compiled before, without this option). Shaders are always validated by Microsoft® Direct3D® before they are set to the device. 
	// D3DXSHADER_SKIPOPTIMIZATION
	//	Instructs the compiler to skip optimization steps during code generation. This option is
	//	not recommended unless you are trying to isolate a code problem and you suspect the compiler.
	//	This option is valid only when calling D3DXCompileShader. 

	// Create the effect in DirectX
	if( Failed(D3DXCreateEffectFromFile(
		d3d_device.m_ptr,			// D3D Device
		m_name.c_str(),				// The file
		0,							// Macro definitions
		0,							// Include interface
		g_shader_flags,				// Flags
		effect_pool.m_ptr,			// Effectpool
		&m_effect.m_ptr,			// The new effect
		&m_compile_errors.m_ptr)) )	// Any compilation errors
	{
		PR_WARN(PR_DBG_RDR, "Failed	to create effect:\n");
		if( m_compile_errors )
		{
			PR_EXPAND(PR_DBG_RDR, const char* str = (const char*)m_compile_errors->GetBufferPointer();)
			PR_WARN  (PR_DBG_RDR, Fmt("Reason: %s", str).c_str());
		}
		PR_ERROR_STR(PR_DBG_RDR, "Failed to load an effect. See Output window");
		return false;
	}

	// Get the techniques that will work on this device
	if( !GetValidTechniques() ) { PR_WARN(PR_DBG_RDR, "Failed to get a valid technique from this effect\n"); return false; }
	GetParameterHandles();
	return true;
}

//*****
// Release the effect
void Base::Release()
{
	m_effect = 0;
	m_compile_errors = 0;
}
