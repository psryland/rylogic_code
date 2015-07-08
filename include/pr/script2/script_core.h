//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/forward.h"
#include "pr/script2/location.h"
#include "pr/script2/buf8.h"

namespace pr
{
	namespace script2
	{
		// Interface to a stream of wchar_t's, essentually a pointer-like interface
		struct Src
		{
			virtual ~Src() {}

			// Debugging helper interface
			virtual ESrcType Type() const { return m_type; }
			virtual Location const& Loc() const { static Location s_loc; return s_loc; }
			virtual SrcConstPtr DbgPtr() const = 0;

			// Pointer-like interface
			virtual wchar_t operator * () const = 0;
			virtual Src&    operator ++() = 0;

			// Convenience methods
			Src& operator +=(int n)
			{
				for (;n--;) ++*this;
				return *this;
			}

		protected:

			ESrcType m_type;
			Src(ESrcType type)
				:m_type(type)
			{}
		};

		// An empty source
		struct NullSrc :Src
		{
			NullSrc() :Src(ESrcType::Null) {}
			SrcConstPtr DbgPtr() const override { return SrcConstPtr(); }
			wchar_t operator * () const override { return 0; }
			NullSrc& operator ++() { return *this; }
		};

		// Allow any type that implements a pointer to implement 'Src'
		template <typename TPtr, typename TLoc = TextLoc> struct Ptr :Src
		{
			TPtr m_ptr; // The pointer-like data source
			TLoc m_loc;

			Ptr(TPtr ptr, ESrcType src_type = ESrcType::Pointer)
				:Src(src_type)
				,m_ptr(ptr)
				,m_loc()
			{}

			// Debugging helper interface
			SrcConstPtr DbgPtr() const override
			{
				return &*m_ptr;
			}
			TextLoc const& Loc() const override
			{
				return m_loc;
			}

			// Pointer-like interface
			wchar_t operator * () const override
			{
				return wchar_t(*m_ptr);
			}
			Ptr& operator ++() override
			{
				if (*m_ptr) ++m_ptr;
				return *this;
			}
		};
		template <typename TLoc = TextLoc> using PtrA = Ptr<char const*, TLoc>;
		template <typename TLoc = TextLoc> using PtrW = Ptr<wchar_t const*, TLoc>;

		// A file char source
		template <typename TLoc = FileLoc> struct FileSrc :Src
		{
			enum class EEncoding { ascii, utf8, utf16, utf16_be, auto_detect };
			using FGet = wchar_t (*)(std::ifstream&);

			mutable std::wifstream m_file; // The file stream source
			BufW4 m_buf;                   // Use a small shift buffer to make debugging easier
			TLoc m_loc;                    // The location within the file (note, TLoc could be Location* to reference an external location object)
			EEncoding m_enc;               // The detected file encoding
			FGet m_fget;                   // The file read function to use based on the file encoding

			template <typename String> explicit FileSrc(String const& filepath, size_t ofs = 0, EEncoding enc = EEncoding::auto_detect, TLoc loc = TLoc())
				:Src(ESrcType::File)
				,m_file()
				,m_buf()
				,m_loc(loc)
				,m_fget()
				,m_enc(enc)
			{
				auto fpath = pr::str::c_str(filepath);

				// Determine file encoding, look for the BOM in the first 3 bytes
				if (m_enc == EEncoding::auto_detect)
				{
					unsigned char bom[3];
					std::ifstream file(fpath, std::ios::binary);
					auto read = file.good() ? file.read(reinterpret_cast<char*>(&bom[0]), sizeof(bom)).gcount() : 0;
					if      (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) { m_enc = EEncoding::utf8;     ofs += 3; }
					else if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)                   { m_enc = EEncoding::utf16_be; ofs += 2; }
					else if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)                   { m_enc = EEncoding::utf16;    ofs += 2; }
					else m_enc = EEncoding::utf8; // If no valid bomb is found, assume UTF-8 as that is a superset of ASCII
				}

