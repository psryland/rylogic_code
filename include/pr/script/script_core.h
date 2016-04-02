//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/location.h"
#include "pr/script/buf8.h"

namespace pr
{
	namespace script
	{
		// Interface to a stream of wchar_t's, essentially a pointer-like interface
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
		template <typename TPtr, bool UTF8 = false, typename TLoc = TextLoc> struct Ptr :Src
		{
			TPtr m_ptr; // The pointer-like data source
			TLoc m_loc;

			Ptr(TPtr ptr, TLoc loc = TLoc(), ESrcType src_type = ESrcType::Pointer)
				:Src(src_type)
				,m_ptr(ptr)
				,m_loc(loc)
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
				return output_char();
			}
			Ptr& operator ++() override
			{
				if (*m_ptr == 0) return *this;
				m_loc.inc(*m_ptr);
				++m_ptr;
				return *this;
			}

		private:

			// Convert a char stream into ASCII or utf8
			template <typename = std::enable_if<!UTF8>> wchar_t output_char() const
			{
				return wchar_t(*m_ptr);
			}
			template <typename = std::enable_if<UTF8>> wchar_t output_char(int = 0) const
			{
				wchar_t ch;
				auto state = std::mbstate_t{};
				std::mbsrtowcs(&ch, &m_ptr, 1, &state);
				return ch;
			}
		};
		template <typename TLoc = TextLoc> using PtrA = Ptr<char const*, false, TLoc>;
		template <typename TLoc = TextLoc> using PtrUTF8 = Ptr<char const*, true, TLoc>;
		template <typename TLoc = TextLoc> using PtrW = Ptr<wchar_t const*, false, TLoc>;

		// A pointer range char source
		template <typename TPtr, typename TLoc = TextLoc> struct PtrRange :Src
		{
			TPtr m_ptr;
			TPtr m_beg, m_end;
			TLoc m_loc;

			PtrRange(TPtr beg, TPtr end, TLoc loc = TLoc(), ESrcType src_type = ESrcType::Range)
				:Src(src_type)
				,m_ptr(beg)
				,m_beg(beg)
				,m_end(end)
				,m_loc(loc)
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
				return m_ptr != m_end ? wchar_t(*m_ptr) : wchar_t();
			}
			PtrRange& operator ++() override
			{
				if (m_ptr == m_end) return *this;
				m_loc.inc(*m_ptr);
				++m_ptr;
				return *this;
			}

			// Reset to the start of the range
			void reset(size_t ofs = 0, TLoc loc = TLoc())
			{
				m_ptr = m_beg + ofs;
				m_loc = loc;
			}

			// Iterator range
			TPtr begin() const
			{
				return m_beg;
			}
			TPtr end() const
			{
				return m_end;
			}

