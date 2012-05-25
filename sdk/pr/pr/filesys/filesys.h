//**********************************************
// File path/File system operations
//  Copyright © Rylogic Ltd 2009
//**********************************************
//
//	Pathname	= full path e.g. Drive:/path/path/file.ext
//	Drive		= the drive e.g. "P". No ':'
//	Path		= the directory without the drive. No leading '/', no trailing '/'. e.g. Path/path
//	Directory	= the drive + path. no trailing '/'. e.g P:/Path/path
//	Extension	= the last string following a '.'
//	Filename	= file name including extension
//	FileTitle	= file name not including extension
//
// A full pathname = drive + ":/" + path + "/" + filetitle + "." + extension
//
#ifndef PR_FILESYS_FILESYS_H
#define PR_FILESYS_FILESYS_H
	
#pragma warning(push, 1)
#include <io.h>
#include <direct.h>
#include <sys/types.h>
#include <sys/stat.h>
#pragma warning(pop)
#include <ctime>
#include <string>
#include <fstream>
#include <algorithm>
	
#ifndef PR_ASSERT
#	define PR_ASSERT_DEFINED
#	define PR_ASSERT(grp, exp, str)
#endif
	
namespace pr
{
	namespace filesys
	{
		namespace EAttrib
		{
			enum Type
			{
				Invalid     = 0,
				Device      = 1 << 0,
				File        = 1 << 1,
				Directory   = 1 << 2,
				Pipe        = 1 << 3,
				WriteAccess = 1 << 4,
				ReadAccess  = 1 << 5,
				ExecAccess  = 1 << 6,
			};
		}
		
		enum Access
		{
			Exists    = 0,
			Write     = 2,
			Read      = 4,
			ReadWrite = Write | Read
		};
		
		// Unix Time = seconds since midnight January 1, 1970 UTC 
		// FILETIME = 100-nanosecond intervals since January 1, 1601 UTC 
		
		// File timestamp data for a file.
		// Note: these timestamps are in UTC unix time
		struct FileTime
		{
			time_t m_last_access;   // Note: time_t is 64bit
			time_t m_last_modified; 
			time_t m_created;       
		};
		
		// Convert a UTC unix time to a local timezone unix time
		inline time_t UTCtoLocal(time_t t)
		{
			struct tm utc,local;
			if (gmtime_s(&utc, &t) != 0 || localtime_s(&local, &t) != 0) throw std::exception("failed to convert utc time to local time");
			return t + (mktime(&local) - mktime(&utc));
		}
		
		// Convert local timezone unix time to UTC unix time
		inline time_t LocaltoUTC(time_t t)
		{
			struct tm utc,local;
			if (gmtime_s(&utc, &t) != 0 || localtime_s(&local, &t) != 0) throw std::exception("failed to convert local time to utc time");
			return t - (mktime(&local) - mktime(&utc));
		}
		
		// Convert between unix time and i64. The resulting i64 can then be converted to FILETIME, SYSTEMTIME, etc
		inline __int64 UnixTimetoI64(time_t  t) { return t * 10000000LL + 116444736000000000LL; }
		inline time_t  I64toUnixTime(__int64 t) { return (t - 116444736000000000LL) / 10000000LL; }
		
		// Conversions between __int64, FILETIME, and SYSTEMTIME
		// Requires <windows.h> to be included
		// Note: the '__int64's here are not the same as the timestamps in 'FileTime'
		// those values are in unix time. Use 'UnixTimetoI64()'
		#ifdef _FILETIME_
		inline __int64    FTtoI64(FILETIME ft)          { __int64  n  = __int64(ft.dwHighDateTime) << 32 | __int64(ft.dwLowDateTime); return n; }
		inline FILETIME   I64toFT(__int64 n)            { FILETIME ft; ft.dwHighDateTime = DWORD((n>>32)&0xFFFFFFFFULL); ft.dwLowDateTime = DWORD(n&0xFFFFFFFFULL); return ft; }
		#ifdef _WINBASE_
		inline SYSTEMTIME FTtoST(FILETIME const& ft)    { SYSTEMTIME st; ::FileTimeToSystemTime(&ft, &st); return st; }
		inline FILETIME   STtoFT(SYSTEMTIME const& st)  { FILETIME   ft; ::SystemTimeToFileTime(&st, &ft); return ft; }
		inline __int64    STtoI64(SYSTEMTIME const& st) { return FTtoI64(STtoFT(st)); }
		inline SYSTEMTIME I64toST(__int64 n)            { return FTtoST(I64toFT(n)); }
		#endif
		#endif
		
