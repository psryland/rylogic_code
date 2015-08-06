//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <map>
#include <istream>
#include <fstream>
#include <sstream>
#include "pr/filesys/filesys.h"
#include "pr/common/resource.h"
#include "pr/common/memstream.h"
#include "pr/common/multi_cast.h"
#include "pr/script/forward.h"
#include "pr/script/fail_policy.h"

namespace pr
{
	namespace script
	{
		// A base class and interface for an include handler
		struct IIncludeHandler
		{
			virtual ~IIncludeHandler() {}

			// Add a path to the include search paths
			virtual void AddSearchPath(string path, size_t index = ~size_t()) = 0;

			// Resolve an include into a full path
			virtual string ResolveInclude(string const& include, bool search_paths_only = false, Location const& loc = Location()) = 0;

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			virtual std::unique_ptr<Src> Open(string const& include, bool search_paths_only = false, Location const& loc = Location()) = 0;

			// Open 'include' as an ascii stream
			virtual std::unique_ptr<std::istream> OpenStreamA(string const& include, bool search_paths_only = false, bool binary = false, Location const& loc = Location()) = 0;
		};

		#pragma region No Includes
		// An include handler that doesn't handle any includes.
		template <typename FailPolicy = ThrowOnFailure>
		struct NoIncludes :IIncludeHandler
		{
			// Add a path to the include search paths
			void AddSearchPath(string, size_t = ~size_t()) override
			{}

			// Resolve an include into a full path
			string ResolveInclude(string const&, bool = false, Location const& loc = Location()) override
			{
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported"), string();
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const&, bool = false, Location const& loc = Location()) override
			{
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported"), nullptr;
			}

			// Open 'include' as an ascii stream
			std::unique_ptr<std::istream> OpenStreamA(string const&, bool = false, bool = false, Location const& loc = Location()) override
			{
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported"), nullptr;
			}
		};
		#pragma endregion

		#pragma region File Includes
		// A file include handler
		template <typename FailPolicy = ThrowOnFailure>
		struct FileIncludes :IIncludeHandler
		{
			pr::vector<string> m_paths;
			bool m_ignore_missing_includes;

			FileIncludes(string search_paths = string())
				:m_paths()
				,m_ignore_missing_includes()
			{
				SearchPaths(search_paths);
			}

			// Get/Set the search paths as a comma or semicolon separated list
			virtual string SearchPaths() const
			{
				string paths;
				for (auto& path : m_paths)
					paths.append(paths.empty() ? L"" : L",").append(path);
				return paths;
			}
			virtual void SearchPaths(string paths)
			{
				pr::str::Split<string>(paths, L",;", [&](string const& p, size_t s, size_t e)
				{
					m_paths.push_back(p.substr(s, e-s));
				});
			}

			// Add a path to the include search paths
			void AddSearchPath(string path, size_t index = ~size_t()) override
			{
				// Remove 'path' if already in the 'm_paths' collection
				auto end = std::remove_if(std::begin(m_paths), std::end(m_paths), [&](string const& s) { return pr::str::EqualI(s, path); });
				m_paths.erase(end, std::end(m_paths));
				m_paths.insert(std::begin(m_paths) + std::min(m_paths.size(), index), path);
			}

			// Resolve an include into a full path
			string ResolveInclude(string const& include, bool search_paths_only = false, Location const& loc = Location()) override
			{
				string searched_paths;

				// If we should search the local directory first, find the local directory name from 'loc'
				string local_dir, *current_dir = nullptr;
				if (!search_paths_only)
				{
					local_dir = pr::filesys::GetDirectory(loc.StreamName());
					current_dir = !local_dir.empty() ? &local_dir : nullptr;
				}

				// Resolve the filepath
				auto filepath = pr::filesys::ResolvePath(include, m_paths, current_dir, false, &searched_paths);
				if (!filepath.empty())
					return filepath;

				// Ignore if missing includes flagged
				if (m_ignore_missing_includes)
					return string();

				// If you hit this, check that the script source is a file source, string sources don't have a relative directory.
				auto filename = pr::filesys::GetFilename(include);
				auto msg = pr::FmtS("Failed to resolve include '%s'\n\nFile not found in search paths:\n%s", Narrow(filename).c_str(), Narrow(searched_paths).c_str());
				return FailPolicy::Fail(EResult::MissingInclude, loc, msg), string();
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool search_paths_only = false, Location const& loc = Location()) override
			{
				auto fullpath = ResolveInclude(include, search_paths_only, loc);
				if (!fullpath.empty())
				{
					FileOpened.Raise(fullpath);
					return std::make_unique<FileSrc<>>(fullpath.c_str());
				}
				return nullptr;
			}

