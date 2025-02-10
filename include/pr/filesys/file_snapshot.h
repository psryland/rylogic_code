//**********************************************
// Snapshot a file for reading
//  Copyright (c) Rylogic Ltd 2025
//**********************************************
#pragma once
#include <string>
#include <iostream>
#include <filesystem>
#include <span>
#include <windows.h>

namespace pr::filesys
{
	struct FileSnapshot
	{
		std::filesystem::path m_ss_filepath;

		FileSnapshot(std::filesystem::path const& filepath)
		{
			if (!std::filesystem::exists(filepath))
				throw std::runtime_error("File does not exist");

			// Get the temporary directory
			wchar_t temp_dir[MAX_PATH + 1];
			auto len = GetTempPathW(_countof(temp_dir), &temp_dir[0]);
			if (len == 0)
				throw std::runtime_error("Failed to get temporary directory");

			// Create a unique temporary file
			wchar_t temp_filepath[MAX_PATH + 1];
			if (GetTempFileNameW(&temp_dir[0], L"ldr-", 0, &temp_filepath[0]) == 0)
				throw std::runtime_error("Failed to create temporary file");

			m_ss_filepath = temp_filepath;

			// Copy the source file over the temporary file
			if (!CopyFileW(filepath.c_str(), temp_filepath, FALSE))
				throw std::runtime_error("Failed to copy file");
		}
		FileSnapshot(FileSnapshot&& rhs) noexcept
			: m_ss_filepath(std::move(rhs.m_ss_filepath))
		{
			rhs.m_ss_filepath.clear();
		}
		FileSnapshot(FileSnapshot const&) = delete;
		FileSnapshot& operator=(FileSnapshot&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			m_ss_filepath = std::move(rhs.m_ss_filepath);
			rhs.m_ss_filepath.clear();
			return *this;
		}
		FileSnapshot& operator=(FileSnapshot const&) = delete;
		~FileSnapshot()
		{
			if (!m_ss_filepath.empty())
				DeleteFileW(m_ss_filepath.c_str());
		}

		// Return the path to the temporary file
		[[nodiscard]] std::filesystem::path const& path() const
		{
			return m_ss_filepath;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/win32/windows_com.h"
namespace pr::filesys
{
	PRUnitTest(FileSnapshotTests)
	{
		FileSnapshot snap(__FILEW__);
		std::ifstream ifs(snap.path());
		PR_EXPECT(!!ifs);
			
		std::string buffer(std::istreambuf_iterator<char>(ifs), {});
		PR_EXPECT(buffer.contains("THIS_TEXT"));
	}
}
#endif