		namespace impl
		{
			// Unicode handling
			template <typename Char> inline int access(Char const* filename, int access_mode);
			template <> inline int access(char    const* filename, int access_mode) { return ::_access (filename, access_mode); }
			template <> inline int access(wchar_t const* filename, int access_mode) { return ::_waccess(filename, access_mode); }
			
			template <typename Char> inline int rename(Char const* oldname, Char const* newname);
			template <> inline int rename(char    const* oldname, char    const* newname) { return ::rename  (oldname, newname); }
			template <> inline int rename(wchar_t const* oldname, wchar_t const* newname) { return ::_wrename(oldname, newname); }
			
			template <typename Char> inline int remove(Char const* path);
			template <> inline int remove(char    const* path) { return ::remove  (path); }
			template <> inline int remove(wchar_t const* path) { return ::_wremove(path); }
			
			template <typename Char> inline int mkdir(Char const* dirname);
			template <> inline int mkdir(char    const* dirname) { return ::_mkdir (dirname); }
			template <> inline int mkdir(wchar_t const* dirname) { return ::_wmkdir(dirname); }
			
			template <typename Char> inline int rmdir(Char const* dirname);
			template <> inline int rmdir(char    const* dirname) { return ::_rmdir (dirname); }
			template <> inline int rmdir(wchar_t const* dirname) { return ::_wrmdir(dirname); }
			
			template <typename Char> inline Char* fullpath(Char* abs_path, Char const* rel_path, size_t max_length);
			template <> inline char*    fullpath(char*    abs_path, char    const* rel_path, size_t max_length) { return ::_fullpath (abs_path, rel_path, max_length); }
			template <> inline wchar_t* fullpath(wchar_t* abs_path, wchar_t const* rel_path, size_t max_length) { return ::_wfullpath(abs_path, rel_path, max_length); }
		}
		
		// Return true if 'ch' is a directory marker
		template <typename Char> inline bool DirMark(Char ch)
		{
			return ch == Char('\\') || ch == Char('/');
		}
		
		// Return true if two characters are the same as far as a path is concerned
		template <typename Char> inline bool EqualPathChar(Char lhs, Char rhs)
		{
			return tolower(lhs) == tolower(rhs) || (DirMark(lhs) && DirMark(rhs));
		}
		
		// Return true if 'path' contains a drive, i.e. is a full path
		template <typename String> inline bool IsFullPath(String const& path)
		{
			typedef String::value_type Char;
			Char const colon[] = {Char(':'), 0};
			return path.find(colon) != String::npos;
		}
		
		// Add quotes to the str if it doesn't already have them
		template <typename String> inline String& AddQuotes(String& str)
		{
			typedef String::value_type Char;
			if (str.size() > 1 && str[0] == Char('"') && str[str.size()-1] == Char('"')) return str;
			str.insert(str.begin(), Char('"'));
			str.insert(str.end  (), Char('"'));
			return str;
		}
		template <typename String> inline String AddQuotesC(String const& str)
		{
			String s = str;
			return AddQuotes(s);
		}
		
		// Remove quotes from 'str' if it has them
		template <typename String> inline String& RemoveQuotes(String& str)
		{
			typedef String::value_type Char;
			if (str.size() < 2 || str[0] != Char('"') || str[str.size()-1] != Char('"')) return str;
			str = str.substr(1, str.size()-2);
			return str;
		}
		template <typename String> inline String RemoveQuotesC(String const& str)
		{
			String s = str;
			return RemoveQuotes(s);
		}
		
