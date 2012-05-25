//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_MODEL_TYPES_H
#define PR_RDR_MODEL_TYPES_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/vertexformats/vertexformat.h"

namespace pr
{
	namespace rdr
	{
		namespace model
		{
			const RdrId DefaultModelId = 0;

			namespace EPrimitive
			{
				enum Type
				{
					PointList     = D3DPT_POINTLIST,
					LineList      = D3DPT_LINELIST,
					LineStrip     = D3DPT_LINESTRIP,
					TriangleList  = D3DPT_TRIANGLELIST,
					TriangleStrip = D3DPT_TRIANGLESTRIP,
					TriangleFan   = D3DPT_TRIANGLEFAN,
					Invalid       = D3DPT_FORCE_DWORD
				};
			}
			namespace EUsage
			{
				enum Type
				{
					// Informs the system that the application writes only to the vertex buffer.
					// Using this flag enables the driver to choose the best memory location for efficient write
					// operations and rendering. Attempts to read from a vertex buffer that is created with this
					// capability will fail. Buffers created with D3DPOOL_DEFAULT that do not specify D3DUSAGE_WRITEONLY
					// may suffer a severe performance penalty.
					// D3DUSAGE_WRITEONLY only affects the performance of D3DPOOL_DEFAULT buffers.
					WriteOnly = D3DUSAGE_WRITEONLY,
					
					// Set to indicate that the vertex buffer requires dynamic memory use.
					// This is useful for drivers because it enables them to decide where to place the buffer.
					// In general, static vertex buffers are placed in video memory and dynamic vertex buffers
					// are placed in AGP memory. Note that there is no separate static use. If you do not specify
					// D3DUSAGE_DYNAMIC, the vertex buffer is made static. D3DUSAGE_DYNAMIC is strictly enforced
					// through the D3DLOCK_DISCARD and D3DLOCK_NOOVERWRITE locking flags. As a result, D3DLOCK_DISCARD
					// and D3DLOCK_NOOVERWRITE are valid only on vertex buffers created with D3DUSAGE_DYNAMIC.
					// They are not valid flags on static vertex buffers. For more information, see Managing Resources (Direct3D 9).
					// For more information about using dynamic vertex buffers, see Performance Optimizations (Direct3D 9).
					// D3DUSAGE_DYNAMIC and D3DPOOL_MANAGED are incompatible and should not be used together. See D3DPOOL.
					// Textures can specify D3DUSAGE_DYNAMIC. However, managed textures cannot use D3DUSAGE_DYNAMIC.
					// For more information about dynamic textures, see Using Dynamic Textures.
					Dynamic   = D3DUSAGE_DYNAMIC
				};
			}

			// Model buffer / Model creation settings
			struct Settings
			{
				vf::Type m_vertex_type;  // The type of vertices to create
				size_t   m_Icount;       // The number of indices wanted in the model buffer (max 65535)
				size_t   m_Vcount;       // The number of vertices wanted in the model buffer
				size_t   m_usage;        // Bitwise OR of 'EUsage'
				Settings()                                                 :m_vertex_type(vf::EVertType::PosNormDiffTex) ,m_Icount(0)      ,m_Vcount(0)      ,m_usage(EUsage::WriteOnly) {}
				Settings(size_t Icount, size_t Vcount)                     :m_vertex_type(vf::EVertType::PosNormDiffTex) ,m_Icount(Icount) ,m_Vcount(Vcount) ,m_usage(EUsage::WriteOnly) {}
				Settings(size_t Icount, size_t Vcount, EUsage::Type usage) :m_vertex_type(vf::EVertType::PosNormDiffTex) ,m_Icount(Icount) ,m_Vcount(Vcount) ,m_usage(usage) {}
			};
			
			typedef pr::Range<size_t> Range;
			const Range RangeZero = {0, 0};
			
			// Contexts for lock calls to an index or vertex buffer
			struct VLock
			{
				D3DPtr<IDirect3DVertexBuffer9> m_buffer;
				vf::iterator                   m_ptr;
				Range                          m_range;
				
				VLock() :m_buffer(0)           {}
				~VLock()                       { Unlock(); }
				void Unlock()                  { if (m_buffer) m_buffer->Unlock(); m_buffer = 0; }
			};
			struct ILock
			{
				D3DPtr<IDirect3DIndexBuffer9> m_buffer;
				Index*                        m_ptr;
				Range                         m_range;
				
				ILock() :m_buffer(0) ,m_ptr(0) {}
				~ILock()                       { Unlock(); }
				void Unlock()                  { if (m_buffer) {m_buffer->Unlock(); m_buffer = 0;} }
			};
		}
	}
}

#endif
