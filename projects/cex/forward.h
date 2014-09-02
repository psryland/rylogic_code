//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// To add new commands add code at the NEW_COMMAND comments

#pragma once

#ifndef _WIN32_WINNT 
#define _WIN32_WINNT 0x0500
#endif

#include <vector>
#include <unordered_map>
#include <iostream>
#include <string>
#include <sstream>
#include <fstream>
#include <iterator>

#include <windows.h>
#include <process.h>
#include <shlobj.h>
#include <atlbase.h>
#include <fcntl.h>
#include <io.h>
#include <shellapi.h>

#include "pr/common/prtypes.h"
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/hresult.h"
#include "pr/common/windows_com.h"
#include "pr/common/clipboard.h"
#include "pr/common/hash.h"
#include "pr/common/guid.h"
#include "pr/common/command_line.h"
#include "pr/common/algorithm.h"
#include "pr/maths/maths.h"
#include "pr/maths/conversion.h"
#include "pr/str/prstring.h"
#include "pr/str/tostring.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/fileex.h"
#include "pr/storage/xml.h"
#include "pr/threads/process.h"

namespace cex
{
	inline void SetEnvVar(std::string const& env_var, std::string const& value)
	{
		pr::Handle fp = pr::FileOpen("~cex.bat", pr::EFileOpen::Writing);
		if (!fp) { std::cerr << "Failed to create '~cex.bat' file\n"; return; }
		pr::FileWrite(fp, pr::FmtS("@echo off\nset %s=%s\n" ,env_var.c_str() ,value.c_str())); //"DEL /Q ~cex.bat\n"
	}
}