		// Remove the leading back slash from 'str' if it exists
		template <typename String> inline String& RemoveLeadingBackSlash(String& str)
		{
			typedef String::value_type Char;
			if (!str.empty() && (str[0] == Char('\\') || str[0] == Char('/'))) str.erase(0,1);
			return str;
		}
		template <typename String> inline String RemoveLeadingBackSlashC(String const& str)
		{
			String s = str;
			return RemoveLeadingBackSlash(s);
		}
		
		// Remove the last back slash from 'str' if it exists
		template <typename String> inline String& RemoveLastBackSlash(String& str)
		{
			typedef String::value_type Char;
			if (!str.empty() && (str[str.size()-1] == Char('\\') || str[str.size()-1] == Char('/'))) str.erase(str.size()-1, 1);
			return str;
		}
		template <typename String> inline String RemoveLastBackSlashC(String const& str)
		{
			String s = str;
			return RemoveLastBackSlash(s);
		}
		
		// Convert 'C:\path0\.\path1\../path2\file.ext' into 'C:\path0\path2\file.ext'
		template <typename String> inline String& Canonicalise(String& str)
		{
			typedef String::value_type Char;
			Char const dir_marks[] = {Char('\\'),Char('/'),0};
			
			size_t d0 = 0, d1, d2, d3;
			for (;;)
			{
				d1 = str.find_first_of(dir_marks, d0);
				if (d1 == String::npos)                                    { return str; }
				if (str[d1] == Char('/'))                                  { str[d1] = Char('\\'); }
				if (d1 == 1 && str[0] == Char('.'))                        { str.erase(0, 2); d0 = 0; continue; }
				
				d2 = str.find_first_of(dir_marks, d1+1);
				if (d2 == String::npos)                                    { return str; }
				if (str[d2] == Char('/'))                                  { str[d2] = Char('\\'); }
				if (d2-d1 == 2 && str[d1+1] == Char('.'))                  { str.erase(d1+1, 2); d0 = 0; continue; }
				
				d3 = str.find_first_of(dir_marks, d2+1);
				if (d3 == String::npos)                                    { return str; }
				if (str[d3] == Char('/'))                                  { str[d3] = Char('\\'); }
				if (d3-d2 == 2 && str[d2+1] == Char('.'))                  { str.erase(d2+1, 2); d0 = 0; continue; }
				if (d3-d2 == 3 && str[d2+1] == Char('.') && str[d2+2] == Char('.') &&
				   (d2-d1 != 3 || str[d1+1] != Char('.') || str[d1+2] != Char('.'))) { str.erase(d1+1, d3-d1); d0 = 0; continue; }
				
				d0 = d1 + 1;
			}
		}
		template <typename String> inline String CanonicaliseC(String const& str)
		{
			String s = str;
			return Canonicalise(s);
		}
		
		// Convert a path name into a standard format of "c:\dir\dir\filename.ext" i.e. back slashes, and lower case
		template <typename String> inline String& Standardise(String& str)
		{
			typedef String::value_type Char;
			RemoveQuotes(str);
			RemoveLastBackSlash(str);
			Canonicalise(str);
			for (String::iterator i = str.begin(), iend = str.end(); i != iend; ++i) {*i = static_cast<String::value_type>(tolower(*i));}
			std::replace  (str.begin(), str.end(), Char('/'), Char('\\'));
			return str;
		}
		template <typename String> inline String StandardiseC(String const& str)
		{
			String s = str;
			return Standardise(s);
		}
		
		// Get the drive letter from a full path description.
		template <typename String> inline String GetDrive(String const& str)
		{
			typedef String::value_type Char;
			Char const colon[] = {Char(':'),0};
			
			size_t pos = str.find(colon);
			if (pos == String::npos) return String();
			return str.substr(0, pos);
		}
		
