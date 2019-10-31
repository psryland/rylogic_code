//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <vector>
#include <memory>
#include <istream>
#include <fstream>
#include <sstream>
#include <filesystem>
#include <unordered_map>
#include "pr/common/resource.h"
#include "pr/common/memstream.h"
#include "pr/common/multi_cast.h"
#include "pr/common/flags_enum.h"
#include "pr/maths/bit_fields.h"
#include "pr/filesys/filesys.h"
#include "pr/script/forward.h"
#include "pr/script/fail_policy.h"
#include "pr/script/location.h"

namespace pr::script
{
	// Info about the data being included
	enum class EIncludeFlags
	{
		None = 0,

		// True if the included data is binary data
		Binary = 1 << 0,

		// True for #include "file", false for #include <file>
		IncludeLocalDir = 1 << 1,

		_bitwise_operators_allowed,
	};

	// Source locations for includes
	enum class EIncludeTypes
	{
		None      = 0,
		Files     = 1 << 0,
		Resources = 1 << 1,
		Strings   = 1 << 2,
		All       = ~None,
		_bitwise_operators_allowed,
	};

	// A base class and interface for an include handler
	struct IIncludeHandler
	{
		IIncludeHandler()
			:IgnoreMissingIncludes()
		{}
		virtual ~IIncludeHandler() {}

		// True if missing includes do not throw exceptions
		bool IgnoreMissingIncludes;

		// Add a path to the include search paths
		virtual void AddSearchPath(std::filesystem::path const& path, size_t index = ~size_t())
		{
			(void)path, index;
		}

		// Resolve an include into a full path
		// 'search_paths_only' is true when the include is within angle brackets (i.e. #include <file>)
		virtual std::filesystem::path ResolveInclude(std::filesystem::path const& include, EIncludeFlags flags, Loc const& loc = Loc()) = 0;

		// Returns a 'Src' corresponding to the string "include".
		// 'search_paths_only' is true for #include <desc> and false for #include "desc".
		// 'loc' is where in the current source the include comes from.
		virtual std::unique_ptr<Src> Open(std::filesystem::path const& include, EIncludeFlags flags, Loc const& loc = Loc()) = 0;

		// Open 'include' as an ASCII stream
		virtual std::unique_ptr<std::istream> OpenStreamA(std::filesystem::path const& include, EIncludeFlags flags, Loc const& loc = Loc()) = 0;
	};

	// An include handler that doesn't handle any includes.
	struct NoIncludes :IIncludeHandler
	{
		// Resolve an include into a full path
		std::filesystem::path ResolveInclude(std::filesystem::path const&, EIncludeFlags, Loc const& loc = Loc()) override
		{
			throw ScriptException(EResult::IncludesNotSupported, loc, "#include is not supported");
		}

		// Returns a 'Src' corresponding to the string "include".
		// 'search_paths_only' is true for #include <desc> and false for #include "desc".
		// 'loc' is where in the current source the include comes from.
		std::unique_ptr<Src> Open(std::filesystem::path const&, EIncludeFlags, Loc const& loc = Loc()) override
		{
			throw ScriptException(EResult::IncludesNotSupported, loc, "#include is not supported");
		}

		// Open 'include' as an ASCII stream
		std::unique_ptr<std::istream> OpenStreamA(std::filesystem::path const&, EIncludeFlags, Loc const& loc = Loc()) override
		{
			throw ScriptException(EResult::IncludesNotSupported, loc, "#include is not supported");
		}
	};

	// An include handler that tries to open include files from resources, search paths, or a string table
	struct Includes :IIncludeHandler
	{
		using paths_t = std::vector<std::filesystem::path>;
		using modules_t = std::vector<HMODULE>;
		using strtable_t = std::unordered_map<string_t, std::string>;

	private:

		// Types of includes supported
		EIncludeTypes m_types;

		// The search paths to resolve include files from
		paths_t m_paths;

		// The binary modules containing resources
		modules_t m_modules;

