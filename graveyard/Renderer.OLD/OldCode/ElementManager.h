//****************************************************************
//
//	Element Manager
//
//****************************************************************
//
//	This class manages the task of batching small element renderables
//	into one big buffer
//

#ifndef ELEMENT_MANAGER_H
#define ELEMENT_MANAGER_H

#include "PR/Common/StdList.h"
#include "PR/Renderer/VertexFormat.h"
#include "PR/Renderer/RenderableBase.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
		//struct ElementSettings
		//{
		//	ElementSettings()
		//	:m_vertex_buffer_byte_size	(0)
		//	,m_index_buffer_byte_size	(0)
		//	{
		//		for( int i = 0; i < vf::EType_NumberOf; ++i ) m_buffer_types[i] = false;
		//	}
		//	uint	m_vertex_buffer_byte_size;
		//	uint	m_index_buffer_byte_size;
		//	bool	m_buffer_types[vf::EType_NumberOf];	// The vertex buffer formats to create
		//};

		//class ElementManager
		//{
		//public:
		//	ElementManager(D3DPtr<IDirect3DDevice9> d3d_device, const ElementSettings& settings);
		//	
		//	bool	CreateDeviceDependentObjects(D3DPtr<IDirect3DDevice9> d3d_device);
		//	void	ReleaseDeviceDependentObjects();
		//	
		//	void	Add(const RenderNugget* nugget);
		//	void	PreRender();

		//private:
		//	Index*	LockElementIndexBuffer();
		//	void*	LockElementVertexBuffer(vf::Type vf);

		//private:
		//	typedef std::list<const RenderNugget*> TNuggetPtrList;

		//	ElementSettings				m_settings;
		//	D3DPtr<IDirect3DDevice9>	m_d3d_device;

		//	// Renderable Element buffers
		//	D3DPtr<IDirect3DIndexBuffer9>	m_index_buffer;
		//	uint							m_index_buffer_byte_offset;
		//	uint							m_index_bytes_to_write;
		//	D3DPtr<IDirect3DVertexBuffer9>	m_vertex_buffer            [vf::EType_NumberOf];
		//	uint							m_vertex_buffer_byte_offset[vf::EType_NumberOf];
		//	uint							m_vertex_bytes_to_write    [vf::EType_NumberOf];

		//	// A list of renderable element draw list elements
		//	TNuggetPtrList	m_element;	// A list of pointers to the element render nuggets
		//};
	}//namespace rdr
}//namespace pr

#endif//ELEMENT_MANAGER_H
