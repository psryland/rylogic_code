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
#include "pr/common/flags_enum.h"
#include "pr/script/forward.h"
#include "pr/script/fail_policy.h"

namespace pr
{
	namespace script
	{
		// A base class and interface for an include handler
		struct IIncludeHandler
		{
			bool m_ignore_missing_includes;

			IIncludeHandler()
				:m_ignore_missing_includes()
			{}
			virtual ~IIncludeHandler() {}

			// Add a path to the include search paths
			virtual void AddSearchPath(string path, size_t index = ~size_t())
			{
				(void)path, index;
			}

			// Resolve an include into a full path
			// 'search_paths_only' is true when the include is within angle brackets (i.e. #include <file>)
			virtual string ResolveInclude(string const& include, bool search_paths_only = false, Location const& loc = Location()) = 0;

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			virtual std::unique_ptr<Src> Open(string const& include, bool search_paths_only = false, Location const& loc = Location()) = 0;

			// Open 'include' as an ASCII stream
			virtual std::unique_ptr<std::istream> OpenStreamA(string const& include, bool search_paths_only = false, bool binary = false, Location const& loc = Location()) = 0;
		};

		#pragma region General Includes
		// An include handler that tries to open include files from resources, search paths, string map
		template <typename FailPolicy = ThrowOnFailure>
		struct Includes :IIncludeHandler
		{
			using Paths = pr::vector<string>;
			using Modules = pr::vector<HMODULE>;
			using StrMap = std::unordered_map<string, std::string>;

			enum class EType
			{
				None      = 0,
				Files     = 1 << 0,
				Resources = 1 << 1,
				Strings   = 1 << 2,
				All       = ~None,
				_bitwise_operators_allowed,
			};

		private:

			// Types of includes supported
			EType m_types;

			// The search paths to resolve include files from
			Paths m_paths;

			// The binary modules containing resources
			Modules m_modules;

			// A map of include names to strings
			// Have to use std::string because 'std::istringstream' expects it
			StrMap m_strtab;

		public:

			Includes(EType types = EType::None)
				:m_types(types)
				,m_paths()
				,m_modules()
				,m_strtab()
				,FileOpened()
			{}
			explicit Includes(string const& search_paths, EType types = EType::None)
				:Includes(types)
			{
				SearchPaths(search_paths);
			}
			explicit Includes(std::initializer_list<HMODULE> const& modules, EType types = EType::None)
				: Includes(types)
			{
				ResourceModules(modules);
			}
			explicit Includes(string const& search_paths, std::initializer_list<HMODULE> const& modules, EType types = EType::None)
				:Includes(types)
			{
				SearchPaths(search_paths);
				ResourceModules(modules);
			}
			Includes(Includes&& rhs)
				:m_types(rhs.m_types)
				,m_paths(std::move(rhs.m_paths))
				,m_modules(std::move(rhs.m_modules))
				,m_strtab(std::move(rhs.m_strtab))
				,FileOpened(std::move(rhs.FileOpened))
			{}

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
				m_paths.resize(0);
				pr::str::Split<string>(paths, L",;", [&](string const& p, size_t s, size_t e)
				{
					m_paths.push_back(p.substr(s, e - s));
				});

				if (m_paths.empty())
					m_types &= ~EType::Files;
				else
					m_types |= EType::Files;
			}

			// Get/Set the modules to check for resources
			virtual Modules const& ResourceModules() const
			{
				return m_modules;
			}
			virtual void ResourceModules(std::initializer_list<HMODULE> const& modules)
			{
				m_modules.assign(std::begin(modules), std::end(modules));

				if (m_modules.empty())
					m_types &= ~EType::Resources;
				else
					m_types |= EType::Resources;
			}

			// Get/Set the string table
			virtual StrMap const& StringTable() const
			{
				return m_strtab;
			}
			virtual void StringTable(StrMap const& strtab)
			{
				m_strtab = strtab;

				if (m_modules.empty())
					m_types &= ~EType::Strings;
				else
					m_types |= EType::Strings;
			}