		// A map of include names to utf-8 strings.
		strtable_t m_strtab;

	public:

		explicit Includes(EIncludeTypes types = EIncludeTypes::All)
			:m_types(types)
			,m_paths()
			,m_modules()
			,m_strtab()
			,FileOpened()
		{}
		explicit Includes(std::wstring_view const& search_paths, EIncludeTypes types = EIncludeTypes::All)
			:Includes(types)
		{
			SearchPathList(search_paths);
		}
		explicit Includes(std::initializer_list<HMODULE> modules, EIncludeTypes types = EIncludeTypes::All)
			:Includes(types)
		{
			ResourceModules(modules);
		}
		explicit Includes(std::wstring_view const& search_paths, std::initializer_list<HMODULE> modules, EIncludeTypes types = EIncludeTypes::All)
			:Includes(types)
		{
			SearchPathList(search_paths);
			ResourceModules(modules);
		}

		// Raised whenever a file is opened
		MultiCast<std::function<void(std::filesystem::path const&)>> FileOpened;

		// Get/Set the locations to look for includes
		EIncludeTypes Types() const
		{
			return m_types;
		}
		void Types(EIncludeTypes types)
		{
			m_types = types;
		}

		// Get/Set the search directories for include files
		paths_t const& SearchPaths() const
		{
			return m_paths;
		}
		void SearchPaths(std::initializer_list<std::filesystem::path> paths)
		{
			m_paths.assign(paths);
		}

		// Get/Set the modules to check for resources
		modules_t const& ResourceModules() const
		{
			return m_modules;
		}
		void ResourceModules(std::initializer_list<HMODULE> modules)
		{
			m_modules.assign(modules);
		}

		// Get/Set the string table
		strtable_t const& StringTable() const
		{
			return m_strtab;
		}
		void StringTable(strtable_t const& strtab)
		{
			m_strtab = strtab;
		}

		// Get/Set the search paths as a delimited list
		std::wstring SearchPathList() const
		{
			std::wstring paths;
			for (auto& path : m_paths)
				paths.append(paths.empty() ? L"" : L",").append(path);

			return paths;
		}
		void SearchPathList(std::wstring_view paths)
		{
			m_paths.resize(0);
			str::Split<std::wstring_view>(paths, L",;\n", [&](auto& p, size_t s, size_t e, int)
			{
				m_paths.push_back(p.substr(s, e - s));
			});

			if (!m_paths.empty())
				m_types |= EIncludeTypes::Files;
		}

		// Add a path to the include search paths. Ensures uniqueness of paths
		void AddSearchPath(std::filesystem::path const& path, size_t index = ~size_t()) override
		{
			// Remove 'path' if already in the 'm_paths' collection
			auto end = std::remove_if(std::begin(m_paths), std::end(m_paths), [=](auto const& s) { return std::filesystem::equivalent(s, path); });
			m_paths.erase(end, std::end(m_paths));
			m_paths.insert(std::begin(m_paths) + std::min(m_paths.size(), index), path);
		}

		// Add a module handle to the modules collection. Ensures uniqueness
		void AddResourceModule(HMODULE module, size_t index = ~size_t())
		{
			// Remove 'module' if already in the 'm_modules' collection
			auto end = std::remove_if(std::begin(m_modules), std::end(m_modules), [&](auto m) { return m == module; });
			m_modules.erase(end, std::end(m_modules));
			m_modules.insert(std::begin(m_modules) + std::min(m_modules.size(), index), module);
		}

		// Add a string to the string include table
		void AddString(string_t key, std::string_view value)
		{
			m_strtab[key] = value;
		}

