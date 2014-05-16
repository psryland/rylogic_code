//***********************************************************************
//  Handle
//  Copyright (c) Rylogic Ltd 2009
//***********************************************************************
// A scoped windows HANDLE plus some helper file functions
//
// Options for access:  Specifies the type of access to the object. An application can obtain read access, write access, read/write
//                      access, or device query access. This parameter can be any combination of the following values.
//  0                   Specifies device query access to the object. An application can query device attributes without accessing
//                      the device.
//  GENERIC_READ        Specifies read access to the object. Data can be read from the file and the file pointer can be moved.
//                      Combine with GENERIC_WRITE for read/write access.
//  GENERIC_WRITE       Specifies write access to the object. Data can be written to the file and the file pointer can be moved.
//                      Combine with GENERIC_READ for read/write access.
// Options for sharing:
//  0                   Not shared.
//  FILE_SHARE_DELETE   Enables subsequent open operations on the object to request delete access. Otherwise, other processes cannot
//                      open the object if they request delete access. If the object has already been opened with delete access, the
//                      sharing mode must include this flag. Windows Me/98/95:  This flag is not supported.
//  FILE_SHARE_READ     Enables subsequent open operations on the object to request read access. Otherwise, other processes cannot
//                      open the object if they request read access. If the object has already been opened with read access, the
//                      sharing mode must include this flag.
//  FILE_SHARE_WRITE    Enables subsequent open operations on the object to request write access. Otherwise, other processes cannot
//                      open the object if they request write access. If the object has already been opened with write access, the
//                      sharing mode must include this flag.
// Options for create:
//  CREATE_ALWAYS       overwrites the file if it exists
//  CREATE_NEW          only creates the file if is doesn't already exist
//  OPEN_ALWAYS         opens or creates a file
//  OPEN_EXISTING       fails to open if file doesn't exist
//  TRUNCATE_EXISTING   opens and clears the file
//
// Options for flags:
//  FILE_ATTRIBUTE_ARCHIVE      Mark the file for backup or removal.
//  FILE_ATTRIBUTE_ENCRYPTED    The file or directory is encrypted. For a file, this means that all data in the file is encrypted.
//                              For a directory, this means that encryption is the default for newly created files and subdirectories.
//                              This flag has no effect if FILE_ATTRIBUTE_SYSTEM is also specified.
//  FILE_ATTRIBUTE_HIDDEN       The file is hidden. It is not to be included in an ordinary directory listing.
//  FILE_ATTRIBUTE_NORMAL       The file has no other attributes set. This attribute is valid only if used alone.
//  FILE_ATTRIBUTE_NOT_CONTENT_INDEXED The file will not be indexed by the content indexing service.
//  FILE_ATTRIBUTE_OFFLINE      The data of the file is not immediately available. This attribute indicates that the file data has been
//                              physically moved to offline storage. This attribute is used by Remote Storage, the hierarchical storage
//                              management software. Applications should not arbitrarily change this attribute.
//  FILE_ATTRIBUTE_READONLY     The file is read only. Applications can read the file but cannot write to it or delete it.
//  FILE_ATTRIBUTE_SYSTEM       The file is part of or is used exclusively by the operating system.
//  FILE_ATTRIBUTE_TEMPORARY    The file is being used for temporary storage. File systems attempt to keep all of the data in memory
//                              for quicker access rather than flushing the data back to mass storage. A temporary file should be
//                              deleted by the application as soon as it is no longer needed.
// Additional options for flags:
//  FILE_FLAG_RANDOM_ACCESS     Indicates that the file is accessed randomly. The system can use this as a hint to optimize file
//                              caching.
//  FILE_FLAG_SEQUENTIAL_SCAN   Indicates that the file is to be accessed sequentially from beginning to end. The system can use
//                              this as a hint to optimize file caching. If an application moves the file pointer for random access,
//                              optimum caching may not occur; however, correct operation is still guaranteed. Specifying this
//                              flag can increase performance for applications that read large files using sequential access.
//                              Performance gains can be even more noticeable for applications that read large files mostly
//                              sequentially, but occasionally skip over small ranges of bytes.
//  FILE_FLAG_DELETE_ON_CLOSE   Indicates that the operating system is to delete the file immediately after all of its handles
//                              have been closed, not just the handle for which you specified FILE_FLAG_DELETE_ON_CLOSE.
//                              Subsequent open requests for the file will fail, unless FILE_SHARE_DELETE is used.
// Seek origin:
//  FILE_BEGIN      The starting point is zero or the beginning of the file.
//  FILE_CURRENT    The starting point is the current value of the file pointer.
//  FILE_END        The starting point is the current end-of-file position.
//
// Common Functions:
//  Read    = ReadFile()
//  Write   = WriteFile()
//  Length  = GetFileSize()
//  Seek    = SetFilePointer()
//  Flush   = FlushFileBuffers()
#pragma once
#ifndef PR_FILE_EX_H
#define PR_FILE_EX_H

