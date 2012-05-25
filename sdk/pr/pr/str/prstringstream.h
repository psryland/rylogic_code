//***********************************************************************
// String stream
//  Copyright © Rylogic Ltd 2008
//***********************************************************************
	
#ifndef PR_STR_STRING_STREAM_H
#define PR_STR_STRING_STREAM_H
	
#include <string.h>
#include "pr/str/prstringcore.h"
	
namespace pr
{
	namespace str
	{
		template <typename tchar=char, size_t LocalCount=256>
		struct stream
		{
			typedef tchar	value_type;
			typedef size_t	size_type;

			value_type	m_buffer[LocalCount];
			value_type*	m_str;
			value_type*	m_ptr;
			value_type*	m_end;
			int			m_decimal_places;

			value_type const*	str() const		{ return m_str; }
			value_type*			str()			{ return m_str; }
			size_type			size() const	{ return static_cast<size_type>(m_ptr - m_str); }
			size_type			capacity() const{ return static_cast<size_type>(m_end - m_str); }

			stream()
			:m_str(m_buffer)
			,m_ptr(m_buffer)
			,m_end(m_buffer + LocalCount)
			,m_decimal_places(6)
			{
				*m_ptr = 0;
			}
			
			// Reserve memory for 'count' characters in the stream
			void reserve(size_type count)
			{
				if( count < LocalCount && m_str != m_buffer )
				{
					size_type len = size();
					Assign(m_str, m_ptr, 0, m_buffer, LocalCount);
					delete m_str;
					m_str = m_buffer;
					m_ptr = m_str + len;
					m_end = m_str + LocalCount;
				}
				else if( count > capacity() )
				{
					size_type len = size();
					value_type* str = new value_type[count];
					Assign(m_str, m_ptr, 0, str, count);
					if( m_str != m_buffer ) delete m_str;
					m_str = str;
					m_ptr = m_str + len;
					m_end = m_str + count;
				}
			}

			// Ensure the stream has enough space for at least
			// 'new_count' characters.
			void grow(size_type new_count)
			{
				++new_count; // space for the null terminator
				if( new_count < capacity() ) return;
				reserve(new_count * 3 / 2);
			}


			// Add a string to the stream
			template <typename TStr>
			stream& operator << (TStr const* str)
			{
				size_type str_len = Length(str);
				grow(size() + str_len);
				Assign(str, str + str_len, 0, m_ptr, capacity() - size());
				m_ptr += str_len;
				return *this;
			}

			// Add a floating point number to the stream
			stream& operator << (double value)
			{
				grow(size() + m_decimal_places + 2);
				_gcvt_s(m_ptr, m_decimal_places + 2, value, m_decimal_places);
				m_ptr += Length(m_ptr);
				return *this;
			}
	
			//// Add an integer to the stream
			//stream& operator << (int value)
			//{
			//	grow(size() + 6dfdfd);
			//	itoa(value, m_ptr, 0);
			//	m_ptr += Length(m_ptr);
			//	return *this;
			//}
		};
	}//namespace str
}//namespace pr

#endif//PR_STR_STRING_STREAM_H