		// Get the path from a full path description
		template <typename String> inline String GetPath(String const& str)
		{
			typedef String::value_type Char;
			Char const colon[] = {Char(':'),0}, dir_marks[] = {Char('\\'),Char('/'),0};
			size_t p, first = 0, last = str.size();
			
			// Find the start of the path
			p = str.find(colon);
			if (p != String::npos) first = p + 1;
			if (first != last && (str[first] == Char('\\') || str[first] == Char('/'))) ++first;
			
			// Find the end of the path
			p = str.find_last_of(dir_marks);
			if (p == String::npos || p <= first) return String();
			last = p;
			
			return str.substr(first, last - first);
		}
		
		// Get the directory including drive letter from a full path description
		template <typename String> inline String GetDirectory(String const& str)
		{
			typedef String::value_type Char;
			Char const dir_marks[] = {Char('\\'),Char('/'),0};
			
			// Find the end of the path
			size_t p = str.find_last_of(dir_marks);
			if (p == String::npos) return String();
			return str.substr(0, p);
		}
		
		// Get the extension from a full path description
		template <typename String> inline String GetExtension(String const& str)
		{
			typedef String::value_type Char;
			Char const dot[] = {Char('.'),0};
			
			size_t p = str.find_last_of(dot);
			if (p == String::npos) return String();
			return str.substr(p+1);
		}
		
		// Get the filename including extension from a full path description
		template <typename String> inline String GetFilename(String const& str)
		{
			typedef String::value_type Char;
			Char const dir_marks[] = {Char('\\'),Char('/'),0};
			
			// Find the end of the path
			size_t p = str.find_last_of(dir_marks);
			if (p == String::npos) return str;
			return str.substr(p+1);
		}
		
		// Get the file title from a full path description
		template <typename String> inline String GetFiletitle(String const& str)
		{
			typedef String::value_type Char;
			Char const dot[] = {Char('.'),0}, dir_marks[] = {Char('\\'),Char('/'),0};
			
			size_t p, first = 0, last = str.size();
			
			// Find the start of the file title
			p = str.find_last_of(dir_marks);
			if (p != String::npos) first = p + 1;
			
			// Find the end of the file title
			p = str.find_last_of(dot);
			if (p != String::npos && p > first) last = p;
			
			return str.substr(first, last - first);
		}
		
		// Remove the drive from 'str'
		template <typename String> inline String& RmvDrive(String& str)
		{
			typedef String::value_type Char;
			Char const colon[] = {Char(':'),0}, dir_marks[] = {Char('\\'),Char('/'),0};
			
			size_t p = str.find(colon);
			if (p == String::npos) return str;
			p = str.find_first_not_of(dir_marks, p+1);
			return str.erase(0, p);
		}
		
		// Remove the path from 'str'
		template <typename String> inline String& RmvPath(String& str)
		{
			typedef String::value_type Char;
			Char const colon[] = {Char(':'),0}, dir_marks[] = {Char('\\'),Char('/'),0};
			
			size_t p, first = 0, last = str.size();
			
			// Find the start of the path
			p = str.find(colon);
			if (p != String::npos) first = p + 1;
			if (first != last && (str[first] == Char('\\') || str[first] == Char('/'))) ++first;
			
			// Find the end of the path
			p = str.find_last_of(dir_marks);
			if (p == String::npos || p <= first) return str;
			last = p + 1;
			str.erase(first, last - first);
			return str;
		}
		
		// Remove the directory from 'str'
		template <typename String> inline String& RmvDirectory(String& str)
		{
			typedef String::value_type Char;
			Char const dir_marks[] = {Char('\\'),Char('/'),0};
			
			// Find the end of the path
			size_t p = str.find_last_of(dir_marks);
			if (p == String::npos) return str;
			str.erase(0, p + 1);
			return str;
		}
		
		// Remove the extension from 'str'
		template <typename String> inline String& RmvExtension(String& str)
		{
			typedef String::value_type Char;
			Char const dot[] = {Char('.'),0};
			
			size_t p = str.find_last_of(dot);
			if (p == String::npos) return str;
			str.erase(p);
			return str;
		}
		
