//********************************
// Ldraw Script Binary Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser.h"

namespace pr::rdr12::ldraw
{
	// The section header
	struct SectionHeader
	{
		// The hash of the keyword (4-bytes)
		EKeyword m_keyword;

		// The length of the section in bytes (excluding the header size)
		int m_size;
	};

	struct BinaryWriter
	{
		template <typename TOut, typename Type> static void Append(TOut& out, Type)
		{
			// Note: if you hit this error, it's probably because Append(out, ???) is being
			// called where ??? is a type not handled by the overloads of Append.
			// Also watch out for the error being a typedef of a common type,
			// e.g. I've seen 'Type=std::ios_base::openmode' as an error, but really it was
			// 'Type=int' that was missing, because of 'typedef int std::ios_base::openmode'
			static_assert(dependent_false<Type>, "no overload for 'Type'");
		}
		template <typename TOut> static void Append(TOut& out, std::string_view str)
		{
			traits<TOut>::write(out, { byte_ptr(str.data()), str.size() });
		}
		template <typename TOut> static void Append(TOut& out, std::string&& s)
		{
			Append(out, std::string_view{ s });
		}
		template <typename TOut> static void Append(TOut& out, char const* s)
		{
			Append(out, std::string_view{ s });
		}
		template <typename TOut> static void Append(TOut& out, bool b)
		{
			uint8_t bool_ = b ? 1 : 0;
			traits<TOut>::write(out, { byte_ptr(&bool_), sizeof(bool_) });
		}
		template <typename TOut> static void Append(TOut& out, int8_t i)
		{
			traits<TOut>::write(out, { byte_ptr(&i), sizeof(i) });
		}
		template <typename TOut> static void Append(TOut& out, int16_t i)
		{
			traits<TOut>::write(out, { byte_ptr(&i), sizeof(i) });
		}
		template <typename TOut> static void Append(TOut& out, int32_t i)
		{
			traits<TOut>::write(out, { byte_ptr(&i), sizeof(i) });
		}
		template <typename TOut> static void Append(TOut& out, int64_t i)
		{
			traits<TOut>::write(out, { byte_ptr(&i), sizeof(i) });
		}
		template <typename TOut> static void Append(TOut& out, uint8_t u)
		{
			traits<TOut>::write(out, { byte_ptr(&u), sizeof(u) });
		}
		template <typename TOut> static void Append(TOut& out, uint16_t u)
		{
			traits<TOut>::write(out, { byte_ptr(&u), sizeof(u) });
		}
		template <typename TOut> static void Append(TOut& out, uint32_t u)
		{
			traits<TOut>::write(out, { byte_ptr(&u), sizeof(u) });
		}
		template <typename TOut> static void Append(TOut& out, uint64_t u)
		{
			traits<TOut>::write(out, { byte_ptr(&u), sizeof(u) });
		}
		template <typename TOut> static void Append(TOut& out, float f)
		{
			traits<TOut>::write(out, { byte_ptr(&f), sizeof(f) });
		}
		template <typename TOut> static void Append(TOut& out, double f)
		{
			traits<TOut>::write(out, { byte_ptr(&f), sizeof(f) });
		}
		template <typename TOut> static void Append(TOut& out, Colour32 c)
		{
			Append(out, c.argb);
		}
		template <typename TOut> static void Append(TOut& out, v2 v)
		{
			traits<TOut>::write(out, { byte_ptr(&v), sizeof(v) });
		}
		template <typename TOut> static void Append(TOut& out, v3 v)
		{
			traits<TOut>::write(out, { byte_ptr(&v), sizeof(v) });
		}
		template <typename TOut> static void Append(TOut& out, v4 v)
		{
			traits<TOut>::write(out, { byte_ptr(&v), sizeof(v) });
		}
		template <typename TOut> static void Append(TOut& out, m4x4 m)
		{
			traits<TOut>::write(out, { byte_ptr(&m), sizeof(m) });
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Vec2<S, void> v)
		{
			traits<TOut>::write(out, { byte_ptr(&v), sizeof(v) });
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Vec3<S, void> v)
		{
			traits<TOut>::write(out, { byte_ptr(&v), sizeof(v) });
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Vec4<S, void> v)
		{
			traits<TOut>::write(out, { byte_ptr(&v), sizeof(v) });
		}
		template <typename TOut, Scalar S> static void Append(TOut& out, Mat4x4<S, void, void> m)
		{
			traits<TOut>::write(out, { byte_ptr(&m), sizeof(m) });
		}
		template <typename TOut> static void Append(TOut& out, EAddrMode addr)
		{
			Append(out, s_cast<int>(addr));
		}
		template <typename TOut> static void Append(TOut& out, EFilter filter)
		{
			Append(out, s_cast<int>(filter));
		}
		template <typename TOut> static void Append(TOut& out, EArrowType type)
		{
			Append(out, s_cast<int>(type));
		}
		template <typename TOut> static void Append(TOut& out, EPointStyle style)
		{
			using ut = std::underlying_type_t<EPointStyle>;
			Append(out, s_cast<ut>(style));
		}
		template <typename TOut> static void Append(TOut& out, VariableInt var_int)
		{
			// Variable sized int, write 6 bits at a time: xx444444 33333322 22221111 11000000
			uint8_t bits[5] = {}; int i = 5;
			for (int val = var_int.m_value; val != 0 && i-- != 0; val >>= 6)
				bits[i] = static_cast<uint8_t>(0x80 | (val & 0b00111111));

			traits<TOut>::write(out, { byte_ptr(&bits[0] + i), static_cast<size_t>(5 - i) });
		}
		template <typename TOut> static void Append(TOut& out, StringWithLength str)
		{
			Append(out, VariableInt{ isize(str.m_value) }); // length in bytes, not utf8 codes
			Append(out, str.m_value);
		}
		template <typename TOut> static void Append(TOut& out, Name n)
		{
			if (n.m_name.empty()) return;
			Write(out, EKeyword::Name, n.m_name);
		}
		template <typename TOut> static void Append(TOut& out, Colour c)
		{
			if (c.m_colour.argb == 0xFFFFFFFF) return;
			Write(out, c.m_kw, c.m_colour.argb);
		}
		template <typename TOut> static void Append(TOut& out, Size s)
		{
			if (s.m_size == 0) return;
			Write(out, EKeyword::Size, s.m_size);
		}
		template <typename TOut> static void Append(TOut& out, Size2 s)
		{
			if (s.m_size == v2::Zero()) return;
			Write(out, EKeyword::Size, s.m_size);
		}
		template <typename TOut> static void Append(TOut& out, Width w)
		{
			if (w.m_width == 0) return;
			Write(out, EKeyword::Width, w.m_width);
		}
		template <typename TOut> static void Append(TOut& out, Scale s)
		{
			if (s.m_scale == 1.0f) return;
			Write(out, EKeyword::Scale, s.m_scale);
		}
		template <typename TOut> static void Append(TOut& out, Scale2 s)
		{
			if (s.m_scale == v2::One()) return;
			Write(out, EKeyword::Scale, s.m_scale);
		}
		template <typename TOut> static void Append(TOut& out, Scale3 s)
		{
			if (s.m_scale == v3::One()) return;
			Write(out, EKeyword::Scale, s.m_scale);
		}
		template <typename TOut> static void Append(TOut& out, PerItemColour c)
		{
			if (!c.m_per_item_colour) return;
			Write(out, EKeyword::PerItemColour);
		}
		template <typename TOut> static void Append(TOut& out, Depth d)
		{
			if (d.m_depth == false) return;
			Write(out, EKeyword::Depth);
		}
		template <typename TOut> static void Append(TOut& out, Hidden h)
		{
			if (!h.m_hide) return;
			Write(out, EKeyword::Hidden);
		}
		template <typename TOut> static void Append(TOut& out, Wireframe w)
		{
			if (!w.m_wire) return;
			Write(out, EKeyword::Wireframe);
		}
		template <typename TOut> static void Append(TOut& out, Solid s)
		{
			if (!s.m_solid) return;
			Write(out, EKeyword::Solid);
		}
		template <typename TOut> static void Append(TOut& out, Smooth s)
		{
			if (!s.m_smooth) return;
			Write(out, EKeyword::Smooth);
		}
		template <typename TOut> static void Append(TOut& out, LeftHanded lh)
		{
			if (!lh.m_lh) return;
			Write(out, EKeyword::LeftHanded);
		}
		template <typename TOut> static void Append(TOut& out, Alpha a)
		{
			if (!a.m_has_alpha) return;
			Write(out, EKeyword::Alpha);
		}
		template <typename TOut> static void Append(TOut& out, AxisId a)
		{
			if (a.m_axis.value == pr::AxisId::None) return;
			Write(out, EKeyword::AxisId, a.m_axis.value);
		}
		template <typename TOut> static void Append(TOut& out, ArrowType a)
		{
			if (a.m_type == EArrowType::Fwd) return;
			Write(out, EKeyword::Style, a.m_type);
		}
		template <typename TOut> static void Append(TOut& out, Pos p)
		{
			if (p.m_pos == v4::Origin()) return;
			Write(out, EKeyword::O2W, [&]
			{
				Write(out, EKeyword::Pos, p.m_pos.xyz);
			});
		}
		template <typename TOut> static void Append(TOut& out, O2W o2w)
		{
			if (o2w.m_mat == m4x4::Identity())
				return;

			if (o2w.m_mat.rot == m3x4::Identity() && o2w.m_mat.pos.w == 1)
				return Write(out, EKeyword::O2W, [&]
					{
						Write(out, EKeyword::Pos, o2w.m_mat.pos.xyz);
					});

			Write(out, EKeyword::O2W, [&]
			{
				if (!IsAffine(o2w.m_mat)) Write(out, EKeyword::NonAffine);
				Write(out, EKeyword::M4x4, o2w.m_mat);
			});
		}
		template <typename TOut, PrimitiveSpanType TSpan> static void Append(TOut& out, TSpan span)
		{
			traits<TOut>::write(out, { byte_ptr(span.data()), span.size() * sizeof(typename TSpan::value_type) });
		}
		template <typename TOut, typename... TItems> static void Append(TOut& out, TItems&&... items)
		{
			(Append(out, std::forward<TItems>(items)), ...);
		}