#include <windows.h>
#include <sstream>
#include <exception>

#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
	// Scoped wrapper around a windows handle
	struct Handle
	{
		HANDLE m_handle;
		Handle() :m_handle(INVALID_HANDLE_VALUE) {}
		Handle(HANDLE handle) :m_handle(handle) {}
		Handle(Handle const& rhs) :m_handle(rhs.m_handle)   { PR_ASSERT(1, rhs.m_handle == INVALID_HANDLE_VALUE, "pr::Handle should only be copied when the file isn't open"); }
		~Handle()                                           { close(); }
		Handle& operator = (HANDLE rhs)                     { close(); m_handle = rhs; return *this; }
		//use release() Handle& operator = (Handle const& rhs); { close(); m_handle = rhs.m_handle; PR_ASSERT(1, rhs.m_handle == INVALID_HANDLE_VALUE, "pr::Handle should only be copied when the file isn't open"); return *this; }
		operator HANDLE& ()                                 { return m_handle; }
		operator HANDLE const& () const                     { return m_handle; }
		void close()                                        { if (m_handle != INVALID_HANDLE_VALUE) CloseHandle(m_handle); m_handle = INVALID_HANDLE_VALUE; }
		HANDLE release()                                    { HANDLE h = m_handle; m_handle = INVALID_HANDLE_VALUE; return h; }
	};
	
	// A template version of FileOpen equal to CreateFile that handles unicode better
	template <typename tchar> inline HANDLE FileOpen(tchar const* lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile);
	template <> inline HANDLE FileOpen(char const* lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
	{
		return CreateFileA(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
	}
	template <> inline HANDLE FileOpen(wchar_t const* lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
	{
		return CreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
	}
	
	// Delete a file or directory
	template <typename tchar> inline void FileDelete(tchar const* filename);
	template <> inline void FileDelete(char const* filename)
	{
		DWORD err;
		if (DeleteFileA(filename) || (err = GetLastError()) == ERROR_FILE_NOT_FOUND) return;
		std::stringstream msg; msg << "failed to delete file '" << filename << "'. Error code: " << err;
		throw std::exception(msg.str().c_str());
	}
	template <> inline void FileDelete(wchar_t const* filename)
	{
		DWORD err;
		if (DeleteFileW(filename) || (err = GetLastError()) == ERROR_FILE_NOT_FOUND) return;
		std::stringstream msg; msg << "failed to delete file '" << filename << "'. Error code: " << err;
		throw std::exception(msg.str().c_str());
	}
	
	// Helper version of CreateFile with less options
	// Except 'CreateFile' is a macro so we can't overload it *sigh*...
	namespace EFileOpen
	{
		enum Type
		{
			Reading,
			Writing,
			Append,
		};
	}
	template <typename tchar> inline HANDLE FileOpen(tchar const* filepath, EFileOpen::Type open_for);
	template <> inline HANDLE FileOpen(char const* filepath, EFileOpen::Type open_for)
	{
		switch (open_for)
		{
		default: return INVALID_HANDLE_VALUE;
		case EFileOpen::Reading: return CreateFileA(filepath, GENERIC_READ , FILE_SHARE_READ|FILE_SHARE_WRITE, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
		case EFileOpen::Writing: return CreateFileA(filepath, GENERIC_WRITE, FILE_SHARE_READ,                  0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
		case EFileOpen::Append : return CreateFileA(filepath, GENERIC_WRITE, FILE_SHARE_READ,                  0, OPEN_ALWAYS  , FILE_ATTRIBUTE_NORMAL, 0);
		}
	}
	template <> inline HANDLE FileOpen(wchar_t const* filepath, EFileOpen::Type open_for)
	{
		switch (open_for)
		{
		default: return INVALID_HANDLE_VALUE;
		case EFileOpen::Reading: return CreateFileW(filepath, GENERIC_READ , FILE_SHARE_READ|FILE_SHARE_WRITE, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
		case EFileOpen::Writing: return CreateFileW(filepath, GENERIC_WRITE, FILE_SHARE_READ,                  0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
		case EFileOpen::Append : return CreateFileW(filepath, GENERIC_WRITE, FILE_SHARE_READ,                  0, OPEN_ALWAYS  , FILE_ATTRIBUTE_NORMAL, 0);
		}
	}
	
	// Read file helper
	inline bool FileRead(HANDLE handle, void* buffer, DWORD byte_count, DWORD* bytes_read)
	{
		return ReadFile(handle, buffer, byte_count, bytes_read, 0) != 0;
	}
	inline bool FileRead(HANDLE handle, void* buffer, DWORD byte_count)
	{
		DWORD bytes_read;
		return FileRead(handle, buffer, byte_count, &bytes_read) && bytes_read == byte_count;
	}
	template <typename POD> inline bool FileReadPod(HANDLE handle, POD& pod)
	{
		return FileRead(handle, &pod, sizeof(pod));
	}
	
	// Write file helper
	inline bool FileWrite(HANDLE handle, void const* buffer, DWORD byte_count, DWORD* bytes_written)
	{
		return WriteFile(handle, buffer, byte_count, bytes_written, 0) != 0;
	}
	inline bool FileWrite(HANDLE handle, void const* buffer, DWORD byte_count)
	{
		DWORD bytes_written;
		return FileWrite(handle, buffer, byte_count, &bytes_written) && bytes_written == byte_count;
	}
	inline bool FileWrite(HANDLE handle, char const* string)
	{
		return FileWrite(handle, string, DWORD(strlen(string)));
	}
	inline bool FileWrite(HANDLE handle, wchar_t const* string)
	{
		return FileWrite(handle, string, DWORD(wcslen(string)*sizeof(wchar_t)));
	}
	template <typename POD> inline bool FileWritePod(HANDLE handle, POD const& pod)
	{
		return FileWrite(handle, &pod, sizeof(pod));
	}
	
	// File time values
	inline FILETIME CreationTime(HANDLE handle)         { FILETIME creation_time; GetFileTime(handle, &creation_time, 0, 0); return creation_time; }
	inline FILETIME LastAccessTime(HANDLE handle)       { FILETIME access_time;   GetFileTime(handle, 0, &access_time, 0);   return access_time;   }
	inline FILETIME LastModifiedTime(HANDLE handle)     { FILETIME modified_time; GetFileTime(handle, 0, 0, &modified_time); return modified_time; }
	
	// Fill a buffer with the contents of a file
	// Note: the buffer could be a std::string
	template <typename tchar, typename tbuf> inline bool FileToBuffer(tchar const* filename, tbuf& buffer, DWORD sharing)
	{
		Handle h = FileOpen(filename, GENERIC_READ, sharing, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
		if (h == INVALID_HANDLE_VALUE) return false;
		DWORD file_size = GetFileSize(h, 0);
		std::size_t buf_size = buffer.size();
		buffer.resize(buf_size + file_size);
		return FileRead(h, &buffer[buf_size], file_size);
	}
	template <typename tchar, typename tbuf> inline bool FileToBuffer(tchar const* filename, tbuf& buffer)
	{
		return FileToBuffer(filename, buffer, FILE_SHARE_READ|FILE_SHARE_WRITE);
	}
	template <typename tchar, typename tbuf> inline tbuf FileToBuffer(tchar const* filename, DWORD sharing)
	{
		tbuf result;
		if (!FileToBuffer(filename, result, sharing)) result.clear();
		return result;
	}
	template <typename tchar, typename tbuf> inline tbuf FileToBuffer(tchar const* filename)
	{
		return FileToBuffer(filename, FILE_SHARE_READ|FILE_SHARE_WRITE);
	}

	// Write a file with 'buffer' as the contents
	// Note: 'buffer' may be a string
	template <typename tchar> inline bool BufferToFile(void const* buffer, DWORD size, tchar const* filename, bool append)
	{
		Handle h = FileOpen(filename, GENERIC_WRITE, FILE_SHARE_READ, 0, append ? OPEN_ALWAYS : CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
		return FileWrite(h, buffer, size);
	}
	template <typename tchar> inline bool BufferToFile(void const* buffer, DWORD size, tchar const* filename)
	{
		return BufferToFile(buffer, size, filename, false);
	}
	template <typename tchar, typename tbuf> inline bool BufferToFile(tbuf const& buffer, tchar const* filename, bool append)
	{
		return BufferToFile(&buffer[0], DWORD(buffer.size()), filename, append);
	}
	template <typename tchar, typename tbuf> inline bool BufferToFile(tbuf const& buffer, tchar const* filename)
	{
		return BufferToFile(buffer, filename, false);
	}
}

#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif

#endif
