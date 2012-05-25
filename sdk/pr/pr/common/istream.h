//*********************************************************************
//
//	Stream interfaces
//
//*********************************************************************
// Each interface represents a fixed location in the data it represents.
// All access to the data is done with a byte offset from this fixed address.
// Note: these objects do not contain data, they merely stream data in/out of
// different kinds of data representations
#ifndef PR_COMMON_STREAM_H
#define PR_COMMON_STREAM_H

#include <cstring>
#include "pr/common/byte_data.h"
#include "pr/common/assert.h"
#include "pr/filesys/fileex.h"

namespace pr
{
	// A source of data
	struct ISrc
	{
		virtual ~ISrc() {}

		// Returns the number of bytes read from the source data. If offset is
		// out of range of the source data then 0 should be returned. This will
		// legitimately happen and could be used to detect the end of the data src
		virtual std::size_t Read(void* dest, std::size_t size, std::size_t offset) const = 0;

		// Return const pointer access to the data starting at 'offset'. If
		// the src data cannot support this method then 0 should be returned. 
		virtual const void* GetData(std::size_t offset) const = 0;
		virtual std::size_t GetDataSize() const = 0;
	};

	// A sink for data
	struct IDest
	{
		virtual ~IDest() {}
		
		// Returns the number of bytes written to the destination
		virtual std::size_t Write(const void* src, std::size_t size, std::size_t offset) = 0;
	};

	// Helpers *******************************************************************

	// Reading/Writing to contiguous memory
	struct RawIO : ISrc, IDest
	{
		uint8*			m_ptr;
		std::size_t		m_max_size;

		RawIO(void* ptr, std::size_t max_size_in_bytes) : m_ptr(static_cast<uint8*>(ptr)), m_max_size(max_size_in_bytes) {}
		const void* GetData(std::size_t offset) const	{ return m_ptr + offset; }
		std::size_t GetDataSize() const					{ return m_max_size; }
		std::size_t Read(void* dest, std::size_t size, std::size_t offset) const
		{
			std::size_t size_max = m_max_size - (offset + size);
			if( size > size_max ) size = size_max;
			memcpy(dest, m_ptr + offset, size);
			return size;
		}
		std::size_t Write(const void* src, std::size_t size, std::size_t offset)
		{
			std::size_t size_max = m_max_size - (offset + size);
			if( size > size_max ) size = size_max;
			memcpy(m_ptr + offset, src, size);
			return size;
		}
	};

	// Reading from contiguous memory
	struct RawI : ISrc
	{
		const uint8*	m_ptr;
		std::size_t		m_max_size;

		RawI(const void* ptr, std::size_t max_size_in_bytes) : m_ptr(static_cast<const uint8*>(ptr)), m_max_size(max_size_in_bytes) {}
		const void* GetData(std::size_t offset) const	{ return m_ptr + offset; }
		std::size_t GetDataSize() const					{ return m_max_size; }
		std::size_t Read(void* dest, std::size_t size, std::size_t offset) const
		{
			std::size_t size_max = m_max_size - (offset + size);
			if( size > size_max ) size = size_max;
			memcpy(dest, m_ptr + offset, size);
			return size;
		}
	};

	// Writing to contiguous memory
	struct RawO : IDest
	{
		uint8*			m_ptr;
		std::size_t		m_max_size;

		RawO(void* ptr, std::size_t max_size_in_bytes) : m_ptr(static_cast<uint8*>(ptr)), m_max_size(max_size_in_bytes) {}
		std::size_t Write(const void* src, std::size_t size, std::size_t offset)
		{
			std::size_t size_max = m_max_size - (offset + size);
			if( size > size_max ) size = size_max;
			memcpy(m_ptr + offset, src, size);
			return size;
		}
	};

	// Reading/Writing to expanding memory
	struct BufferedIO : ISrc, IDest
	{
		ByteCont* m_buffer;

		BufferedIO(ByteCont* buffer) : m_buffer(buffer) {}
		const void* GetData(std::size_t offset) const	{ return &(*m_buffer)[offset]; }
		std::size_t GetDataSize() const					{ return m_buffer->size(); }
		std::size_t Read(void* dest, std::size_t size, std::size_t offset) const
		{
			std::size_t size_max = m_buffer->size() - (offset + size);
			if( size > size_max ) size = size_max;
			memcpy(dest, &(*m_buffer)[offset], size);
			return size;
		}
		std::size_t Write(const void* src, std::size_t size, std::size_t offset)
		{
			std::size_t buffer_size = m_buffer->size();
			if( offset + size > buffer_size ) m_buffer->resize(buffer_size + offset + size);
			memcpy(&(*m_buffer)[offset], src, size);
			return size;
		}
	};

