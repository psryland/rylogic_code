//***************************************************************************
//
//	A default effect file
//
//***************************************************************************
#include "Stdafx.h"
#include "PR/Common/Fmt.h"
#include "PR/Renderer/Effects/FragmentLinker.h"
#include "PR/Renderer/Renderer.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
// Constructor
FragmentLinker::FragmentLinker()
:m_fragment_linker(0)
,m_fragment_buffer(0)
,m_fragment_compile_errors(0)
{}

//*****
// Create the fragment linker
bool FragmentLinker::Create(Renderer* renderer)
{
	PR_ASSERT_STR(PR_DBG_RDR, m_renderer == 0, "Call Release first");

	// Save the renderer pointer
	m_renderer = renderer;

	// Create the fragment linker
	if( Failed(D3DXCreateFragmentLinker(m_renderer->GetD3DDevice().m_ptr, 0, &m_fragment_linker.m_ptr)) )
	{
		PR_ERROR_STR(PR_DBG_RDR, "Failed to create a fragment linker interface");
		Release();
		return false;
	}

	// Query the derived class for files containing fragments
	uint index = 0;
	while( const char* fragment_filename = GetFragmentFilename(index) )
	{
		if( !AddFragments(fragment_filename) ) return false;
		++index;
	}

	// Get handles to the fragments
	return GetFragmentHandles();
}

//*****
// Release()
void FragmentLinker::Release()
{
	m_fragment.clear();
	m_fragment_compile_errors = 0;
	m_fragment_buffer = 0;
	m_fragment_linker = 0;
}

//*****
// Add fragments from an effect file
bool FragmentLinker::AddFragments(const char* fragment_filename)
{
	// Load in the shader fragments.
	if( Failed(D3DXGatherFragmentsFromFile(
		fragment_filename,					// Path to the fragments
		0,									// Macro defines
		0,									// Include interface
		g_shader_flags,						// Shader flags
		&m_fragment_buffer.m_ptr,			// A buffer to hold the fragments
		&m_fragment_compile_errors.m_ptr)) )// Any compile errors
	{
		PR_WARN(PR_DBG_RDR, Fmt("Failed to compile the shader fragments: %s\n", fragment_filename).c_str());
		if( m_fragment_compile_errors )
		{
			PR_EXPAND(PR_DBG_RDR, const char* str = (const char*)m_fragment_compile_errors->GetBufferPointer();)
			PR_WARN  (PR_DBG_RDR, Fmt("Reason: %s", str).c_str());
		}
		PR_ERROR_STR(PR_DBG_RDR, "Failed to compile the shader fragments. See output window");
		Release();
		return false;
	}

	// Add the fragments to the linker
	if( Failed(m_fragment_linker->AddFragments((DWORD*)m_fragment_buffer->GetBufferPointer())) )
	{
		PR_ERROR_STR(PR_DBG_RDR, "Failed to add fragments to the linker");
		Release();
		return false;
	}
	return true;
}

//*****
// Link the fragments together to form a vertex shader. Remember to call "D3DRelease(vertex_shader, false);"
bool FragmentLinker::BuildVertexShader(const std::vector<D3DXHANDLE>& fragment, D3DPtr<IDirect3DVertexShader9>& vertex_shader)
{
	// Link the fragments together to form a shader    
	if( Failed(m_fragment_linker->LinkVertexShader("vs_1_1", D3DXSHADER_DEBUG & g_shader_flags, &fragment[0], (UINT)fragment.size(), &vertex_shader.m_ptr, &m_fragment_compile_errors.m_ptr)) )
	{
		vertex_shader = 0;
		if( m_fragment_compile_errors )
		{
			PR_EXPAND(PR_DBG_RDR, const char* str = (const char*)m_fragment_compile_errors->GetBufferPointer();)
			PR_WARN  (PR_DBG_RDR, Fmt("Reason: %s", str).c_str());
		}
		PR_ERROR_STR(PR_DBG_RDR, "Failed to link shader fragments. See output window");
		return false;
	}
	return true;
}

//*****
// Link the fragments together to form a vertex shader. Remember to call "D3DRelease(pixel_shader, false);"
bool FragmentLinker::BuildPixelShader(const std::vector<D3DXHANDLE>& fragment, D3DPtr<IDirect3DPixelShader9>& pixel_shader)
{
	// Link the fragments together to form a shader    
	if( Failed(m_fragment_linker->LinkPixelShader("ps_1_1", D3DXSHADER_DEBUG & g_shader_flags, &fragment[0], (UINT)fragment.size(), &pixel_shader.m_ptr, &m_fragment_compile_errors.m_ptr)) )
	{
		pixel_shader = 0;
		if( m_fragment_compile_errors )
		{
			PR_EXPAND(PR_DBG_RDR, const char* str = (const char*)m_fragment_compile_errors->GetBufferPointer();)
			PR_WARN  (PR_DBG_RDR, Fmt("Reason: %s", str).c_str());
		}
		PR_ERROR_STR(PR_DBG_RDR, "Failed to link shader fragments. See output window");
		return false;
	}
	return true;
}