		// Resolve a partial include file path into a full path
		// 'search_paths_only' is true when the include is within angle brackets (i.e. #include <file>)
		std::filesystem::path ResolveInclude(std::filesystem::path const& include, EIncludeFlags flags, Loc const& loc = Loc()) override
		{
			// Search files regardless of 'm_types' since this function is specifically for resolving filepaths
			std::filesystem::path fullpath;
			std::vector<std::filesystem::path> searched_paths;
			if (ResolveFileInclude(include, AllSet(flags, EIncludeFlags::IncludeLocalDir), loc, fullpath, searched_paths))
				return fullpath;

			// Ignore if missing includes flagged
			if (IgnoreMissingIncludes)
				return std::filesystem::path{};

			// Raise an include missing error
			auto msg = Fmt(L"Failed to resolve include '%s'", include.c_str());
			if (!searched_paths.empty()) msg.append(L"\n\nNot found in these search paths:");
			for (auto& path : searched_paths) msg.append(L"\n").append(path);
			throw ScriptException(EResult::MissingInclude, loc, msg);
		}

		// Returns a 'Src' corresponding to the string "include".
		// 'loc' is where in the current source the include comes from.
		// Although 'include' is a filesystem path, it can be an ordinary string for modules and string table includes.
		std::unique_ptr<Src> Open(std::filesystem::path const& include, EIncludeFlags flags, Loc const& loc = Loc()) override
		{
			if (AllSet(flags, EIncludeFlags::Binary))
				throw std::runtime_error("Binary includes cannot be opened with this method. 'Src' streams are text");

			// Try file includes
			std::filesystem::path fullpath;
			std::vector<std::filesystem::path> searched_paths;
			if (AllSet(m_types, EIncludeTypes::Files) && ResolveFileInclude(include, AllSet(flags, EIncludeFlags::IncludeLocalDir), loc, fullpath, searched_paths))
			{
				FileOpened.Raise(fullpath);
				return std::make_unique<FileSrc>(fullpath);
			}

			// Try resources
			HMODULE module;
			auto id = ResId(include);
			if (AllSet(m_types, EIncludeTypes::Resources) && ResolveResourceInclude(id, false, module))
			{
				// The resource remains in scope until the DLL is unloaded, so we don't need to
				// copy the resource data to the buffer of the StringSrc.
				auto res = resource::Read<char>(id, L"TEXT", module);
				return std::make_unique<StringSrc>(std::string_view(res.m_data, res.m_len));
			}

			// Try the string table
			strtable_t* strtab;
			auto tag = include.wstring();
			if (AllSet(m_types, EIncludeTypes::Strings) && ResolveStringInclude(tag, strtab))
			{
				// The string table string remains in scope for the lifetime of this includes instance.
				auto const& str = (*strtab)[tag];
				return std::make_unique<StringSrc>(str);
			}

			// If ignoring missing includes, return an empty source
			if (IgnoreMissingIncludes)
			{
				return std::make_unique<NullSrc>();
			}

			// Raise an include missing error
			auto msg = Fmt(L"Failed to open include '%s'", include.c_str());
			if (!searched_paths.empty()) msg.append(L"\n\nNot found in these search paths:");
			for (auto& path : searched_paths) msg.append(L"\n").append(path);
			throw ScriptException(EResult::MissingInclude, loc, msg);
		}

