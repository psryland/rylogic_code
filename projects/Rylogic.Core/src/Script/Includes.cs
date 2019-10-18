using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

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
		public Includes(EType types = EType.Files)
		{
			Types = types;
			SearchPaths = new List<string>();
			Assemblies = new List<Assembly>();
			StrTable = new Dictionary<string, string>();
			FileOpened = null;
		}
		public Includes(string search_paths, EType types = EType.Files)
			:this(types)
		{
			SearchPathList = search_paths;
		}
		public Includes(IEnumerable<Assembly> assemblies, EType types = EType.Resources)
			:this(types)
		{
			Assemblies.Assign(assemblies);
		}
		public Includes(string search_paths, IEnumerable<Assembly> assemblies, EType types = EType.Files|EType.Resources)
			:this(types)
		{
			SearchPathList = search_paths;
			Assemblies.Assign(assemblies);
		}

		/// <summary>Types of includes supported</summary>
		public EType Types { get; set; }

		/// <summary>The directory paths to search</summary>
		public List<string> SearchPaths { get; }

		/// <summary>The assemblies containing resources</summary>
		public List<Assembly> Assemblies { get; }

		/// <summary>A map of include names to strings</summary>
		public Dictionary<string, string> StrTable { get; }

		/// <summary>If true, missing includes do not throw</summary>
		public bool IgnoreMissingIncludes { get; set; }

		/// <summary>Get/Set the search paths as a delimited list</summary>
		public string SearchPathList
		{
			get => string.Join(",", SearchPaths.Where(x => x.Length != 0));
			set
			{
				SearchPaths.Clear();
				foreach (var path in value.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
					SearchPaths.Add(path);

				if (SearchPaths.Count != 0)
					Types |= EType.Files;
			}
		}

		/// <summary> Convert 'name' into a resource string id</summary>
		public static string ResId(string name)
		{
			return name.Replace(".", "_");
		}

		/// <summary>Add/Insert a path in the search paths</summary>
		public void AddSearchPath(string path, int? index = null)
		{
			index ??= SearchPaths.Count;
			path = Path_.Canonicalise(path);
			SearchPaths.Insert(Math.Min(index.Value, SearchPaths.Count), path);
			SearchPaths.RemoveIf(x => !ReferenceEquals(x,path) && Path_.Compare(x, path) == 0);
		}

		/// <summary>Resolve an include into a full path</summary>
		public string ResolveInclude(string include, EIncludeFlags flags, Loc? loc = null)
		{
			// Try file includes
			var searched_paths = new List<string>();
			if (Types.HasFlag(EType.Files) && ResolveFileInclude(include, flags.HasFlag(EIncludeFlags.IncludeLocalDir), loc, out var result, out searched_paths))
				return result;

			// TODO: this is broken, is 'result' a filepath or the content of the include??

		//	// Try assemblies
		//	if (Types.HasFlag(EType.Resources) && ResolveResourceInclude(include, out result, out _))
		//		return result;
		//
		//	// Try the string table
		//	if (Types.HasFlag(EType.Strings) && ResolveStringInclude(include, out result))
		//		return result;

			// Ignore if missing includes flagged
			if (IgnoreMissingIncludes)
				return string.Empty;

			throw new ScriptException(EResult.MissingInclude, loc ?? new Loc(), searched_paths.Count != 0
				? $"Failed to resolve include '{include}'\n\nNot found in these search paths:\n{string.Join("\n", searched_paths)}"
				: $"Failed to resolve include '{include}'");
		}

		/// <summary>
		/// Returns a 'Src' corresponding to the string "include".
		/// 'search_paths_only' is true for #include <desc> and false for #include "desc".
		/// 'loc' is where in the current source the include comes from.</summary>
		public Src Open(string include, EIncludeFlags flags, Loc? loc = null)
		{
			// Try file includes
			var searched_paths = new List<string>();
			if (Types.HasFlag(EType.Files) && ResolveFileInclude(include, flags.HasFlag(EIncludeFlags.IncludeLocalDir), loc, out var result, out searched_paths))
			{
				FileOpened?.Invoke(this, new ValueEventArgs(result));
				return new FileSrc(result);
			}

			// Try resources
			var id = ResId(include);
			if (Types.HasFlag(EType.Resources) && ResolveResourceInclude(id, out var assembly))
			{
				using var stream = assembly!.GetManifestResourceStream(id) ?? throw new Exception("Resource not found");
				using var reader = new StreamReader(stream, Encoding.UTF8, true);
				return new StringSrc(reader.ReadToEnd());
			}

			// Try the string table
			var key = include;
			if (Types.HasFlag(EType.Strings) && ResolveStringInclude(key, out var strtab))
			{
				return new StringSrc(strtab![key]);
			}

			// If ignoring missing includes, return an empty source
			if (IgnoreMissingIncludes)
			{
				return new NullSrc();
			}

			// Throw on missing include
			throw new ScriptException(EResult.MissingInclude, loc ?? new Loc(), searched_paths.Count != 0
					? $"Failed to open include '{include}'\n\nNot found in these search paths:\n{string.Join("\n", searched_paths)}"
					: $"Failed to open include '{include}'");
		}

		/// <summary>Open 'include' as a stream</summary>
		public Stream OpenStream(string include, EIncludeFlags flags, Loc? loc = null)
		{
			// Try file includes
			var searched_paths = new List<string>();
			if (Types.HasFlag(EType.Files) && ResolveFileInclude(include, flags.HasFlag(EIncludeFlags.IncludeLocalDir), loc, out var fullpath, out searched_paths))
			{
				FileOpened?.Invoke(this, new ValueEventArgs(fullpath));
				return new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			// Try resources
			var id = ResId(include);
			if (Types.HasFlag(EType.Resources) && ResolveResourceInclude(id, out var assembly))
			{
				return assembly!.GetManifestResourceStream(id) ?? throw new Exception("Resource not found");
			}

			// Try the string table
			var key = include;
			if (Types.HasFlag(EType.Strings) && ResolveStringInclude(key, out var strtab))
			{
				return new MemoryStream(Encoding.UTF8.GetBytes(strtab![key]));
			}

			// If ignoring missing includes, return an empty source
			if (IgnoreMissingIncludes)
			{
				return new MemoryStream(Array.Empty<byte>());
			}

			// Throw on missing include
			throw new ScriptException(EResult.MissingInclude, loc ?? new Loc(), searched_paths.Count != 0
					? $"Failed to open include '{include}'\n\nNot found in these search paths:\n{string.Join("\n", searched_paths)}"
					: $"Failed to open include '{include}'");
		}

		/// <summary>Raised whenever a file is opened</summary>
		public event EventHandler<ValueEventArgs>? FileOpened;

		/// <summary>Resolve an include partial path into a full path</summary>
		private bool ResolveFileInclude(string include, bool include_local_dir, Loc? loc, out string fullpath, out List<string> searched_paths)
		{
			fullpath = string.Empty;

			// If we should search the local directory first, find the local directory name from 'loc'.
			// Note: 'local_dir' means the directory local to the current source location 'loc'.
			// Don't use the executable local directory: "Environment.CurrentDirectory"
			// If the executable directory is wanted, add it manually before calling resolve.
			var local_dir = include_local_dir && loc?.Uri is string uri ? Path_.Directory(uri) : null;

			// Resolve the filepath
			searched_paths = new List<string>();
			var filepath = Path_.ResolvePath(include, SearchPaths, local_dir, false, ref searched_paths);
			if (filepath.Length == 0)
				return false;

			// Return the resolved include
			fullpath = filepath;
			return true;
		}

		/// <summary>Resolve a resource key into the assembly that contains that resource</summary>
		private bool ResolveResourceInclude(string resource_key, out Assembly? assembly)
		{
			foreach (var ass in Assemblies)
			{
				if (ass.GetManifestResourceStream(resource_key) == null) continue;
				assembly = ass;
				return true;
			}
			assembly = null;
			return false;
		}

		/// <summary>Resolve a string include tag into the value from the string table</summary>
		private bool ResolveStringInclude(string tag, out Dictionary<string,string>? str_table)
		{
			// Future versions may have multiple string tables
			str_table = StrTable.ContainsKey(tag) ? StrTable : null;
			return str_table != null;
		}

		/// <summary>The locations to search for includes</summary>
		[Flags]
		public enum EType
		{
			None      = 0,
			Files     = 1 << 0,
			Resources = 1 << 1,
			Strings   = 1 << 2,
			All       = Files | Resources | Strings,
		}
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
