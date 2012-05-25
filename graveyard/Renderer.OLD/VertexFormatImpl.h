//****************************************************************************
//
//	Vertex Format Implementation
//
//****************************************************************************
#ifndef VERTEX_FORMAT_IMPL_H
#define VERTEX_FORMAT_IMPL_H

#include "PR/Common/PRAssert.h"
#include "PR/Renderer/RendererAssertEnable.h"

namespace pr
{
	namespace rdr
	{
        namespace vf
		{
			inline uint GetSize(Type type)
			{
				switch( type )
				{
				case EType_PosNormDiffTex:											return sizeof(PosNormDiffTex);
				case EType_PosNormDiffTexFuture:									return sizeof(PosNormDiffTexFuture);
				default: PR_ERROR_STR(PR_DBG_RDR, "Unknown vertex format type");	return 0;
				};
			}

			inline EType GetEType(Type type)
			{
				PR_ASSERT_STR(PR_DBG_RDR, type < EType_NumberOf, "Unknown vertex format type");
				return reinterpret_cast<const EType&>(type);
			}

			inline EType GetTypeFromGeomType(GeomType geom_type)
			{
				using namespace geometry;
				switch( geom_type )
				{
				case EType_Vertex:								                    return EType_PosNormDiffTex;
				case EType_Vertex | EType_Normal:						            return EType_PosNormDiffTex;
				case EType_Vertex                | EType_Colour:				    return EType_PosNormDiffTex;
				case EType_Vertex | EType_Normal | EType_Colour:				    return EType_PosNormDiffTex;
				case EType_Vertex                               | EType_Texture:	return EType_PosNormDiffTex;
				case EType_Vertex | EType_Normal                | EType_Texture:	return EType_PosNormDiffTex;
				case EType_Vertex                | EType_Colour | EType_Texture:	return EType_PosNormDiffTex;
				case EType_Vertex | EType_Normal | EType_Colour | EType_Texture:	return EType_PosNormDiffTex;
				default: PR_ERROR_STR(PR_DBG_RDR, "Unknown combination of geometry types"); return EType_Invalid;
				}
			}

			inline Format GetFormat(Type type)
			{
				switch( type )
				{
				case EType_PosNormDiffTex:			return EFormat_Pos | EFormat_Norm | EFormat_Diff | EFormat_Tex;
				case EType_PosNormDiffTexFuture:	return EFormat_Pos | EFormat_Norm | EFormat_Diff | EFormat_Tex | EFormat_Future;
				default: PR_ERROR_STR(PR_DBG_RDR, "Unknown vertex format type"); return 0;
				};
			}
		}//namespace vf
	}//namespace rdr
}//namespace pr

#endif//VERTEX_FORMAT_IMPL_H