//***********************************************************************************
// Direct Input
//  (c)opyright Rylogic Limited 2009
//***********************************************************************************

#ifndef PR_DINPUT_FORWARD_H
#define PR_DINPUT_FORWARD_H

#include "pr/common/min_max_fix.h"
#include <windows.h>
#include <dinput.h>
#include "pr/common/assert.h"
#include "pr/common/prtypes.h"
#include "pr/input/directinput/errors.h"

#ifndef DIRECTINPUT_VERSION
#   define DIRECTINPUT_VERSION 0x0800
#endif

#ifndef PR_DBG_DINPUT
#   define PR_DBG_DINPUT  PR_DBG_COMMON
#endif

namespace pr
{
	namespace dinput
	{
		namespace EDeviceClass
		{
			enum Type
			{
				All      = DI8DEVCLASS_ALL,
				Keyboard = DI8DEVCLASS_KEYBOARD,
				Mouse    = DI8DEVCLASS_POINTER,
				Joystick = DI8DEVCLASS_GAMECTRL
			};
		}
		namespace EFlag
		{
			enum Type // Enumerate Devices flags
			{
				AllDevices      = DIEDFL_ALLDEVICES,
				AttachedOnly    = DIEDFL_ATTACHEDONLY,
				ForceFeedback   = DIEDFL_FORCEFEEDBACK,
				IncludeAliases  = DIEDFL_INCLUDEALIASES,
				IncludeHidden   = DIEDFL_INCLUDEHIDDEN,
				IncludePhantoms = DIEDFL_INCLUDEPHANTOMS
			};
		}
		//enum EWaitResult
		//{
		//	EWaitResult_Event           = WAIT_OBJECT_0,        //  The event A key is pressed.
		//	EWaitResult_TimedOut        = WAIT_TIMEOUT,         //  The timeout value was reached
		//	EWaitResult_IOCompletion    = WAIT_IO_COMPLETION    //  This thread was alerted by a call to "QueueUserAPC".
		//};
		enum
		{
			BufferedBlockReadSize = 64
		};
		
		// Forward declare device classes
		class Device;
		class Keyboard;
		class Mouse;
		class Joystick;
		class Context;
		
		struct DeviceInstance
		{
			unsigned int    m_device_type;
			GUID            m_instance_guid;
			GUID            m_product_guid;
			std::string     m_instance_name;
			std::string     m_product_name;
		};
	}
}
#endif
