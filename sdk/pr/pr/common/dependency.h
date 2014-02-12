//**********************************************
// Dependency Checker
//  Copyright © Rylogic Ltd 2007
//**********************************************

// Code file dependency checker
// Loads a table of filenames and modified times.
// Each file is either modified, not modified, or unknown. When an unknown
// file is queried, it's dependent files are checked (recursive). This is
// used to decide one way or the other whether a file is modified.
// Note: modified means changed itself or includes a changed file

#ifndef PR_DEPENDENCY_H
#define PR_DEPENDENCY_H

#include <iostream>
#include <sstream>
#include <fstream>
#include <vector>
#include <string>
#include <map>
#include <set>
#include "pr/common/assert.h"
#include "pr/common/crc.h"
#include "pr/common/fmt.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/str/prstring.h"

namespace pr
{
	typedef std::vector<std::string> TPaths;

	enum EDepChk
	{
		EDepChk_TreadMissingAsModified	= 1 << 0,
		EDepChk_IncludesInQuotsOnly		= 1 << 1,
		EDepChk_DefaultFlags			= EDepChk_TreadMissingAsModified
	};

	namespace impl
	{
		typedef std::set<CRC> TDepFiles;

		struct OutputFunctorBooleanTest
		{
			bool operator()(std::string const&, bool) { return true; }
		};
		struct OutputFunctorToStdOut
		{
			void operator()(std::string const& filename, std::size_t nest_level)
			{
				if( nest_level == 0 )	std::cout << std::endl;
				else					std::cout << std::string(nest_level, ' ');
										std::cout << filename << std::endl;
			}
		};

		template <typename T>
		class DependencyChecker
		{
		public:
			// Create an empty dependency checker. All files will appear modified
			DependencyChecker(unsigned int flags = EDepChk_DefaultFlags)
			:m_dep_filename("")
			,m_flags(flags)
			{}

			// Load a dependency file on construction
			explicit DependencyChecker(char const* dependency_file, unsigned int flags = EDepChk_DefaultFlags)
			:m_dep_filename(dependency_file)
			,m_flags(flags)
			{
				LoadDependencyFile(m_dep_filename.c_str());
			}

			// Load a dependency file on construction
			DependencyChecker(char const* dependency_file, TPaths const& include_paths, unsigned int flags = EDepChk_DefaultFlags)
			:m_dep_filename(dependency_file)
			,m_flags(flags)
			{
				SetIncludePaths(include_paths);
				LoadDependencyFile(m_dep_filename.c_str());
			}

			// Load a dependency file on construction
			// 'paths' is a string containing include paths delimited with ';'
			DependencyChecker(char const* dependency_file, char const* include_paths, unsigned int flags = EDepChk_DefaultFlags)
			:m_dep_filename(dependency_file)
			,m_flags(flags)
			{
				SetIncludePaths(include_paths);
				LoadDependencyFile(m_dep_filename.c_str());
			}

			// On destruction save the dependency data
			~DependencyChecker()
			{
				SaveDependencyFile(m_dep_filename.c_str());
			}

			// Set how to consider missing files (modified, or not)
			// Combination of 'EDepChk' flags
			unsigned int GetBehaviour() const					{ return m_flags; }
			void SetBehaviour(unsigned int flags)				{ m_flags = flags; }

			// Clear all dependency data
			void Clear()										{ m_dep.clear(); }

			// Get/Set include paths for resolving files to full path names
			TPaths const& GetIncludePaths() const				{ return m_include; }
			void SetIncludePaths(TPaths const& include_paths)	{ m_include = include_paths; }
			void SetIncludePaths(char const* include_paths) // string containing include paths delimited with ';'
			{
				m_include.clear();
				if( !include_paths || *include_paths == '\0' ) return;

				std::string paths = include_paths;
				std::size_t s = 0, e = paths.find_first_of(";");
				if( e == std::string::npos ) m_include.push_back(paths);
				else
				{
					for( ; e != std::string::npos; s = e + 1, e = paths.find_first_of(";", s) )
						m_include.push_back(paths.substr(s, e - s));
					if( s != paths.size() )
						m_include.push_back(paths.substr(s, paths.size() - s));
				}
			}