		// Write custom data within a section
		template <typename TOut, std::invocable<> AddBodyFn>
		static void Write(TOut& out, EKeyword keyword, Name name, Colour colour, AddBodyFn body_cb)
		{
			// Record the write pointer position
			auto ofs = traits<TOut>::tellp(out);

			// Write a dummy header
			SectionHeader header = { .m_keyword = keyword };
			traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) });

			// Optional name/colour
			Append(out, name);
			Append(out, colour);

			// Write the section body
			body_cb();

			// Update the header with the correct size
			header.m_size = s_cast<int>(traits<TOut>::tellp(out) - ofs - sizeof(header));
			traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) }, ofs);
		}
		template <typename TOut, std::invocable AddBodyFn>
		static void Write(TOut& out, EKeyword keyword, AddBodyFn body_cb)
		{
			Write(out, keyword, {}, {}, body_cb);
		}

		// Write a single primitive type
		template <typename TOut, typename... TItem> requires (!std::is_invocable_v<TItem> && ...)
		static void Write(TOut& out, EKeyword keyword, Name name, Colour colour, TItem&&... items)
		{
			return Write(out, keyword, name, colour, [&]
			{
				(Append(out, std::forward<TItem>(items)), ...);
			});
		}
		template <typename TOut, typename... TItem> requires (!std::is_invocable_v<TItem> && ...)
		static void Write(TOut& out, EKeyword keyword, TItem&&... items)
		{
			return Write(out, keyword, {}, {}, [&]
			{
				(Append(out, std::forward<TItem>(items)), ...);
			});
		}
	};

	struct BinaryReader : IReader
	{
		// Byte offsets from the start of the stream for the range of the data in the current section (excludes the header)
		using SectionSpan = struct { int64_t m_beg, m_end; };
		using SectionStack = pr::vector<SectionSpan>;
		using istream_t = std::basic_istream<char>; // 'char' to support ifstream

		istream_t& m_src;            // The input byte stream
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
			m_src.seekg(last.m_end - m_pos, std::ios::cur).peek(); // Peek to test for EOF
			m_pos = last.m_end;
			if (m_pos < 0)
				throw std::runtime_error("Corrupt ldraw data");

			// If this is the end of the parent section then there are no more sections at this level.
			if (m_pos == parent.m_end || m_src.eof())
				return false;

			// Read the next section header at this level
			SectionHeader header;
			Read(&header, sizeof(header));
			kw = static_cast<int>(header.m_keyword);

			// Replace the top of the stack
			m_section.back() = { m_pos, m_pos + header.m_size };
			return true;
		}
		
		// Read an identifier from the current section. Leading '10xxxxxx' bytes are the length (in bytes). Default length is the full section
		virtual string32 IdentifierImpl() override
		{
			auto length = ReadLengthBytes();

			// Read the string 
			string32 str(length, 0);
			Read(str.data(), str.size());
			return str;
		}

		// Read a utf8 string from the current section.
		virtual string32 StringImpl(char) override
		{
			auto length = ReadLengthBytes();

			// Read the string 
			string32 str(length, 0);
			Read(str.data(), str.size());
			return str;
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
		
		// Read an enum value from the current section
		virtual int64_t EnumImpl(int byte_count, ParseEnumIdentCB) override
		{
			return IntImpl(byte_count);
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
			if (size < 0)
				throw std::runtime_error("Invalid read size");

			if (!m_src.read(char_ptr(buf), s_cast<size_t>(size)).good())
			{
				ReportError(EParseError::DataMissing, Loc(), "Read failed");
				memset(buf, 0, s_cast<size_t>(size));
			}
			m_pos += size;
		}

		// Read bytes with pattern '10xxxxxx'. These are invalid UTF-8 characters which I'm using for length information
		size_t ReadLengthBytes()
		{
			size_t length = 0;
			for (;;)
			{
				auto ch = m_src.peek();
				if (ch == std::char_traits<char>::eof() || (ch & 0xC0) != 0x80)
					break;

				length = (length << 6) + (ch & 0b00111111);
				m_src.read(char_ptr(&ch), 1);
				++m_pos;
			}

			// If length is zero, infer the length from the section length
			if (length == 0)
			{
				auto& header = m_section.back();
				length = s_cast<size_t>(header.m_end - m_pos);
			}

			return length;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::rdr12::ldraw
{
	PRUnitTest(LDrawBinarySerialiserTests)
	{
		std::vector<char> data;

		pr::mem_ostream<char> strm(data);
		BinaryWriter::Write(strm, EKeyword::Point, [&]
		{
			BinaryWriter::Write(strm, EKeyword::Name, "TestPoints");
			BinaryWriter::Write(strm, EKeyword::Colour, 0xFF00FF00);
			BinaryWriter::Write(strm, EKeyword::Data, v3(1,1,1), v3(2,2,2), v3(3,3,3));
			BinaryWriter::Write(strm, EKeyword::Line, [&]
			{
				BinaryWriter::Write(strm, EKeyword::Name, "TestLines");
				BinaryWriter::Write(strm, EKeyword::Colour, 0xFF0000FF);
				BinaryWriter::Write(strm, EKeyword::Data, v3(-1,-1,0), v3(1,1,0), v3(-1,1,0), v3(1,-1,0));
			});
			BinaryWriter::Write(strm, EKeyword::Sphere, [&]
			{
				BinaryWriter::Write(strm, EKeyword::Name, "TestSphere");
				BinaryWriter::Write(strm, EKeyword::Colour, 0xFFFF0000);
				BinaryWriter::Write(strm, EKeyword::Data, 1.0f);
			});
			BinaryWriter::Write(strm, EKeyword::Custom, [&]
			{
				std::string_view s = "ShortString";
				BinaryWriter::Write(strm, EKeyword::Name, StringWithLength{ s }, StringWithLength{ s });
			});
		});

		PR_EXPECT(data.size() != 0);

		#if 1
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

			PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Custom);
			{
				auto sphere = reader.SectionScope();
				PR_EXPECT(reader.NextKeyword(kw) && kw == EKeyword::Name);
				PR_EXPECT(reader.String<string32>() == "ShortString");
				PR_EXPECT(reader.String<string32>() == "ShortString");
			}

			PR_EXPECT(!reader.NextKeyword(kw));
			PR_EXPECT(reader.IsSectionEnd());
		}

		PR_EXPECT(reader.Loc().m_offset == isize(data));
	}
}
#endif