			// Open 'include' as an ascii stream
			std::unique_ptr<std::istream> OpenStreamA(string const& include, bool search_paths_only = false, bool binary = false, Location const& loc = Location()) override
			{
				auto fullpath = ResolveInclude(include, search_paths_only, loc);
				if (!fullpath.empty())
				{
					FileOpened.Raise(fullpath);
					return std::make_unique<std::ifstream>(fullpath.c_str(), binary ? std::istream::binary : 0);
				}
				return nullptr;
			}

			// Raised whenever a file is opened
			pr::MultiCast<std::function<void(string const&)>> FileOpened;
		};
		#pragma endregion

		#pragma region String includes
		// A stirng include handler
		template <typename FailPolicy = ThrowOnFailure>
		struct StrIncludes :IIncludeHandler
		{
			// A map of include names to strings
			// Have to use std::string because std::istringstream expects it
			std::unordered_map<string, std::string> m_strings;

			// Add a path to the include search paths
			void AddSearchPath(string, size_t = ~size_t()) override
			{}

			// Resolve an include into a full path
			string ResolveInclude(string const& include, bool = false, Location const& = Location()) override
			{
				return include;
			}
			
			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool = false, Location const& loc = Location()) override
			{
				auto i = m_strings.find(include);
				if (i != std::end(m_strings)) return std::make_unique<PtrA<>>(i->second.c_str());
				return FailPolicy::Fail(EResult::MissingInclude, loc, pr::FmtS("Failed to open %s", Narrow(include).c_str())), nullptr;
			}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::istream> OpenStreamA(string const& include, bool = false, bool = false, Location const& loc = Location()) override
			{
				auto i = m_strings.find(include);
				if (i != std::end(m_strings)) return std::make_unique<istringstream>(i->second);
				return FailPolicy::Fail(EResult::MissingInclude, loc, pr::FmtS("Failed to open %s", Narrow(include).c_str())), nullptr;
			}
		};
		#pragma endregion

		#pragma region Resource Includes
		// An include handler that reads from an embedded resource
		template <typename FailPolicy = ThrowOnFailure>
		struct ResIncludes :IIncludeHandler
		{
			HMODULE m_module;

			ResIncludes(HMODULE module = 0)
				:m_module(module)
			{}

			// Convert 'name' into a resouce string id
			string ResId(string const& name) const
			{
				auto id = name;
				pr::str::Replace(id, L".", L"_");
				return pr::str::UpperCase(id);
			}

			// Add a path to the include search paths
			void AddSearchPath(string, size_t = ~size_t()) override
			{}

			// Resolve an include into a full path
			string ResolveInclude(string const& include, bool = false, Location const& = Location()) override
			{
				return ResId(include);
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is used to distinguish between #include <desc> and
			// #include "desc", it's value is true for the first case.
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool = false, Location const& = Location()) override
			{
				auto id = ResolveInclude(include);
				auto res = pr::resource::Read<char>(id.c_str(), L"TEXT", m_module);
				return std::make_unique<PtrA<>>(res.m_data);
			}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::istream> OpenStreamA(string const& include, bool = false, bool binary = false, Location const& = Location()) override
			{
				auto id = ResId(include);
				wchar_t const* type = binary ? L"BINARY" : L"TEXT";
				auto res = pr::resource::Read<char>(id.c_str(), type, m_module);
				return std::make_unique<pr::imemstream>(res.m_data, res.size());
			}
		};
		#pragma endregion
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
#include "pr/win32/win32.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_includes)
		{
			using namespace pr::str;
			using namespace pr::script;
			using string = pr::string<wchar_t>;

			auto script_include = L"script_include.txt";
			char data[] = "Included";
			{// Create the file
				std::ofstream fout(script_include);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}
			{
				FileIncludes<> inc;
				inc.AddSearchPath(pr::filesys::GetDirectory(pr::win32::ExePath<string>()));
				inc.AddSearchPath(pr::filesys::CurrentDirectory<string>());

				auto src_ptr = inc.Open(script_include);
				auto& src = *src_ptr;

				std::wstring r; for (;*src; ++src) r.push_back(*src);
				PR_CHECK(pr::str::Equal(r, data), true);
			}
			pr::filesys::EraseFile(script_include);
		}
	}
}
#endif