			// Load the checker with saved dependency data. This is a text file containing
			// full path filenames followed by last modified times. Returns false if the
			// file failed to load. Passing '0' or "" to this function has no effect
			bool LoadDependencyFile(char const* dependency_file)
			{
				if( !dependency_file || *dependency_file == '\0' ) return true;
				m_dep_filename = dependency_file;
				Clear();

				std::ifstream dep_file(m_dep_filename.c_str());
				if( dep_file.bad() ) return false;
				while( !dep_file.eof() )
				{
					char file[MAX_PATH];	dep_file.getline(file, MAX_PATH, ',');
					filesys::TFileTime	tm;	dep_file >> tm;
					if( dep_file.fail() ) break;
					char junk[10];			dep_file.getline(junk, 10);

					CRC file_crc			= pr::Crc(file, strlen(file));
					if( !filesys::DoesFileExist(file) ) continue;

					Info& info				= m_dep[file_crc];
					info.m_filename			= file;
					info.m_last_mod_time	= filesys::GetFileTimeStats(file).m_last_modified;
					info.m_modified			= info.m_last_mod_time != tm ? EMod_Yes : EMod_Unknown;
				}
				return true;
			}

			// Save the dependency data contained in the checker to 'dep_filename'
			// Passing '0' or "" to this function has no effect
			bool SaveDependencyFile() { return SaveDependencyFile(m_dep_filename.c_str()); }
			bool SaveDependencyFile(char const* dep_filename)
			{
				if( !dep_filename || *dep_filename == '\0' ) return true;

				std::ofstream dep_file(dep_filename, std::ios_base::out|std::ios_base::trunc);
				if( dep_file.bad() ) return false;
				for( TDepData::const_iterator i = m_dep.begin(), i_end = m_dep.end(); i != i_end; ++i )
					dep_file << i->second.m_filename << "," << i->second.m_last_mod_time << std::endl;
				return true;
			}

			// Resolve a file name into a full path using the include paths.
			// 'file' - the file to resolve
			// 'include' - an array of paths to prefix 'file' with
			// Returns the first match with an existing file.
			// Returns an empty string if the file could not be resolved
			bool ResolveFilename(std::string const& file, std::string& resolved) const
			{
				resolved = filesys::Standardise(file);

				// Check the filename itself first
				if( filesys::DoesFileExist(file) ) { return true; }

				// Try concatinating it with the include directories
				std::string full_path;
				for( TPaths::const_iterator p = m_include.begin(), p_end = m_include.end(); p != p_end; ++p )
				{
					full_path = filesys::Make(*p, file);
					if( filesys::DoesFileExist(full_path) ) { resolved = filesys::Standardise(full_path); return true; }
				}

				// <shrug>
				return false;
			}

			// Given a set of files, look for any that have been modified.
			// Returns true when the first modified (or depends on a modified) file is found.
			bool FilesModified(TPaths const& files)
			{
				return FilesModified(files, OutputFunctorBooleanTest());
			}

			// Given a set of files, look for all that have been modified
			// Returns true if at least one file is modified (or depends on a modified file)
			// Outputs all files and whether they have been modified or not
			//	struct OutFunctor
			//	{
			//		// Return false to terminate searching for modified files
			//		// true to carry on searching
			//		bool operator()(std::string const& filename, bool modified);
			//	};
			template <typename OutFunctor>
			bool FilesModified(TPaths const& files, OutFunctor out)
			{
				bool modified_file_found = false;
				for( TPaths::const_iterator p = files.begin(), p_end = files.end(); p != p_end; ++p )
				{
					// Check whether the file (or a file it depends upon) has been modified
					TDepFiles dep_files;
					std::string filename = *p;
					bool modified = FileModified(filename, dep_files);
					modified_file_found |= modified;

					if( !out(filename, modified) ) return modified_file_found;
				}
				return modified_file_found;
			}

