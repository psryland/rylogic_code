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
#include "pr/script2/forward.h"
#include "pr/script2/fail_policy.h"

namespace pr
{
	namespace script2
	{
		// A base class and interface for an include handler
		template <typename FailPolicy = ThrowOnFailure>
		struct IncludeHandler
		{
			using string = pr::string<wchar_t>;

			virtual ~IncludeHandler() {}

			// Add a path to the include search paths
			virtual void AddSearchPath(string path, size_t index = ~0U) = 0;

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			virtual std::unique_ptr<Src> Open(string const& include, bool search_paths_only = false, Location const& loc = Location()) = 0;

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			virtual std::unique_ptr<std::wistream> OpenStream(string const& include, bool binary = false, Location const& loc = Location()) = 0;
		};

		#pragma region No Includes
		// An include handler that doesn't handle any includes.
		template <typename FailPolicy = ThrowOnFailure>
		struct NoIncludes :IncludeHandler<FailPolicy>
		{
			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool search_paths_only = false, Location const& loc = Location()) override
			{
				(void)include,loc,search_paths_only;
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported"), nullptr;
			}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::wistream> OpenStream(string const& include, bool binary = false, Location const& loc = Location()) override
			{
				(void)include,binary,loc;
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported"), nullptr;
			}
		};
		#pragma endregion

		#pragma region File Includes
		// A file include handler
		template <typename FailPolicy = ThrowOnFailure>
		struct FileIncludes :IncludeHandler<FailPolicy>
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
			void AddSearchPath(string path, size_t index = ~0U) override
			{
				// Remove 'path' if already in the 'm_paths' collection
				auto end = std::remove_if(std::begin(m_paths), std::end(m_paths), [&](string const& s) { return pr::str::EqualI(s, path); });
				m_paths.erase(end, std::end(m_paths));
				m_paths.insert(std::begin(m_paths) + std::min(m_paths.size(), index), path);
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool search_paths_only = false, Location const& loc = Location()) override
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
					return std::make_unique<FileSrc<>>(filepath.c_str());

				// Ignore if missing includes flagged
				if (m_ignore_missing_includes)
					return nullptr;

				// If you hit this, check that the script source is a file source, string sources don't have a relative directory.
				auto filename = pr::filesys::GetFilename(include);
				auto msg = pr::FmtS("Failed to open '%s'\n\nFile not found in search paths:\n%s", Narrow(filename).c_str(), Narrow(searched_paths).c_str());
				throw Exception(EResult::MissingInclude, loc, msg);
			}

			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::wistream> OpenStream(string const& include, bool binary = false, Location const& loc = Location()) override
			{
				string searched_paths;

				// Find the local directory name from 'loc'
				auto local_path = pr::filesys::GetDirectory(loc.StreamName());
				auto current_dir = !local_path.empty() ? &local_path : nullptr;
				
				// Resolve the filepath
				auto filepath = pr::filesys::ResolvePath(include, m_paths, current_dir, false, &searched_paths);
				if (!filepath.empty())
					return std::make_unique<std::wifstream>(filepath.c_str(), binary ? std::wistream::binary : 0);

				// Check that the script source is a file source, String sources don't have a relative directory
				auto filename = pr::filesys::GetFilename(include);
				auto msg = pr::FmtS("Failed to open '%s'\n\nFile not found in search paths:\n%s", Narrow(filename).c_str(), Narrow(searched_paths).c_str());
				throw Exception(EResult::MissingInclude, loc, msg);
			}
		};
		#pragma endregion

		#pragma region String includes
		// A stirng include handler
		template <typename FailPolicy = ThrowOnFailure>
		struct StrIncludes :IncludeHandler<FailPolicy>
		{
			// A map of include names to strings
			// Have to use std::wstring because std::wistringstream expects it
			std::unordered_map<string, std::wstring> m_strings;

			// Add a path to the include search paths
			void AddSearchPath(string path, size_t index = ~0U) override
			{
				(void)path,index;
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool = false, Location const& loc = Location()) override
			{
				auto i = m_strings.find(include);
				if (i != std::end(m_strings)) return std::make_unique<PtrW<>>(i->second.c_str());
				throw Exception(EResult::MissingInclude, loc, pr::FmtS("Failed to open %s", Narrow(include).c_str()));
			}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::wistream> OpenStream(string const& include, bool = false, Location const& loc = Location()) override
			{
				auto i = m_strings.find(include);
				if (i != std::end(m_strings)) return std::make_unique<wistringstream>(i->second);
				throw Exception(EResult::MissingInclude, loc, pr::FmtS("Failed to open %s", Narrow(include).c_str()));
			}
		};
		#pragma endregion

		#pragma region Resource Includes
		// An include handler that reads from an embedded resource
		template <typename FailPolicy = ThrowOnFailure>
		struct ResIncludes :IncludeHandler<FailPolicy>
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

			// Add a path to the include search paths
			void AddSearchPath(string path, size_t index = ~0U) override
			{
				(void)path,index;
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is used to distinguish between #include <desc> and
			// #include "desc", it's value is true for the first case.
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool = false, Location const& = Location()) override
			{
				auto id = ResId(include);
				auto res = pr::resource::Read<char>(id.c_str(), L"TEXT", m_module);
				return std::make_unique<PtrA>(res.m_data);
			}

			// Returns an input stream corresponding to 'include'
			// 'mode' indicates if the stream is text or binary
			// 'loc' is the current position in the source (used to open streams relative to the current file)
			std::unique_ptr<std::istream> OpenStream(string const& include, bool binary = false, Location const& = Location()) override
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
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script2_includes)
		{
			using namespace pr::str;
			using namespace pr::script2;
			using string = pr::string<wchar_t>;

			auto script_include = L"script_include.txt";
			char data[] = "Included";
			{// Create the file
				std::ofstream fout(script_include);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}
			{
				FileIncludes<> inc;
				inc.AddSearchPath(pr::filesys::GetDirectory(pr::filesys::ExePath<string>()));
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