			// Return a sub range of this range
			PtrRange Subrange(size_t ofs, size_t count, TLoc loc = TLoc()) const
			{
				return PtrRange(m_beg + ofs, m_beg + ofs + count, loc);
			}
		};

		// A file char source
		template <typename TLoc = FileLoc> struct FileSrc :Src
		{
			enum class EEncoding { ascii, utf8, utf16, utf16_be, auto_detect };

			mutable std::wifstream m_file; // The file stream source
			BufW4 m_buf;                   // Use a small shift buffer to make debugging easier
			TLoc m_loc;                    // The location within the file (note, TLoc could be Location* to reference an external location object)
			EEncoding m_enc;               // The detected file encoding

			FileSrc()
				:Src(ESrcType::File)
				,m_file()
				,m_buf()
				,m_loc()
				,m_enc()
			{}
			template <typename String> explicit FileSrc(String const& filepath, size_t ofs = 0, EEncoding enc = EEncoding::auto_detect, TLoc loc = TLoc())
				:FileSrc()
			{
				if (!pr::str::Empty(filepath))
					Open(filepath, ofs, enc, loc);
			}

			// Open a file as a stream source
			template <typename String> void Open(String const& filepath, size_t ofs = 0, EEncoding enc = EEncoding::auto_detect, TLoc loc = TLoc())
			{
				Close();

				auto fpath = pr::str::c_str(filepath);
				m_enc = enc;
				m_loc = loc;

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

			// Close the file stream
			void Close()
			{
				m_file.close();
				m_buf.clear();
				m_loc = TLoc();
				m_enc = EEncoding::auto_detect;
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

		// A counter that never goes below 0. Used for holding the read position in Buffer<>
		struct EmitCount
		{
			int m_value;
			EmitCount(int value = 0) :m_value(value) {}
			operator size_t() const                  { return size_t(m_value); }
			EmitCount& operator = (int n)            { m_value = n > 0 ? n : 0; return *this; }
			EmitCount& operator ++()                 { return *this = m_value + 1; }
			EmitCount& operator --()                 { return *this = m_value - 1; }
			EmitCount  operator ++(int)              { auto x = *this; ++*this; return x; }
			EmitCount  operator --(int)              { auto x = *this; --*this; return x; }
			EmitCount& operator +=(int n)            { return *this = m_value + n; }
			EmitCount& operator -=(int n)            { return *this = m_value - n; }
		};

		// Src buffer. Provides random access within a buffered range.
		// Handy debugging tip: call buffer_all(); on the buffers to preload them will all the data
		// It makes them easier to read in the watch windows.
		template <typename TBuf = pr::deque<wchar_t>> struct Buffer :Src
		{
			using value_type = typename TBuf::value_type;
			using buffer_type = TBuf;

			mutable TBuf m_buf;       // The buffered stream data read from 'm_src'
			Src* m_src;              // The character stream that feeds 'm_buf'
			NullSrc m_null;           // Used when 'm_src' == nullptr;

			Buffer(ESrcType type = ESrcType::Buffered)
				:Src(type)
				,m_buf()
				,m_src(&m_null)
				,m_null()
			{}
			explicit Buffer(Src& src)
				:Buffer()
			{
				Source(src);
			}
			template <typename Iter> Buffer(ESrcType type, Iter first, Iter last)
				:Src(type)
				,m_buf(first, last)
				,m_src(&m_null)
				,m_null()
			{}
			template <typename TPtr> Buffer(ESrcType type, TPtr ptr)
				:Src(type)
				,m_buf()
				,m_src(&m_null)
				,m_null()
			{
				buffer_all(ptr);
			}
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

			// Set the source. Note, doesn't reset any buffered data
			void Source(Src& src)
			{
				m_type = src.Type();
				m_src = &src;
			}

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

			// Read all the data from 'm_src' into the buffer
			void buffer_all() const
			{
				buffer_all(*m_src);
			}

			// Read all data from 'ptr' into the buffer
			template <typename TPtr> void buffer_all(TPtr& ptr) const
			{
				for (; *ptr != 0; ++ptr)
					m_buf.push_back(wchar_t(*ptr));
			}
			template <typename TPtr> void buffer_all(TPtr const& ptr) const
			{
				auto p = ptr;
				buffer_all(p);
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
			void erase(size_t ofs = 0, size_t count = ~size_t())
			{
				auto first = std::begin(m_buf) + ofs;
				count = std::min(count, m_buf.size() - ofs);
				m_buf.erase(first, first + count);
			}

			// Return the buffered text as a string
			pr::string<value_type> str(size_t ofs = 0, size_t count = ~size_t()) const
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
				if (adv_if_match && r) pop_front(r);
				return r;
			}
		};

		#pragma region Global Functions

		// Return the hash of a single character
		inline HashValue hashfunc(wchar_t ch, HashValue r = ~HashValue())
		{
			unsigned int const prime = 16777619U;
			r ^= HashValue(ch);
			r *= prime;
			return r;
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
		template <typename String> inline HashValue Hash(String const& name, size_t ofs = 0, size_t count = ~size_t())
		{
			auto beg = std::begin(name);
			auto end = std::end(name);
			assert(ofs <= size_t(end - beg));
			assert(count == ~size_t() || count <= size_t((end - beg) - ofs)); // If given, count must be within 'name'
			return Hash(beg + ofs, beg + ofs + (count != ~size_t() ? count : size_t((end - beg) - ofs)));
		}

		// Buffer an identifier into 'buf'. 'emit' is the read position in 'buf'
		template <typename TBuf> inline bool BufferIdentifier(TBuf& buf, EmitCount& emit)
		{
			// If the buffer is currently empty, peek at the next character so that it doesn't get buffered
			if (!pr::str::IsIdentifier(emit == 0 ? *buf : buf[emit], true)) return false;
			for (++emit; pr::str::IsIdentifier(buf[emit], false); ++emit) {}
			return true;
		}

		// Buffer up to the next '\n' into 'buf'. 'emit' is the read position in 'buf'
		template <typename TBuf> inline void BufferLine(TBuf& buf, EmitCount& emit)
		{
			// If the buffer is currently empty, peek at the next character so that it doesn't get buffered
			if (pr::str::IsNewLine(emit == 0 ? *buf : buf[emit])) return;
			for (++emit; !pr::str::IsNewLine(buf[emit]); ++emit) {}
		}

		// Buffer a literal string or character into 'buf'. 'emit' is the read position in 'buf'
		// Callers should check that buf[emit] == end to verify a complete string has been read.
		template <typename TBuf> inline void BufferLiteral(TBuf& buf, EmitCount& emit, wchar_t end = buf[emit])
		{
			++emit;
			for (int esc = 0; buf[emit] && (buf[emit] != end || esc); esc = int(buf[emit] == L'\\'), ++emit) {}
		}

		// Buffer up to 'end'. If 'include_end' is false, 'end' is removed from
		// the buffer once read. 'emit' is the read position in 'buf'.
		template <typename TBuf> inline bool BufferTo(TBuf& buf, EmitCount& emit, wchar_t const* end, bool include_end)
		{
			int i = 0;
			for (; end[i]; ++emit)
			{
				auto ch = buf[emit];
				if      (ch == end[i]) ++i;
				else if (ch == end[0]) i = 1;
				else i = 0;
			}
			if (end[i] != 0) return false;
			if (!include_end) buf.erase(emit - i, i);
			return true;
		}

		// Call '++src' until 'pred' returns false
		template <typename TSrc, typename Pred> void Eat(TSrc& src, int eat_initial, int eat_final, Pred pred)
		{
			for (src += eat_initial; *src && pred(*src); ++src) {}
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
			assert(*src == '\"' || *src == L'\''); // don't call this unless 'src' is pointing at a literal string
			auto match = *src;
			auto escape = false;
			Eat(src, 1, 0, [&](wchar_t ch)
			{
				auto end = ch == match && !escape;
				escape = ch == L'\\';
				return !end;
			});
			if (*src == match) ++src; else return false;
			return true;
		}
		template <typename TSrc, typename Char> inline void EatDelimiters(TSrc& src, Char const* delim)
		{
			for (; *pr::str::FindChar(delim, *src) != 0; ++src) {}
		}
		template <typename TSrc> inline void EatLineComment(TSrc& src)
		{
			assert(*src == '/'); // don't call this unless 'src' is pointing at a line comment
			Eat(src, 2, 0, [&](wchar_t ch){ return ch != '\n'; });
		}
		template <typename TSrc> inline bool EatBlockComment(TSrc& src)
		{
			assert(*src == '/'); // don't call this unless 'src' is pointing at a block comment
			auto star = false;
			Eat(src, 2, 0, [&](wchar_t ch)
			{
				auto end = star && ch == '/';
				star = ch == '*';
				return !end;
			});
			if (*src == '/') ++src; else return false;
			return true;
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
		PRUnitTest(pr_script_checkhashes)
		{
			using namespace pr::script;
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
		PRUnitTest(pr_script_core)
		{
			using namespace pr::str;
			using namespace pr::script;

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
			{// Preload buffer
				Buffer<> buf(ESrcType::Buffered, "abcd1234");
				PR_CHECK(buf.match(L"abcd1234") != 0, true);
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