			// Check a single file for being modified
			// Returns true if 'filename' has been modified according to our data
			bool FileModified(std::string const& file)
			{
				TDepFiles dep_files;
				std::string filename = file;
				return FileModified(filename, dep_files);
			}

			// Calls an output functor for paths in an include tree
			//	struct OutFunctor
			//	{
			//		void operator()(std::string const& filename, std::size_t nest_level);
			//	};
			template <typename OutFunctor> void ShowIncludes(TPaths const& files, OutFunctor out)
			{
				for( TPaths::const_iterator f = files.begin(), f_end = files.end(); f != f_end; ++f )
					ShowIncludes(*f, out);
			}
			template <typename OutFunctor> void ShowIncludes(std::string const& filename, OutFunctor out)
			{
				TDepFiles dep_files;
				ShowIncludes(filename, out, 0, dep_files);
			}
			void ShowIncludes(TPaths const& files)			{ ShowIncludes(files, OutputFunctorToStdOut()); }
			void ShowIncludes(std::string const& filename)	{ ShowIncludes(filename, OutputFunctorToStdOut()); }

		private:
			// Returns true if 'file' has been modified according to our data
			// Returns the full path in 'filename'. Recursive function.
			bool FileModified(std::string& filename, TDepFiles& dep_files)
			{
				// Resolve the file name using the include paths.
				// If a file cannot be resolved assume it's been modified.
				if( !ResolveFilename(filename, filename) ) { return m_flags & EDepChk_TreadMissingAsModified; }
				PR_ASSERT(PR_DBG, filesys::DoesFileExist(filename), "");

				// Make a crc for the filename
				CRC file_crc = pr::Crc(&filename[0], filename.size());

				// Check the cache to see whether we already know if this file has been modified
				TDepData::iterator dep = m_dep.find(file_crc);
				if( dep != m_dep.end() )
				{
					Info& info = dep->second;
					PR_ASSERT(PR_DBG, info.m_filename == filename, "");
					if( info.m_modified != EMod_Unknown ) return info.m_modified == EMod_Yes;
				}
				// Otherwise, add the file to the cache. We have to
				// add it as modified because we haven't seen it before
				else
				{
					Info& info				= m_dep[file_crc];
					info.m_filename			= filename;
					info.m_last_mod_time	= filesys::GetFileTimeStats(filename).m_last_modified;
					info.m_modified			= EMod_Yes;
				}

				// Ensure all of the dependents of this file are added to the checker
				// Extract all of the files referred to by #include
				// An error in the file counts as a modified file
				TPaths dependents;
				if( !PreprocessFile(filename, dependents) ) return true;

				// Add this file to the dependents set
				dep_files.insert(file_crc);

				// Check each dependent file for being modified
				bool modified = false;
				for( TPaths::const_iterator p = dependents.begin(), p_end = dependents.end(); p != p_end; ++p )
				{
					// Resolve the file name using the include paths.
					// If a file cannot be resolved assume it's been modified.
					std::string dep;
					if( !ResolveFilename(*p, dep) ) { modified |= (m_flags & EDepChk_TreadMissingAsModified); continue; }

					// Prevent circular dependencies causing infinite loops
					// by only considering files we haven't seen before.
					CRC dep_crc = pr::Crc(dep.c_str(), dep.size());
					if( dep_files.find(dep_crc) != dep_files.end() ) continue;

					modified |= FileModified(dep, dep_files);
				}

				// Remove this file from the dependents set
				dep_files.erase(file_crc);

				// Update the record for this file
				Info& info = m_dep[file_crc];
				if( info.m_modified == EMod_Unknown ) info.m_modified = modified ? EMod_Yes : EMod_No;
				return info.m_modified == EMod_Yes;
			}

