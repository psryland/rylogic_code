//***********************************************************************************
// Direct Input
//	(c)opyright Rylogic Limited 2009
//***********************************************************************************

#ifndef PR_ERROR_CODE
#define PR_ERROR_CODE(name,code)
#endif
PR_ERROR_CODE(Success                      ,0         )
PR_ERROR_CODE(BufferOverflow               ,1         )
PR_ERROR_CODE(MoreDataAvailable            ,2         )
PR_ERROR_CODE(Failed                       ,0x80000000)
PR_ERROR_CODE(CreateInterfaceFailed        ,0x80000001)  // "Failed to create a direct input interface" 
PR_ERROR_CODE(EnumerateDevicesFailed       ,0x80000002)  // "Failed to enumerate the devices" 
PR_ERROR_CODE(EnumerateDeviceObjectsFailed ,0x80000003)  // "Failed to enumerate the objects of the device" 
PR_ERROR_CODE(CreateDeviceFailed           ,0x80000004)  // "Failed to create device" 
PR_ERROR_CODE(NoSuitableDeviceFound        ,0x80000005)  // "No suitable device found" 
PR_ERROR_CODE(SetDataFormatFailed          ,0x80000006)  // "Failed to set the data format" 
PR_ERROR_CODE(SetCooperativeLevelFailed    ,0x80000007)  // "Failed to set the co-operative level" 
PR_ERROR_CODE(AcquireDeviceFailed          ,0x80000008)  // "Failed to acquire a device" 
PR_ERROR_CODE(UnAcquireDeviceFailed        ,0x80000009)  // "Failed to unacquire a device" 
PR_ERROR_CODE(SetBufferSizeFailed          ,0x8000000A)  // "Failed to set buffer size" 
PR_ERROR_CODE(CreateEventFailed            ,0x8000000B)  // "Failed to create an event for event notification" 
PR_ERROR_CODE(SetEventFailed               ,0x8000000C)  // "Failed to set an event for event notification" 
PR_ERROR_CODE(DeviceNotFound               ,0x8000000D)  // "Device not found" 
PR_ERROR_CODE(InputLost                    ,0x8000000E)  // "Input lost"
PR_ERROR_CODE(DataPending                  ,0x8000000F)  // 
#undef PR_ERROR_CODE

#ifndef PR_DINPUT_ERRORS_H
#define PR_DINPUT_ERRORS_H

#include <string>
#include "pr/common/hresult.h"

namespace pr
{
	namespace dinput
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
	template <> inline std::string ToString(dinput::EResult::Type result)
	{
		switch (result)
		{
		default: return "";
#		define PR_ERROR_CODE(name,value) case dinput::EResult::name: return "DirectInput: "#name;
#		include "errors.h"
		}
	}
}
#endif
