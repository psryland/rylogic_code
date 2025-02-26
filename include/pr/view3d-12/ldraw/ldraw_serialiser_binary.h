//********************************
// Ldraw Script Binary Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12::ldraw
{
	// Types that can be serialized into the buffer
	template <typename T> concept PrimitiveType = requires(T)
	{
		std::is_integral_v<T> ||
			std::is_floating_point_v<T> ||
			std::is_enum_v<T> ||
			maths::VecOrMatType<T>;
	};
	template <typename T> concept SpanType = requires(T t)
	{
		PrimitiveType<typename T::value_type>;
		{ t.data() } -> std::same_as<typename T::value_type*>;
		{ t.size() } -> std::same_as<std::size_t>;
	};

	// The section header
	struct Section
	{
		// The hash of the keyword (4-bytes)
		EKeyword m_keyword;

		// The length of the section in bytes (including the header size)
		int m_size;
	};

	#pragma region Traits
	template <typename TOut> struct traits
	{
		static int64_t tellp(TOut& out)
		{
			return out.tellp();
		}
		static TOut& write(TOut& out, std::span<std::byte const> data, int64_t ofs = -1)
		{
			if (ofs != -1)
				out.seekp(ofs).write(char_ptr(data.data()), data.size()).seekp(0, std::ios::end);
			else
				out.write(char_ptr(data.data()), data.size());
			return out;
		}
	};
	template <> struct traits<byte_data<4>>
	{
		static int64_t tellp(byte_data<4>& out)
		{
			return out.size();
		}
		static byte_data<4>& write(byte_data<4>& out, std::span<std::byte const> data, int64_t ofs = -1)
		{
			if (ofs != -1)
				out.overwrite(ofs, data);
			else
				out.append(data);
			return out;
		}
	};
	#pragma endregion

	struct ByteWriter
	{
		// Notes:
		//  - Each Write function returns the size (in bytes) added to 'out'
		//  - To write out only part of a File, delete the parts in a temporary copy of the file.

		// Write custom data within a section
		template <typename TOut, std::invocable<TOut&> AddBodyFn>
		static int64_t Write(TOut& out, EKeyword keyword, AddBodyFn body_cb)
		{
			// Record the write pointer position
			auto ofs = traits<TOut>::tellp(out);

			// Write a dummy header
			Section header = { .m_keyword = keyword };
			traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) });

			// Write the section body
			body_cb(out);

			// Update the header with the correct size
			header.m_size = s_cast<int>(traits<TOut>::tellp(out) - ofs);
			traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) }, ofs);
			return header.m_size;
		}

		// Write an empty section
		template <typename TOut>
		static int64_t Write(TOut& out, EKeyword keyword)
		{
			return Write(out, keyword, [](auto&) {});
		}

		// Write a string section
		template <typename TOut>
		static int64_t Write(TOut& out, EKeyword keyword, std::string_view str)
		{
			return Write(out, keyword, [&](auto&)
			{
				traits<TOut>::write(out, { byte_ptr(str.data()), str.size() });
			});
		}

		// Write a single primitive type
		template <typename TOut, PrimitiveType TItem, PrimitiveType... TItems>
		static int64_t Write(TOut& out, EKeyword keyword, TItem item, TItems&&... items)
		{
			return Write(out, keyword, [&](auto&)
			{
				traits<TOut>::write(out, { byte_ptr(&item), sizeof(item) });
				(traits<TOut>::write(out, { byte_ptr(&items), sizeof(items) }), ...);
			});
		}

		// Write a span of items
		template <typename TOut, PrimitiveType TItem>
		static int64_t Write(TOut& out, EKeyword keyword, std::span<TItem const> span)
		{
			return Write(out, keyword, [&](auto&)
			{
				traits<TOut>::write(out, { byte_ptr(span.data()), span.size() * sizeof(TItem) });
			});
		}

		// Write an immediate list of items
		template <typename TOut, PrimitiveType TItem>
		static int64_t Write(TOut& out, EKeyword keyword, std::initializer_list<TItem const> items)
		{
			return Write(out, keyword, [&](auto&)
			{
				traits<TOut>::write(out, { byte_ptr(items.begin()), items.size() * sizeof(TItem) });
			});
		}
	};

	struct ByteReader : IReader
	{
		using Section = struct { int64_t beg, end; }; // byte offsets from 'Src.begin()'
		using SectionStack = pr::vector<Section>;

		std::istream& m_src;
		SectionStack m_section;
		std::filesystem::path m_src_filepath;

		ByteReader(std::istream& src, std::filesystem::path src_filepath = {});

		// Return the current location in the source
		virtual Location Loc() const override;

		// Move into a nested section
		virtual void PushSection() override;

		// Leave the current nested section
		virtual void PopSection() override;

		// Get the next keyword within the current section.
		// Returns false if at the end of the section
		virtual bool NextKeyword(ldraw::EKeyword& kw) override;

		// Search the current section, from the current position, for the given keyword.
		// Does not affect the 'current' position used by 'NextKeyword'
		virtual bool FindKeyword(ldraw::EKeyword kw) override;

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() override;

		// Read a utf8 string from the current section.
		// If 'has_length' is false, assume the whole section is the string.
		// If 'has_length' is true, assume the string is prefixed by its length.
		virtual string32 String(bool has_length = false) override;

		// Read an integral value from the current section
		virtual int64_t Int(int byte_count, int radix) override;

		// Read a floating point value from the current section
		virtual double Real(int byte_count) override;

		// Open a byte stream corresponding to 'path'
		virtual std::unique_ptr<std::istream> OpenStream(std::filesystem::path const& path) override;
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr::rdr12::ldraw
{
	PRUnitTest(LDrawBinarySerialiserTests)
	{
		byte_data<4> data;
		Write(data, EKeyword::Point, [](auto& data)
		{
			Write(data, EKeyword::Name, "TestPoints");
			Write(data, EKeyword::Colour, 0xFF00FF00);
			Write(data, EKeyword::Data, { v4(1,1,1,1), v4(2,2,2,1), v4(3,3,3,1) });
		});


	}
}
#endif