		// Remove the filename from 'str'
		template <typename String> inline String& RmvFilename(String& str)
		{
			typedef String::value_type Char;
			Char const dir_marks[] = {Char('\\'),Char('/'),0};
			
			// Find the end of the path
			size_t p = str.find_last_of(dir_marks);
			if (p == String::npos) return str;
			str.erase(p);
			return str;
		}
		
		// Remove the file title from 'str'
		template <typename String> inline String& RmvFiletitle(String& str)
		{
			typedef String::value_type Char;
			Char const dot[] = {Char('.'),0}, dir_marks[] = {Char('\\'),Char('/'),0};
			
			size_t p, first = 0, last = str.size();
			
			// Find the start of the file title
			p = str.find_last_of(dir_marks);
			if (p != String::npos) first = p + 1;
			
			// Find the end of the file title
			p = str.find_last_of(dot);
			if (p != String::npos && p > first) last = p;
			
			str.erase(first, last - first);
			return str;
		}
		
		// Make a pathname out of bits
		template <typename String> inline String Make(String const& directory, String const& filename)
		{
			typedef String::value_type Char;
			String pathname;
			pathname  = RemoveLastBackSlashC(directory);
			pathname += Char('/');
			pathname += filename;
			return Standardise(pathname);
		}
		template <typename String> inline String Make(String const& directory, String const& filetitle, String const& extension)
		{
			typedef String::value_type Char;
			String pathname;
			pathname  = RemoveLastBackSlashC(directory);
			pathname += Char('/');
			pathname += filetitle;
			pathname += Char('.');
			pathname += extension;
			return Standardise(pathname);
		}
		template <typename String> inline String Make(String const& drive, String const& path, String const& filetitle, String const& extension)	
		{
			typedef String::value_type Char;
			String pathname;
			pathname  = GetDrive(drive);
			pathname += Char(':');
			pathname += Char('/');
			pathname += RemoveLeadingBackSlashC(RemoveLastBackSlashC(path));
			pathname += Char('/');
			pathname += filetitle;
			pathname += Char('.');
			pathname += extension;
			return Standardise(pathname);
		}
		
		// Delete a file
		template <typename String> inline bool EraseFile(String const& filepath)
		{
			return impl::remove(filepath.c_str()) == 0;
		}
		
		// Delete an empty directory
		template <typename String> inline bool EraseDir(String const& path)
		{
			return impl::rmdir(path.c_str()) == 0;
		}
		
		// Delete a file or empty directory
		template <typename String> inline bool Erase(String const& path)
		{
			return EraseFile(path) || EraseDir(path);
		}
		
		// Rename a file
		template <typename String> inline bool RenameFile(String const& old_filepath, String const& new_filepath)
		{
			return impl::rename(old_filepath.c_str(), new_filepath.c_str()) == 0;
		}
		
		// Copy a file
		template <typename String> inline bool CpyFile(String const& src_filepath, String const& dst_filepath)
		{
			std::ifstream src (src_filepath.c_str(), std::ios::binary);
			std::ofstream dest(dst_filepath.c_str(), std::ios::binary);
			if (!src.is_open() || !dest.is_open()) return false;
			dest << src.rdbuf();
			return true;
		}
		
		// Move 'src' to 'dst' replacing 'dst' if it already exists
		template <typename String> inline bool ReplaceFile(String const& src, String const& dst)
		{
			if (!FileExists(dst)) return RenameFile(src, dst);
			return ::ReplaceFileA(dst.c_str(), src.c_str(), 0, REPLACEFILE_WRITE_THROUGH|REPLACEFILE_IGNORE_MERGE_ERRORS, 0, 0) != 0;
		}
		
		// Return the length of a file, without opening it
		template <typename String> inline __int64 FileLength(String const& filepath)
		{
			struct __stat64 info;
			if (_stat64(filepath.c_str(), &info) != 0) return 0;
			return info.st_size;
		}
		
