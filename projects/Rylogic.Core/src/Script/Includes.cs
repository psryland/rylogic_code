using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Rylogic.Common;

namespace Rylogic.Script
{
	public interface IIncludeHandler
	{
		/// <summary>Add a path to the include search paths</summary>
		void AddSearchPath(string path, int? index = null);

		/// <summary>
		/// Resolve an include into a full path.
		/// 'search_paths_only' is true when the include is within angle brackets (i.e. #include <file>)</summary>
		string ResolveInclude(string include, EIncludeFlags flags, Loc? loc = null);

		/// <summary>
		/// Returns a 'Src' corresponding to the string "include".
		/// 'search_paths_only' is true for #include <desc> and false for #include "desc".
		/// 'loc' is where in the current source the include comes from.</summary>
		Src Open(string include, EIncludeFlags flags, Loc? loc = null);

		/// <summary>Open 'include' as a stream</summary>
		Stream OpenStream(string include, EIncludeFlags flags, Loc? loc = null);
	}

	[Flags]
	public enum EIncludeFlags
	{
		None = 0,

		// True if the included data is binary data
		Binary = 1 << 0,

		// True for #include "file", false for #include <file>
		IncludeLocalDir = 1 << 1,
	};

	/// <summary>An include handler that tries to open include files from resources, search paths, or a string table</summary>
	public class Includes : IIncludeHandler
	{
		[Flags] public enum EType
		{
			None      = 0,
			Files     = 1 << 0,
			Resources = 1 << 1,
			Strings   = 1 << 2,
			All       = ~None,
		}

		/// <summary>Types of includes supported</summary>
		private EType m_types;

		/// <summary>The search paths to resolve include files from</summary>
		private readonly List<string> m_paths;

		// The binary modules containing resources
		//private List<IntPtr> m_modules;

		/// <summary>A map of include names to strings</summary>
		private readonly Dictionary<string, string> m_strtab;

		public Includes(EType types = EType.Files)
		{
			m_types = types;
			m_paths = new List<string>();
			//m_modules()
			m_strtab = new Dictionary<string, string>();
			FileOpened = null;
		}
		public Includes(string search_paths, EType types = EType.Files)
			:this(types)
		{
			SearchPaths = search_paths;
		}
		//public Includes(std::initializer_list<HMODULE> modules, EType types = EType::Files)
		//	:Includes(types)
		//{
		//	ResourceModules(modules);
		//}
		//public Includes(string search_paths, std::initializer_list<HMODULE> modules, EType types = EType::None)
		//	:Includes(types)
		//{
		//	SearchPaths(search_paths);
		//	ResourceModules(modules);
		//}

