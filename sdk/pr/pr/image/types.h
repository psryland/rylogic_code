//********************************************************
//
//	Image forwards and types
//
//********************************************************
#ifndef PR_IMAGE_TYPES_H
#define PR_IMAGE_TYPES_H

#define PR_SUPPORT_D3D_HRESULTS
#include "pr/common/hresult.h"
#include "pr/common/exception.h"

namespace pr
{
	namespace image
	{
		enum EResult
		{
			EResult_Success,
			EResult_CreateD3DInterfaceFailed,
			EResult_CreateD3DDeviceFailed,
			EResult_CreateTextureFailed,
		};
		typedef pr::Exception<pr::image::EResult> Exception;

		struct Context;
		struct Lock;
		struct ImageInfo;

		struct Image2D;
		struct ImageCube;
	}//namespace image

	inline bool Failed   (image::EResult result)	{ return result  < 0; }
	inline bool Succeeded(image::EResult result)	{ return result >= 0; }
	inline void Verify   (image::EResult result)	{ (void)result; PR_ASSERT(PR_DBG, Succeeded(result), "Verify failure"); }
}//namespace pr

#endif//PR_IMAGE_TYPES_H