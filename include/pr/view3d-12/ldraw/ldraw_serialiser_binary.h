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
	template <typename T> concept PrimitiveType =
		std::is_integral_v<T> ||
		std::is_floating_point_v<T> ||
		std::is_enum_v<T> ||
		std::is_same_v<T, bool> ||
		maths::VecOrMatType<T>;
	template <typename T> concept SpanType = requires(T t)
	{
		PrimitiveType<typename T::value_type>;
		{ t.data() } -> std::same_as<typename T::value_type*>;
		{ t.size() } -> std::same_as<std::size_t>;
	};

	// The section header
	struct SectionHeader
	{
		// The hash of the keyword (4-bytes)
		EKeyword m_keyword;

		// The length of the section in bytes (excluding the header size)
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

	#pragma region Static Tests
	namespace static_tests
	{
		static_assert(std::is_integral_v<decltype("HELLO")> == false);
		static_assert(std::is_floating_point_v<decltype("HELLO")> == false);
		static_assert(std::is_enum_v<decltype("HELLO")> == false);
		static_assert(maths::VecOrMatType<decltype("HELLO")> == false);
		static_assert(PrimitiveType<decltype("HELLO")> == false);
	}
	#pragma endregion

	struct BinaryWriter
	{
		// Write custom data within a section
		template <typename TOut, std::invocable<TOut&> AddBodyFn>
		static void Write(TOut& out, EKeyword keyword, AddBodyFn body_cb)
		{
			// Record the write pointer position
			auto ofs = traits<TOut>::tellp(out);

			// Write a dummy header
			SectionHeader header = { .m_keyword = keyword };
			traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) });

			// Write the section body
			body_cb(out);

			// Update the header with the correct size
			header.m_size = s_cast<int>(traits<TOut>::tellp(out) - ofs - sizeof(header));
			traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) }, ofs);
		}

		// Write an empty section
		template <typename TOut>
		static void Write(TOut& out, EKeyword keyword)
		{
			return Write(out, keyword, [](auto&) {});
		}

		// Write a string section
		template <typename TOut>
		static void Write(TOut& out, EKeyword keyword, std::string_view str)
		{
			return Write(out, keyword, [&](auto&)
			{
				traits<TOut>::write(out, { byte_ptr(str.data()), str.size() });
			});
		}

		// Write a single primitive type
		template <typename TOut, PrimitiveType TItem, PrimitiveType... TItems>
		static void Write(TOut& out, EKeyword keyword, TItem item, TItems&&... items)
		{
			static auto DoWrite = [](TOut& out, TItem item)
			{
				if constexpr (std::is_same_v<TItem, bool>)
				{
					uint8_t b = item ? 1 : 0;
					traits<TOut>::write(out, { byte_ptr(&b), 1 });
				}
				else
				{
					traits<TOut>::write(out, { byte_ptr(&item), sizeof(item) });
				}
			};
			return Write(out, keyword, [&](auto&)
			{
				DoWrite(out, item);
				(DoWrite(out, items), ...);
			});
		}

		// Write a span of items
		template <typename TOut, PrimitiveType TItem>
		static void Write(TOut& out, EKeyword keyword, std::span<TItem const> span)
		{
			return Write(out, keyword, [&](auto&)
			{
				traits<TOut>::write(out, { byte_ptr(span.data()), span.size() * sizeof(TItem) });
			});
		}

		// Write an immediate list of items
		template <typename TOut, PrimitiveType TItem>
		static void Write(TOut& out, EKeyword keyword, std::initializer_list<TItem const> items)
		{
			return Write(out, keyword, [&](auto&)
			{
				traits<TOut>::write(out, { byte_ptr(items.begin()), items.size() * sizeof(TItem) });
			});
		}
	};

	struct BinaryReader : IReader
	{
		// Byte offsets from the start of the stream for the range of the data in the current section (excludes the header)
		using SectionSpan = struct { int64_t m_beg, m_end; };
		using SectionStack = pr::vector<SectionSpan>;
		using istream_t = std::basic_istream<char>; // 'char' to support ifstream

		istream_t& m_src;         // The input byte stream
		int64_t m_pos;               // The number of bytes read from the stream so far, or the index of the next byte to read (same thing)
		SectionStack m_section;      // A stack of section headers. back() == top == current section.
		mutable Location m_location; // Source location description

		BinaryReader(istream_t& src, std::filesystem::path src_filepath, ReportErrorCB report_error_cb = nullptr, ParseProgressCB progress_cb = nullptr, IPathResolver const& resolver = PathResolver::Instance())
			: IReader(report_error_cb, progress_cb, resolver)
			, m_src(src)
			, m_pos()
			, m_section({ {0, std::numeric_limits<int64_t>::max()} })
			, m_location({ src_filepath })
		{
			PushSection();
		}
		BinaryReader(BinaryReader&&) = delete;
		BinaryReader(BinaryReader const&) = delete;
		BinaryReader& operator=(BinaryReader&&) = delete;
		BinaryReader& operator=(BinaryReader const&) = delete;

		// Return the current location in the source
		virtual Location const& Loc() const override
		{
			m_location.m_offset = m_pos;
			return m_location;
		}

		// Move into a nested section
		virtual void PushSection() override
		{
			// The current top of the stack becomes the parent
			m_section.push_back({ m_pos, m_pos });
		}

		// Leave the current nested section
		virtual void PopSection() override
		{
			m_section.pop_back();

			// Should always have the dummy "global" parent section and a 'current' section
			assert(m_section.size() >= 2);
		}

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() override
		{
			return m_pos == m_section.back().m_end;
		}

		// True when the source is exhausted
		virtual bool IsSourceEnd() override
		{
			return m_src.eof();
		}

		// Get the next keyword within the current section.
		// Returns false if at the end of the section
		virtual bool NextKeywordImpl(int& kw) override
		{
			// Out of data
			if (m_src.eof())
				return false;

			// The top of the stack is the last section read at the current nesting level
			// The next on the stack is the parent level.
			auto& last = m_section[m_section.size() - 1];
			auto& parent = m_section[m_section.size() - 2];

			// Seek to the end of the current section.
			m_src.seekg(last.m_end - m_pos, std::ios::cur);
			m_pos = last.m_end;

			// If this is the end of the parent section then there are no more sections at this level.
			if (m_pos == parent.m_end)
				return false;

			// Read the next section header at this level
			SectionHeader header;
			Read(&header, sizeof(header));
			kw = static_cast<int>(header.m_keyword);

			// Replace the top of the stack
			m_section.back() = { m_pos, m_pos + header.m_size };
			return true;
		}

		// Read a utf8 string from the current section.
		// If 'has_length' is false, assume the whole section is the string.
		// If 'has_length' is true, assume the string is prefixed by its length.
		virtual string32 StringImpl(bool has_length = false) override
		{
			if (has_length)
			{
				size_t length = {};

				// Read the default 16-bit length
				uint16_t len16;
				Read(&len16, sizeof(len16));
				len16 = len16;

				// Only 15 bits are used
				length = len16 & 0x7FFF;

				// Use the high bit to indicate 31-bit length
				if (AllSet(len16, 0x8000))
				{
					Read(&len16, sizeof(len16));
					len16 = len16;
					length = (length << 16) | len16;
				}

				// Read the string 
				string32 str(length, 0);
				Read(str.data(), str.size());
				return str;
			}
			else
			{
				// Assume the remainder of the section is the string
				auto& header = m_section.back();
				string32 str(header.m_end - m_pos, 0);
				Read(str.data(), str.size());
				return str;
			}
		}

		// Read an integral value from the current section
		virtual int64_t IntImpl(int byte_count, int = 0) override
		{
			switch (byte_count)
			{
				case 1: { int8_t v; Read(&v, sizeof(v)); return v; }
				case 2: { int16_t v; Read(&v, sizeof(v)); return v; }
				case 4: { int32_t v; Read(&v, sizeof(v)); return v; }
				case 8: { int64_t v; Read(&v, sizeof(v)); return v; }
				default: throw std::runtime_error("Invalid byte count");
			}
		}

		// Read a floating point value from the current section
		virtual double RealImpl(int byte_count) override
		{
			auto value = IntImpl(byte_count, 0);
			switch (byte_count)
			{
				case 2: return reinterpret_cast<half_t const&>(value);
				case 4: return reinterpret_cast<float const&>(value);
				case 8: return reinterpret_cast<double const&>(value);
				default: throw std::runtime_error("Invalid byte count");
			}
		}
		
		// Read a boolean value from the current section
		virtual bool BoolImpl() override
		{
			return IntImpl(1, 0) != 0;
		}

	private:

		// Read 'size' bytes into 'buf'
		void Read(void* buf, int64_t size)
		{
			if (!m_src.read(char_ptr(buf), s_cast<size_t>(size)).good())
				throw std::runtime_error("Read failed");

			m_pos += size;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/memstream.h"
#include "pr/maths/maths.h"
namespace pr::rdr12::ldraw
{
	PRUnitTest(LDrawBinarySerialiserTests)
	{
		std::vector<char> data;

		pr::mem_ostream<char> strm(data);
		BinaryWriter::Write(strm, EKeyword::Point, [&](auto&)
		{
			BinaryWriter::Write(strm, EKeyword::Name, "TestPoints");
			BinaryWriter::Write(strm, EKeyword::Colour, 0xFF00FF00);
			BinaryWriter::Write(strm, EKeyword::Data, { v3(1,1,1), v3(2,2,2), v3(3,3,3) });
			BinaryWriter::Write(strm, EKeyword::Line, [&](auto&)
			{
				BinaryWriter::Write(strm, EKeyword::Name, "TestLines");
				BinaryWriter::Write(strm, EKeyword::Colour, 0xFF0000FF);
				BinaryWriter::Write(strm, EKeyword::Data, v3(-1,-1,0), v3(1,1,0), v3(-1,1,0), v3(1,-1,0));
			});
			BinaryWriter::Write(strm, EKeyword::Sphere, [&](auto&)
			{
				BinaryWriter::Write(strm, EKeyword::Name, "TestSphere");
				BinaryWriter::Write(strm, EKeyword::Colour, 0xFFFF0000);
				BinaryWriter::Write(strm, EKeyword::Data, 1.0f);
			});
		});

		PR_EXPECT(data.size() != 0);

		#if 0
		{
			std::ofstream ofile(temp_dir / "ldraw_test.lbr", std::ios::binary);
			ofile.write(data.data(), data.size());
		}
		#endif

		mem_istream<char> src(data);
		BinaryReader reader(src, {});

		PR_EXPECT(reader.Loc().m_offset == 0);
		
		EKeyword kw;
		PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Point);
		{
			auto points = reader.SectionScope();
			PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Name);
			PR_EXPECT(reader.Identifier<string32>() == "TestPoints");

			PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Colour);
			PR_EXPECT(reader.Int<uint32_t>() == 0xFF00FF00);

			PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Data);
			PR_EXPECT(reader.Vector3f().w1() == v4(1,1,1,1));
			PR_EXPECT(reader.Vector3f().w1() == v4(2,2,2,1));
			PR_EXPECT(reader.Vector3f().w1() == v4(3,3,3,1));

			PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Line); // Skip Line
			
			PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Sphere);
			{
				auto sphere = reader.SectionScope();
				PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Name);
				PR_EXPECT(reader.Identifier<string32>() == "TestSphere");

				PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Colour);
				PR_EXPECT(reader.Int<uint32_t>() == 0xFFFF0000);

				PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Data);
				PR_EXPECT(reader.Real<float>() == 1.0f);
			}

			PR_EXPECT(!reader.NextKeyword(kw));
			PR_EXPECT(reader.IsSectionEnd());
		}

		PR_EXPECT(reader.Loc().m_offset == isize(data));
	}
}
#endif
