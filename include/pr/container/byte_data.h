//******************************************
// ByteData
//  Copyright (c) Oct 2003 Paul Ryland
//******************************************

#pragma once

#include <malloc.h>
#include <vector>

namespace pr
{
	// Handy typedef of a vector of bytes
	using ByteCont = std::vector<unsigned char>;

	// Append data to a byte container
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
	inline ByteCont& AppendData(ByteCont& data, void const* buffer, size_t buffer_size)
	{
		auto p = static_cast<unsigned char const*>(buffer);
		data.insert(data.end(), p, p + buffer_size);
		return data;
	}
	
	// A dynamically allocating container of bytes with alignment.
	// Loosely like 'std::vector<unsigned char>' except the buffer is aligned.
	// Note: 'Type' is not a class template parameter here so that the contents
	// of the container can be interpreted as different types as needed.
	// Also note: this container is intended as a byte bucket, so don't expect
	// constructors/destructors/etc to be called on types you add to this container.
	template <int Alignment = 1> struct ByteData
	{
		using value_type = unsigned char;

		void*  m_ptr;
		size_t m_size;
		size_t m_capacity;

		ByteData()
			:m_ptr(0)
			,m_size(0)
			,m_capacity(0)
		{}
		ByteData(ByteData&& rhs)
			:m_ptr(rhs.m_ptr)
			,m_size(rhs.m_size)
			,m_capacity(rhs.m_capacity)
		{
			rhs.m_ptr = nullptr;
			rhs.m_size = 0;
			rhs.m_capacity = 0;
		}
		ByteData(ByteData const& rhs)
			:m_ptr(0)
			,m_size(0)
			,m_capacity(0)
		{
			grow(rhs.m_capacity);
			memcpy(m_ptr, rhs.m_ptr, rhs.m_size);
			m_size = rhs.m_size;
		}
		~ByteData()
		{
			grow(0);
		}

		ByteData& operator = (ByteData&& rhs)
		{
			if (this != &rhs)
			{
				std::swap(m_ptr, rhs.m_ptr);
				std::swap(m_size, rhs.m_size);
				std::swap(m_capacity, rhs.m_capacity);
			}
			return *this;
		}
		ByteData& operator = (ByteData const& rhs)
		{
			if (this != &rhs)
			{
				grow(rhs.m_capacity);
				memcpy(m_ptr, rhs.m_ptr, rhs.m_size);
				m_size = rhs.m_size;
			}
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
		template <typename Type> size_t size() const
		{
			return m_size / sizeof(Type);
		}
		size_t size() const
		{
			return m_size;
		}

		// Resize the container to contain 'new_size' multiples of 'Type'
		template <typename Type> void resize(size_t new_size)
		{
			resize(new_size * sizeof(Type));
		}
		void resize(size_t new_size)
		{
			if (m_capacity < new_size)
				grow(new_size * 3 / 2);
			m_size = new_size;
		}
		void resize(size_t new_size, unsigned char fill)
		{
			auto old_size = m_size;
			resize(new_size);
			if (old_size < new_size)
				memset(begin() + old_size, fill, new_size - old_size);
		}

		// Pre-allocate space
		void reserve(size_t new_capacity)
		{
			grow(new_capacity);
		}

		// Append the contents of 'rhs' to this container
		void append(ByteData const& rhs)
		{
			push_back(rhs.data(), rhs.size());
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
		void push_back(void const* data, size_t size)
		{
			if (m_capacity < m_size + size)
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
		template <typename Type> Type const* cbegin() const
		{
			return static_cast<Type const*>(m_ptr);
		}
		unsigned char const* cbegin() const
		{
			return static_cast<unsigned char const*>(m_ptr);
		}

		// Return a const pointer to the end of the contained range
		template <typename Type> Type const* cend() const
		{
			return cbegin<Type>() + size<Type>(); // note: end() - begin() is always an integer multiple of 'Type'
		}
		unsigned char const* cend() const
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
		template <typename Type> Type const& at(size_t index) const
		{
			return begin<Type>()[index];
		}
		unsigned char const& at(size_t index) const
		{
			return begin()[index];
		}
		template <typename Type> Type& at(size_t index)
		{
			return begin<Type>()[index];
		}
		unsigned char& at(size_t index)
		{
			return begin()[index];
		}

		// Return a reference to 'Type' at a byte offset into the container
		template <typename Type> Type const& at_byte_ofs(size_t index) const
		{
			return *reinterpret_cast<Type const*>(begin()[index]);
		}
		template <typename Type> Type& at_byte_ofs(size_t index)
		{
			return *reinterpret_cast<Type*>(begin()[index]);
		}

		// Array access to bytes
		unsigned char const& operator[](size_t index) const
		{
			return at(index);
		}
		unsigned char& operator[](size_t index)
		{
			return at(index);
		}

		// A pointer to the data
		unsigned char const* data() const { return begin(); }
		unsigned char*       data()       { return begin(); }

		// Streaming access
		template <typename Type> Type const& read(size_t& ofs)
		{
			assert(ofs + sizeof(Type) <= size());
			auto& r = at_byte_ofs<Type>(ofs);
			ofs += sizeof(Type);
			return r;
		}

	private:
		void grow(size_t capacity)
		{
			void* ptr = 0;
			if (capacity > 0)
			{
				ptr = _aligned_malloc(capacity, Alignment);
			}
			m_capacity = capacity;
			if (m_size > m_capacity)
			{
				m_size = m_capacity;
			}
			if (ptr)
			{
				memcpy(ptr, m_ptr, m_size);
			}
			if (m_ptr)
			{
				_aligned_free(m_ptr);
			}
			m_ptr = ptr;
		}
	};

	// An iterator for moving over bytes interpreting as other types
	struct ByteDataCPtr
	{
		uint8_t const* m_beg;
		uint8_t const* m_end;
		
		ByteDataCPtr()
			:ByteDataCPtr(nullptr, nullptr)
		{}
		template <int N> ByteDataCPtr(uint8_t const (&data)[N])
			:ByteDataCPtr(&data[0], &data[0] + N)
		{}
		ByteDataCPtr(void const* beg, void const* end)
			:m_beg(static_cast<uint8_t const*>(beg))
			,m_end(static_cast<uint8_t const*>(end))
		{}

		// Interpret the current 'm_beg' pointer as 'Type'
		template <typename Type> Type const& as() const
		{
			return *reinterpret_cast<Type const*>(m_beg);
		}
		
		// Advance the pointer by the size of 'Type'
		template <typename Type> Type const& read()
		{
			if (m_beg >= m_end)
				throw std::out_of_range("buffer overrun");

			auto& r = as<Type>();
			m_beg += sizeof(Type);
			return r;
		}
	};
	struct ByteDataMPtr
	{
		uint8_t* m_beg;
		uint8_t* m_end;
		
		ByteDataMPtr()
			:ByteDataMPtr(nullptr, nullptr)
		{}
		template <int N> ByteDataMPtr(uint8_t (&data)[N])
			:ByteDataMPtr(&data[0], &data[0] + N)
		{}
		ByteDataMPtr(void* beg, void* end)
			:m_beg(static_cast<uint8_t*>(beg))
			,m_end(static_cast<uint8_t*>(end))
		{}

		// Interpret the current 'm_beg' pointer as 'Type'
		template <typename Type> Type& as() const
		{
			return *reinterpret_cast<Type*>(m_beg);
		}
		
		// Advance the pointer by the size of 'Type'
		template <typename Type> void write(Type const& value)
		{
			if (m_beg >= m_end)
				throw std::out_of_range("buffer overrun");

			as<Type>() = value;
			m_beg += sizeof(Type);
		}

		// Convertable to const
		operator ByteDataCPtr() const
		{
			return ByteDataCPtr(m_beg, m_end);
		}
	};
}
