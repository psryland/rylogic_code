//**********************************************
// File path/File system operations
//  Copyright (c) Rylogic Ltd 2009
//**********************************************
#pragma once
#include <filesystem>
#include "pr/win32/win32.h"

namespace pr::filesys
{
	// Scoped object that blocks until it can create a file called 'filepath.locked'.
	// 'filepath.locked' is deleted as soon as it goes out of scope.
	// Used as a file system mutex-file
	// Requires <windows.h> to be included
	struct LockFile
	{
		win32::Handle m_handle;
		LockFile(std::filesystem::path const& filepath, int max_attempts = 3, int max_block_time_ms = 1000)
		{
			// Arithmetic series: Sn = 1+2+3+..+n = n(1 + n)/2. 
			// For n attempts, the i'th attempt sleep time is: max_block_time_ms * i / Sn
			// because the sum of sleep times need to add up to max_block_time_ms.
			//  sleep_time(a) = a * back_off = a * max_block_time_ms / Sn
			//  back_off = max_block_time_ms / Sn = 2*max_block_time_ms / n(1+n)
			auto max = static_cast<double>(max_attempts);
			auto back_off = 2.0 * max_block_time_ms / (max * (1 + max));

			auto fpath = filepath;
			fpath += L".locked";
			for (auto a = 0; a != max_attempts; ++a)
			{
				m_handle = win32::FileOpen(fpath, GENERIC_READ | GENERIC_WRITE, 0, CREATE_NEW, FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_TEMPORARY, FILE_FLAG_DELETE_ON_CLOSE);
				if (m_handle != INVALID_HANDLE_VALUE)
					return;

				auto err = GetLastError();
				if (err == ERROR_SHARING_VIOLATION || err == ERROR_FILE_EXISTS)
					Sleep(DWORD(a * back_off));
				else
					break;
			}
			throw std::runtime_error(std::string("Failed to lock file: '").append(filepath.string()).append("'").c_str());
		}
	};
}