		/// <summary>Get/Set the search paths as a comma or semicolon separated list</summary>
		public string SearchPaths
		{
			get
			{
				var paths = new StringBuilder();
				foreach (var path in m_paths)
					paths.Append(paths.Length != 0 ? "," : "").Append(path);

				return paths.ToString();
			}
			set
			{
				m_paths.Clear();
				foreach (var path in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
					m_paths.Add(path);

				if (m_paths.Count != 0)
					m_types |= EType.Files;
			}
		}

		//// Get/Set the modules to check for resources
		//public virtual Modules const& ResourceModules() const
		//{
		//	return m_modules;
		//}
		//public virtual void ResourceModules(std::initializer_list<HMODULE> modules)
		//{
		//	m_modules.assign(std::begin(modules), std::end(modules));
		//
		//	if (!m_modules.empty())
		//		m_types |= EType::Resources;
		//}

		///// <summary>Get/Set the string table</summary>
		//public IDictionary<string, string> StringTable
		//{
		//	get => m_strtab;
		//	set
		//	{
		//		m_strtab = value;
		//		if (m_strtab.Count != 0)
		//			m_types |= EType.Strings;
		//	}
		//}

		/// <summary>Add a path to the include search paths. Ensures uniqueness of paths</summary>
		public void AddSearchPath(string path, int? index = null)
		{
			m_types |= EType.Files;

			// Remove 'path' if already in the 'm_paths' collection
			path = Path_.Canonicalise(path);
			m_paths.Remove(path);
			m_paths.Add(path);
		}

		//// Add a module handle to the modules collection. Ensures uniqueness
		//void AddResourceModule(HMODULE module, size_t index = ~size_t())
		//{
		//	m_types |= EType::Resources;
		//
		//	// Remove 'module' if already in the 'm_modules' collection
		//	auto end = std::remove_if(std::begin(m_modules), std::end(m_modules), [&](HMODULE m) { return m == module; });
		//	m_modules.erase(end, std::end(m_modules));
		//	m_modules.insert(std::begin(m_modules) + std::min(m_modules.size(), index), module);
		//}

		//// Add a string to the string include table
		//void AddString(string key, std::string value)
		//{
		//	m_types |= EType::Strings;
		//	m_strtab[key] = value;
		//}

		//// Convert 'name' into a resource string id
		//string ResId(string const& name) const
		//{
		//	auto id = name;
		//	pr::str::Replace(id, L".", L"_");
		//	return pr::str::UpperCase(id);
		//}

		// Resolve an include into a full path
		// 'search_paths_only' is true when the include is within angle brackets (i.e. #include <file>)
		public string ResolveInclude(string include, EIncludeFlags flags, Loc? loc = null)
		{
#if false
			string result, searched_paths;
			HMODULE module;

			// Try file includes
			if (pr::AllSet(m_types, EType::Files) && ResolveFileInclude(include, AllSet(flags, EFlags::IncludeLocalDir), loc, result, searched_paths))
				return result;

			// Try resources
			if (pr::AllSet(m_types, EType::Resources) && ResolveResourceInclude(include, result, AllSet(flags, EFlags::Binary), module))
				return result;

			// Try the string table
			if (pr::AllSet(m_types, EType::Strings) && ResolveStringInclude(include, result))
				return result;

			// Ignore if missing includes flagged
			if (m_ignore_missing_includes)
				return string();

			throw Exception(EResult::MissingInclude, loc, !searched_paths.empty()
				? pr::FmtS("Failed to resolve include '%S'\n\nNot found in these search paths:\n%S", include.c_str(), searched_paths.c_str())
				: pr::FmtS("Failed to resolve include '%S'", include.c_str()));
#endif
			throw new NotImplementedException();
		}

		// Resolve an include into a full path
		public bool ResolveFileInclude(string include, bool include_local_dir, Loc? loc, out string result, out string searched_paths)
		{
			result = string.Empty;
			searched_paths = string.Empty;
#if false
			// If we should search the local directory first, find the local directory name from 'loc'
			string local_dir, *current_dir = nullptr;
			if (include_local_dir)
			{
				// Note: 'local_dir' means the directory local to the current source location 'loc'.
				// Don't use the executable local directory: "pr::filesys::CurrentDirectory<string>()"
				// If the executable directory is wanted, add it manually before calling resolve.
				local_dir = pr::filesys::GetDirectory(loc.StreamName());
				current_dir = !local_dir.empty() ? &local_dir : nullptr;
			}

			// Resolve the filepath
			auto filepath = pr::filesys::ResolvePath<string>(include, m_paths, current_dir, false, &searched_paths);
			if (filepath.empty())
				return false;

			// Return the resolved include
			result = filepath;
			return true;
#endif
			throw new NotImplementedException();
		}

		//// Resolve an include from the available modules
		//public bool ResolveResourceInclude(string include, out string result, bool binary, HMODULE& module)
		//{
		//	result = ResId(include);
		//	for (auto m : m_modules)
		//	{
		//		if (!pr::resource::Find(result.c_str(), binary ? L"BINARY" : L"TEXT", m)) continue;
		//		module = m;
		//		return true;
		//	}
		//	return false;
		//}

		/// <summary>Resolve an include into a string that is in the string table</summary>
		public bool ResolveStringInclude(string include, out string result)
		{
			return m_strtab.TryGetValue(include, out result);
		}

		// Returns a 'Src' corresponding to the string "include".
		// 'search_paths_only' is true for #include <desc> and false for #include "desc".
		// 'loc' is where in the current source the include comes from.
		public Src Open(string include, EIncludeFlags flags, Loc? loc = null)
		{
	throw new NotImplementedException();
#if false
			string result, searched_paths;
			HMODULE module;

			// Try file includes
			if (AllSet(m_types, EType::Files) && ResolveFileInclude(include, AllSet(flags, EFlags::IncludeLocalDir), loc, result, searched_paths))
			{
				FileOpened.Raise(result);
				return std::make_unique<FileSrc>(result.c_str());
			}

			// Try resources
			if (AllSet(m_types, EType::Resources) && ResolveResourceInclude(include, result, AllSet(flags, EFlags::Binary), module))
			{
				auto res = pr::resource::Read<char>(result.c_str(), L"TEXT", module);
				return std::make_unique<PtrA>(res.m_data);
			}

			// Try the string table
			if (AllSet(m_types, EType::Strings) && ResolveStringInclude(include, result))
			{
				auto i = m_strtab.find(result);
				return std::make_unique<PtrA>(i->second.c_str());
			}

			// If ignoring missing includes, return an empty source
			if (m_ignore_missing_includes)
			{
				return std::make_unique<PtrA>("");
			}

			throw Exception(EResult::MissingInclude, loc, !searched_paths.empty()
				? pr::FmtS("Failed to open include '%S'\n\nFile not found in search paths:\n%S", include.c_str(), searched_paths.c_str())
				: pr::FmtS("Failed to open include '%S'", include.c_str()));
#endif
		}

		// Open 'include' as an ASCII stream
		public Stream OpenStream(string include, EIncludeFlags flags, Loc? loc = null)
		{
#if false
			string result, searched_paths;
			HMODULE module;

			// Try file includes
			if (AllSet(m_types, EType::Files) && ResolveFileInclude(include, AllSet(flags, EFlags::IncludeLocalDir), loc, result, searched_paths))
			{
				FileOpened.Raise(result);
				return std::make_unique<std::ifstream>(result.c_str(), AllSet(flags, EFlags::Binary) ? std::istream::binary : 0);
			}

			// Try resources
			if (AllSet(m_types, EType::Resources) && ResolveResourceInclude(include, result, AllSet(flags, EFlags::Binary), module))
			{
				auto res = pr::resource::Read<char>(result.c_str(), AllSet(flags, EFlags::Binary) ? L"BINARY" : L"TEXT", module);
				return std::unique_ptr<std::basic_istream<char>>(new mem_istream<char>(res.m_data, res.size()));
			}

			// Try the string table
			if (AllSet(m_types, EType::Strings) && ResolveStringInclude(include, result))
			{
				auto i = m_strtab.find(result);
				return std::make_unique<std::istringstream>(i->second);
			}

			throw Exception(EResult::MissingInclude, loc, !searched_paths.empty()
				? pr::FmtS("Failed to resolve include '%S'\n\nFile not found in search paths:\n%S", include.c_str(), searched_paths.c_str())
				: pr::FmtS("Failed to resolve include '%S'", include.c_str()));
#endif
			throw new NotImplementedException();
		}

		/// <summary>Raised whenever a file is opened</summary>
		public event EventHandler<ValueEventArgs>? FileOpened;
	}

	/// <summary>An include handler that doesn't handle any includes.</summary>
	public class NoIncludes :IIncludeHandler
	{
		public NoIncludes()
		{}

		/// <summary>Add a path to the include search paths</summary>
		public void AddSearchPath(string path, int? index = null)
		{
			// Ignore
		}

		/// <summary>Resolve an include into a full path</summary>
		public string ResolveInclude(string include, EIncludeFlags flags, Loc? loc = null)
		{
			throw new ScriptException(EResult.IncludesNotSupported, loc ?? new Loc(), "#include is not supported");
		}

		/// <summary>
		/// Returns a 'Src' corresponding to the string "include".
		/// 'search_paths_only' is true for #include <desc> and false for #include "desc".
		/// 'loc' is where in the current source the include comes from.</summary>
		public Src Open(string include, EIncludeFlags flags, Loc? loc = null)
		{
			throw new ScriptException(EResult.IncludesNotSupported, loc ?? new Loc(), "#include is not supported");
		}

		/// <summary>Open 'include' as a stream</summary>
		public Stream OpenStream(string include, EIncludeFlags flags, Loc? loc = null)
		{
			throw new ScriptException(EResult.IncludesNotSupported, loc ?? new Loc(), "#include is not supported");
		}
	};
}