			// Add a path to the include search paths. Ensures uniqueness of paths
			void AddSearchPath(string path, size_t index = ~size_t()) override
			{
				static_assert(has_bitwise_operators_allowed<EType>::value, "");
				m_types |= EType::Files;

				// Remove 'path' if already in the 'm_paths' collection
				auto end = std::remove_if(std::begin(m_paths), std::end(m_paths), [&](string const& s) { return pr::str::EqualI(s, path); });
				m_paths.erase(end, std::end(m_paths));
				m_paths.insert(std::begin(m_paths) + std::min(m_paths.size(), index), path);
			}

			// Add a module handle to the modules collection. Ensures uniqueness
			void AddResourceModule(HMODULE module, size_t index = ~size_t())
			{
				m_types |= EType::Resources;

				// Remove 'module' if already in the 'm_modules' collection
				auto end = std::remove_if(std::begin(m_modules), std::end(m_modules), [&](HMODULE m) { return m == module; });
				m_modules.erase(end, std::end(m_modules));
				m_modules.insert(std::begin(m_modules) + std::min(m_modules.size(), index), module);
			}

			// Add a string to the string include table
			void AddString(string key, std::string value)
			{
				m_types |= EType::Strings;
				m_strtab[key] = value;
			}

			// Convert 'name' into a resource string id
			string ResId(string const& name) const
			{
				auto id = name;
				pr::str::Replace(id, L".", L"_");
				return pr::str::UpperCase(id);
			}

			// Resolve an include into a full path
			// 'search_paths_only' is true when the include is within angle brackets (i.e. #include <file>)
			string ResolveInclude(string const& include, bool search_paths_only = false, Location const& loc = Location()) override
			{
				string result, searched_paths;
				HMODULE module;

				// Try file includes
				if (int(m_types & EType::Files) != 0 && ResolveFileInclude(include, search_paths_only, loc, result, searched_paths))
					return result;

				// Try resources
				if (int(m_types & EType::Resources) != 0 && ResolveResourceInclude(include, result, module))
					return result;

				// Try the string table
				if (int(m_types & EType::Strings) != 0 && ResolveStringInclude(include, result))
					return result;

				// Ignore if missing includes flagged
				if (m_ignore_missing_includes)
					return string();

				auto msg = !searched_paths.empty()
					? pr::FmtS("Failed to resolve include '%S'\n\nNot found in these search paths:\n%S", include.c_str(), searched_paths.c_str())
					: pr::FmtS("Failed to resolve include '%S'", include.c_str());
				return FailPolicy::Fail(EResult::MissingInclude, loc, msg, string());
			}

			// Resolve an include into a full path
			bool ResolveFileInclude(string const& include, bool search_paths_only, Location const& loc, string& result, string& searched_paths)
			{
				result.resize(0);

				// If we should search the local directory first, find the local directory name from 'loc'
				string local_dir, *current_dir = nullptr;
				if (!search_paths_only)
				{
					local_dir = pr::filesys::GetDirectory(loc.StreamName());
					current_dir = !local_dir.empty() ? &local_dir : nullptr;
				}

				// Resolve the filepath
				auto filepath = pr::filesys::ResolvePath(include, m_paths, current_dir, false, &searched_paths);
				if (filepath.empty())
					return false;

				// Return the resolved include
				result = filepath;
				return true;
			}

			// Resolve an include from the available modules
			bool ResolveResourceInclude(string const& include, string& result, HMODULE& module)
			{
				result = ResId(include);
				for (auto m : m_modules)
				{
					if (!pr::resource::Find(result.c_str(), L"TEXT", m)) continue;
					module = m;
					return true;
				}
				return false;
			}

