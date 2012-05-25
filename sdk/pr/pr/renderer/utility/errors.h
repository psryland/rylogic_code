//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#ifndef PR_ERROR_CODE
#define PR_ERROR_CODE(name,code)
#endif
PR_ERROR_CODE(Success                                             ,0         )
PR_ERROR_CODE(SuccessAlreadyLoaded                                ,1         )
PR_ERROR_CODE(Failed                                              ,0x80000000)
PR_ERROR_CODE(CreateInterfaceFailed                               ,0x80000001)
PR_ERROR_CODE(CreateD3DDeviceFailed                               ,0x80000002)
PR_ERROR_CODE(DependencyMissing                                   ,0x80000003)
PR_ERROR_CODE(UnsupportedShaderModelVersion                       ,0x80000004)
PR_ERROR_CODE(DeviceNotSupported                                  ,0x80000005)
PR_ERROR_CODE(DisplayFormatNotSupported                           ,0x80000006)
PR_ERROR_CODE(TextureFormatNotSupported                           ,0x80000007)
PR_ERROR_CODE(DepthStencilFormatNotSupported                      ,0x80000008)
PR_ERROR_CODE(DepthStencilFormatIncompatibleWithDisplayFormat     ,0x80000009)
PR_ERROR_CODE(NoMultiSamplingTypeSupported                        ,0x8000000A)
PR_ERROR_CODE(CreateDepthStencilFailed                            ,0x8000000B)
PR_ERROR_CODE(SetDepthStencilFailed                               ,0x8000000C)
PR_ERROR_CODE(FailedToCreateDefaultConfig                         ,0x8000000D)
PR_ERROR_CODE(AutoSelectDisplayModeFailed                         ,0x8000000E)
PR_ERROR_CODE(CreateDefaultEffectsFailed                          ,0x8000000F)
PR_ERROR_CODE(CreateEffectPoolFailed                              ,0x80000010)
PR_ERROR_CODE(LoadEffectFailed                                    ,0x80000011)
PR_ERROR_CODE(LoadTextureFailed                                   ,0x80000012)
PR_ERROR_CODE(EffectNotFound                                      ,0x80000013)
PR_ERROR_CODE(TextureNotFound                                     ,0x80000014)
PR_ERROR_CODE(CreateModelBufferFailed                             ,0x80000015)
PR_ERROR_CODE(CreateModelFailed                                   ,0x80000016)
PR_ERROR_CODE(DeviceLost                                          ,0x80000017)
PR_ERROR_CODE(ResetDeviceFailed                                   ,0x80000018)
PR_ERROR_CODE(ModelIdAlreadyExists                                ,0x80000019)
PR_ERROR_CODE(CorruptPackage                                      ,0x8000001A)
#undef PR_ERROR_CODE

#ifndef PR_RDR_ERRORS_H
#define PR_RDR_ERRORS_H

#include "pr/common/hresult.h"
#include "pr/common/exception.h"

namespace pr
{
	namespace rdr
	{
		namespace EResult
		{
			enum Type
			{
#				define PR_ERROR_CODE(name,value) name = value,
#				include "errors.h"
			};
		}
	}
	template <> inline std::string ToString(rdr::EResult::Type result)
	{
		switch (result)
		{
		default: return "";
#		define PR_ERROR_CODE(name,value) case rdr::EResult::name: return "Renderer: "#name;
#		include "errors.h"
		}
	}

	typedef pr::Exception<pr::rdr::EResult::Type> RdrException;
}

#endif
