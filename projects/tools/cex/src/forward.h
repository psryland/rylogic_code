//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// To add new commands add code at the NEW_COMMAND comments

#pragma once

#include <vector>
#include <unordered_map>
#include <iostream>
#include <string>
#include <sstream>
#include <fstream>
#include <filesystem>
#include <iterator>

#include <sdkddkver.h>
#include <windows.h>
#include <process.h>
#include <shlobj.h>
#include <fcntl.h>
#include <io.h>
#include <shellapi.h>

#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/cast.h"
#include "pr/common/hresult.h"
#include "pr/common/clipboard.h"
#include "pr/common/guid.h"
#include "pr/common/hash.h"
#include "pr/common/command_line.h"
#include "pr/common/algorithm.h"
#include "pr/maths/maths.h"
#include "pr/maths/conversion.h"
#include "pr/str/extract.h"
#include "pr/str/to_string.h"
#include "pr/filesys/filesys.h"
#include "pr/script/script_core.h"
#include "pr/script/filter.h"
#include "pr/storage/xml.h"
#include "pr/threads/process.h"
#include "pr/win32/win32.h"
#include "pr/win32/windows_com.h"

namespace cex
{
	inline void SetEnvVar(std::string const& env_var, std::string const& value)
	{
		try
		{
			std::ofstream file("~cex.bat");
			file << pr::FmtS("@echo off\nset %s=%s\n", env_var.c_str(), value.c_str()); //"DEL /Q ~cex.bat\n"
		}
		catch (std::exception const& ex)
		{
			std::cerr << "Failed to create '~cex.bat' file\n" << ex.what();
		}
	}
}