			// Resolve an include into a string that is in the string table
			bool ResolveStringInclude(string const& include, string& result)
			{
				result = include;
				return m_strtab.find(include) != std::end(m_strtab);
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const& include, bool search_paths_only = false, Location const& loc = Location()) override
			{
				string result, searched_paths;
				HMODULE module;

				// Try file includes
				if (int(m_types & EType::Files) != 0 && ResolveFileInclude(include, search_paths_only, loc, result, searched_paths))
				{
					FileOpened.Raise(result);
					return std::make_unique<FileSrc<>>(result.c_str());
				}

				// Try resources
				if (int(m_types & EType::Resources) != 0 && ResolveResourceInclude(include, result, module))
				{
					auto res = pr::resource::Read<char>(result.c_str(), L"TEXT", module);
					return std::make_unique<PtrA<>>(res.m_data);
				}

				// Try the string table
				if (int(m_types & EType::Strings) != 0 && ResolveStringInclude(include, result))
				{
					auto i = m_strtab.find(result);
					return std::make_unique<PtrA<>>(i->second.c_str());
				}

				auto msg = !searched_paths.empty()
					? pr::FmtS("Failed to open include '%S'\n\nFile not found in search paths:\n%S", include.c_str(), searched_paths.c_str())
					: pr::FmtS("Failed to open include '%S'", include.c_str());
				return FailPolicy::Fail(EResult::MissingInclude, loc, msg, std::unique_ptr<Src>());
			}

			// Open 'include' as an ASCII stream
			std::unique_ptr<std::istream> OpenStreamA(string const& include, bool search_paths_only = false, bool binary = false, Location const& loc = Location()) override
			{
				string result, searched_paths;
				HMODULE module;

				// Try file includes
				if (int(m_types & EType::Files) != 0 && ResolveFileInclude(include, search_paths_only, loc, result, searched_paths))
				{
					FileOpened.Raise(result);
					return std::make_unique<std::ifstream>(result.c_str(), binary ? std::istream::binary : 0);
				}

				// Try resources
				if (int(m_types & EType::Resources) != 0 && ResolveResourceInclude(include, result, module))
				{
					auto res = pr::resource::Read<char>(result.c_str(), binary ? L"BINARY" : L"TEXT", module);
					return std::make_unique<pr::imemstream>(res.m_data, res.size());
				}

				// Try the string table
				if (int(m_types & EType::Strings) != 0 && ResolveStringInclude(include, result))
				{
					auto i = m_strtab.find(result);
					return std::make_unique<std::istringstream>(i->second);
				}

				auto msg = !searched_paths.empty()
					? pr::FmtS("Failed to resolve include '%S'\n\nFile not found in search paths:\n%S", include.c_str(), searched_paths.c_str())
					: pr::FmtS("Failed to resolve include '%S'", include.c_str());
				return FailPolicy::Fail(EResult::MissingInclude, loc, msg, std::unique_ptr<std::istream>());
			}

			// Raised whenever a file is opened
			pr::MultiCast<std::function<void(string const&)>> FileOpened;
		};
		#pragma endregion

		#pragma region No Includes
		// An include handler that doesn't handle any includes.
		template <typename FailPolicy = ThrowOnFailure>
		struct NoIncludes :IIncludeHandler
		{
			// Resolve an include into a full path
			string ResolveInclude(string const&, bool = false, Location const& loc = Location()) override
			{
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported", string());
			}

			// Returns a 'Src' corresponding to the string "include".
			// 'search_paths_only' is true for #include <desc> and false for #include "desc".
			// 'loc' is where in the current source the include comes from.
			std::unique_ptr<Src> Open(string const&, bool = false, Location const& loc = Location()) override
			{
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported", std::unique_ptr<Src>());
			}

			// Open 'include' as an ASCII stream
			std::unique_ptr<std::istream> OpenStreamA(string const&, bool = false, bool = false, Location const& loc = Location()) override
			{
				return FailPolicy::Fail(EResult::IncludesNotSupported, loc, "#include is not supported", std::unique_ptr<std::istream>());
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
				Includes<> inc;
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
