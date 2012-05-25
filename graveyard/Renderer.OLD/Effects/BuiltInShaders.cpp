//**************************************************************************
//
//	Built in shaders
//
//**************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/Effects/BuiltInShaders.h"
#include "PR/FileSys/FileSys.h"
#include "PR/Crypt/Crypt.h"
//
//namespace pr
//{
//	namespace rdr
//	{
//		namespace effect
//		{
//            const uint8 g_shader_data[] = 
//			{
//				#include "PR/Renderer/Effects/BuiltInShadersInc.h"
//				0
//			};
//		}//namespace effect
//	}//namespace rdr
//}//namespace pr
//
//using namespace pr;
//using namespace pr::rdr;
//using namespace pr::rdr::effect;
//
////*****
//// Look for a nugget in the shader data with an id that corresponds to 'filename'
//bool BuiltInShaderLoader::FindFile(const char* filename)
//{
//	// Get the nugget id corresponding to the filename
//	std::string nugget_id = file_sys::Standardise(filename);
//	m_nugget_id = crypt::Crc(nugget_id.c_str(), (uint)nugget_id.length());
//	
//	m_shader_data = 0;
//	m_shader_data_length = 0;
//
//	// Search the shader data for a nugget with an id that matches 'm_nugget_id'
//	nugget::RawDataI src_data(g_shader_data, sizeof(g_shader_data));
//	nugget::Load(src_data, sizeof(g_shader_data), nugget::EFlag_Reference, *this);
//
//	// Return true is data was found
//	return m_shader_data != 0;
//}
//
////*****
//// Call back from the nugget loader,
//bool BuiltInShaderLoader::AddNugget(nugget::Nugget nugget)
//{
//	// Not the correct id? Keep looking
//	if( nugget.GetId() != m_nugget_id ) return true;
//
//	// Access the nugget data
//	m_shader_data			= nugget.GetData();
//	m_shader_data_length	= nugget.GetDataSize();
//	return false;	// Found it, stop looking
//}
//
////*****
//// Call back from the D3DXCreateEffect method. Used to
//// open/close files '#include'd in the buildin effect data
//HRESULT BuiltInShaderLoader::Open(D3DXINCLUDE_TYPE, LPCSTR pFileName, LPCVOID, LPCVOID* ppData, UINT* pBytes)
//{
//	if( !FindFile(pFileName) ) return E_FAIL;
//	
//	*ppData = m_shader_data;
//	*pBytes = m_shader_data_length;
//	return S_OK;
//}
//HRESULT Close(LPCVOID)
//{
//	return S_OK;
//}
//