		// Return the amount of free disk space. 'drive' = 'A', 'B', 'C', etc
		inline unsigned __int64 GetDiskFree(char drive)
		{
			_diskfree_t drive_info;
			drive = char(toupper(drive));
			if (_getdiskfree(drive - 'A' + 1, &drive_info) != 0) return 0;
			unsigned __int64 size = 1;
			size *= drive_info.bytes_per_sector;
			size *= drive_info.sectors_per_cluster;
			size *= drive_info.avail_clusters;
			return size;
		}
		
		// Return the size of a disk. 'drive' = 'A', 'B', 'C', etc
		inline unsigned __int64 GetDiskSize(char drive)
		{
			_diskfree_t drive_info;
			drive = char(toupper(drive));
			if (_getdiskfree(drive - 'A' + 1, &drive_info) != 0) return 0;
			unsigned __int64 size = 1;
			size *= drive_info.bytes_per_sector;
			size *= drive_info.sectors_per_cluster;
			size *= drive_info.total_clusters;
			return size;
		}
		
		// Return a bitwise combination of Attribs for 'str'
		template <typename String> inline unsigned int GetAttribs(String const& str)
		{
			struct __stat64 info;
			if (_stat64(str.c_str(), &info) != 0) return 0;
			
			// Interpret the stats
			unsigned int attribs = 0;
			if ((info.st_mode & _S_IFREG ) != 0 && (info.st_mode & _S_IFCHR) != 0 )	attribs |= EAttrib::Device;
			if ((info.st_mode & _S_IFREG ) != 0 && (info.st_mode & _S_IFCHR) == 0 )	attribs |= EAttrib::File;
			if ( info.st_mode & _S_IFDIR )	attribs |= EAttrib::Directory;
			if ( info.st_mode & _S_IFIFO )	attribs |= EAttrib::Pipe;
			if ( info.st_mode & _S_IREAD )	attribs |= EAttrib::ReadAccess;
			if ( info.st_mode & _S_IWRITE)	attribs |= EAttrib::WriteAccess;
			if ( info.st_mode & _S_IEXEC )	attribs |= EAttrib::ExecAccess;
			return attribs;
		}
		
		// Return the creation, last modified, and last access time of a file
		// Note: these timestamps are in UTC unix time
		template <typename String> inline FileTime GetFileTimeStats(String const& str)
		{
			FileTime file_time = {0, 0, 0};

			struct __stat64 info;
			if (_stat64(str.c_str(), &info) != 0) return file_time;
			file_time.m_created       = info.st_ctime;
			file_time.m_last_modified = info.st_mtime;
			file_time.m_last_access   = info.st_atime;
			return file_time;
		}
		
		// Return true if 'filepath' is a file that exists
		template <typename String> inline bool FileExists(String const& filepath)
		{
			return impl::access(&filepath[0], Exists) == 0;
		}
		
		// Return true if 'directory' exists
		template <typename String> inline bool DirectoryExists(String const& directory)
		{
			return impl::access(&directory[0], Exists) == 0;
		}
		
		// Recursively create 'directory'
		template <typename String> inline bool CreateDir(String const& directory)
		{
			typedef String::value_type Char;
			Char const dir_marks[] = {Char('\\'),Char('/'),0};
			
			String dir = CanonicaliseC(directory);
			for (size_t n = dir.find_first_of(dir_marks); n != String::npos; n = dir.find_first_of(dir_marks, n+1))
			{
				if (n < 1 || dir[n-1] == Char(':')) continue;
				if (impl::mkdir(dir.substr(0, n).c_str()) < 0 && errno != EEXIST)
					return false;
			}
			return impl::mkdir(dir.c_str()) == 0 || errno == EEXIST;
		}
		
		// Check the access on a file
		template <typename String> inline Access GetAccess(String const& str)
		{
			int acc = 0;
			if (impl::access(str.c_str(), Read ) == 0) acc |= Read;
			if (impl::access(str.c_str(), Write) == 0) acc |= Write;
			return static_cast<Access>(acc);
		}
		
		// Set the attributes on a file
		template <typename String> inline bool SetAccess(String const& str, Access state)
		{
			int mode = 0;
			if (state & Read ) mode |= _S_IREAD;
			if (state & Write) mode |= _S_IWRITE;
			return _chmod(str.c_str(), mode) == 0;
		}
		
