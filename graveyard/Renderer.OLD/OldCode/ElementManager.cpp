//****************************************************************
//
//	Element Manager
//
//****************************************************************
//
//	This class manages the task of batching small element renderables
//	into one big buffer
//

#include "Stdafx.h"
#include "PR/Common/D3DHelpers.h"
#include "PR/Renderer/ElementManager.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/RenderNugget.h"
#include "PR/Renderer/RenderableElement.h"

using namespace pr;
using namespace pr::rdr;

////*****
//// Constructor
//ElementManager::ElementManager(D3DPtr<IDirect3DDevice9> d3d_device, const ElementSettings& settings)
//:m_settings					(settings)
//,m_index_buffer				(0)
//,m_index_buffer_byte_offset	(0)
//,m_index_bytes_to_write		(0)
//{
//	for( uint i = 0; i < vf::EType_NumberOf; ++i )
//	{
//		m_vertex_buffer[i]             = 0;
//		m_vertex_buffer_byte_offset[i] = 0;
//		m_vertex_bytes_to_write[i]     = 0;
//	}
//	CreateDeviceDependentObjects(d3d_device);
//}
//
////*****
//// Add an element nugget.
//void ElementManager::Add(const RenderNugget* nugget)
//{
//	// Add the nugget to the list of elements for pre-render processing
//	m_element.push_back(nugget);
//
//	// Update the used size of the element vertex and index buffers
//	m_index_bytes_to_write										+= nugget->m_index_length	* sizeof(Index);
//	m_vertex_bytes_to_write[nugget->m_owner->m_vertex_type]		+= nugget->m_vertex_length	* vf::GetSize(nugget->m_owner->m_vertex_type);
//	PR_ASSERT_STR(PR_DBG_RDR, m_index_bytes_to_write									< m_settings.m_index_buffer_byte_size,  "Number of element indices in frame is greater than index buffer size");
//	PR_ASSERT_STR(PR_DBG_RDR, m_vertex_bytes_to_write[nugget->m_owner->m_vertex_type]	< m_settings.m_vertex_buffer_byte_size, "Number of element vertices in frame is greater than vertex buffer size");
//}
//
////*****
//// Create the vertex and index buffers to use for the elements
//void ElementManager::CreateDeviceDependentObjects(D3DPtr<IDirect3DDevice9> d3d_device)
//{
//	// Create an index buffer to copy element model data into before drawing
//	if( m_settings.m_index_buffer_byte_size != 0 &&
//		Failed(d3d_device->CreateIndexBuffer(	m_settings.m_index_buffer_byte_size * sizeof(Index),
//												D3DUSAGE_DYNAMIC | D3DUSAGE_WRITEONLY,
//												D3DFMT_INDEX16,
//												D3DPOOL_DEFAULT,
//												&m_index_buffer.m_ptr,
//												0 )) )
//	{ throw Exception(EResult_CreateIndexBufferFailed); }
//
//	// Create vertex buffers for each requested type of fvf
//	for( vf::Type vf = 0; vf < vf::EType_NumberOf; ++vf )
//	{
//		if( m_settings.m_buffer_types[vf] )
//		{
//			PR_ASSERT(PR_DBG_RDR, m_settings.m_vertex_buffer_byte_size > 0);
//			if( Failed(d3d_device->CreateVertexBuffer(	m_settings.m_vertex_buffer_byte_size * vf::GetSize(vf),
//														D3DUSAGE_DYNAMIC | D3DUSAGE_WRITEONLY,
//														0,
//														D3DPOOL_DEFAULT,
//														&m_vertex_buffer[vf],
//														0 )) )
//			{ throw Exception(EResult_CreateVertexBufferFailed); }
//		}
//	}
//}
//
////*****
//// Release the vertex and index buffers
//void ElementManager::ReleaseDeviceDependentObjects()
//{
//	D3DRelease(m_index_buffer, true);
//	for( vf::Type vf = 0; vf < vf::EType_NumberOf; ++vf )
//	{
//		D3DRelease(m_vertex_buffer[vf], true);
//	}
//}
//
////*****
//// Prepare the elements for rendering.
//// This involves setting the index buffer and vertex buffer pointers
//// in each renderable. The reason this isn't done as the elements are
//// added is because we don't want to lock/unlock the vb/ib too often
//void ElementManager::PreRender()
//{
//	// No elements? no work to do
//	if( m_element.empty() ) return;
//
//	uint index_bytes_written;
//	uint vertex_bytes_written[vf::EType_NumberOf];
//
//	// Lock the element index buffer
//	Index* ibuffer = 0;
//	index_bytes_written = 0;
//	if( m_index_bytes_to_write > 0 ) ibuffer = LockElementIndexBuffer();
//	
//	// Lock the element vertex buffers
//	void* vbuffer[vf::EType_NumberOf];
//	for( vf::Type vf = 0; vf < vf::EType_NumberOf; ++vf )
//	{
//		vbuffer[vf] = 0;
//		vertex_bytes_written[vf] = 0;
//		if( m_vertex_bytes_to_write[vf] > 0 ) vbuffer[vf] = LockElementVertexBuffer(vf);
//	}
//
//	// Process the element nuggets:
//	for( TNuggetPtrList::iterator iter = m_element.begin(), iter_end = m_element.end(); iter != iter_end; ++iter )
//	{
//		#pragma message(PR_LINK "Const violation here, this needs fixing")
//		RenderNugget* nugget = const_cast<RenderNugget*>(*iter);
//
//		RenderableElement* renderable		= static_cast<RenderableElement*>(nugget->m_owner);
//		nugget->m_owner->m_index_buffer		= m_index_buffer;
//		nugget->m_owner->m_vertex_buffer	= m_vertex_buffer[nugget->m_owner->m_vertex_type];
//		nugget->m_index_byte_offset			= m_index_buffer_byte_offset + index_bytes_written;
//		nugget->m_vertex_byte_offset		= m_vertex_buffer_byte_offset[nugget->m_owner->m_vertex_type] + vertex_bytes_written[nugget->m_owner->m_vertex_type];
//
//		// Memcpy the nugget data into the element buffers
//		PR_ASSERT(PR_DBG_RDR, ibuffer && vbuffer[nugget->m_owner->m_vertex_type]);
//		memcpy(
//			reinterpret_cast<uint8*>(ibuffer) + index_bytes_written,
//			&renderable->m_element_ibuffer[nugget->m_index_byte_offset],
//			nugget->m_index_length * sizeof(Index));
//
//		memcpy(
//			reinterpret_cast<uint8*>(vbuffer[nugget->m_owner->m_vertex_type]) + vertex_bytes_written[nugget->m_owner->m_vertex_type],
//			&renderable->m_element_vbuffer[nugget->m_vertex_byte_offset],
//			nugget->m_vertex_length * vf::GetSize(nugget->m_owner->m_vertex_type));
//
//		// Move the buffer indices
//		index_bytes_written										+= nugget->m_index_length * sizeof(Index);
//		vertex_bytes_written[nugget->m_owner->m_vertex_type]	+= nugget->m_vertex_length * vf::GetSize(nugget->m_owner->m_vertex_type);
//		PR_ASSERT(PR_DBG_RDR, index_bytes_written									<= m_index_bytes_to_write);
//		PR_ASSERT(PR_DBG_RDR, vertex_bytes_written[nugget->m_owner->m_vertex_type]	<= m_vertex_bytes_to_write[nugget->m_owner->m_vertex_type]);
//	}
//
//	// Reset the render statistics and unlock the element buffers
//	PR_ASSERT(PR_DBG_RDR, index_bytes_written == m_index_bytes_to_write);
//	m_index_buffer_byte_offset += index_bytes_written;
//	if( ibuffer ) { m_index_bytes_to_write = 0; m_index_buffer->Unlock(); }
//
//	for( vf::Type vf = 0; vf < vf::EType_NumberOf; ++vf )
//	{
//		PR_ASSERT(PR_DBG_RDR, vertex_bytes_written[vf]	== m_vertex_bytes_to_write[vf]);
//		m_vertex_buffer_byte_offset[vf] += vertex_bytes_written[vf];
//		if( vbuffer[vf] ) { m_vertex_bytes_to_write[vf] = 0; m_vertex_buffer[vf]->Unlock(); }
//	}
//}
//
////*****
//// Lock the element index buffer
//rdr::Index* ElementManager::LockElementIndexBuffer()
//{
//	rdr::Index* ibuffer = 0;
//
//	// Lock the index buffer
//	uint flags = D3DLOCK_NOOVERWRITE;
//	uint buffer_remaining = m_settings.m_index_buffer_byte_size - m_index_buffer_byte_offset;
//	if( buffer_remaining < m_index_bytes_to_write )
//	{
//		m_index_buffer_byte_offset = 0;
//		flags = D3DLOCK_DISCARD;
//	}
//	if( Failed(m_index_buffer->Lock(m_index_buffer_byte_offset, m_index_bytes_to_write, (void**)&ibuffer, flags)) ) return 0;
//	return ibuffer;
//}
//
////*****
//// Lock an element vertex buffer
//void* ElementManager::LockElementVertexBuffer(vf::Type vf)
//{
//	void* vbuffer = 0;
//
//	uint flags = D3DLOCK_NOOVERWRITE;
//	uint buffer_remaining = m_settings.m_vertex_buffer_byte_size - m_vertex_buffer_byte_offset[vf];
//	if( buffer_remaining < m_vertex_bytes_to_write[vf] )
//	{
//		m_vertex_buffer_byte_offset[vf] = 0;
//		flags = D3DLOCK_DISCARD;
//	}
//	if( Failed(m_vertex_buffer[vf]->Lock(m_vertex_buffer_byte_offset[vf], m_vertex_bytes_to_write[vf], (void**)&vbuffer, flags)) ) return 0;
//	return vbuffer;
//}
//
