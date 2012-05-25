//**************************************************************************
//
//	Error Reporting for the renderer
//
//**************************************************************************
#ifndef RDR_ERRORS_H
#define RDR_ERRORS_H

#define PR_SUPPORT_D3D_HRESULTS
#include "PR/Common/HResult.h"
#include "PR/Common/PRException.h"

namespace pr
{
	namespace rdr
	{
		// Result testing
		enum EResult
		{
			EResult_Success	= 0,
			
			EResult_Failed	= 0x80000000,
			EResult_CreateD3DInterfaceFailed,
			EResult_CreateD3DDeviceFailed,
			EResult_DeviceNotSupported,
			EResult_DisplayFormatNotSupported,
			EResult_DepthStencilFormatNotSupported,
			EResult_DepthStencilFormatIncompatibleWithDisplayFormat,
			EResult_NoMultiSamplingTypeSupported,
			EResult_CreateDepthStencilFailed,
			EResult_SetDepthStencilFailed,
			EResult_FailedToCreateDefaultConfig,
			EResult_AutoSelectDisplayModeFailed,
			EResult_CreateDefaultEffectsFailed,
			EResult_CreateEffectPoolFailed,
			EResult_LoadTextureFailed,
			EResult_LoadEffectFailed,
			EResult_ResolveShaderPathFailed,
			EResult_DeviceLost,
			EResult_EnumateTerminated,
			EResult_CorruptPackage,
		};
		struct Exception : pr::Exception
		{
			Exception()                                                               {}
			explicit Exception(int value)             : pr::Exception(value)          {}
			Exception(int value, const char* message) : pr::Exception(value, message) {}
		};
	}//namespace rdr
	inline bool Failed   (rdr::EResult result)	{ return result  < 0; }
	inline bool Succeeded(rdr::EResult result)	{ return result >= 0; }
	inline void Verify   (rdr::EResult result)	{ (void)result; PR_ASSERT_STR(PR_DBG_COMMON, Succeeded(result), "Verify failure"); }
}//namespace pr

#endif//RDR_ERRORS_H
