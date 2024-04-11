//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#pragma once
#include <memory>
#include <filesystem>
#include <shlobj_core.h>

namespace pr::filesys
{
	// Return the user's documents folder
	inline std::filesystem::path GetUserDocumentsPath()
	{
		PWSTR documents_path;
		if (SHGetKnownFolderPath(FOLDERID_Documents, 0, nullptr, &documents_path) != S_OK)
			throw std::runtime_error("Failed to get the user's documents folder");

		std::shared_ptr<void> guard(documents_path, CoTaskMemFree);
		return documents_path;
	}

	// Return the executable path
	inline std::filesystem::path GetExecutablePath()
	{
		wchar_t buffer[MAX_PATH];
		if (!GetModuleFileNameW(nullptr, buffer, MAX_PATH))
			throw std::runtime_error("Failed to get the executable path");

		return buffer;
	}
}