				// Open the input file stream
				m_file.open(fpath, std::ios::binary);
				if (!m_file.good())
					throw std::exception(pr::FmtS("Failed to open file %s", Narrow(filepath).c_str()));

				// "Imbue" the file stream so that character codes get converted
				static std::locale global_locale;
				switch (m_enc)
				{
				default: throw std::exception("Unsupport file encoding");
				case EEncoding::ascii:
					{
						break;
					}
				case EEncoding::utf8:
					{
						static std::locale utf8_locale(global_locale, new std::codecvt_utf8<wchar_t>);
						m_file.imbue(utf8_locale);
						break;
					}
				case EEncoding::utf16:
					{
						static std::locale utf16_locale(global_locale, new std::codecvt_utf16<wchar_t, 0x10ffff, std::little_endian>);
						m_file.imbue(utf16_locale);
						break;
					}
				case EEncoding::utf16_be:
					{
						static std::locale utf16_locale(global_locale, new std::codecvt_utf16<wchar_t>);
						m_file.imbue(utf16_locale);
						break;
					}
				}

				// Seek to the position to start reading from (may include the skip over the BOM)
				m_file.seekg(ofs);

				// Load the shift register
				for (int n = BufW4::Capacity; n--;)
				{
					auto ch = m_file.get();
					m_buf.shift(m_file.good() ? ch : 0);
				}
			}

			// Debugging helper interface
			bool IsOpen() const
			{
				return m_file.is_open();
			}
			SrcConstPtr DbgPtr() const override
			{
				return &m_buf.m_ch[0];
			}
			TLoc const& Loc() const override
			{
				return m_loc;
			}

