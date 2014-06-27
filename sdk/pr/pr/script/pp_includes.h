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
		// Interface for a object that stores macro definitions
		struct IIncludes
		{
			virtual ~IIncludes() {}
			
			// Return a character stream (allocated using new) that corresponds
			// to the string "include". The caller is responsible for deleting the returned stream.
			// 'search_paths_only' is used to distinguish betweem #include <desc> and
			// #include "desc", it's value is true for the first case
			virtual Src* Open(pr::script::string const& include, pr::script::Loc const& loc, bool search_paths_only) = 0;
			
			// Allow missing includes to be ignored
			virtual bool IgnoreMissing() const = 0;
			virtual void IgnoreMissing(bool ignore) = 0;
		};
		
		// An implementation that ignores includes
		struct IgnoreIncludes :IIncludes
		{
			Src* Open(pr::script::string const&, pr::script::Loc const&, bool) { return 0; }
			bool IgnoreMissing() const { return true; }
			void IgnoreMissing(bool) {}
		};
		
		// A default implementation of an include handler for files
		struct FileIncludes :IIncludes
		{
			typedef pr::Array<string> Paths;
			Paths m_paths;          // Search paths for looking up includes
			bool  m_ignore_missing; // True if missing includes are just ignored
			
			FileIncludes()
				:m_paths()
				,m_ignore_missing(false)
			{}
			
			// Open an include file
			FileSrc* Open(pr::script::string const& include, pr::script::Loc const& loc, bool search_paths_only)
			{
				string searched_paths;

				// Search the directory of the current source (if it's a file)
				if (!search_paths_only && !loc.m_file.empty())
				{
					string dir = pr::filesys::GetDirectory(loc.m_file);
					string path = pr::filesys::CombinePath(dir, include);
					if (pr::filesys::FileExists(path))
						return new FileSrc(path.c_str());
					else
						searched_paths.append(dir).append("\n");
				}
				
				// Search the include paths
				string path;
				for (auto& dir : m_paths)
				{
					path = pr::filesys::CombinePath(dir, include);
					if (pr::filesys::FileExists(path))
						return new FileSrc(path.c_str());
					else
						searched_paths.append(dir).append("\n");
				}
				
				if (!m_ignore_missing)
				{
					// Check that the script source is a file source, String sources don't have a relative directory
					auto msg = fmt("Failed to open %s\n\nFile not found in search paths:\n%s", include.c_str(), searched_paths.c_str());
					throw Exception(EResult::MissingInclude, loc, msg);
				}
				return 0;
			}
			
			// Allow missing includes to be ignored
			bool IgnoreMissing() const      { return m_ignore_missing; }
			void IgnoreMissing(bool ignore) { m_ignore_missing = ignore; }
		};

		// A default implementation of an include handler for strings
		struct StrIncludes :IIncludes
		{
			typedef std::map<string, string> Strings;
			Strings m_strings;        // A map of include names to strings
			bool    m_ignore_missing; // True if missing strings are just ignored
			
			StrIncludes()
			:m_strings()
			,m_ignore_missing(false)
			{}
			
			// Open an included 'string'
			PtrSrc* Open(pr::script::string const& include, pr::script::Loc const& loc, bool)
			{
				Strings::const_iterator i = m_strings.find(include);
				if (i != m_strings.end()) return new PtrSrc(i->second.c_str());
				if (!m_ignore_missing) throw Exception(EResult::MissingInclude, loc, fmt("Failed to open %s", include.c_str()));
				return 0;
			}
			
			// Allow missing includes to be ignored
			bool IgnoreMissing() const      { return m_ignore_missing; }
			void IgnoreMissing(bool ignore) { m_ignore_missing = ignore; }
		};
	}
}

#endif
