//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#include "src/forward.h"
#include "src/index.h"
#include "src/dir_scanner.h"

namespace blitzsearch
{
	void LoadToMemory(std::filesystem::path filepath)
	{
		std::ifstream file(filepath, std::ios::binary);
		if (!file) throw std::runtime_error("Failed to read file");
		std::vector<uint8_t> data{std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>()};
	}

	MainIndex::MainIndex(Settings const& settings)
		:m_files()
	{
		// Scan for files
		auto time_this = pr::profile::TimeThis().Start("Finding files ... ");
		pr::filesys::DirScanner scanner(settings.SearchPaths, [settings](fs::path const& filepath) -> bool
		{
			// Filter files by extension
			auto extn = filepath.extension().string();
			if (!std::ranges::any_of(settings.FileExtensions, [extn](auto& e) { return e == extn; }))
				return false;

			return true;
		});
		time_this.Stop().Display();

		auto files = scanner.GetFiles();

		// Add files to the index
		time_this.Start("Adding files ...");
		pr::threads::ThreadPool thread_pool;
		for (auto& file : files)
			thread_pool.QueueTask([file] { LoadToMemory(file); });
		
		thread_pool.WaitAll();

		time_this.Stop().Display();
	}




	// Add a file to the search index
	void MainIndex::AddFile(std::filesystem::path filepath)
	{
		// Create file index object based on the upper levels of the suffix array
		FileIndex file_index;
		file_index.m_filepath = filepath;

		// Read the file into memory
		std::ifstream file(filepath, std::ios::binary);
		if (!file) throw std::runtime_error("Failed to read file");
		std::vector<uint8_t> data{ std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>() };

		// Generate the full suffix array
		file_index.m_sa = std::vector<int>(data.size());
		pr::suffix_array::Build<uint8_t>(data, file_index.m_sa, 256);

		m_files.push_back(file_index);
	}

	// Search the index for matches to the pattern
	void MainIndex::Search(std::span<uint8_t const> pattern)
	{
		// Search each file
		for (auto& searchee : m_files)
		{
			// Read the file into memory
			std::ifstream file(searchee.m_filepath, std::ios::binary);
			if (!file) throw std::runtime_error("Failed to read file");
			std::vector<uint8_t> data{ std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>() };

			auto result = pr::suffix_array::Find<uint8_t>(pattern, data, searchee.m_sa);
			
		}
	}
}
