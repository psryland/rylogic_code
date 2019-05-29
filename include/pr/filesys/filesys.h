//**********************************************
// File path/File system operations
//  Copyright (c) Rylogic Ltd 2009
//**********************************************
// Pathname  = full path e.g. Drive:/path/path/file.ext
// Drive     = the drive e.g. "P". No ':'
// Path      = the directory without the drive. No leading '/', no trailing '/'. e.g. Path/path
// Directory = the drive + path. no trailing '/'. e.g P:/Path/path
// Extension = the last string following a '.'
// Filename  = file name including extension
// FileTitle = file name not including extension
// A full pathname = drive + ":/" + path + "/" + file-title + "." + extension
//
#pragma once

#pragma warning(push, 1)
#include <io.h>
#include <direct.h>
#include <sys/types.h>
#include <sys/stat.h>
#pragma warning(pop)
#include <ctime>
#include <string>
#include <algorithm>
#include <type_traits>
#include <cassert>

namespace pr
{
	namespace filesys
	{
		enum class EAttrib
		{
			None        = 0,
			Device      = 1 << 0,
			File        = 1 << 1,
			Directory   = 1 << 2,
			Pipe        = 1 << 3,
			WriteAccess = 1 << 4,
			ReadAccess  = 1 << 5,
			ExecAccess  = 1 << 6,
			_bitwise_operators_allowed,
		};
		enum class Access
		{
			Exists    = 0,
			Write     = 2,
			Read      = 4,
			ReadWrite = Write | Read
		};

		// Unix Time = seconds since midnight January 1, 1970 UTC
		// FILETIME = 100-nanosecond intervals since January 1, 1601 UTC
		// File timestamp data for a file.
		// Note: these timestamps are in UTC Unix time
		struct FileTime
		{
			time_t m_last_access;   // Note: time_t is 64bit
			time_t m_last_modified;
			time_t m_created;
		};

		// Convert a UTC Unix time to a local time zone Unix time
		inline time_t UTCtoLocal(time_t t)
		{
			struct tm utc,local;
			if (gmtime_s(&utc, &t) != 0 || localtime_s(&local, &t) != 0) throw std::exception("failed to convert UTC time to local time");
			return t + (mktime(&local) - mktime(&utc));
		}

		// Convert local time-zone Unix time to UTC Unix time
		inline time_t LocaltoUTC(time_t t)
		{
			struct tm utc,local;
			if (gmtime_s(&utc, &t) != 0 || localtime_s(&local, &t) != 0) throw std::exception("failed to convert local time to UTC time");
			return t - (mktime(&local) - mktime(&utc));
		}

		// Convert between Unix time and i64. The resulting i64 can then be converted to FILETIME, SYSTEMTIME, etc
		inline __int64 UnixTimetoI64(time_t  t) { return t * 10000000LL + 116444736000000000LL; }
		inline time_t  I64toUnixTime(__int64 t) { return (t - 116444736000000000LL) / 10000000LL; }

		// Conversions between __int64, FILETIME, and SYSTEMTIME
		// Requires <windows.h> to be included
		// Note: the '__int64's here are not the same as the timestamps in 'FileTime'
		// those values are in Unix time. Use 'UnixTimetoI64()'
		struct FILETIME;
		struct SYSTEMTIME;
		template <typename = void> inline __int64 FTtoI64(FILETIME ft)
		{
			__int64  n  = __int64(ft.dwHighDateTime) << 32 | __int64(ft.dwLowDateTime);
			return n;
		}
		template <typename = void> inline FILETIME I64toFT(__int64 n)
		{
			FILETIME ft = {DWORD(n&0xFFFFFFFFULL), DWORD((n>>32)&0xFFFFFFFFULL)};
			return ft;
		}
		template <typename = void> inline SYSTEMTIME FTtoST(FILETIME const& ft)
		{
			SYSTEMTIME st = {};
			if (!::FileTimeToSystemTime(&ft, &st)) throw std::exception("FileTimeToSystemTime failed");
			return st;
		}
		template <typename = void> inline FILETIME STtoFT(SYSTEMTIME const& st)
		{
			FILETIME ft = {};
			if (!::SystemTimeToFileTime(&st, &ft)) throw std::exception("SystemTimeToFileTime failed");
			return ft;
		}
		template <typename = void> inline __int64 STtoI64(SYSTEMTIME const& st)
		{
			return FTtoI64(STtoFT(st));
		}
		template <typename = void> inline SYSTEMTIME I64toST(__int64 n)
		{
			return FTtoST(I64toFT(n));
		}