		// Make a unique filename. Template should have the form: "FilenameXXXXXX". X's are replaced. Note, no extension
		template <typename String> inline String MakeUniqueFilename(String const& tmplate)
		{
			String str = tmplate;
			return _mktemp_s(&str[0], str.size() + 1) == 0 ? str : String();
		}
		
		// Return the current directory
		template <typename String> inline String CurrentDirectory()
		{
			char buf[_MAX_PATH];
			String path = _getdcwd(_getdrive(), buf, _MAX_PATH);
			return Standardise(path);
		}
		
		// Replaces the extension of 'path' with 'new_extn'
		template <typename String> inline String ChangeExtn(String const& path, String const& new_extn)
		{
			typedef String::value_type Char;
			String s = path;
			RmvExtension(s);
			s += Char('.');
			s += new_extn;
			return s;
		}
		
		// Insert 'prefix' before, and 'postfix' after the file title in 'path', without modifying the extension
		template <typename String> inline String ChangeFilename(String const& path, String const& prefix, String const& postfix)
		{
			typedef String::value_type Char;
			String s = path;
			RmvFilename(s);
			s += Char('\\');
			s += prefix;
			s += GetFiletitle(path);
			s += postfix;
			s += Char('.');
			s += GetExtension(path);
			return s;
		}
		
		// Combine two path fragments into a combined path
		template <typename String> inline String CombinePath(String const& lhs, String const& rhs)
		{
			typedef String::value_type Char;
			if (IsFullPath(rhs)) return rhs;
			return CanonicaliseC(RemoveLastBackSlashC(lhs) + Char('\\') + RemoveLeadingBackSlashC(rhs));
		}
		
		// Convert a relative path into a full path
		template <typename String> inline String GetFullPath(String const& str)
		{
			typedef String::value_type Char;
			Char buf[_MAX_PATH];
			String path(const_cast<Char const*>(impl::fullpath(buf, str.c_str(), _MAX_PATH)));
			return Standardise<String>(path);
		}
		
		// Make 'full_path' relative to 'relative_to'.  e.g.  C:/path1/path2/file relative to C:/path1/path3/ = ../path2/file
		template <typename String> inline String GetRelativePath(String const& full_path, String const& relative_to)
		{
			typedef String::value_type Char;
			Char const colon[] = {Char(':'),0}, prev_dir[] = {Char('.'),Char('.'),Char('/'),0}, dir_marks[] = {Char('\\'),Char('/'),0};
			
			// Find where the paths differ, recording the last common directory marker
			size_t i, d = String::npos;
			char const* fpath = full_path.c_str();
			char const* rpath = relative_to.c_str();
			for (i = 0; EqualPathChar(fpath[i], rpath[i]); ++i)
			{
				if (DirMark(full_path[i]))
					d = i;
			}
			
			// If the paths match for all of 'relative_to' just return the remander of 'full_path'
			if (DirMark(fpath[i]) && rpath[i] == 0)
				return full_path.substr(i + 1);
			
			// If d==npos then none of the paths matched.
			// If either path contains a drive then return 'full_path'
			if (d == String::npos && (full_path.find(colon) != String::npos || relative_to.find(colon) != String::npos))
				return full_path;
			
			// Otherwise, assuming they match before the first path
			String path = full_path.substr(d + 1);
			do
			{
				d = relative_to.find_first_of(dir_marks, d + 1);
				path.insert(0, prev_dir);
			}
			while (d != String::npos);
			return path;
		}
		
		// Return the name of the currently running executable
		template <typename String> inline String ExePath()
		{
			char temp[MAX_PATH];
			DWORD len = GetModuleFileNameA(0, temp, MAX_PATH);
			temp[len] = 0;
			return temp;
		}
	}
}
	
#ifdef PR_ASSERT_DEFINED
#	undef PR_ASSERT_DEFINED
#	undef PR_ASSERT
#endif
	
#endif