		// Open 'include' as an text or binary stream
		std::unique_ptr<std::basic_istream<char>> OpenStreamA(std::filesystem::path const& include, EIncludeFlags flags, Loc const& loc = Loc()) override
		{
			// Try file includes
			std::filesystem::path fullpath;
			std::vector<std::filesystem::path> searched_paths;
			if (AllSet(m_types, EIncludeTypes::Files) && ResolveFileInclude(include, AllSet(flags, EIncludeFlags::IncludeLocalDir), loc, fullpath, searched_paths))
			{
				FileOpened.Raise(fullpath);
				return std::make_unique<std::ifstream>(fullpath, AllSet(flags, EIncludeFlags::Binary) ? std::istream::binary : 0);
			}

			// Try resources
			HMODULE module;
			auto id = ResId(include);
			if (AllSet(m_types, EIncludeTypes::Resources) && ResolveResourceInclude(id, AllSet(flags, EIncludeFlags::Binary), module))
			{
				auto res = resource::Read<char>(id, AllSet(flags, EIncludeFlags::Binary) ? L"BINARY" : L"TEXT", module);
				return std::unique_ptr<std::basic_istream<char>>(new mem_istream<char>(res.m_data, res.size()));
			}

			// Try the string table
			strtable_t* strtab;
			auto tag = include.wstring();
			if (AllSet(m_types, EIncludeTypes::Strings) && ResolveStringInclude(tag, strtab))
			{
				auto const& str = (*strtab)[tag];
				return std::make_unique<std::istringstream>(str);
			}

			// If ignoring missing includes, return an empty source
			if (IgnoreMissingIncludes)
			{
				return std::make_unique<std::istringstream>("");
			}

			// Raise an include missing error
			auto msg = Fmt(L"Failed to open include stream '%s'", include.c_str());
			if (!searched_paths.empty()) msg.append(L"\n\nNot found in these search paths:");
			for (auto& path : searched_paths) msg.append(L"\n").append(path);
			throw ScriptException(EResult::MissingInclude, loc, msg);
		}

	private:

		// Resolve an include into a full path
		bool ResolveFileInclude(std::filesystem::path const& include, bool include_local_dir, Loc const& loc, std::filesystem::path& result, std::vector<std::filesystem::path>& searched_paths)
		{
			result.clear();

			// If we should search the local directory first, find the local directory name from 'loc'
			std::filesystem::path local_dir, *local_dir_ptr = nullptr;
			if (include_local_dir && std::filesystem::exists(loc.Filepath()))
			{
				// Note: 'local_dir' means the directory local to the current source location 'loc'.
				// Don't use the executable local directory: "pr::filesys::CurrentDirectory<string>()"
				// If the executable directory is wanted, add it manually before calling resolve.
				local_dir = loc.Filepath().parent_path();
				local_dir_ptr = !local_dir.empty() ? &local_dir : nullptr;
			}

			// Resolve the filepath
			auto filepath = filesys::ResolvePath(include, m_paths, local_dir_ptr, false, &searched_paths);
			if (filepath.empty())
				return false;

			// Return the resolved include
			result = filepath;
			return true;
		}

		// Resolve an include from the available modules
		bool ResolveResourceInclude(std::wstring_view id, bool binary, HMODULE& module)
		{
			for (auto m : m_modules)
			{
				if (!resource::Find(id, binary ? L"BINARY" : L"TEXT", m)) continue;
				module = m;
				return true;
			}
			return false;
		}

		// Resolve an include into a string that is in the string table
		bool ResolveStringInclude(string_t const& tag, strtable_t*& strtab)
		{
			// Future version may have multiple string tables
			strtab = m_strtab.find(tag) != std::end(m_strtab) ? &m_strtab : nullptr;
			return strtab != nullptr;
		}

		// Convert 'name' into a resource string id
		static std::wstring ResId(std::filesystem::path const& name)
		{
			auto id = name.wstring();
			str::Replace(id, L".", L"_");
			return str::UpperCase(id);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
#include "pr/win32/win32.h"
namespace pr::script
{
	PRUnitTest(IncludesTests)
	{
		using namespace pr::str;
		using string = pr::string<wchar_t>;

		char data[] = "Included";
		auto script_include = L"script_include.txt";
		auto cleanup = CreateScope([] {}, [=] { std::filesystem::remove(script_include); });

		{// Create the file
			std::ofstream fout(script_include);
			fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
		}

		{
			Includes inc;

			inc.AddSearchPath(win32::ExePath<std::filesystem::path>().parent_path());
			inc.AddSearchPath(std::filesystem::current_path());

			auto src_ptr = inc.Open(script_include, EIncludeFlags::None);
			auto& src = *src_ptr;

			std::wstring r; for (;*src; ++src) r.push_back(*src);
			PR_CHECK(pr::str::Equal(r, data), true);
		}
	}
}
#endif