			// Pointer-like interface
			wchar_t operator * () const override
			{
				return m_buf.front();
			}
			FileSrc& operator ++() override
			{
				m_loc.inc(m_buf.front());
				auto ch = m_file.get();
				m_buf.shift(m_file.good() ? ch : 0);
				return *this;
			}
		};

		// Src buffer. Provides random access within a buffered range.
		template <typename TBuf = pr::deque<wchar_t>, typename TSrc = Src> struct Buffer :Src
		{
			using value_type = typename TBuf::value_type;
			using buffer_type = TBuf;

			mutable TBuf m_buf; // The buffered stream data read from 'm_src'
			TSrc* m_src;         // The character stream that feeds 'm_buf'
			NullSrc m_null;     // Used when 'm_src' == nullptr;

			Buffer(TSrc& src)
				:Src(src.Type())
				,m_buf()
				,m_src(&src)
				,m_null()
			{}
			template <typename Iter> Buffer(ESrcType type)
				:Src(type)
				,m_buf()
				,m_src(&m_null)
				,m_null()
			{}
			template <typename Iter> Buffer(ESrcType type, Iter first, Iter last)
				:Src(type)
				,m_buf(first, last)
				,m_src(&m_null)
				,m_null()
			{}
			Buffer(Buffer&& rhs)
				:Src(std::move(rhs))
				,m_buf(std::move(rhs.m_buf))
				,m_src(rhs.m_src)
				,m_null()
			{}
			Buffer(Buffer const& rhs)
				:Src(rhs)
				,m_buf(rhs.m_buf)
				,m_src(rhs.m_src)
				,m_null()
			{}

			// Debugging helper interface
			Location const& Loc() const override
			{
				return m_src->Loc();
			}
			SrcConstPtr DbgPtr() const override
			{
				return m_src->DbgPtr();
			}
			TBuf const* DbgBuf() const
			{
				return &m_buf;
			}

			// Pointer-like interface
			value_type operator *() const override
			{
				return empty() ? **m_src : m_buf[0];
			}
			Buffer& operator ++() override
			{
				if (!empty()) pop_front();
				else ++*m_src;
				return *this;
			}

			// Array access to the buffered data. Buffer size grows to accomodate 'i'
			value_type operator [](size_t i) const
			{
				if (i == 0) return **this;
				ensure(i);
				return m_buf[i];
			}
			value_type& operator [](size_t i)
			{
				// Returning a reference requires the character be buffered
				ensure(i);
				return m_buf[i];
			}

			// Returns true if no data is buffered
			bool empty() const
			{
				return m_buf.empty();
			}

			// The count of buffered characters
			size_t size() const
			{
				return m_buf.size();
			}

			// Removes all buffered data
			void clear()
			{
				// don't define resize() as its confusing which portion is kept (front or back?)
				m_buf.resize(0);
			}

			// Return the first buffered character
			value_type front() const
			{
				return m_buf.front()
			}

			// Return the last buffered character
			value_type back() const
			{
				return m_buf.back();
			}

			// Iterator range access to the buffer
			auto begin() const -> decltype(std::begin(m_buf))
			{
				return std::begin(m_buf);
			}
			auto end() const -> decltype(std::end(m_buf))
			{
				return std::end(m_buf);
			}

			// Returns the source that is feeding the buffer
			Src& stream() const
			{
				return *m_src;
			}
			
			// Push a character onto the front of the buffer (making it the next character read)
			void push_front(value_type ch)
			{
				m_buf.push_front(ch);
			}

			// Pop n characters from the front of the buffer
			void pop_front()
			{
				m_buf.pop_front();
			}
			void pop_front(size_t n)
			{
				auto first = std::begin(m_buf);
				m_buf.erase(first, first + n);
			}

			// Pop n characters from the back of the buffer
			void pop_back(size_t n = 1)
			{
				auto first = std::begin(m_buf);
				auto count = m_buf.size();
				m_buf.erase(first + count - n, first + count);
			}

			// Buffer the next 'n' characters from the source stream
			void buffer(size_t n = 1) const
			{
				for (;n-- > 0; ++*m_src)
					m_buf.push_back(**m_src);
			}

			// Ensure a total of 'n' characters are buffered
			void ensure(size_t n) const
			{
				if (n < m_buf.size()) return;
				buffer(1 + n - m_buf.size());
			}

			// Insert 'count' * 'ch's at 'ofs' in the buffer
			void insert(size_t ofs, size_t count, value_type ch)
			{
				ensure(ofs);
				m_buf.insert(std::begin(m_buf) + ofs, count, ch);
			}

			// Insert 'count' 'ch's at 'ofs' in the buffer
			template <typename Iter> void insert(size_t ofs, Iter first, Iter last)
			{
				ensure(ofs);
				m_buf.insert(std::begin(m_buf) + ofs, first, last);
			}

			// Erase a range within the buffered characters
			void erase(size_t ofs = 0, size_t count = ~0U)
			{
				auto first = std::begin(m_buf) + ofs;
				count = std::min(count, m_buf.size() - ofs);
				m_buf.erase(first, first + count);
			}

			// Return the buffered text as a string
			pr::string<value_type> str(size_t ofs = 0, size_t count = ~0U) const
			{
				auto first = std::begin(m_buf) + ofs;
				count = std::min(count, m_buf.size() - ofs);
				return pr::string<value_type>(first, first + count);
			}

			// String compare - note asymmetric: i.e. buf="abcd", str="ab", buf.match(str) == true
			// Buffers the input stream and compares it to 'str' return true if they match.
			// If 'adv_if_match' is true, the matching characters are popped from the buffer.
			// Note: *only* buffers matching characters. This prevents the buffer containing extra
			// data after a mismatch, and can be used to determine the length of a partial match.
			// Returns the length of the match == Length(str) if successful, 0 if not a match.
			// Not returning partial match length as that makes use as a boolean expression tricky.
			template <typename Str> int match(Str const& str) const
			{
				// Can't use wcscmp(), m_buf is not guaranteed contiguous
				size_t i = 0, count = pr::str::Length(str);

				// If the buffer contains data already, test that first
				for (auto buf_count = size_buf(); i != count && i < buf_count && str[i] == m_buf[i]; ++i) {}

				// Buffer extra matching characters if needed
				for (; i != count && str[i] == *stream(); buffer(), ++i) {}

				// Return match success/fail
				return i == count ? count : 0;
			}
			template <typename Char> int match(Char const* str) const
			{
				size_t i = 0;

				// If the buffer contains data already, test that first
				for (auto buf_count = size(); str[i] && i < buf_count && str[i] == m_buf[i]; ++i) {}

				// Buffer extra matching characters if needed
				for (; *stream() && *stream() == str[i]; buffer(), ++i) {}

				// Return match success/fail
				return str[i] == 0 ? int(i) : 0;
			}
			template <typename Str> int match(Str const& str, bool adv_if_match)
			{
				auto r = match(str);
				if (r && adv_if_match) pop_front(r);
				return r;
			}
		};

		#pragma region Global Functions

		// Return the hash of a single character
		inline HashValue hashfunc(wchar_t ch, HashValue r = ~HashValue())
		{
			static std::hash<wchar_t> s_hash;
			return r * 137 ^ HashValue(s_hash(ch));
		}

		// Return the hash value for a string
		inline HashValue Hash(wchar_t const* name)
		{
			auto r = ~HashValue();
			for (; *name; ++name) r = hashfunc(*name, r);
			return r;
		}
		inline HashValue HashLwr(wchar_t const* name)
		{
			auto r = ~HashValue();
			for (; *name; ++name) r = hashfunc(towlower(*name), r);
			return r;
		}
		template <typename Iter> inline HashValue Hash(Iter first, Iter last)
		{
			auto r = ~HashValue();
			for (; first != last; ++first) r = hashfunc(*first, r);
			return r;
		}
		template <typename String> inline HashValue Hash(String const& name)
		{
			return Hash(std::begin(name), std::end(name));
		}

		// Buffer an identifier into 'src'.
		template <typename TBuf> inline bool BufferIdentifier(TBuf& src)
		{
			if  (  pr::str::IsIdentifier(*src.stream(), true )) src.buffer(); else return false;
			for (; pr::str::IsIdentifier(*src.stream(), false); src.buffer()) {}
			return true;
		}

		// Buffer up to the next '\n' into 'src'
		template <typename TBuf> inline void BufferLine(TBuf& src)
		{
			for (; !pr::str::IsNewLine(*src.stream()); src.buffer()) {}
		}

		// Buffer up to 'end'. If 'include_end' is false, 'end' is removed from the buffer once read
		template <typename TBuf> inline bool BufferTo(TBuf& src, wchar_t const* end, bool include_end)
		{
			int i = 0;
			for (; end[i]; src.buffer())
			{
				auto ch = *src.stream();
				if      (ch == end[i]) ++i;
				else if (ch == end[0]) i = 1;
				else i = 0;
			}
			if (end[i] != 0) return false;
			if (!include_end) src.pop_back(i);
			return true;
		}

		// Call '++src' until 'pred' returns false
		template <typename TSrc, typename Pred> void Eat(TSrc& src, int eat_initial, int eat_final, Pred pred)
		{
			for (src += eat_initial; pred(*src); ++src) {}
			src += eat_final;
		}
		template <typename TSrc> inline void EatLineSpace(TSrc& src, int eat_initial, int eat_final)
		{
			Eat(src, eat_initial, eat_final, pr::str::IsLineSpace<wchar_t>);
		}
		template <typename TSrc> inline void EatWhiteSpace(TSrc& src, int eat_initial, int eat_final)
		{
			Eat(src, eat_initial, eat_final, pr::str::IsWhiteSpace<wchar_t>);
		}
		template <typename TSrc> inline void EatLine(TSrc& src, int eat_initial, int eat_final)
		{
			Eat(src, eat_initial, eat_final, [](wchar_t ch){ return !pr::str::IsNewLine(ch); });
		}
		template <typename TSrc> inline bool EatLiteralString(TSrc& src)
		{
			if (*src != L'\"' && *src != L'\'')
				return false;

			auto end = *src;
			auto escape = false;
			Eat(src, 1, 0, [&](wchar_t ch)
			{
				auto r = ch != end || escape;
				escape = ch == L'\\';
				return r;
			});
			if (*src == end) ++src; else return false;
			return true;
		}
		template <typename TSrc, typename Char> inline void EatDelimiters(TSrc& src, Char const* delim)
		{
			for (; *pr::str::FindChar(delim, *src) != 0; ++src) {}
		}

		#pragma endregion
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script2_checkhashes)
		{
			using namespace pr::script2;
			pr::CheckHashEnum<EKeyword, wchar_t>(
				[](wchar_t const* name)
				{
					return Hash(name);
				},
				[](char const* msg)
				{
					PR_FAIL(msg);
				});

			pr::CheckHashEnum<EPPKeyword, wchar_t>(
				[](wchar_t const* name)
				{
					return Hash(name);
				},
				[](char const* msg)
				{
					PR_FAIL(msg);
				});
		}
		PRUnitTest(pr_script2_core)
		{
			using namespace pr::str;
			using namespace pr::script2;

			{// Simple buffering
				char const str[] = "123abc";
				PtrA<> ptr(str);
				Buffer<> buf(ptr);

				PR_CHECK(*buf  , L'1');
				PR_CHECK(buf[5], L'c');
				PR_CHECK(buf[0], L'1');

				PR_CHECK(*(++buf)   , L'2');
				PR_CHECK(*(buf += 3), L'b');
				PR_CHECK(*(++buf)   , L'c');

				PR_CHECK(*(++buf),  0);
			}
			{// Matching
				wchar_t const str[] = L"0123456789";
				PtrW<> ptr(str);
				Buffer<> buf(ptr);

				PR_CHECK(buf.match(L"0123") != 0, true);
				PR_CHECK(buf.match(L"012345678910") != 0, false);
				buf += 5;
				PR_CHECK(buf.match(L"567") != 0, true);
			}
			char const* script_utf = "script_utf.txt";
			{// UTF8 big endian File source
				// UTF-16be data (if host system is little-endian)
				unsigned char data[] = {0xef, 0xbb, 0xbf, 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd}; // ni hao (chinesse)
				wchar_t str[] = {0x4f60, 0x597d};

				{// Create the file
					std::ofstream fout(script_utf);
					fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
				}

				FileSrc<> file(script_utf);
				PR_CHECK(*file, str[0]); ++file;
				PR_CHECK(*file, str[1]); ++file;
			}
			{// UTF 16 little endian File source
				// UTF-16le data (if host system is little-endian)
				unsigned short data[] = {0xfeff, 0x4f60, 0x597d}; // ni hao (chinesse)
				wchar_t str[] = {0x4f60, 0x597d};

				{// Create the file
					std::ofstream fout(script_utf);
					fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
				}

				FileSrc<> file(script_utf);
				PR_CHECK(*file, str[0]); ++file;
				PR_CHECK(*file, str[1]); ++file;
			}
			{// UTF 16 big endian File source
				// UTF-16be data (if host system is little-endian)
				unsigned short data[] = {0xfffe, 0x604f, 0x7d59}; // ni hao (chinesse)
				wchar_t str[] = {0x4f60, 0x597d};

				{// Create the file
					std::ofstream fout(script_utf);
					fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
				}

				FileSrc<> file(script_utf);
				PR_CHECK(*file, str[0]); ++file;
				PR_CHECK(*file, str[1]); ++file;
			}
			pr::filesys::EraseFile(script_utf);
		}
	}
}
#endif