		#pragma region Traits
		template <typename C> struct is_char :std::false_type {};
		template <>           struct is_char<char> :std::true_type {};
		template <>           struct is_char<wchar_t> :std::true_type {};
		template <typename Char> using enable_if_char = typename std::enable_if<is_char<Char>::value>::type;
		#pragma endregion

		#pragma region String helpers
		template <typename Char, typename = enable_if_char<Char>> inline Char const* c_str(Char const* s)
		{
			return s;
		}

		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Char const* c_str(Str const& s)
		{
			return s.c_str();
		}

		// Returns a pointer in 's' of the first instance of 'x' or the null terminator
		template <typename Char, typename = enable_if_char<Char>> inline Char const* find(Char const* s, Char x)
		{
			for (; *s != 0 && *s != x; ++s) {}
			return s;
		}

		// Returns a pointer into 's' of the first instance of any character in 'x'
		template <typename Char, typename = enable_if_char<Char>> inline Char const* find_first(Char const* s, Char const* x)
		{
			for (; *s != 0 && *find(x,*s) == 0; ++s) {}
			return s;
		}
		#pragma endregion

		#pragma region wchar_t/ char handling
		namespace impl
		{
			inline int strlen(char    const* s) { return int(::strlen(s)); }
			inline int strlen(wchar_t const* s) { return int(::wcslen(s)); }

			inline int access(char    const* filename, Access access_mode) { return ::_access (filename, int(access_mode)); }
			inline int access(wchar_t const* filename, Access access_mode) { return ::_waccess(filename, int(access_mode)); }

			inline int rename(char    const* oldname, char    const* newname) { return ::rename  (oldname, newname); }
			inline int rename(wchar_t const* oldname, wchar_t const* newname) { return ::_wrename(oldname, newname); }

			inline int remove(char    const* path) { return ::remove  (path); }
			inline int remove(wchar_t const* path) { return ::_wremove(path); }

			inline int mkdir(char    const* dirname) { return ::_mkdir (dirname); }
			inline int mkdir(wchar_t const* dirname) { return ::_wmkdir(dirname); }

			inline int rmdir(char    const* dirname) { return ::_rmdir (dirname); }
			inline int rmdir(wchar_t const* dirname) { return ::_wrmdir(dirname); }

			inline char*    getdcwd(int drive, char*    buf, size_t buf_size)    { return ::_getdcwd(drive, buf, int(buf_size)); }
			inline wchar_t* getdcwd(int drive, wchar_t* buf, size_t buf_size) { return ::_wgetdcwd(drive, buf, int(buf_size)); }

			inline char*    fullpath(char*    abs_path, char    const* rel_path, size_t max_length) { return ::_fullpath (abs_path, rel_path, max_length); }
			inline wchar_t* fullpath(wchar_t* abs_path, wchar_t const* rel_path, size_t max_length) { return ::_wfullpath(abs_path, rel_path, max_length); }

			inline int stat64(char    const* filepath, struct __stat64* info) { return _stat64(filepath, info); }
			inline int stat64(wchar_t const* filepath, struct __stat64* info) { return _wstat64(filepath, info); }

			inline int chmod(char    const* filepath, int mode) { return _chmod(filepath, mode); }
			inline int chmod(wchar_t const* filepath, int mode) { return _wchmod(filepath, mode); }

			inline errno_t mktemp(char*    templ, int length) { return _mktemp_s(templ, length); }
			inline errno_t mktemp(wchar_t* templ, int length) { return _wmktemp_s(templ, length); }
		}
		#pragma endregion

		// Return true if 'ch' is a directory marker
		template <typename Char, typename = enable_if_char<Char>> inline bool DirMark(Char ch)
		{
			return ch == '\\' || ch == '/';
		}

		// Return true if two characters are the same as far as a path is concerned
		template <typename Char, typename = enable_if_char<Char>> inline bool EqualPathChar(Char lhs, Char rhs)
		{
			return tolower(lhs) == tolower(rhs) || (DirMark(lhs) && DirMark(rhs));
		}

		// Return true if 'path' is an absolute path. (i.e. contains a drive or is a UNC path)
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline bool IsFullPath(Str const& path)
		{
			return path.size() >= 2 && (
				(str::IsAlpha(path[0]) && path[1] == ':') || // Rooted path
				(path[0] == '\\' && path[1] == '\\')); // UNC path
		}

		// Add quotes to the str if it doesn't already have them
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str AddQuotes(Str str)
		{
			if (str.size() > 1 && str[0] == '"' && str[str.size()-1] == '"') return str;
			str.insert(str.begin(), '"');
			str.insert(str.end  (), '"');
			return str;
		}

