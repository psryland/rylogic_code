//******************************************
// ByteData
//  Copyright (c) Oct 2003 Paul Ryland
//******************************************

#ifndef PR_BYTE_DATA_H
#define PR_BYTE_DATA_H

#include <malloc.h>
#include <vector>

namespace pr
{
	// Handy typedef of a vector of bytes
	typedef std::vector<unsigned char> ByteCont;
	template <typename T> inline ByteCont& AppendData(ByteCont& data, const T& object)
	{
		data.insert(data.end(), reinterpret_cast<const unsigned char*>(&object), reinterpret_cast<const unsigned char*>(&object + 1));
		return data;
	}
	template <> inline ByteCont& AppendData(ByteCont& data, ByteCont const& more_data)
	{
		data.insert(data.end(), more_data.begin(), more_data.end());
		return data;
	}
	inline ByteCont& AppendData(ByteCont& data, void const* begin, void const* end)
	{
		data.insert(data.end(), static_cast<const unsigned char*>(begin), static_cast<const unsigned char*>(end));
		return data;
	}
	inline ByteCont& AppendData(ByteCont& data, void const* buffer, std::size_t buffer_size)
	{
		data.insert(data.end(), static_cast<const unsigned char*>(buffer), static_cast<const unsigned char*>(buffer) + buffer_size);
		return data;
	}
	
	// A dynamically allocating container of bytes with alignment.
	// Loosely like 'std::vector<unsigned char>' except the buffer is aligned.
	// Note: 'Type' is not a class template parameter here so that the contents
	// of the container can be interpreted as different types as needed.
	// Also note: this container is intended as a byte bucket, so don't expect
	// constructors/destructors/etc to be called on types you add to this container.
	template <std::size_t Alignment> struct ByteData
	{
		void*       m_ptr;
		std::size_t m_size;
		std::size_t m_capacity;

		ByteData()
		:m_ptr(0)
		,m_size(0)
		,m_capacity(0)
		{}

		ByteData(ByteData const& copy)
		:m_ptr(0)
		,m_size(0)
		,m_capacity(0)
		{
			grow(copy.m_capacity);
			memcpy(m_ptr, copy.m_ptr, copy.m_size);
			m_size = copy.m_size;
		}

		~ByteData()
		{
			grow(0);
		}

		ByteData operator = (ByteData const& copy)
		{
			grow(copy.m_capacity);
			memcpy(m_ptr, copy.m_ptr, copy.m_size);
			m_size = copy.m_size;
			return *this;
		}

		// Release memory the container has allocated.
		void clear()
		{
			grow(0);
		}

		// Return true if the container is empty
		bool empty() const
		{
			return m_size == 0;
		}
		
		// Return the size of the data in the container in multiples of 'Type'
		template <typename Type> std::size_t size() const
		{
			return m_size / sizeof(Type);
		}
		std::size_t size() const
		{
			return m_size;
		}

		// Resize the container to contain 'new_size' multiples of 'Type'
		template <typename Type> void resize(std::size_t new_size)
		{
			resize(new_size * sizeof(Type));
		}
		void resize(std::size_t new_size)
		{
			if( m_capacity < new_size )
				grow(new_size * 3 / 2);
			m_size = new_size;
		}

		// Add 'type' to the end of the container
		template <typename Type> void push_back()
		{
			push_back(Type());
		}
		template <typename Type> void push_back(Type const& type)
		{
			push_back(&type, sizeof(type));
		}
		void push_back(void const* data, std::size_t size)
		{
			if( m_capacity < m_size + size )
				grow(((m_size + size) * 3) / 2);
			
			memcpy(static_cast<unsigned char*>(m_ptr) + m_size, data, size);
			m_size += size;
		}

		// Return a pointer to the start of the contained range
		template <typename Type> Type const* begin() const
		{
			return static_cast<Type const*>(m_ptr);
		}
		template <typename Type> Type* begin()
		{
			return static_cast<Type*>(m_ptr);
		}
		unsigned char const* begin() const
		{
			return static_cast<unsigned char const*>(m_ptr);
		}
		unsigned char* begin()
		{
			return static_cast<unsigned char*>(m_ptr);
		}

		// Return a pointer to the end of the contained range
		template <typename Type> Type const* end() const
		{
			return begin<Type>() + size<Type>(); // note: end() - begin() is always an integer multiple of 'sizeof(Type)'
		}
		template <typename Type> Type* end()
		{
			return begin<Type>() + size<Type>(); // note: end() - begin() is always an integer multiple of 'sizeof(Type)'
		}
		unsigned char const* end() const
		{
			return begin() + size();
		}
		unsigned char* end()
		{
			return begin() + size();
		}

		// Return a const pointer to the start of the contained range
		template <typename Type> Type const* cbegin()
		{
			return static_cast<Type const*>(m_ptr);
		}
		unsigned char const* cbegin()
		{
			return static_cast<unsigned char const*>(m_ptr);
		}

		// Return a const pointer to the end of the contained range
		template <typename Type> Type const* cend()
		{
			return cbegin<Type>() + size<Type>(); // note: end() - begin() is always an integer multiple of 'Type'
		}
		unsigned char const* cend()
		{
			return cbegin() + size();
		}

		// Return the buffer interpreted as 'Type'
		template <typename Type> Type const& as() const
		{
			return *begin<Type>();
		}
		unsigned char const& as() const
		{
			return *begin();
		}
		template <typename Type> Type& as()
		{
			return *begin<Type>();
		}
		unsigned char& as()
		{
			return *begin();
		}

		// Indexed addressing in the buffer interpreted as an array of 'Type'
		template <typename Type> Type const& at(std::size_t index) const
		{
			return begin<Type>()[index];
		}
		unsigned char const& at(std::size_t index) const
		{
			return begin()[index];
		}
		template <typename Type> Type& at(std::size_t index)
		{
			return begin<Type>()[index];
		}
		unsigned char& at(std::size_t index)
		{
			return begin()[index];
		}

		// Array access to bytes
		unsigned char const& operator[](std::size_t index) const
		{
			return at(index);
		}
		unsigned char& operator[](std::size_t index)
		{
			return at(index);
		}

	private:
		void grow(std::size_t capacity)
		{
			void* ptr = 0;
			if( capacity > 0 )
			{
				ptr = _aligned_malloc(capacity, Alignment);
			}
			m_capacity = capacity;
			if( m_size > m_capacity )
			{
				m_size = m_capacity;
			}
			if( ptr )
			{
				memcpy(ptr, m_ptr, m_size);
			}
			if( m_ptr )
			{
				_aligned_free(m_ptr);
			}
			m_ptr = ptr;
		}
	};
}

#endif
