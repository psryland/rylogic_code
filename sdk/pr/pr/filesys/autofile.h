//*************************************
// Auto File
//  Copyright © Rylogic Ltd 2007
//*************************************
#pragma once
#ifndef PR_COMMON_AUTOFILE_H
#define PR_COMMON_AUTOFILE_H

#include <stdio.h>
#include "pr/common/assert.h"
#include "pr/common/Console.h"

namespace pr
{
	inline FILE*&       FilePtr()      { static FILE* fp = 0; return fp; }
	inline char const*& LastFilename() { static char const* filename = 0; return filename; }
	
	inline void StartFile(const char* filename, const char* mode)
	{
		PR_ASSERT(PR_DBG, !FilePtr(), "Missing EndFile somewhere");
		if (FilePtr()) return;
		LastFilename() = filename;
		fopen_s(&FilePtr(), filename, mode);
	}
	inline void StartFile(const char* filename) { StartFile(filename, "wt"); }
	inline void AppendFile(const char* filename)
	{
		PR_ASSERT(PR_DBG, !FilePtr(), "Missing EndFile somewhere");
		if (FilePtr()) return;
		fopen_s(&FilePtr(), filename, "a+t");
	}
	inline void AppendFile() { AppendFile(LastFilename()); }
	inline void EndFile()
	{
		PR_ASSERT(PR_DBG, FilePtr(), "Missing StartFile somewhere");
		if (FilePtr()) fclose(FilePtr());
		FilePtr() = 0;
	}
	inline void ClearFile(char const* filename)
	{
		StartFile(filename, "wt");
		EndFile();
	}
	inline void Print(const std::string& str)
	{
		if (FilePtr()) fprintf(FilePtr(), "%s", str.c_str());
		else           pr::cons().Write(str);
	}
	inline void Seek(long offset, int origin)
	{
		if (FilePtr()) fseek(FilePtr(), offset, origin);
		else {} // not implemented
	}
	
	struct AutoFile
	{
		AutoFile(const char* filename) { StartFile(filename); }
		~AutoFile()                    { EndFile();           }
	};
}

#endif
