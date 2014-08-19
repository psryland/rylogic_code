//**********************************
// Preprocessor includes handler
//  Copyright (c) Rylogic Ltd 2011
//**********************************
#pragma once

#include <map>
#include <istream>
#include <fstream>
#include <sstream>
#include "pr/filesys/filesys.h"
#include "pr/common/resource.h"
#include "pr/common/memstream.h"
#include "pr/script/script_core.h"
#include "pr/script/char_stream.h"

namespace pr
{
	namespace script
	{
		// Interface for resolving filepaths such as preprocessor include statements
		// Can also be used to resolve general script relative filepaths.
		struct IIncludes
		{
			// Search paths for resolving relative filepaths
			pr::vector<string> m_paths;

			IIncludes(char const* search_paths = nullptr) { SearchPaths(search_paths); }
			virtual ~IIncludes() {}

			// Get/Set the paths to search, 'search_paths' is a comma/semicolon separated list of paths.
			string SearchPaths() const
			{
				string paths;
				for (auto& path : m_paths)
					paths.append(paths.empty() ? "" : ",").append(path);
				return paths;
			}
			void SearchPaths(char const* search_paths)
			{
				if (search_paths == nullptr) return;
				pr::str::Split<string>(search_paths, ",;", [&](string const& paths, size_t s, size_t e)
				{
					m_paths.push_back(paths.substr(s, e-s));
				});
			}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			virtual std::unique_ptr<std::istream> OpenStream(string const& include, bool binary = false, Loc const& loc = Loc()) = 0;

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is used to distinguish between #include <desc> and
			// #include "desc", it's value is true for the first case.
			// 'loc' is where in the current source the include comes from.
			virtual std::unique_ptr<Src> Open(string const& include, Loc const& loc, bool search_paths_only) = 0;
		};

		// A default implementation for file includes
		struct FileIncludes :IIncludes
		{
			FileIncludes(char const* search_paths = nullptr) :IIncludes(search_paths) {}
			
			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::istream> OpenStream(string const& include, bool binary = false, Loc const& loc = Loc()) override
			{
				string searched_paths;
				string const* current_dir = !loc.m_file.empty() ? &loc.m_file : nullptr;
				auto filepath = pr::filesys::ResolvePath(include, m_paths, current_dir, false, &searched_paths);
				if (!filepath.empty())
					return std::make_unique<std::ifstream>(filepath.c_str(), binary ? std::istream::binary : 0);

				// Check that the script source is a file source, String sources don't have a relative directory
				auto filename = pr::filesys::GetFilename(include);
				auto msg = fmt("Failed to open '%s'\n\nFile not found in search paths:\n%s", filename.c_str(), searched_paths.c_str());
				throw Exception(EResult::MissingInclude, loc, msg);
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is used to distinguish between #include <desc> and
			// #include "desc", it's value is true for the first case.
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, Loc const& loc, bool search_paths_only) override
			{
				string searched_paths;
				string const* current_dir = !search_paths_only && !loc.m_file.empty() ? &loc.m_file : nullptr;
				auto filepath = pr::filesys::ResolvePath(include, m_paths, current_dir, false, &searched_paths);
				if (!filepath.empty())
					return std::make_unique<FileSrc>(filepath.c_str());

				// Check that the script source is a file source, String sources don't have a relative directory
				auto filename = pr::filesys::GetFilename(include);
				auto msg = fmt("Failed to open '%s'\n\nFile not found in search paths:\n%s", filename.c_str(), searched_paths.c_str());
				throw Exception(EResult::MissingInclude, loc, msg);
			}
		};

		// A default implementation of an include handler for strings
		struct StrIncludes :IIncludes
		{
			// A map of include names to strings
			std::map<string, string> m_strings;

			StrIncludes(char const* search_paths = nullptr) :IIncludes(search_paths) {}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::istream> OpenStream(string const& include, bool binary = false, Loc const& loc = Loc()) override
			{
				auto i = m_strings.find(include);
				if (i != std::end(m_strings))
					return std::make_unique<std::istringstream>(i->second, binary ? std::istream::binary : 0);
			
				throw Exception(EResult::MissingInclude, loc, fmt("Failed to open %s", include.c_str()));
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is used to distinguish between #include <desc> and
			// #include "desc", it's value is true for the first case.
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, Loc const& loc, bool) override
			{
				auto i = m_strings.find(include);
				if (i != std::end(m_strings)) return std::make_unique<PtrSrc>(i->second.c_str());
				throw Exception(EResult::MissingInclude, loc, fmt("Failed to open %s", include.c_str()));
			}
		};

		// An implementation that reads from Resources
		struct ResIncludes :IIncludes
		{
			HMODULE m_module;

			ResIncludes(HMODULE module = 0)
				:m_module(module)
			{}

			// Convert 'name' into a resouce string id
			std::wstring ResId(string const& name) const
			{
				auto id = pr::To<std::wstring>(name);
				pr::str::Replace(id, L".", L"_");
				return pr::str::UpperCase(id);
			}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::istream> OpenStream(string const& include, bool binary = false, Loc const& = Loc()) override
			{
				auto id = ResId(include);
				wchar_t const* type = binary ? L"BINARY" : L"TEXT";
				auto res = pr::resource::Read<char>(id.c_str(), type, m_module);
				return std::make_unique<pr::imemstream>(res.m_data, res.size());
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is used to distinguish between #include <desc> and
			// #include "desc", it's value is true for the first case.
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, Loc const&, bool) override
			{
				auto id = ResId(include);
				auto res = pr::resource::Read<char>(id.c_str(), L"TEXT", m_module);
				return std::make_unique<PtrSrc>(res.m_data);
			}
		};

		// An implementation that ignores includes
		struct IgnoreIncludes :IIncludes
		{
			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::istream> OpenStream(string const&, bool = false, Loc const& = Loc()) override
			{
				return std::make_unique<std::istringstream>("");
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is used to distinguish between #include <desc> and
			// #include "desc", it's value is true for the first case.
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const&, Loc const&, bool) override
			{
				return nullptr;
			}
		};

	}
}
