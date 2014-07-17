//**********************************
// Preprocessor includes handler
//  Copyright (c) Rylogic Ltd 2011
//**********************************
#ifndef PR_SCRIPT_PP_INCLUDES_H
#define PR_SCRIPT_PP_INCLUDES_H
	
#include "pr/filesys/filesys.h"
#include "pr/script/script_core.h"
#include "pr/script/char_stream.h"
	
namespace pr
{
	namespace script
	{
		// Interface for resolving preprocessor include statements
		struct IIncludes
		{
			pr::Array<string> m_paths; // Search paths for looking up includes
			bool m_ignore_missing; // Allow missing includes to be ignored

			IIncludes(bool ignore_missing = false) :m_paths() ,m_ignore_missing(ignore_missing) {}
			virtual ~IIncludes() {}

			// Add paths to the search paths
			// 'search_paths' is a comma/semicolon separated list of paths
			void AddSearchPaths(char const* search_paths)
			{
				if (search_paths == nullptr) return;
				pr::str::Split<string>(search_paths, ",;", [&](string const& paths, size_t s, size_t e)
				{
					m_paths.push_back(paths.substr(s, e-s));
				});
			}

			// Returns a character stream that corresponds to the string "include".
			// 'search_paths_only' is used to distinguish betweem #include <desc> and
			// #include "desc", it's value is true for the first case.
			virtual std::unique_ptr<Src> Open(string const& include, Loc const& loc, bool search_paths_only) = 0;
		};

		// An implementation that ignores includes
		struct IgnoreIncludes :IIncludes
		{
			IgnoreIncludes() :IIncludes(true) {}
			std::unique_ptr<Src> Open(string const&, Loc const&, bool) override { return nullptr; }
		};
		
		// A default implementation of an include handler for files
		struct FileIncludes :IIncludes
		{
			FileIncludes(bool ignore_missing = false) :IIncludes(ignore_missing) {}
			std::unique_ptr<Src> Open(string const& include, Loc const& loc, bool search_paths_only) override
			{
				using namespace pr::filesys;
				string searched_paths;

				// Search the directory of the current source (if it's a file)
				if (!search_paths_only && !loc.m_file.empty())
				{
					string dir = GetDirectory(loc.m_file);
					string path = CombinePath(dir, include);
					if (FileExists(path))
						return std::make_unique<FileSrc>(path.c_str());

					searched_paths.append(dir).append("\n");
				}

				// Search the include paths
				string path;
				for (auto& dir : m_paths)
				{
					path = CombinePath(dir, include);
					if (FileExists(path))
						return std::make_unique<FileSrc>(path.c_str());

					searched_paths.append(dir).append("\n");
				}

				if (!m_ignore_missing)
				{
					// Check that the script source is a file source, String sources don't have a relative directory
					auto msg = fmt("Failed to open %s\n\nFile not found in search paths:\n%s", include.c_str(), searched_paths.c_str());
					throw Exception(EResult::MissingInclude, loc, msg);
				}
				return nullptr;
			}
		};

		// A default implementation of an include handler for strings
		struct StrIncludes :IIncludes
		{
			std::map<string, string> m_strings; // A map of include names to strings
			StrIncludes(bool ignore_missing = false) :IIncludes(ignore_missing) {}
			std::unique_ptr<Src> Open(string const& include, Loc const& loc, bool) override
			{
				auto i = m_strings.find(include);
				if (i != m_strings.end()) return std::make_unique<PtrSrc>(i->second.c_str());
				if (!m_ignore_missing) throw Exception(EResult::MissingInclude, loc, fmt("Failed to open %s", include.c_str()));
				return nullptr;
			}
		};
	}
}

#endif
