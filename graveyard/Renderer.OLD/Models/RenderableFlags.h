//***************************************************************
//
//	Flags used with renderables
//
//***************************************************************
#ifndef PR_RDR_RENDERERABLE_FLAGS_H
#define PR_RDR_RENDERERABLE_FLAGS_H

#include "PR/Renderer/D3DHeaders.h"

namespace pr
{
	namespace rdr
	{
		enum EPrimitiveType
		{
			EPrimitiveType_PointList		= D3DPT_POINTLIST,
			EPrimitiveType_LineList			= D3DPT_LINELIST,
			EPrimitiveType_LineStrip		= D3DPT_LINESTRIP,
			EPrimitiveType_TriangleList		= D3DPT_TRIANGLELIST,
			EPrimitiveType_TriangleStrip	= D3DPT_TRIANGLESTRIP,
			EPrimitiveType_TriangleFan		= D3DPT_TRIANGLEFAN,
			EPrimitiveType_Invalid			= D3DPT_FORCE_DWORD
		};

		enum EUsage
		{
			EUsage_WriteOnly				= D3DUSAGE_WRITEONLY,
			EUsage_Dynamic					= D3DUSAGE_DYNAMIC
		};

		enum EMemPool
		{
			EMemPool_Default				= D3DPOOL_DEFAULT,
			EMemPool_Managed				= D3DPOOL_MANAGED,
			EMemPool_SystemMem				= D3DPOOL_SYSTEMMEM,
			EMemPool_Scratch				= D3DPOOL_SCRATCH
		};

	}//namespace rdr
}//namespace pr

#endif//PR_RDR_RENDERERABLE_FLAGS_H