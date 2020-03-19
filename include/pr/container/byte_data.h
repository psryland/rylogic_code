//******************************************
// byte_data
//  Copyright (c) Oct 2003 Paul Ryland
//******************************************

#pragma once

#include <cstdint>
#include <vector>
#include <initializer_list>
#include <malloc.h>
#include <intrin.h>
#include "pr/container/span.h"

namespace pr
{
	// A dynamically allocating container of bytes with alignment.
	// Loosely like 'std::vector<unsigned char>' except the buffer is aligned.
	// Note: 'Type' is not a class template parameter here so that the contents
	// of the container can be interpreted as different types as needed.
	// Also note: this container is intended as a byte bucket, so don't expect
	// constructors/destructors/etc to be called on types you add to this container.
	template <int Alignment = 4> struct byte_data
	{
		using value_type = uint8_t;

		union
		{
			void* m_ptr;
			uint8_t* m_u8;
		};
		size_t m_size;
		size_t m_capacity;

		byte_data()
			:m_ptr(0)
			, m_size(0)
			, m_capacity(0)
		{
		}
		byte_data(byte_data&& rhs)
			:m_ptr(rhs.m_ptr)
			, m_size(rhs.m_size)
			, m_capacity(rhs.m_capacity)
		{
			rhs.m_ptr = nullptr;
			rhs.m_size = 0;
			rhs.m_capacity = 0;
		}
		byte_data(byte_data const& rhs)
			:m_ptr(0)
			, m_size(0)
			, m_capacity(0)
		{
			set_capacity(rhs.m_capacity);
			memcpy(m_ptr, rhs.m_ptr, rhs.m_size);
			m_size = rhs.m_size;
		}
		~byte_data()
		{
			set_capacity(0);
		}

		byte_data& operator = (byte_data&& rhs)
		{
			if (this != &rhs)
			{
				std::swap(m_ptr, rhs.m_ptr);
				std::swap(m_size, rhs.m_size);
				std::swap(m_capacity, rhs.m_capacity);
			}
			return *this;
		}
		byte_data& operator = (byte_data const& rhs)
		{
			if (this != &rhs)
			{
				set_capacity(rhs.m_capacity);
				memcpy(m_ptr, rhs.m_ptr, rhs.m_size);
				m_size = rhs.m_size;
			}
			return *this;
		}

		// Release memory the container has allocated.
		void clear()
		{
			set_capacity(0);
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

		// The reserved space
		template <typename Type> size_t capacity() const
		{
			return m_capacity / sizeof(Type);
		}
		size_t capacity() const
		{
			return m_capacity;
		}

		// Resize the container to contain 'new_size' multiples of 'Type'
		template <typename Type> void resize(size_t new_size)
		{
			resize(new_size * sizeof(Type));
		}
		template <typename Type> void resize(size_t new_size, Type fill)
		{
			auto n = std::max<ptrdiff_t>(0, new_size - size<Type>());
			resize<Type>(new_size);
			for (auto ptr = end<Type>() - n; ptr != end<Type>(); ++ptr)
				memcpy(ptr, &fill, sizeof(Type));
		}
		void resize(size_t new_size)
		{
			ensure_capacity(new_size);
			m_size = new_size;
		}
		void resize(size_t new_size, value_type fill)
		{
			auto n = std::max<ptrdiff_t>(0, new_size - size());
			resize(new_size);
			memset(end() - n, fill, n);
		}

		// Pre-allocate space
		template <typename Type> void reserve(size_t new_capacity)
		{
			reserve(new_capacity * sizeof(Type));
		}
		void reserve(size_t new_capacity)
		{
			// Don't use ensure_capacity because the caller is specifically setting the capacity
			if (new_capacity > m_capacity)
				set_capacity(new_capacity);
		}

		// Append the contents of 'rhs' to this container
		void append(byte_data const& rhs)
		{
			push_back(rhs.data(), rhs.size());
		}
		template <typename Type> void append(Type const& rhs)
		{
			push_back<Type>(rhs);
		}
		template <typename Type> void append(Type const* data, size_t count)
		{
			push_back(data, count * sizeof(Type));
		}
		template <typename Type> void append(std::initializer_list<Type> items)
		{
			push_back(items.begin(), items.size() * sizeof(Type));
		}

		// Insert bytes into this container
		void insert(ptrdiff_t ofs, void const* data, size_t size)
		{
			// Notes:
			//  Cannot add a template overload for inserting an array of 'Type' because
			//  the method signature is ambiguous.

			// Insert from a sub range
			if (inside(data))
			{
				// The subrange may span the insertion point
				auto b = byte_ptr(data) - begin(); // The offset to the start of the sub range
				auto n = std::clamp<ptrdiff_t>(ofs - b, 0, size); // The number of bytes before the insertion point

				make_hole(ofs, size);

				// Copy bytes from before and after the insertion point
				auto ptr = byte_ptr(m_ptr);
				auto ins = ptr + ofs;
				if (n != 0)
					memcpy(ins, ptr + b, n);
				if (n != static_cast<ptrdiff_t>(size))
					memcpy(ins + n, ins + size, size - n);
			}
			else
			{
				make_hole(ofs, size);

				// Insert the bytes from 'data'
				auto ins = byte_ptr(m_ptr) + ofs;
				memcpy(ins, data, size);
			}
			m_size += size;
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
			// Copying from a sub range
			if (inside(data))
			{
				auto ofs = byte_ptr(data) - begin();

				ensure_capacity(m_size + size);
				memcpy(byte_ptr(m_ptr) + m_size, byte_ptr(m_ptr) + ofs, size);
				m_size += size;
			}
			else
			{
				ensure_capacity(m_size + size);
				memcpy(byte_ptr(m_ptr) + m_size, data, size);
				m_size += size;
			}
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
		value_type const* begin() const
		{
			return static_cast<value_type const*>(m_ptr);
		}
		value_type* begin()
		{
			return static_cast<value_type*>(m_ptr);
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
		value_type const* end() const
		{
			return begin() + size();
		}
		value_type* end()
		{
			return begin() + size();
		}

		// Return a const pointer to the start of the contained range
		template <typename Type> Type const* cbegin() const
		{
			return static_cast<Type const*>(m_ptr);
		}
		value_type const* cbegin() const
		{
			return static_cast<value_type const*>(m_ptr);
		}

		// Return a const pointer to the end of the contained range
		template <typename Type> Type const* cend() const
		{
			return cbegin<Type>() + size<Type>(); // note: end() - begin() is always an integer multiple of 'Type'
		}
		value_type const* cend() const
		{
			return cbegin() + size();
		}

		// Return the buffer interpreted as 'Type'
		template <typename Type> Type const& as() const
		{
			return *begin<Type>();
		}
		value_type const& as() const
		{
			return *begin();
		}
		template <typename Type> Type& as()
		{
			return *begin<Type>();
		}
		value_type& as()
		{
			return *begin();
		}

		// Indexed addressing in the buffer interpreted as an array of 'Type'
		template <typename Type> Type const& at(size_t index) const
		{
			return begin<Type>()[index];
		}
		value_type const& at(size_t index) const
		{
			return begin()[index];
		}
		template <typename Type> Type& at(size_t index)
		{
			return begin<Type>()[index];
		}
		value_type& at(size_t index)
		{
			return begin()[index];
		}

		// Return a reference to 'Type' at a byte offset into the container
		template <typename Type> Type const& at_byte_ofs(size_t index) const
		{
			return *reinterpret_cast<Type const*>(byte_ptr(m_ptr) + index);
		}
		template <typename Type> Type& at_byte_ofs(size_t index)
		{
			return *reinterpret_cast<Type*>(byte_ptr(m_ptr) + index);
		}

		// Array access to bytes
		value_type const& operator[](size_t index) const
		{
			return at(index);
		}
		value_type& operator[](size_t index)
		{
			return at(index);
		}

		// A pointer to the data
		template <typename Type> Type const* data() const
		{
			return begin<Type>();
		}
		template <typename Type> Type* data()
		{
			return begin<Type>();
		}
		value_type const* data() const
		{
			return begin();
		}
		value_type* data()
		{
			return begin();
		}

		// Facade access
		template <typename Type> std::span<Type const> span() const
		{
			return std::span<Type const>(begin<Type>(), size<Type>());
		}

		// Streaming access
		template <typename Type> Type const& read(size_t& ofs) const
		{
			if (ofs + sizeof(Type) > size())
				throw std::out_of_range("read attempt beyond buffer end");

			auto& r = at_byte_ofs<Type>(ofs);
			ofs += sizeof(Type);
			return r;
		}

	private:

		// Convert a void pointer to a byte pointer
		static value_type const* byte_ptr(void const* ptr)
		{
			return static_cast<value_type const*>(ptr);
		}
		static value_type* byte_ptr(void* ptr)
		{
			return static_cast<value_type*>(ptr);
		}

		// Test a pointer for being within the data range of this container
		bool inside(void const* ptr) const
		{
			// A block of memory should not partially span our internal allocation.
			// That would be undefined behaviour
			auto p = byte_ptr(ptr);
			return p >= begin() && p < end();
		}

		// Make a hole at 'ofs' of size 'size'
		void make_hole(ptrdiff_t ofs, size_t size)
		{
			ensure_capacity(m_size + size);
			auto ins = byte_ptr(m_ptr) + ofs; // The insertion point of the hole
			auto rem = m_size - ofs;          // The number of remaining bytes after 'ofs'
			memmove(ins + size, ins, rem);    // Move the bytes after the insertion point to the end
		}

		// Grow the allocation to at least 'required_size'
		void ensure_capacity(size_t required_size)
		{
			if (m_capacity >= required_size) return;
			set_capacity(required_size * 3 / 2);
		}

		// Grow/Shrink the allocation size of the container
		void set_capacity(size_t capacity)
		{
			// Round up to the alignment size.
			// Setting the capacity smaller than the size, truncates the data.
			auto new_capacity = PadTo(capacity, static_cast<size_t>(Alignment));
			auto new_size = std::min(m_size, capacity);
			if (m_capacity == new_capacity)
				return;

			// Allocate a new buffer with the desired capacity
			void* ptr = nullptr;
			if (new_capacity != 0)
			{
				ptr = _aligned_malloc(new_capacity, Alignment);
				if (ptr == nullptr)
					throw std::bad_alloc();

				// Copy the current buffer contents to the new location
				memcpy(ptr, m_ptr, new_size);
			}

			// Release the old buffer
			if (m_ptr != nullptr)
				_aligned_free(m_ptr);

			// Point to the new buffer
			m_capacity = new_capacity;
			m_size = new_size;
			m_ptr = ptr;
		}
	};

	// An iterator for moving over bytes interpreting as other types
	struct byte_data_cptr
	{
		uint8_t const* m_beg;
		uint8_t const* m_end;
		
		byte_data_cptr()
			:byte_data_cptr(nullptr, nullptr)
		{}
		template <int N> byte_data_cptr(uint8_t const (&data)[N])
			:byte_data_cptr(&data[0], &data[0] + N)
		{}
		byte_data_cptr(void const* beg, void const* end)
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
	struct byte_data_mptr
	{
		uint8_t* m_beg;
		uint8_t* m_end;
		
		byte_data_mptr()
			:byte_data_mptr(nullptr, nullptr)
		{}
		template <int N> byte_data_mptr(uint8_t (&data)[N])
			:byte_data_mptr(&data[0], &data[0] + N)
		{}
		byte_data_mptr(void* beg, void* end)
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
		operator byte_data_cptr() const
		{
			return byte_data_cptr(m_beg, m_end);
		}
	};

	// Handy typedef of a vector of bytes
	using bytes_t = std::vector<unsigned char>;

	// Append data to a byte container
	template <typename T> inline bytes_t& AppendData(bytes_t& data, const T& object)
	{
		data.insert(data.end(), reinterpret_cast<const unsigned char*>(&object), reinterpret_cast<const unsigned char*>(&object + 1));
		return data;
	}
	template <> inline bytes_t& AppendData(bytes_t& data, bytes_t const& more_data)
	{
		data.insert(data.end(), more_data.begin(), more_data.end());
		return data;
	}
	inline bytes_t& AppendData(bytes_t& data, void const* begin, void const* end)
	{
		data.insert(data.end(), static_cast<const unsigned char*>(begin), static_cast<const unsigned char*>(end));
		return data;
	}
	inline bytes_t& AppendData(bytes_t& data, void const* buffer, size_t buffer_size)
	{
		auto p = static_cast<unsigned char const*>(buffer);
		data.insert(data.end(), p, p + buffer_size);
		return data;
	}

	// Deprecated names
	template <int Alignment = 4> using ByteData = byte_data<Alignment>;
	using ByteDataCPtr = byte_data_cptr;
	using ByteDataMPtr = byte_data_mptr;
	using ByteCont = bytes_t;
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::container
{
	PRUnitTest(ByteDataTests)
	{
		{ // Constructors
			byte_data buf0;
			PR_CHECK(buf0.data(), nullptr);
			PR_CHECK(buf0.capacity(), 0U);
			PR_CHECK(buf0.size(), 0U);

			byte_data buf1;
			buf1.push_back(0);
			buf1.push_back(1);
			buf1.push_back(2);
			PR_CHECK(buf1.capacity<int>() >= 3U, true);
			PR_CHECK(buf1.size<int>(), 3U);
			PR_CHECK(buf1.at<int>(0), 0);
			PR_CHECK(buf1.at<int>(1), 1);
			PR_CHECK(buf1.at<int>(2), 2);

			// Move constructor
			byte_data buf2(std::move(buf1));
			PR_CHECK(buf1.capacity<int>(), 0U);
			PR_CHECK(buf2.capacity<int>() >= 3U, true);
			PR_CHECK(buf1.size<int>(), 0U);
			PR_CHECK(buf2.size<int>(), 3U);
			PR_CHECK(buf2.at<int>(0), 0);
			PR_CHECK(buf2.at<int>(1), 1);
			PR_CHECK(buf2.at<int>(2), 2);

			// Copy constructor
			byte_data buf3(buf2);
			PR_CHECK(buf2.capacity<int>() >= 3U, true);
			PR_CHECK(buf3.capacity<int>(), buf2.capacity<int>());
			PR_CHECK(buf3.size<int>(), buf2.size<int>());
			PR_CHECK(buf3.at<int>(0), buf2.at<int>(0));
			PR_CHECK(buf3.at<int>(1), buf2.at<int>(1));
			PR_CHECK(buf3.at<int>(2), buf2.at<int>(2));
		}
		{ // Assignment
			byte_data buf0;
			buf0.push_back<short>(0);
			buf0.push_back<short>(1);
			buf0.push_back<short>(2);
			PR_CHECK(buf0.capacity<short>() >= 3U, true);
			PR_CHECK(buf0.size<short>(), 3U);

			byte_data buf1;
			buf1 = buf0;
			PR_CHECK(buf1.capacity<short>() >= 3U, true);
			PR_CHECK(buf1.size<short>(), 3U);
			PR_CHECK(buf1.at<short>(0), 0);
			PR_CHECK(buf1.at<short>(1), 1);
			PR_CHECK(buf1.at<short>(2), 2);

			byte_data buf2;
			buf2 = std::move(buf0);
			PR_CHECK(buf0.capacity(), 0U);
			PR_CHECK(buf2.capacity<short>() >= 3U, true);
			PR_CHECK(buf0.size(), 0U);
			PR_CHECK(buf2.size<short>(), 3U);
			PR_CHECK(buf2.at<short>(0), 0);
			PR_CHECK(buf2.at<short>(1), 1);
			PR_CHECK(buf2.at<short>(2), 2);
		}
		{ // Methods
			byte_data buf0;
			buf0.push_back<char>('A');
			buf0.push_back<short>(0x5555);
			buf0.push_back<char>('B');
			buf0.push_back<int>(42);

			// size/capacity
			PR_CHECK(buf0.capacity() >= 8U, true);
			PR_CHECK(buf0.size(), 8U);

			// clear
			byte_data buf1 = buf0;
			PR_CHECK(buf1.capacity(), buf0.capacity());
			PR_CHECK(buf1.size(), buf0.size());
			buf1.clear();
			PR_CHECK(buf1.capacity(), 0U);
			PR_CHECK(buf1.size(), 0U);

			// empty
			PR_CHECK(buf1.empty(), true);

			// resize
			buf1 = buf0;
			buf1.resize<int>(1);
			PR_CHECK(buf1.capacity() >= 4U, true);
			PR_CHECK(buf1.size(), 4U);
			PR_CHECK(buf1.at<char>(0), 'A');
			PR_CHECK(buf1.at<char>(1), 0x55);
			PR_CHECK(buf1.at<char>(2), 0x55);
			PR_CHECK(buf1.at<char>(3), 'B');
			buf1.resize<char>(3);
			PR_CHECK(buf1.capacity() >= 3U, true);
			PR_CHECK(buf1.size(), 3U);
			PR_CHECK(buf1.at<char>(0), 'A');
			PR_CHECK(buf1.at<char>(1), 0x55);
			PR_CHECK(buf1.at<char>(2), 0x55);
			buf1.resize<char>(8, static_cast<char>(0xAA));
			PR_CHECK(buf1.capacity() >= 8U, true);
			PR_CHECK(buf1.size(), 8U);
			PR_CHECK(buf1.at<char>(0), 'A');
			PR_CHECK(buf1.at<char>(1), static_cast<char>(0x55));
			PR_CHECK(buf1.at<char>(2), static_cast<char>(0x55));
			PR_CHECK(buf1.at<char>(3), static_cast<char>(0xAA));
			PR_CHECK(buf1.at<char>(4), static_cast<char>(0xAA));
			PR_CHECK(buf1.at<char>(5), static_cast<char>(0xAA));
			PR_CHECK(buf1.at<char>(6), static_cast<char>(0xAA));
			PR_CHECK(buf1.at<char>(7), static_cast<char>(0xAA));

			// reserve
			buf1.reserve(16);
			PR_CHECK(buf1.capacity() >= 16U, true);
			buf1.reserve<int>(16);
			PR_CHECK(buf1.capacity<int>() >= 16U, true);
			PR_CHECK(buf1.capacity() >= 16U * sizeof(int), true);

			// append
			buf1 = buf0;
			buf1.append(buf0);
			PR_CHECK(buf1.size(), 2 * buf0.size());
			for (int i = 0, iend = int(buf1.size()); i != iend; ++i)
				PR_CHECK(buf1[i], buf0[i % buf0.size()]);
			buf1.clear();
			buf1.append({0, 1, 2, 3});
			PR_CHECK(buf1.size<int>(), 4U);
			PR_CHECK(buf1.at<int>(0), 0);
			PR_CHECK(buf1.at<int>(1), 1);
			PR_CHECK(buf1.at<int>(2), 2);
			PR_CHECK(buf1.at<int>(3), 3);
			buf1.clear();
			buf1.append(buf0.data() + 4, 4);
			PR_CHECK(buf1.size<int>(), 1U);
			PR_CHECK(buf1.at<uint8_t>(0), 42);
			PR_CHECK(buf1.at<uint8_t>(1), 0);
			PR_CHECK(buf1.at<uint8_t>(2), 0);
			PR_CHECK(buf1.at<uint8_t>(3), 0);
			
			// insert
			buf1 = buf0;
			buf1.insert(1, buf0.data(), buf0.size());
			PR_CHECK(buf1.size(), 2 * buf0.size());
			PR_CHECK(memcmp(&buf1[0], &buf0[0], 1), 0);
			PR_CHECK(memcmp(&buf1[1], &buf0[0], buf0.size()), 0);
			PR_CHECK(memcmp(&buf1[1+buf0.size()], &buf0[1], buf0.size()-1), 0);
			buf1 = buf0;
			buf1.insert(1, buf1.data(), 3); // subrange insert
			PR_CHECK(buf1.size(), buf0.size() + 3U);
			PR_CHECK(buf1[0], buf0[0]);
			PR_CHECK(buf1[1], buf0[0]);
			PR_CHECK(buf1[2], buf0[1]);
			PR_CHECK(buf1[3], buf0[2]);
			PR_CHECK(buf1[4], buf0[1]);
			PR_CHECK(buf1[5], buf0[2]);

			// push_back
			buf1 = buf0;
			buf1.push_back<int>(123);
			PR_CHECK(buf1.at<int>(0), 0x42555541);
			PR_CHECK(buf1.at<int>(1), 42);
			PR_CHECK(buf1.at<int>(2), 123);
			buf1.push_back(buf1.data(), 4);
			PR_CHECK(buf1.at<int>(3), 0x42555541);
		}
		{ // Access
			byte_data buf0;
			buf0.append({0, 1, 2, 3});
			PR_CHECK(buf0.size<int>(), 4U);
			
			// as
			auto& arr0 = buf0.as<int[4]>();
			PR_CHECK(arr0[0], 0);
			PR_CHECK(arr0[1], 1);
			PR_CHECK(arr0[2], 2);
			PR_CHECK(arr0[3], 3);

			// at_byte_ofs
			auto v1 = buf0.at_byte_ofs<int>(2);
			PR_CHECK(v1, 0x00010000);

			auto s = buf0.span<int>();
			PR_CHECK(s.size(), 4U);
			PR_CHECK(s[0], 0);
			PR_CHECK(s[1], 1);
			PR_CHECK(s[2], 2);
			PR_CHECK(s[3], 3);
		}
		{ // Stream
			byte_data buf0;
			buf0.push_back<char>('A');
			buf0.push_back<short>(0x5555);
			buf0.push_back<char>('B');
			buf0.push_back<int>(42);

			size_t ofs = 0;
			PR_CHECK(buf0.read<char>(ofs), 'A');
			PR_CHECK(buf0.read<short>(ofs), 0x5555);
			PR_CHECK(buf0.read<char>(ofs), 'B');
			PR_CHECK(buf0.read<int>(ofs), 42);
		}
	}
}
#endif
