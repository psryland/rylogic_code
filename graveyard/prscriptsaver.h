//*************************************************************
//
// ScriptLoader
//
//*************************************************************

#ifndef PRSCRIPTSAVER_H
#define PRSCRIPTSAVER_H

// Don't include this directly. Include PRScript.h
#include "pr/common/PRScript.h"

namespace pr
{
	namespace impl
	{
		template <typename T>
		class ScriptSaver
		{
		public:
			ScriptSaver()									{ Reset(); }
			void	Reset()									{ m_indent = 0; m_source.clear(); }
			void	Reserve(uint size_in_bytes)				{ m_source.reserve(size_in_bytes); }
			bool	Save(const char* filename);
			void	WriteComment(const char* comment)		{ Write(Fmt("// %s", comment).c_str()); Newline(); }
			void	WriteKeyword(const char* keyword)		{ Write(Fmt("*%s", keyword).c_str()); Space(); }
			void	WriteSectionStart()						{ Newline(); m_source.push_back('{'); ++m_indent; Newline(); }
			void	WriteSectionEnd()						{ --m_indent; Newline(); m_source.push_back('}'); Newline(); }
			void	WriteLong(long long_)					{ Write(Fmt("%d", long_).c_str()); Space(); }
			void	WriteULong(unsigned long ulong_, int radix);
			void	WriteInt(int int_)						{ WriteLong(int_); }
			void	WriteUInt(unsigned int uint_, int radix){ WriteULong(uint_, radix); }
			void	WriteDouble(double double_)				{ Write(Fmt("%f", double_).c_str()); Space(); }
			void	WriteFloat(float float_)				{ WriteDouble(float_); }
			void	WriteBool(bool b)						{ b ? Write("1") : Write("0"); Space(); }
			void	WriteVector3(const v4& vec)				{ Write(Fmt("%f %f %f", vec.x, vec.y, vec.z).c_str()); Space(); }
			void	WriteVector4(const v4& vec)				{ Write(Fmt("%f %f %f %f", vec.x, vec.y, vec.z, vec.w).c_str()); Space(); }
			void	WriteQuaternion(const Quat& quat)		{ Write(Fmt("%f %f %f %f", quat.x, quat.y, quat.z, quat.w).c_str()); Space(); }
			void	WriteM4x4(const m4x4& mat)				{ for( uint i = 0; i < 4; ++i ) { WriteVector4(mat[i]);            } Space(); }
			void	WriteM4x4Sqr(const m4x4& mat)			{ for( uint i = 0; i < 4; ++i ) { WriteVector4(mat[i]); NewLine(); } Space(); }
			void	WriteString(const char* str)			{ m_source.push_back('"'); Write(str); m_source.push_back('"'); Space(); }
			void	WriteCString(const char* str);
			void	WriteBinary(const void* data, uint size, uint bytes_per_row = 16);
			void	Space(int howmuch = 1)					{ for(int i = 0; i < howmuch; ++i) m_source.push_back(' '); }
			void	Newline()								{ m_source.push_back('\r'); m_source.push_back('\n'); PR_ASSERT(PR_DBG_COMMON, m_indent >= 0); for( int i = 0; i < m_indent; ++i ) m_source.push_back('\t'); }

		private:
			void	Write(const char* str)					{ while( *str ) m_source.push_back(*str++); }

		private:
			int			m_indent;		// How many tabs to indent
			std::string	m_source;
			std::string	m_keyword;
		};

		// Save the file
		template <typename T>
		bool ScriptSaver<T>::Save(const char* filename)
		{
			Handle file = FileOpen(filename, EFileOpen_Writing);
			if (file == INVALID_HANDLE_VALUE) return false;
			return FileWrite(file, m_source.c_str(), DWORD(m_source.length()));
		}

		// Save a string
		template <typename T>
		void ScriptSaver<T>::WriteCString(const char* str)
		{
			m_source.push_back('"');
			while( *str != '\0' )
			{
				switch( *str )
				{
				case '\a':
				case '\b':
				case '\f':
				case '\n':
				case '\r':
				case '\t':
				case '\v':
				case '\\':
				case '\?':
				case '\'':
				case '\"':
					m_source.push_back('\\');
					break;
				};
				m_source.push_back(*str);
				++str;
			}
			m_source.push_back('"');
		}

		// Write a ulong into the data
		template <typename T>
		void ScriptSaver<T>::WriteULong(unsigned long ulong_, int radix)
		{
			if( radix == 10 )								Write(Fmt("%u", ulong_).c_str());
			else { PR_ASSERT(PR_DBG_COMMON, radix == 16);	Write(Fmt("%8.8X", ulong_).c_str()); }
			Space();
		}

		// Write a block of binary data as
		template <typename T>
		void ScriptSaver<T>::WriteBinary(const void* data, uint size, uint bytes_per_row)
		{
			const unsigned char* ptr = static_cast<const unsigned char*>(data);
			std::string row;
			for( uint b = 0; b < size; ++b )
			{
				row += Fmt("%2.2X ", *ptr);
				++ptr;
				if( (b % bytes_per_row) == bytes_per_row - 1 )
				{
					Write(row.c_str());
					Newline();
					row = "";
				}
			}

			if( !row.empty() ) Write(row.c_str());
		}
	}//namespace impl
}//namespace pr

#endif//PRSCRIPTSAVER_H