		// Remove quotes from 'str' if it has them
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str RemoveQuotes(Str str)
		{
			if (str.size() < 2 || str[0] != '"' || str[str.size()-1] != '"') return str;
			str = str.substr(1, str.size()-2);
			return str;
		}

		// Remove the leading back slash from 'str' if it exists
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str RemoveLeadingBackSlash(Str str)
		{
			if (!str.empty() && (str[0] == '\\' || str[0] == '/')) str.erase(0,1);
			return str;
		}

		// Remove the last back slash from 'str' if it exists
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str RemoveLastBackSlash(Str str)
		{
			if (!str.empty() && (str[str.size()-1] == '\\' || str[str.size()-1] == '/')) str.erase(str.size()-1, 1);
			return str;
		}

		// Convert 'C:\path0\.\path1\../path2\file.ext' into 'C:\path0\path2\file.ext'
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str Canonicalise(Str str)
		{
			Char const dir_marks[] = {Char('\\'),Char('/'),0};

			size_t d0 = 0, d1, d2, d3;
			for (;;)
			{
				d1 = str.find_first_of(dir_marks, d0);
				if (d1 == Str::npos)                                    { return str; }
				if (str[d1] == Char('/'))                                  { str[d1] = Char('\\'); }
				if (d1 == 1 && str[0] == Char('.'))                        { str.erase(0, 2); d0 = 0; continue; }

				d2 = str.find_first_of(dir_marks, d1+1);
				if (d2 == Str::npos)                                    { return str; }
				if (str[d2] == Char('/'))                                  { str[d2] = Char('\\'); }
				if (d2-d1 == 2 && str[d1+1] == Char('.'))                  { str.erase(d1+1, 2); d0 = 0; continue; }

				d3 = str.find_first_of(dir_marks, d2+1);
				if (d3 == Str::npos)                                    { return str; }
				if (str[d3] == Char('/'))                                  { str[d3] = Char('\\'); }
				if (d3-d2 == 2 && str[d2+1] == Char('.'))                  { str.erase(d2+1, 2); d0 = 0; continue; }
				if (d3-d2 == 3 && str[d2+1] == Char('.') && str[d2+2] == Char('.') &&
					(d2-d1 != 3 || str[d1+1] != Char('.') || str[d1+2] != Char('.'))) { str.erase(d1+1, d3-d1); d0 = 0; continue; }

				d0 = d1 + 1;
			}
		}

		// Convert a path name into a standard format of "c:\dir\dir\filename.ext" i.e. back slashes, and lower case
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str Standardise(Str str)
		{
			str = RemoveQuotes(str);
			str = RemoveLastBackSlash(str);
			str = Canonicalise(str);
			for (auto& i : str) i = static_cast<Char>(tolower(i));
			std::replace(str.begin(), str.end(), Char('/'), Char('\\'));
			return str;
		}

		// Get the drive letter from a full path description.
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str GetDrive(Str const& str)
		{
			Char const colon[] = {Char(':'),0};

			auto pos = str.find(colon);
			if (pos == Str::npos) return Str();
			return str.substr(0, pos);
		}

		// Get the path from a full path description
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str GetPath(Str const& str)
		{
			Char const colon[] = {Char(':'),0}, dir_marks[] = {Char('\\'),Char('/'),0};
			size_t p, first = 0, last = str.size();

			// Find the start of the path
			p = str.find(colon);
			if (p != Str::npos) first = p + 1;
			if (first != last && (str[first] == Char('\\') || str[first] == Char('/'))) ++first;

			// Find the end of the path
			p = str.find_last_of(dir_marks);
			if (p == Str::npos || p <= first) return Str();
			last = p;

			return str.substr(first, last - first);
		}

		// Get the directory including drive letter from a full path description
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str GetDirectory(Str const& str)
		{
			Char const dir_marks[] = {Char('\\'),Char('/'),0};

			// Find the end of the path
			auto p = str.find_last_of(dir_marks);
			if (p == Str::npos) return Str();
			return str.substr(0, p);
		}

		// Get the extension from a full path description (does not include the '.')
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str GetExtension(Str const& str)
		{
			Char const dot[] = {Char('.'),0};

			auto p = str.find_last_of(dot);
			if (p == Str::npos) return Str();
			return str.substr(p+1);
		}

