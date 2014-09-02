//***********************************************************************//
//                     A class to wrap FILE*                             //
//	Original version:                                                    //
//		P. Ryland, 2003                                                  //
//                                                                       //
//***********************************************************************//
//
// 'PRFile' is intended to be a substitute for FILE*. The file is closed
// when it goes out of scope and can be used whereever FILE* is used (e.g. fgets)
//
// Example usage:
//			File myfile("test.txt", "r");
//			if( !myfile.IsOpen() ) return;
//
//			while( !feof(myfile) )
//			{
//				fgets(str, NUM, myfile);
//			}

#pragma once
#ifndef PR_FILE_H
#define PR_FILE_H

#include <stdio.h>

namespace pr
{
	// Scoped file pointer wrapper
	struct FilePtr
	{
		FILE* m_fp;
		FilePtr(FILE* fp) :m_fp(fp)		{}
		~FilePtr()						{ if (m_fp) fclose(m_fp); } 
		operator FILE*&()				{ return m_fp; }
		operator FILE* const&() const	{ return m_fp; }
	};

	//class File
	//{
	//public:
	//	enum SeekFrom { Beginning = SEEK_SET, Current = SEEK_CUR, End = SEEK_END };
	//	File() : m_fp(0) {}
	//	File(const char* filename, const char* mode) : m_fp(0)		{ Open(filename, mode); }
	//	File(FILE* fp) : m_fp(fp)									{} // Adopt a file
	//	~File()														{ if( m_fp ) fclose(m_fp); }
	//	
	//	// State methods
	//	bool IsOpen() const									{ return m_fp != 0; }
	//	bool IsEndOfFile() const							{ return feof(m_fp) != 0; }

	//	// Utility methods
	//	bool Open(const char* filename, const char* mode)	{ Close(); fopen_s(&m_fp, filename, mode); return m_fp != 0; }
	//	void Close()										{ if( m_fp ) fclose(m_fp); m_fp = 0; }
	//	std::size_t Length();
	//	std::size_t Read(void* buffer, std::size_t byte_count);
	//	std::size_t ReadLine(void* buffer, std::size_t byte_count);
	//	std::size_t Write(const void* buffer, std::size_t byte_count);
	//	void Print(const char* str);
	//	std::size_t GetFilePosition() const					{ return static_cast<std::size_t>(ftell(m_fp)); }
	//	void Seek(SeekFrom seek_origin, std::size_t offset)	{ fseek(m_fp, static_cast<long>(offset), seek_origin); }
	//	void Flush()										{ fflush(m_fp); }

	//	// FILE* interface
	//	operator FILE*()									{ return m_fp; }
	//	operator const FILE*() const						{ return m_fp; }
	//	FILE* operator ->()									{ return m_fp; }
	//	const FILE* operator ->() const						{ return m_fp; }

	//private:
	//	FILE* m_fp;
	//};

	////*****************************************************************************
	//// Implementation
	////*****
	//// Length()
	//inline std::size_t File::Length()
	//{
	//	long pos = ftell(m_fp);
	//	long length = 0;
	//	if( fseek(m_fp, 0L, SEEK_END) == 0 ) length = ftell(m_fp);
	//	fseek(m_fp, pos, SEEK_SET);
	//	return length;
	//}

	////*****
	//// Read()
	//inline std::size_t File::Read(void* buffer, std::size_t byte_count)
	//{
	//	return static_cast<std::size_t>(fread(buffer, sizeof(char), byte_count, m_fp));
	//}

	////*****
	//// Returns the number of characters read including '\n'
	//inline std::size_t File::ReadLine(void* buffer, std::size_t byte_count)
	//{
	//	long s = ftell(m_fp);
	//	if( fgets(static_cast<char*>(buffer), (int)byte_count, m_fp) == 0 ) return 0;
	//	return static_cast<std::size_t>(ftell(m_fp) - s);
	//}

	////*****
	//// Write()
	//inline std::size_t File::Write(const void* buffer, std::size_t byte_count)
	//{
	//	return static_cast<std::size_t>(fwrite(buffer, sizeof(char), byte_count, m_fp));
	//}

	////*****
	//// Print()
	//inline void File::Print(const char* str)
	//{
	//	fprintf(m_fp, "%s", str);
	//}
} // namespace pr

#endif//PR_FILE_H
