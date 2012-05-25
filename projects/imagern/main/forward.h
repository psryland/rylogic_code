//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#pragma once
#ifndef IMAGERN_FORWARD_H
#define IMAGERN_FORWARD_H

// Change these values to use different versions
#define  WINVER       0x0600//0x0501//0x0400//
#define _WIN32_WINNT  0x0600//0x0501//0x0400//
#define _WIN32_IE     0x0700//0x0501//0x0400//
#define _RICHEDIT_VER 0x0300//0x0200

// Change these values to use different versions
#define _WTL_NO_CSTRING

#include "pr/common/min_max_fix.h"
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlcom.h>
#include <atlmisc.h>
#include <atlddx.h>
#include <atlframe.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <atlcrack.h>

#include <d3d9.h>
#include <d3dx9.h>
#include <sstream>
#include <knownfolders.h>

#include "pr/common/assert.h"
#include "pr/common/hresult.h"
#include "pr/common/exception.h"
#include "pr/common/events.h"
#include "pr/maths/maths.h"
#include "pr/str/prstring.h"
#include "pr/str/prstdstring.h"
#include "pr/macros/count_of.h"
#include "pr/gui/recent_files.h"
#include "pr/gui/misc.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/fileex.h"
#include "pr/threads/thread.h"
#include "pr/script/reader.h"
#include "pr/renderer/renderer.h"
#include "pr/storage/sqlite.h"
//#include "pr/renderer/materials/video/video_ctrl_dlg.h"

#define DBG PR_DBG
extern CAppModule g_app_module;

namespace EResult
{
	enum Type
	{
		Success = 0,
		Failed  = 0x80000000,
		FailedToLoadMediaFile,
	};
}

typedef pr::string<> string;
typedef pr::Exception<EResult::Type> ImgException;

struct Event_Message;
struct UserSettings;
struct MediaFile;
struct MainGUI;
class MediaList;
class Imager;

#endif