		// Returns a pointer to the extension part of a filepath (does not include the '.')
		template <typename Char, typename = enable_if_char<Char>> inline Char const* GetExtensionInPlace(Char const* str)
		{
			Char const* extn = 0, *p;
			for (p = str; *p; ++p)
				if (*p == '.') extn = p + 1;

			return extn ? extn : p;
		}

		// Get the filename including extension from a full path description
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str GetFilename(Str const& str)
		{
			Char const dir_marks[] = {Char('\\'),Char('/'),0};

			// Find the end of the path
			auto p = str.find_last_of(dir_marks);
			if (p == Str::npos) return str;
			return str.substr(p+1);
		}

		// Get the file title from a full path description
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str GetFiletitle(Str const& str)
		{
			Char const dot[] = {Char('.'),0}, dir_marks[] = {Char('\\'),Char('/'),0};

			size_t p, first = 0, last = str.size();

			// Find the start of the file title
			p = str.find_last_of(dir_marks);
			if (p != Str::npos) first = p + 1;

			// Find the end of the file title
			p = str.find_last_of(dot);
			if (p != Str::npos && p > first) last = p;

			return str.substr(first, last - first);
		}

		// Remove the drive from 'str'
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str& RmvDrive(Str& str)
		{
			Char const colon[] = {Char(':'),0}, dir_marks[] = {Char('\\'),Char('/'),0};

			auto p = str.find(colon);
			if (p == Str::npos) return str;
			p = str.find_first_not_of(dir_marks, p+1);
			return str.erase(0, p);
		}

		// Remove the path from 'str'
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str& RmvPath(Str& str)
		{
			Char const colon[] = {Char(':'),0}, dir_marks[] = {Char('\\'),Char('/'),0};

			size_t p, first = 0, last = str.size();

			// Find the start of the path
			p = str.find(colon);
			if (p != Str::npos) first = p + 1;
			if (first != last && (str[first] == Char('\\') || str[first] == Char('/'))) ++first;

			// Find the end of the path
			p = str.find_last_of(dir_marks);
			if (p == Str::npos || p <= first) return str;
			last = p + 1;
			str.erase(first, last - first);
			return str;
		}

		// Remove the directory from 'str'
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str& RmvDirectory(Str& str)
		{
			Char const dir_marks[] = {Char('\\'),Char('/'),0};

			// Find the end of the path
			size_t p = str.find_last_of(dir_marks);
			if (p == Str::npos) return str;
			str.erase(0, p + 1);
			return str;
		}

		// Remove the extension from 'str'
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str& RmvExtension(Str& str)
		{
			Char const dot[] = {Char('.'),0};

			size_t p = str.find_last_of(dot);
			if (p == Str::npos) return str;
			str.erase(p);
			return str;
		}

		// Remove the filename from 'str'
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str& RmvFilename(Str& str)
		{
			Char const dir_marks[] = {Char('\\'),Char('/'),0};

			// Find the end of the path
			size_t p = str.find_last_of(dir_marks);
			if (p == Str::npos) return str;
			str.erase(p);
			return str;
		}

		// Remove the file title from 'str'
		template <typename Str, typename Char = Str::value_type, typename = enable_if_char<Char>> inline Str& RmvFiletitle(Str& str)
		{
			Char const dot[] = {Char('.'),0}, dir_marks[] = {Char('\\'),Char('/'),0};

			size_t p, first = 0, last = str.size();

			// Find the start of the file title
			p = str.find_last_of(dir_marks);
			if (p != Str::npos) first = p + 1;

			// Find the end of the file title
			p = str.find_last_of(dot);
			if (p != Str::npos && p > first) last = p;

			str.erase(first, last - first);
			return str;
		}

		// Delete a file
		template <typename Str> inline bool EraseFile(Str const& filepath)
		{
			return impl::remove(c_str(filepath)) == 0;
		}

		// Delete an empty directory
		template <typename Str> inline bool EraseDir(Str const& path)
		{
			return impl::rmdir(c_str(path)) == 0;
		}

		// Delete a file or empty directory
		template <typename Str> inline bool Erase(Str const& path)
		{
			return EraseFile(path) || EraseDir(path);
		}

		// Rename a file
		template <typename Str1, typename Str2> inline bool RenameFile(Str1 const& old_filepath, Str2 const& new_filepath)
		{
			return impl::rename(c_str(old_filepath), c_str(new_filepath)) == 0;
		}

		// Copy a file
		template <typename Str1, typename Str2> inline bool CpyFile(Str1 const& src_filepath, Str2 const& dst_filepath)
		{
			std::ifstream src (c_str(src_filepath), std::ios::binary);
			std::ofstream dest(c_str(dst_filepath), std::ios::binary);
			if (!src.is_open() || !dest.is_open()) return false;
			dest << src.rdbuf();
			return true;
		}

