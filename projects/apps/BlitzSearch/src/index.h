//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#pragma once
#include "src/forward.h"
#include "src/settings.h"

namespace blitzsearch
{
	struct FileIndex
	{
		std::filesystem::path m_filepath;
		std::vector<int> m_sa;
	};

	struct MainIndex
	{
		std::vector<FileIndex> m_files;

		MainIndex(Settings const& settings);

		// Add a file to the index
		void AddFile(std::filesystem::path filepath);

		// Search the index for matches to the pattern
		void Search(std::span<uint8_t const> pattern);
	};
}