			// Output the include file dependency in 'tree'
			template <typename OutFunctor>
			void ShowIncludes(std::string const& file, OutFunctor out, std::size_t level, TDepFiles& dep_files)
			{
				bool look_for_dependents = true;

				// Resolve the file name using the include paths.
				std::string filename;
				if( !ResolveFilename(file, filename) )
				{
					filename = Fmt("[%s]", file.c_str());
					look_for_dependents = false;
				}

				// Get the dependent filenames
				TPaths dependents;
				if( look_for_dependents && !PreprocessFile(filename, dependents) )
				{
					filename = Fmt("[%s] - include error", file.c_str());
					look_for_dependents = false;
				}

				// Give up here if an error occured
				if( !look_for_dependents ) { out(filename, level); return; }

				// Prevent circular dependencies causing infinite loops
				// by only considering files we haven't seen before.
				CRC file_crc = pr::Crc(filename.c_str(), filename.size());
				if( dep_files.find(file_crc) != dep_files.end() )
				{
					out(filename + " - (circular)", level);
					return;
				}

				// Add the filename
				out(filename, level);

				// Add this file to the dependents set
				dep_files.insert(file_crc);

				// Add each dependent file
				for( TPaths::const_iterator p = dependents.begin(), p_end = dependents.end(); p != p_end; ++p )
				{
					ShowIncludes(*p, out, level + 1, dep_files);
				}

				// Remove this file from the dependents set
				dep_files.erase(file_crc);
			}

			// Open the file 'filename' and parse it for include files (dependents).
			// Maintain a set of #defined symbols to prevent circular includes
			// Returns true if the file was preprocessed successfully.
			bool PreprocessFile(std::string const& filename, TPaths& dependents)
			{
				std::string file;
				if( !FileToString(filename.c_str(), file) ) return false;

				// Reduce the file to just the 'code'
				str::StripComments(file);

				// Scan through the file looking for the '#' symbol.
				// We need to skip over strings when we find them
				char const *s_start = file.c_str(), *s_end = s_start + file.size(), *s = s_start, *e = s_start;
				for( s = s_start; s != s_end; ++s )
				{
					switch( *s )
					{
					case '#':
						if( strncmp(s, "#include", 8) == 0 )
						{
							// Found an include file
							for( s += 8;    s != s_end && *s != '"' && *s != '<'; ++s )	{} if( s == s_end ) return false;
							for( e = s + 1; e != s_end && *e != '"' && *e != '>'; ++e )	{} if( e == s_end ) return false;
							std::string include_file(s+1, e - (s+1));
							if( m_flags & EDepChk_IncludesInQuotsOnly ) { if(*s == '"') dependents.push_back(include_file); }
							else										{				dependents.push_back(include_file); }
							s = e;
						}break;
					// Skip over strings that may contain '#' charactors
					case '"':
					case '\'':
						e = s;
						for( ++s; s != s_end && *s != *e; ++s )		{ if(*s == '\\') {++s;} }
						if( s == s_end ) return false;
						break;
					}
				}
				return true;
			}

		private:
			enum EMod { EMod_No, EMod_Yes, EMod_Unknown };
			struct Info
			{
				std::string			m_filename;			// Full path of the file
				pr::filesys::uint64	m_last_mod_time;	// The time the file was last modified
				EMod				m_modified;			// True if according to our records the file has been modified
			};
			typedef std::map<CRC, Info> TDepData;
			std::string		m_dep_filename;				// The name of the dependency text file to open/create
			TDepData		m_dep;						// Dependency data
			TPaths			m_include;					// Include paths for resolving file names to full paths
			unsigned int	m_flags;					// Combination of EDepChk
		};
	}//namespace impl

	typedef impl::DependencyChecker<void> DependencyChecker;
}//namespace pr

#endif//PR_DEPENDENCY_H