		// Compare the contents of two files and return true if they are the same.
		// Returns true if both files doesn't exist, or false if only one file exists.
		template <typename Str> inline bool EqualContents(Str const& lhs, Str const& rhs)
		{
			auto e0 = FileExists(lhs);
			auto e1 = FileExists(rhs);
			if (e0 != e1) return false;
			if (!e0) return true;

			std::ifstream f0(c_str(lhs), std::ios::binary);
			std::ifstream f1(c_str(rhs), std::ios::binary);

			auto s0 = f0.seekg(0, std::ifstream::end).tellg();
			auto s1 = f1.seekg(0, std::ifstream::end).tellg();
			if (s0 != s1) return false;
			f0.seekg(0, std::ifstream::beg);
			f1.seekg(0, std::ifstream::beg);

			enum { BlockSize = 4096 };
			char buf0[BlockSize];
			char buf1[BlockSize];
			for (;f0 && f1;)
			{
				auto r0 = static_cast<size_t>(f0.read(buf0, sizeof(buf0)).gcount());
				auto r1 = static_cast<size_t>(f1.read(buf1, sizeof(buf1)).gcount());
				if (r0 != r1) return false;
				if (memcmp(buf0, buf1, r1) != 0) return false;
			}
			return f0.eof() == f1.eof(); // both files reached EOF at the same time
		}

		// Move 'src' to 'dst' replacing 'dst' if it already exists
		template <typename Str> inline bool RepFile(Str const& src, Str const& dst)
		{
			if (FileExists(dst)) EraseFile(dst);
			return RenameFile(src, dst);
		}