	// Reading from expanding memory
	struct BufferedI : ISrc
	{
		const ByteCont* m_buffer;

		BufferedI(const ByteCont* buffer) : m_buffer(buffer) {}
		const void* GetData(std::size_t offset) const	{ return &(*m_buffer)[offset]; }
		std::size_t GetDataSize() const					{ return m_buffer->size(); }
		std::size_t Read(void* dest, std::size_t size, std::size_t offset) const
		{
			std::size_t size_max = m_buffer->size() - (offset + size);
			if( size > size_max ) size = size_max;
			memcpy(dest, &(*m_buffer)[offset], size);
			return size;
		}
	};

	// Writing to expanding memory
	struct BufferedO : IDest
	{
		ByteCont* m_buffer;

		BufferedO(ByteCont* buffer) : m_buffer(buffer) {}
		std::size_t Write(const void* src, std::size_t size, std::size_t offset)
		{
			std::size_t buffer_size = m_buffer->size();
			if( offset + size > buffer_size ) m_buffer->resize(buffer_size + offset + size);
			memcpy(&(*m_buffer)[offset], src, size);
			return size;
		}
	};

	// Reading/Writing to file
	struct FileIO : ISrc, IDest
	{
		HANDLE m_file;
		mutable std::size_t m_last_offset;	// Save unnecessary seeks

		FileIO(HANDLE file) : m_file(file), m_last_offset(0) {}
		const void* GetData(std::size_t) const			{ PR_ASSERT(PR_DBG, false, "'GetData()' not supported for file streams"); return 0; }
		std::size_t GetDataSize() const					{ return GetFileSize(m_file, 0); }
		std::size_t Read(void* dest, std::size_t size, std::size_t offset) const
		{
			PR_ASSERT(1, false, "");// This code is broken
			if( m_last_offset != offset ) { m_last_offset = offset; SetFilePointer(m_file, long(m_last_offset), 0, FILE_BEGIN); }
			DWORD read;
			FileRead(m_file, dest, DWORD(size), &read);
			m_last_offset += read;
			return m_last_offset - offset;
		}
		std::size_t Write(const void* src, std::size_t size, std::size_t offset)
		{
			if( m_last_offset != offset ) { m_last_offset = offset; SetFilePointer(m_file, long(m_last_offset), 0, FILE_BEGIN); }
			m_last_offset += FileWrite(m_file, src, DWORD(size));
			return m_last_offset - offset;
		}
	};

	// Reading from file
	struct FileI : ISrc
	{
		HANDLE m_file;
		mutable std::size_t	m_last_offset;	// Save unnecessary seeks

		FileI(HANDLE file) : m_file(file), m_last_offset(0) {}
		const void* GetData(std::size_t) const			{ PR_ASSERT(PR_DBG, false, "'GetData()' not supported for file streams"); return 0; }
		std::size_t GetDataSize() const					{ return GetFileSize(m_file, 0); }
		std::size_t Read(void* dest, std::size_t size, std::size_t offset) const
		{
			PR_ASSERT(1, false, "");// This code is broken
			if( m_last_offset != offset ) { m_last_offset = offset; SetFilePointer(m_file, long(m_last_offset), 0, FILE_BEGIN); }
			DWORD read;
			FileRead(m_file, dest, DWORD(size), &read);
			m_last_offset += read;
			return m_last_offset - offset;
		}
	};

	// Writing to file
	struct FileO : IDest
	{
		HANDLE m_file;
		mutable std::size_t	m_last_offset;	// Save unnecessary seeks

		FileO(HANDLE file) : m_file(file), m_last_offset(0) {}
		std::size_t Write(const void* src, std::size_t size, std::size_t offset)
		{
			PR_ASSERT(1, false, "");// This code is broken
			if( m_last_offset != offset ) { m_last_offset = offset; SetFilePointer(m_file, long(m_last_offset), 0, FILE_BEGIN); }
			DWORD writ;
			FileWrite(m_file, src, DWORD(size), &writ);
			m_last_offset += writ;
			return m_last_offset - offset;
		}
	};

}//namespace pr

#endif//PR_COMMON_STREAM_H