		// Return the length of a file, without opening it
		template <typename Str> inline __int64 FileLength(Str const& filepath)
		{
			struct __stat64 info;
			if (impl::stat64(c_str(filepath), &info) != 0) return 0;
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

		// Return a bitwise combination of Attributes for 'str'
		template <typename Str> inline EAttrib GetAttribs(Str const& str)
		{
			struct __stat64 info;
			if (impl::stat64(c_str(str), &info) != 0)
				return EAttrib::None;

			// Interpret the stats
			auto attribs = EAttrib::None;
			if ((info.st_mode & _S_IFREG ) != 0 && (info.st_mode & _S_IFCHR) != 0 ) attribs |= EAttrib::Device;
			if ((info.st_mode & _S_IFREG ) != 0 && (info.st_mode & _S_IFCHR) == 0 ) attribs |= EAttrib::File;
			if ( info.st_mode & _S_IFDIR ) attribs |= EAttrib::Directory;
			if ( info.st_mode & _S_IFIFO ) attribs |= EAttrib::Pipe;
			if ( info.st_mode & _S_IREAD ) attribs |= EAttrib::ReadAccess;
			if ( info.st_mode & _S_IWRITE) attribs |= EAttrib::WriteAccess;
			if ( info.st_mode & _S_IEXEC ) attribs |= EAttrib::ExecAccess;
			return attribs;
		}

		// Return the creation, last modified, and last access time of a file
		// Note: these timestamps are in UTC Unix time
		template <typename Str> inline FileTime FileTimeStats(Str const& str)
		{
			FileTime file_time = {0, 0, 0};

			struct __stat64 info;
			if (impl::stat64(c_str(str), &info) != 0) return file_time;
			file_time.m_created       = info.st_ctime;
			file_time.m_last_modified = info.st_mtime;
			file_time.m_last_access   = info.st_atime;
			return file_time;
		}

		// Return true if 'filepath' is a file that exists
		template <typename Str> inline bool FileExists(Str const& filepath)
		{
			return impl::access(c_str(filepath), Access::Exists) == 0;
		}

		// Return true if 'directory' exists
		template <typename Str> inline bool DirectoryExists(Str const& directory)
		{
			return impl::access(c_str(directory), Access::Exists) == 0;
		}

		// Recursively create 'directory'
		template <typename Str, typename Char = Str::value_type> inline bool CreateDir(Str const& directory)
		{
			Char const dir_marks[] = {Char('\\'),Char('/'),0};

			auto dir = Canonicalise(directory);
			for (size_t n = dir.find_first_of(dir_marks); n != Str::npos; n = dir.find_first_of(dir_marks, n+1))
			{
				if (n < 1 || dir[n-1] == Char(':')) continue;
				if (impl::mkdir(dir.substr(0, n).c_str()) < 0 && errno != EEXIST)
					return false;
			}
			return impl::mkdir(c_str(dir)) == 0 || errno == EEXIST;
		}

		// Check the access on a file
		template <typename Str> inline Access GetAccess(Str const& str)
		{
			int acc = 0;
			if (impl::access(c_str(str), Read ) == 0) acc |= Read;
			if (impl::access(c_str(str), Write) == 0) acc |= Write;
			return static_cast<Access>(acc);
		}

		// Set the attributes on a file
		template <typename Str> inline bool SetAccess(Str const& str, Access state)
		{
			int mode = 0;
			if (state & Read ) mode |= _S_IREAD;
			if (state & Write) mode |= _S_IWRITE;
			return impl::chmod(c_str(str), mode) == 0;
		}

		// Make a unique filename. Template should have the form: "FilenameXXXXXX". X's are replaced. Note, no extension
		template <typename Str> inline Str MakeUniqueFilename(Str const& tmplate)
		{
			auto str = tmplate;
			return impl::mktemp(&str[0], impl::strlen(&str[0]) + 1) == 0 ? str : Str();
		}

		// Return the current directory
		template <typename Str, typename Char = Str::value_type> inline Str CurrentDirectory()
		{
			Char buf[_MAX_PATH];
			auto path = impl::getdcwd(_getdrive(), buf, _countof(buf));
			return Standardise<Str>(path);
		}

		// Replaces the extension of 'path' with 'new_extn'
		template <typename Str, typename Char = Str::value_type> inline Str ChangeExtn(Str const& path, decltype(path) new_extn)
		{
			auto s = path;
			RmvExtension(s);
			s += Char('.');
			s += new_extn;
			return s;
		}

		// Insert 'prefix' before, and 'postfix' after the file title in 'path', without modifying the extension
		template <typename Str, typename Char = Str::value_type> inline Str ChangeFilename(Str const& path, decltype(path) prefix, decltype(path) postfix)
		{
			auto s = path;
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
		template <typename Str> inline Str CombinePath(Str const& lhs, decltype(lhs) rhs)
		{
			if (IsFullPath(rhs)) return rhs;
			return Canonicalise(RemoveLastBackSlash(lhs).append(1,'\\').append(RemoveLeadingBackSlash(rhs)));
		}
		template <typename Str, typename... Parts> inline Str CombinePath(Str const& lhs, decltype(lhs) rhs, Parts&&... rest)
		{
			return CombinePath(CombinePath(lhs, rhs), rest...);
		}

		// Convert a relative path into a full path
		template <typename Str, typename Char = Str::value_type> inline Str GetFullPath(Str const& str)
		{
			Char buf[_MAX_PATH];
			Str path(const_cast<Char const*>(impl::fullpath(buf, c_str(str), _MAX_PATH)));
			return Standardise<Str>(path);
		}

		// Make 'full_path' relative to 'relative_to'.  e.g.  C:/path1/path2/file relative to C:/path1/path3/ = ../path2/file
		template <typename Str, typename Char = Str::value_type> inline Str GetRelativePath(Str const& full_path, Str const& relative_to)
		{
			Char const prev_dir[]  = {'.','.','/',0};
			Char const dir_marks[] = {'\\','/',0};

			// Find where the paths differ, recording the last common directory marker
			int i, d = -1;
			auto fpath = c_str(full_path);
			auto rpath = c_str(relative_to);
			for (i = 0; EqualPathChar(fpath[i], rpath[i]); ++i)
			{
				if (DirMark(full_path[i]))
					d = i;
			}

			// If the paths match for all of 'relative_to' just return the remainder of 'full_path'
			if (DirMark(fpath[i]) && rpath[i] == 0)
				return full_path.substr(i + 1);

			// If 'd==-1' then none of the paths matched.
			// If either path contains a drive then return 'full_path'
			if (d == -1 && (*find(fpath, Char(':')) != 0 || *find(rpath, Char(':')) != 0))
				return full_path;

			// Otherwise, the part of the path up to and including 'd' matches, so it's not part of the relative path
			Str path(fpath + d + 1);
			for (Char const *end = rpath + d + 1; *end; end += *end != 0)
			{
				auto ptr = end;
				end = find_first(ptr, dir_marks);
				path.insert(0, prev_dir);
			}
			return path;
		}

		// Attempt to resolve a partial filepath given a list of directories to search
		template <typename Str, typename Cont = std::vector<Str>> inline Str ResolvePath(Str const& partial_path, Cont const& search_paths = Cont(), Str const* current_dir = nullptr, bool check_working_dir = true, Str* searched_paths = nullptr)
		{
			Str path;

			// If the partial path is actually a full path
			if (IsFullPath(partial_path))
			{
				if (FileExists(partial_path))
					return partial_path;
			}

			// If 'current_dir' != null
			if (current_dir)
			{
				path = CombinePath(*current_dir, partial_path);
				if (FileExists(path))
					return path;

				if (searched_paths)
					searched_paths->append(GetDirectory(path)).append(1, '\n');
			}

			// Check the working directory
			if (check_working_dir)
			{
				path = GetFullPath(partial_path);
				if (FileExists(path))
					return path;

				if (searched_paths)
					searched_paths->append(GetDirectory(path)).append(1, '\n');
			}

			// Search the search paths
			for (auto& dir : search_paths)
			{
				path = CombinePath<Str>(dir, partial_path);
				if (FileExists(path))
					return path;
				
				// If the search paths contain partial paths, resolve recursively
				if (!IsFullPath(path))
				{
					auto paths = search_paths;
					paths.erase(std::remove_if(begin(paths), end(paths), [&](auto& p) { return p == dir; }), end(paths));
					path = ResolvePath<Str>(path, paths, current_dir, check_working_dir, searched_paths);
					if (FileExists(path))
						return path;
				}

				if (searched_paths)
					searched_paths->append(GetDirectory(path)).append(1, '\n');
			}

			// Return an empty string for unresolved
			return Str();
		}
	}
}

#if PR_UNITTESTS
#include <fstream>
#include "pr/common/unittests.h"
#include "pr/common/flags_enum.h"
namespace pr::filesys
{
	PRUnitTest(FilesysTests)
	{
		using namespace pr::filesys;

		{//Quotes
			std::string no_quotes = "path\\path\\file.extn";
			std::string has_quotes = "\"path\\path\\file.extn\"";
			std::string p = no_quotes;
			p = RemoveQuotes(p); PR_CHECK(no_quotes , p);
			p = AddQuotes(p);    PR_CHECK(has_quotes, p);
			p = AddQuotes(p);    PR_CHECK(has_quotes, p);
		}
		{//Slashes
			std::string has_slashes1 = "\\path\\path\\";
			std::string has_slashes2 = "/path/path/";
			std::string no_slashes1 = "path\\path";
			std::string no_slashes2 = "path/path";

			has_slashes1 = RemoveLeadingBackSlash(has_slashes1);
			has_slashes1 = RemoveLastBackSlash(has_slashes1);
			PR_CHECK(no_slashes1, has_slashes1);

			has_slashes2 = RemoveLeadingBackSlash(has_slashes2);
			has_slashes2 = RemoveLastBackSlash(has_slashes2);
			PR_CHECK(no_slashes2, has_slashes2);
		}
		{//Canonicalise
			std::string p0 = "C:\\path/.././path\\path\\path\\../../../file.ext";
			std::string P0 = "C:\\file.ext";
			p0 = Canonicalise(p0);
			PR_CHECK(P0, p0);

			std::string p1 = ".././path\\path\\path\\../../../file.ext";
			std::string P1 = "..\\file.ext";
			p1 = Canonicalise(p1);
			PR_CHECK(P1, p1);
		}
		{//Standardise
			std::string p0 = "c:\\path/.././Path\\PATH\\path\\../../../PaTH\\File.EXT";
			std::string P0 = "c:\\path\\file.ext";
			p0 = Standardise(p0);
			PR_CHECK(P0, p0);
		}
		{//GetDrive
			std::string p0 = GetDrive<std::string>("drive:/path");
			std::string P0 = "drive";
			PR_CHECK(P0, p0);
		}
		{//GetPath
			std::string p0 = GetPath<std::string>("drive:/path0/path1/file.ext");
			std::string P0 = "path0/path1";
			PR_CHECK(P0, p0);
		}
		{//GetDirectory
			std::string p0 = GetDirectory<std::string>("drive:/path0/path1/file.ext");
			std::string P0 = "drive:/path0/path1";
			PR_CHECK(P0, p0);
		}
		{//GetExtension
			std::string p0 = GetExtension<std::string>("drive:/pa.th0/path1/file.stuff.extn");
			std::string P0 = "extn";
			PR_CHECK(P0, p0);
		}
		{//GetFilename
			std::string p0 = GetFilename<std::string>("drive:/pa.th0/path1/file.stuff.extn");
			std::string P0 = "file.stuff.extn";
			PR_CHECK(P0, p0);
		}
		{//GetFiletitle
			std::string p0 = GetFiletitle<std::string>("drive:/pa.th0/path1/file.stuff.extn");
			std::string P0 = "file.stuff";
			PR_CHECK(P0, p0);
		}
		{//RmvDrive
			std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
			std::string P0 = "pa.th0/path1/file.stuff.extn";
			p0 = RmvDrive(p0);
			PR_CHECK(P0, p0);
		}
		{//RmvPath
			std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
			std::string P0 = "drive:/file.stuff.extn";
			p0 = RmvPath(p0);
			PR_CHECK(P0, p0);
		}
		{//RmvDirectory
			std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
			std::string P0 = "file.stuff.extn";
			p0 = RmvDirectory(p0);
			PR_CHECK(P0, p0);
		}
		{//RmvExtension
			std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
			std::string P0 = "drive:/pa.th0/path1/file.stuff";
			p0 = RmvExtension(p0);
			PR_CHECK(P0, p0);
		}
		{//RmvFilename
			std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
			std::string P0 = "drive:/pa.th0/path1";
			p0 = RmvFilename(p0);
			PR_CHECK(P0, p0);
		}
		{//RmvFiletitle
			std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
			std::string P0 = "drive:/pa.th0/path1/.extn";
			p0 = RmvFiletitle(p0);
			PR_CHECK(P0, p0);
		}
		{//Files
			std::string dir = CurrentDirectory<std::string>();
			PR_CHECK(DirectoryExists(dir), true);

			std::string fn = MakeUniqueFilename<std::string>("test_fileXXXXXX");
			PR_CHECK(FileExists(fn), false);

			std::string path = CombinePath(dir, fn);

			std::ofstream f(path.c_str());
			f << "Hello World";
			f.close();

			PR_CHECK(FileExists(path), true);
			std::string fn2 = MakeUniqueFilename<std::string>("test_fileXXXXXX");
			std::string path2 = GetFullPath(fn2);

			RenameFile(path, path2);
			PR_CHECK(FileExists(path2), true);
			PR_CHECK(FileExists(path), false);

			CpyFile(path2, path);
			PR_CHECK(FileExists(path2), true);
			PR_CHECK(FileExists(path), true);

			EraseFile(path2);
			PR_CHECK(FileExists(path2), false);
			PR_CHECK(FileExists(path), true);

			__int64 size = FileLength(path);
			PR_CHECK(size, 11);

			auto attr = GetAttribs(path);
			auto flags = EAttrib::File|EAttrib::WriteAccess|EAttrib::ReadAccess;
			PR_CHECK(attr == flags, true);

			attr = GetAttribs(dir);
			flags = EAttrib::Directory|EAttrib::WriteAccess|EAttrib::ReadAccess|EAttrib::ExecAccess;
			PR_CHECK(attr == flags, true);

			std::string drive = GetDrive(path);
			unsigned __int64 disk_free = GetDiskFree(drive[0]);
			unsigned __int64 disk_size = GetDiskSize(drive[0]);
			PR_CHECK(disk_size > disk_free, true);

			EraseFile(path);
			PR_CHECK(FileExists(path), false);
		}
		{//DirectoryOps
			{
				std::string p0 = "C:/path0/../";
				std::string p1 = "./path4/path5";
				std::string P  = "C:\\path4\\path5";
				std::string R  = CombinePath(p0, p1);
				PR_CHECK(P, R);
			}
			{
				std::string p0 = "C:/path0/path1/path2/path3/file.extn";
				std::string p1 = "C:/path0/path4/path5";
				std::string P  = "../../path1/path2/path3/file.extn";
				std::string R  = GetRelativePath(p0, p1);
				PR_CHECK(P, R);
			}
			{
				std::string p0 = "/path1/path2/file.extn";
				std::string p1 = "/path1/path3/path4";
				std::string P  = "../../path2/file.extn";
				std::string R  = GetRelativePath(p0, p1);
				PR_CHECK(P, R);
			}
			{
				std::string p0 = "/path1/file.extn";
				std::string p1 = "/path1";
				std::string P  = "file.extn";
				std::string R  = GetRelativePath(p0, p1);
				PR_CHECK(P, R);
			}
			{
				std::string p0 = "path1/file.extn";
				std::string p1 = "path2";
				std::string P  = "../path1/file.extn";
				std::string R  = GetRelativePath(p0, p1);
				PR_CHECK(P, R);
			}
			{
				std::string p0 = "c:/path1/file.extn";
				std::string p1 = "d:/path2";
				std::string P  = "c:/path1/file.extn";
				std::string R  = GetRelativePath(p0, p1);
				PR_CHECK(P, R);
			}
		}
	}
}
#endif
