//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/location.h"
#include "pr/script/fail_policy.h"
#include "pr/script/buf8.h"
#pragma warning(disable:4355) // this used in base member initializer list

namespace pr::script
{
	// Base class for a stream of characters
	struct Src
	{
		// Notes:
		//  - The source is exhausted when 'Peek' returns '\0'
		//    *Careful*, only Read() returns EOF, not *src.
		//  - This class supports local buffering (through ReadAhead)
		//  - Don't make 'Read' public, that would by-pass the local buffering.
		//  - Windows strings (using wchar_t) are actually UTF-16 encoded.
		//  - The stream operates on single characters so they have to be a fixed width.
		//    Use wchar_t so that most characters are covered, any beyond that will have
		//    to be treated as encoding errors.
		//  - Src can also wrap another stream, 

		static int const EOS = -1; // end of stream

	protected:

		// A reference to a wrapped source (or this). This member should not generally be
		// used by 'Src' itself, its mainly a convenience for types that wrap a 'Src'.
		Src& m_src;

		// A local read ahead buffer
		string_t m_buffer;

		// Encoding of data returned from 'Read()'
		EEncoding m_enc;

		// Multi-byte read state
		std::mbstate_t m_mb;

		// Stream position (i.e. position of m_buffer[0])
		Loc m_loc;

		// Remaining characters to read.
		int64_t m_remaining;

		#if PR_DBG
		BufW8 m_history;
		#endif

	protected:

		// Constructor for character sources
		Src(EEncoding enc, Loc const& loc = Loc()) noexcept
			:m_src(*this)
			,m_buffer()
			,m_enc(enc)
			,m_mb()
			,m_loc(loc)
			,m_remaining(std::numeric_limits<int64_t>::max())
			#if PR_DBG
			,m_history()
			#endif
		{}

		// Constructor for source wrappers
		Src(Src& wrapped, EEncoding enc) noexcept
			:m_src(wrapped)
			,m_buffer()
			,m_enc(enc)
			,m_mb()
			,m_loc()
			,m_remaining(std::numeric_limits<int64_t>::max())
			#if PR_DBG
			,m_history()
			#endif
		{}

		// Return the next byte or decoded character from the underlying stream.
		// The interpretation of what is returned from 'Read()' is based on 'm_enc'.
		// For all encodings except 'already_decode', the 'ReadAhead()' function will assume 'Read()'
		// returns bytes and convert encodings to utf-16. For these sources, returning EOS is needed
		// because 0 maybe a valid byte in the encoding. If 'm_enc' is 'already_decoded' the 'Read()'
		// is assumed to return already decoded utf-16 characters and so should never see an 'EOS'.
		virtual int Read() = 0;

	public:

		virtual ~Src() {}

		// Move only
		Src(Src&& rhs) noexcept
			:m_src(&rhs == &rhs.m_src ? *this : rhs.m_src)
			,m_buffer(std::move(rhs.m_buffer))
			,m_enc(rhs.m_enc)
			,m_mb(rhs.m_mb)
			,m_loc(rhs.m_loc)
		{}
		Src(Src const& rhs) = delete;

		// The current position in the root underlying source
		virtual Loc Location() const noexcept
		{
			auto s = this;
			for (; s != &m_src; s = &s->m_src) { }
			return s->m_loc;
		}

		// A local cache of characters read from the source
		string_t const& Buffer() const noexcept
		{
			return m_buffer;
		}
		string_t& Buffer() noexcept
		{
			return m_buffer;
		}

		// Buffer up to (start + count) and return a string view within this range
		string_view_t Buffer(int start, int count)
		{
			auto len = ReadAhead(start + count);
			if (len < start + count)
				throw ScriptException(EResult::UnexpectedEndOfFile, Location(), Fmt("Could not buffer %d characters. End of stream reached", start + count));

			auto str = m_buffer.data();
			return string_view_t(str + start, count);
		}

		// Get/Set the maximum number of characters to emit from this stream (can be less than that underlying source length)
		int64_t Limit() const
		{
			return m_remaining;
		}
		void Limit(int64_t remaining)
		{
			m_remaining = remaining >= 0 ? remaining : std::numeric_limits<int64_t>::max();
		}

		// Peek
		char_t operator *()
		{
			return (*this)[0];
		}

		// Read ahead array access
		char_t operator[](int i)
		{
			ReadAhead(i + 1);
			return i < s_cast<int>(m_buffer.size()) ? m_buffer[i] : '\0';
		}

		// Advance by 1 character
		Src& operator ++()
		{
			Next();
			return *this;
		}

		// Advance by n characters
		Src& operator += (int n)
		{
			Next(n);
			return *this;
		}

		// Increment to the next character
		void Next(int n = 1)
		{
			if (n < 0)
				throw std::runtime_error("Cannot seek backwards");

			for (;;)
			{
				// Consume from the buffered characters first
				auto remove = std::min(n, static_cast<int>(m_buffer.size()));
				for (int i = 0; i != remove; ++i)
				{
					#if PR_DBG
					m_history.shift(m_buffer[i]);
					#endif
					m_loc.inc(m_buffer[i]);
				}

				m_buffer.erase(0, remove);
				m_remaining -= remove;
				n -= remove;
				if (n == 0)
					break;

				// Buffer and dump, since we need to read whole characters from the underlying source
				if (ReadAhead(std::min(n, 4096)) == 0)
					break;
			}
		}

		// Attempt to buffer 'n' characters locally. Less than 'n' characters can be buffered if EOS is hit.
		// Returns the number of characters actually buffered (a value in the range [0, n])
		int ReadAhead(int n)
		{
			assert(m_remaining >= 0);
			if (n > m_remaining) n = static_cast<int>(m_remaining);
			for (; n > s_cast<int>(m_buffer.size());)
			{
				// Ensure 'Buffer's length grows with each loop
				auto const count = m_buffer.size();

				// Read the next complete character from the underlying stream
				int ch = '\0';
				switch (m_enc)
				{
				default:
					{
						throw std::runtime_error(FmtS("Unsupported character encoding: %d", m_enc));
					}
				case EEncoding::already_decoded:
					{
						ch = Read();
						if (ch == EOS) throw std::runtime_error("Read() should not return EOS for 'already_decoded' streams.");
						break;
					}
				case EEncoding::ascii:
					{
						auto const b = Read();
						if (b == EOS) break;
						if (b > 127) throw ScriptException(EResult::WrongEncoding, Location(), Fmt(L"Source is not an ASCII character stream. Invalid character with value %d found", b));
						ch = b;
						break;
					}
				case EEncoding::utf8:
					{
						// Interpret a multi byte UTF8 character
						for (auto mb = std::mbstate_t{};;)
						{
							// Read a byte at a time until a complete multibyte character is found (or EOS)
							auto const b = Read();
							if (b == EOS) break;

							auto c = char(b);
							char16_t c16 = 0;
							auto const r = std::mbrtoc16(&c16, &c, 1, &mb);
							if (r == static_cast<size_t>(-1)) throw ScriptException(EResult::WrongEncoding, Location(), "UTF-8 encoding error in source character stream");
							if (r == static_cast<size_t>(-2)) continue;
							ch = c16;
							break;
						}
						break;
					}
				case EEncoding::utf16_le:
					{
						int lo, hi;
						if ((lo = Read()) == EOS) break;
						if ((hi = Read()) == EOS) break;
						ch = (static_cast<uint8_t>(hi) << 8) | static_cast<uint8_t>(lo);
						if (ch < 0 || ch > UnicodeMaxValue) throw ScriptException(EResult::WrongEncoding, Location(), Fmt("Unsupported UTF-16 encoding. Value %d is out of range", ch));
						break;
					}
				case EEncoding::utf16_be:
					{
						int lo, hi;
						if ((hi = Read()) == EOS) break;
						if ((lo = Read()) == EOS) break;
						ch = (static_cast<uint8_t>(hi) << 8) | static_cast<uint8_t>(lo);
						if (ch < 0) throw ScriptException(EResult::WrongEncoding, Location(), Fmt("Unsupported UTF-16 encoding. Value %d is out of range", ch));
						break;
					}
				}

				// Buffer the read character
				if (ch != '\0')
				{
					assert(ch != EOS);
					m_buffer.append(1, s_cast<char_t>(ch));
					continue;
				}
				
				// Stop reading if the buffer isn't growing
				if (m_buffer.size() <= count)
					break;
			}
			return std::min(n, s_cast<int>(m_buffer.size()));
		}

		// String compare. Note: asymmetric, i.e. src="abcd", str="ab", src.Match(str) == true
		bool Match(string_view_t str) { return Match(str, 0); }
		bool Match(string_view_t str, int start) { return Match(str, start, int(str.size())); }
		bool Match(string_view_t str, int start, int count)
		{
			// 'start' is where to start looking in the buffer
			// 'count' is the number of characters to compare.
			if (count > s_cast<int>(str.size()))
				throw std::runtime_error(Fmt("Src.Match comparing % characters but match string length is only %d", count, s_cast<int>(str.size())));

			auto len = ReadAhead(start + count);
			if (len < start + count)
				return false;

			int i = start;
			int const iend = start + count;
			for (; i != iend && str[size_t(i) - start] == m_buffer[i]; ++i) { }
			return i == iend;
		}

		// String compare and consume if matched
		bool Match(string_view_t str, bool consume)
		{
			if (!Match(str)) return false;
			if (consume) Next(int(str.size()));
			return true;
		}
		
		// Buffer and hash characters on the range [start, start + count)
		int Hash(int start, int count)
		{
			auto len = ReadAhead(start + count);
			if (len < start + count)
				throw ScriptException(EResult::UnexpectedEndOfFile, Location(), Fmt("Could not buffer %d characters. End of stream reached", start + count));

			auto str = m_buffer.c_str();
			return hash::Hash(str + start, str + start + count);
		}

		// Read 'count' characters from the string and return them as a string
		string_t ReadN(int count)
		{
			string_t sb; sb.reserve(count);
			int i = 0;

			// Copy from the buffer in one block
			auto buffered = std::min(count, s_cast<int>(m_buffer.size()));
			sb.assign(m_buffer, 0, buffered);
			Next(buffered);
			i += buffered;

			// Fill the remaining from the underlying source
			for (char_t ch; i != count && (ch = **this) != '\0'; ++i, Next()) sb.append(1, ch);
			if (i != count) throw ScriptException(EResult::UnexpectedEndOfFile, Location(), Fmt("Could not read %d characters. End of stream reached", count));
			return sb;
		}

		// Reads all characters from the src and returns them as one string.
		string_t ReadToEnd()
		{
			string_t sb; sb.reserve(4096);

			// Copy from the buffer in one block
			auto buffered = s_cast<int>(m_buffer.size());
			sb.assign(m_buffer, 0, buffered);
			Next(buffered);

			// Fill the remaining from the underlying source
			for (char_t ch; (ch = **this) != '\0'; Next()) sb.append(1, ch);
			return sb;
		}

		// Reads characters up to and including a new-line. A new-line is a carriage return ('\r'),
		// a line feed ('\n'), or a carriage return immediately followed by a line feed ('\r\n').
		// If 'include_newline' is true, the returned string will include the newline character(s).
		string_t ReadLine(bool include_newline)
		{
			string_t sb; sb.reserve(256);
			for (char_t ch; (ch = **this) != '\0'; Next())
			{
				if (ch != '\r' && ch != '\n')
				{
					sb.append(1, ch);
					continue;
				}

				if (include_newline) sb.append(1, ch);
				Next();

				if (ch == '\r' && (ch = **this) == '\n')
				{
					if (include_newline) sb.append(1, ch);
					Next();
				}

				break;
			}
			return sb;
		}
	};

	// An empty source
	struct NullSrc :Src
	{
	protected:

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() noexcept override
		{
			return '\0';
		}

	public:

		NullSrc() noexcept
			:Src(EEncoding::already_decoded, m_loc)
		{}
	};

	// A string source
	struct StringSrc :Src
	{
		// Note:
		//  - StringSrc only returns bytes so should *NOT* use the 'already_decoded' encoding.
		//  - A useful techique is to default construct a StringSrc then push text into its buffer.
		//    When the buffer is empty, 'Read()' will return EOS. Technically, the same would work
		//    with 'NullSrc', but that's likely to be confusing.
		enum class EFlags
		{
			None = 0,
			BufferLocally = 1 << 0,
			_bitwise_operators_allowed,
		};

	protected:

		char const* m_ptr;
		char const* m_end;

		// Return the next byte or decoded character from the underlying stream.
		int Read() noexcept override
		{
			// We're returning bytes. Already decoded would mean we're returning utf-16 char_t's
			assert(m_enc != EEncoding::already_decoded);
			return m_ptr != m_end ? *m_ptr++ : EOS;
		}

		// Read all data into the local buffer
		template <typename Char>
		void BufferLocally(std::basic_string_view<Char> str)
		{
			// Special case for already decoded strings
			if constexpr (std::is_same_v<std::basic_string_view<Char>, string_view_t>)
			{
				m_ptr = nullptr;
				m_end = nullptr;
				m_buffer.assign(str);
			}
			else
			{
				m_ptr = char_ptr(str.data());
				m_end = char_ptr(str.data() + str.size());
				ReadAhead(s_cast<int>(str.size()));
			}
		}

	public:

		explicit StringSrc(EEncoding enc = EEncoding::utf16_le, Loc const& loc = Loc())
			:Src(enc, loc)
			,m_ptr(nullptr)
			,m_end(nullptr)
		{}
		explicit StringSrc(std::string_view str, EFlags flags = EFlags::None, EEncoding enc = EEncoding::utf8, Loc const& loc = Loc())
			:Src(enc, loc)
			,m_ptr(str.data())
			,m_end(str.data() + str.size())
		{
			if (AllSet(flags, EFlags::BufferLocally))
				BufferLocally(str);
		}
		explicit StringSrc(std::wstring_view str, EFlags flags = EFlags::None, EEncoding enc = EEncoding::utf16_le, Loc const& loc = Loc())
			:Src(enc, loc)
			,m_ptr(char_ptr(str.data()))
			,m_end(char_ptr(str.data() + str.size()))
		{
			// Make a local copy of 'str' in the buffer
			if (AllSet(flags, EFlags::BufferLocally))
				BufferLocally(str);
		}
	};

	// A file source
	struct FileSrc :Src
	{
	protected:

		// The open file stream
		std::ifstream m_file;

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() override
		{
			auto ch = m_file.get();
			return m_file.good() ? ch : EOS;
		}

	public:

		FileSrc(std::filesystem::path const& filepath, std::streamsize ofs = 0, EEncoding enc = EEncoding::auto_detect, Loc* loc = nullptr)
			:Src(enc, Loc())
			,m_file()
		{
			if (!filepath.empty())
				Open(filepath, ofs, enc, loc);
		}

		// Open a file as a stream source
		void Open(std::filesystem::path const& filepath, std::streamsize ofs = 0, EEncoding enc = EEncoding::auto_detect, Loc* loc = nullptr)
		{
			Close();

			// Determine file encoding, look for the BOM in the first 3 bytes
			m_enc = enc;
			auto bom_size = 0;
			if (m_enc == EEncoding::auto_detect)
			{
				std::array<unsigned char, 3> bom;
				std::ifstream file(filepath, std::ios::binary);
				auto read = file.good() ? file.read(reinterpret_cast<char*>(bom.data()), bom.size()).gcount() : 0;
				if      (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) { m_enc = EEncoding::utf8;     bom_size = 3; }
				else if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)                   { m_enc = EEncoding::utf16_le; bom_size = 2; }
				else if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)                   { m_enc = EEncoding::utf16_be; bom_size = 2; }
				else                                                                      { m_enc = EEncoding::utf8;     bom_size = 0; } // If no valid bom is found, assume UTF-8
			}

			// Open the input file stream
			m_file.open(filepath, std::ios::binary);
			if (!m_file.good())
				throw ScriptException(EResult::FileNotFound, Loc(filepath), std::wstring(L"Failed to open file ").append(filepath));

			// Seek to the offset position
			m_file.seekg(bom_size + ofs);

			// Update the location
			m_loc = loc ? *loc : Loc(filepath, bom_size + ofs, 1, 1, ofs == 0);
		}

		// Close the file stream
		void Close()
		{
			m_file.close();
			m_enc = EEncoding::auto_detect;
			m_loc = Loc();
		}
	};

	// A wrapped source with truncated length
	struct WrapSrc :Src
	{
		// Notes:
		//  - This source type is useful for creating a subrange of an existing source

	protected:

		int m_count;

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() override
		{
			if (m_count == 0)
				return 0;

			auto ch = *m_src;
			++m_src;
			--m_count;
			return ch;
		}

	public:

		WrapSrc(Src& src, int count = std::numeric_limits<int>::max())
			:Src(src, EEncoding::already_decoded)
			,m_count(count)
		{}
	};

	// Call '++src' until 'pred' returns false.
	// 'eat_initial' and 'eat_final' are the number of characters to consume before
	// applying the predicate 'pred' and the number to consume after 'pred' returns false.
	template <typename TSrc, typename Char> inline void EatDelimiters(TSrc& src, Char const* delim)
	{
		for (; *str::FindChar(delim, *src) != 0; ++src) {}
	}
	template <typename TSrc, typename Pred> void Eat(TSrc& src, int eat_initial, int eat_final, Pred pred)
	{
		for (src += eat_initial; *src && pred(src); ++src) {}
		src += eat_final;
	}
	template <typename TSrc> inline void EatLineSpace(TSrc& src, int eat_initial, int eat_final)
	{
		Eat(src, eat_initial, eat_final, [](TSrc& s) { return str::IsLineSpace(*s); });
	}
	template <typename TSrc> inline void EatWhiteSpace(TSrc& src, int eat_initial, int eat_final)
	{
		Eat(src, eat_initial, eat_final, [](TSrc& s) { return str::IsWhiteSpace(*s); });
	}
	template <typename TSrc> inline void EatLine(TSrc& src, int eat_initial, int eat_final)
	{
		Eat(src, eat_initial, eat_final, [](TSrc& s){ return !(*s == '\n') && !(s[0] == '\r' && s[1] == '\n'); });
	}
	template <typename TSrc> inline void EatBlock(TSrc& src, string_view_t block_beg, string_view_t block_end)
	{
		if (block_beg.empty())
			throw std::runtime_error("The block start marker cannot have length = 0");
		if (block_end.empty())
			throw std::runtime_error("The block end marker cannot have length = 0");
		if (!src.Match(block_beg))
			throw std::runtime_error("Don't call 'EatBlock' unless 'src' is pointing at the block start");

		Eat(src, static_cast<int>(block_beg.size()), static_cast<int>(block_end.size()), [=](TSrc& s) { return !s.Match(block_end); });
	}
	template <typename TSrc> inline void EatLiteral(TSrc& src, Loc const& loc = Loc())
	{
		// Don't call this unless 'src' is pointing at a literal string
		auto quote = *src;
		if (quote != '\"' && quote != '\'')
			throw ScriptException(EResult::InvalidString, loc, Fmt(L"Expected the start of a string literal, but the next character is: %c", quote));

		for (bool esc = true; *src != '\0' && (esc || *src != quote); esc = !esc && *src == '\\', ++src) {}
		if (*src == quote) ++src; else throw ScriptException(EResult::InvalidString, loc, "Incomplete literal string or character");
	}
	template <typename TSrc> inline void EatLineComment(TSrc& src, string_view_t line_comment = "//")
	{
		assert(*src == line_comment[0]); // don't call this unless 'src' is pointing at a line comment
		EatLine(src, static_cast<int>(line_comment.size()), 0);
	}
	template <typename TSrc> inline void EatBlockComment(TSrc& src, string_view_t block_beg = "/*", string_view_t block_end = "*/")
	{
		assert(*src == block_beg[0]); // don't call this unless 'src' is pointing at a block comment
		EatBlock(src, block_beg, block_end);
	}

	// Buffer an identifier in 'src'. Returns true if a valid identifier was buffered.
	// On return, 'len' contains the length of the buffer up to and including the end
	// of the identifier (i.e. start + strlen(identifier)).
	inline bool BufferIdentifier(Src& src, int start = 0, int* len = nullptr)
	{
		auto i = start;
		auto x = AtExit([&]{ if (len) *len = i; });
		if (!str::IsIdentifier(src[i], true)) return false;
		for (++i; str::IsIdentifier(src[i], false); ++i) {}
		return true;
	}

	// Buffer a literal string or character in 'src'. Returns true if a complete literal string or
	// character was buffered. On return, 'len' contains the length of the buffer up to, and
	// including, the literal. (i.e. start + strlen(literal))
	inline bool BufferLiteral(Src& src, int start = 0, int* len = nullptr)
	{
		auto i = start;
		auto x = AtExit([&]{ if (len) *len = i; });

		// Don't call this unless 'src' is pointing at a literal string
		auto quote = src[i];
		if (quote != '\"' && quote != '\'')
			return false;

		// Find the end of the literal
		for (bool esc = true; src[i] != '\0' && (esc || src[i] != quote); esc = !esc && src[i] == '\\', ++i) {}
		if (src[i] == quote) ++i; else return false;
		return true;
	}

	// Buffer characters for a number (real or int) in 'src'.
	// Format: [delim][{+|-}][0[{x|X|b|B}]][digits][.digits][{d|D|e|E|p|P}[{+|-}]digits][U][L][L]
	// Returns true if valid number characters where buffered
	inline bool BufferNumber(Src& src, int& radix, int start = 0, int* len = nullptr, str::ENumType type = str::ENumType::Any)
	{
		// Notes:
		//  - This duplicates the BufferNumber function in pr::str :-/
		//    The pr::str version consumes characters from a stream where as this version
		//    simply buffers the characters in 'src'. I don't want pr::str to depend on pr::script
		//    and I don't want to change the behaviour of the pr::str version, so duplication is
		//    the only option.

		auto i = start;
		auto x = AtExit([&] { if (len) *len = i; });

		// Convert a character to it's numerical value
		static auto digit = [](int ch)
		{
			if (ch >= '0' && ch <= '9') return ch - '0';
			if (ch >= 'a' && ch <= 'z') return 10 + ch - 'a';
			if (ch >= 'A' && ch <= 'Z') return 10 + ch - 'A';
			return std::numeric_limits<int>::max();
		};

		auto digits_found = false;
		auto allow_fp = AllSet(type, str::ENumType::FP);
		auto fp = false;

		// Look for the optional sign character
		// Ideally we'd prefer not to advance 'src' passed the '+' or '-' if the next
		// character is not the start of a number. However doing so means 'src' can't
		// be a forward only input stream. Therefore, I'm pushing the responsibility
		// back to the caller, they need to check that if *src is a '+' or '-' then
		// the following char is a decimal digit.
		if (src[i] == '+' || src[i] == '-')
			++i;

		// Look for a radix prefix on the number, this overrides 'radix'.
		// If the first digit is zero, then the number may have a radix prefix.
		// '0x' or '0b' must have at least one digit following the prefix
		// Adding 'o' for octal, in addition to standard C literal syntax
		if (src[i] == '0')
		{
			++i;
			auto radix_prefix = false;
			if (false) { }
			else if (tolower(src[i]) == 'x') { radix = 16; ++i; radix_prefix = true; }
			else if (tolower(src[i]) == 'o') { radix = 8; ++i; radix_prefix = true; }
			else if (tolower(src[i]) == 'b') { radix = 2; ++i; radix_prefix = true; }
			else
			{
				// If no radix prefix is given, then assume octal zero (for conformance with C) 
				if (radix == 0) radix = str::IsDigit(src[i]) ? 8 : 10;
				digits_found = true;
			}

			// Check for the required integer
			if (radix_prefix && digit(src[i]) >= radix)
				return false;
		}
		else if (radix == 0)
		{
			radix = 10;
		}

		// Read digits up to a delimiter, decimal point, or digit >= radix.
		auto assumed_fp_len = 0; // the length of the number when we first assumed a FP number.
		for (; src[i] != 0; ++i)
		{
			// If the character is greater than the radix, then assume a FP number.
			// e.g. 09.1 could be an invalid octal number or a FP number. 019 is assumed to be FP.
			auto d = digit(src[i]);
			if (d < radix)
			{
				digits_found = true;
				continue;
			}

			if (radix == 8 && allow_fp && d < 10)
			{
				if (assumed_fp_len == 0) assumed_fp_len = i;
				continue;
			}

			break;
		}

		// If we're assuming this is a FP number but no decimal point is found,
		// then truncate the string at the last valid character given 'radix'.
		// If a decimal point is found, change the radix to base 10.
		if (assumed_fp_len != 0)
		{
			if (src[i] == '.') radix = 10;
			else i = assumed_fp_len;
		}

		// FP numbers can be in dec or hex, but not anything else...
		allow_fp &= radix == 10 || radix == 16;
		if (allow_fp)
		{
			// If floating point is allowed, read a decimal point followed by more digits, and an optional exponent
			if (src[i] == '.' && str::IsDecDigit(src[++i]))
			{
				fp = true;
				digits_found = true;

				// Read decimal digits up to a delimiter, sign, or exponent
				for (; str::IsDecDigit(src[i]); ++i) { }
			}

			// Read an optional exponent
			auto ch = tolower(src[i]);
			if (ch == 'e' || ch == 'd' || (ch == 'p' && radix == 16))
			{
				++i;

				// Read the optional exponent sign
				if (src[i] == '+' || src[i] == '-')
					++i;

				// Read decimal digits up to a delimiter, or suffix
				for (; str::IsDecDigit(src[i]); ++i) { }
			}
		}

		// Read the optional number suffixes
		if (allow_fp && tolower(src[i]) == 'f')
		{
			fp = true;
			++i;
		}
		if (!fp && tolower(src[i]) == 'u')
		{
			++i;
		}
		if (!fp && tolower(src[i]) == 'l')
		{
			++i;
			if (tolower(src[i]) == 'l')
				++i;
		}
		return digits_found;
	}

	// Buffer up to the next '\n' in 'src'. Returns true if a new line or at least one character is buffered.
	// if 'include_newline' is false, the new line is removed from the buffer once read.
	// On return, 'len' contains the length of the buffer up to, and including the new line character (even if the newline character is removed).
	inline bool BufferLine(Src& src, bool include_newline, int start = 0, int* len = nullptr)
	{
		auto i = start;
		auto x = AtExit([&]{ if (len) *len = i; });

		if (src[i] == '\0') return false;
		for (; src[i] != '\0' && src[i] != '\n'; ++i) {}
		if (src[i] != '\0') { if (include_newline) i += 1; else src.Buffer().erase(i, 1); }
		return true;
	}

	// Buffer up to and including 'end'. If 'include_end' is false, 'end' is removed from the buffer once read.
	// On return, 'len' contains the length of the buffer up to, and including 'end' (even if end is removed).
	inline bool BufferTo(Src& src, string_view_t end, bool include_end, int start = 0, int* len = nullptr)
	{
		auto i = start;
		auto x = AtExit([&]{ if (len) *len = i; });

		for (; src[i] != '\0' && !src.Match(end, i); ++i) {}
		if (src[i] != '\0') { if (include_end) i += s_cast<int>(end.size()); else src.Buffer().erase(i, end.size()); }
		return src[i] != '\0';
	}

	// Buffer until 'adv' returns 0. Signature for AdvFunc: int(Src&,int)
	// Returns true if buffered stopped due to 'adv' returning 0.
	// On return, 'len' contains the length of the buffer up to where 'adv' returned false.
	template <typename AdvFunc, typename = std::enable_if_t<std::is_invocable_r_v<int, AdvFunc, Src&, int>>>
	inline bool BufferWhile(Src& src, AdvFunc adv, int start = 0, int* len = nullptr)
	{
		auto i = start;
		auto x = AtExit([&]{ if (len) *len = i; });

		for (auto inc = 0; src[i] != '\0' && (inc = adv(src, i)) != 0; i += inc) {}
		if (src[i] == '\0') i = s_cast<int>(src.Buffer().size());
		return src[i] != '\0';
	}

}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/scope.h"
#include "pr/str/string_core.h"
namespace pr::script
{
	PRUnitTest(CoreTests)
	{
		using namespace pr::str;

		std::filesystem::path const script_utf = L"script_utf.txt";
		auto cleanup = pr::CreateScope([]{}, [&]{ std::filesystem::remove(script_utf); });

		{// Simple buffering
			char const str[] = "123abc";
			StringSrc ptr(str);

			PR_CHECK(*ptr  , L'1');
			PR_CHECK(ptr[5], L'c');
			PR_CHECK(ptr[0], L'1');

			PR_CHECK(*(++ptr)   , L'2');
			PR_CHECK(*(ptr += 3), L'b');
			PR_CHECK(*(++ptr)   , L'c');

			PR_CHECK(*(++ptr),  0);
		}
		{// Limited source
			char const str[] = "1234567890";
			StringSrc ptr(str);
			ptr.Limit(3);

			PR_CHECK(ptr[0], L'1');
			PR_CHECK(ptr[1], L'2');
			PR_CHECK(ptr[2], L'3');
			PR_CHECK(ptr[3], L'\0');

			PR_CHECK(*ptr, L'1'); ++ptr;
			PR_CHECK(*ptr, L'2'); ++ptr;
			PR_CHECK(*ptr, L'3'); ++ptr;
			PR_CHECK(*ptr, L'\0'); ++ptr;
		}
		{// Matching
			wchar_t const str[] = L"0123456789";
			StringSrc ptr(str);

			PR_CHECK(ptr.Match(L"0123") != 0, true);
			PR_CHECK(ptr.Match(L"012345678910") != 0, false);
			ptr += 5;
			PR_CHECK(ptr.Match(L"567") != 0, true);
		}
		{// UTF8 File source

			// UTF-8 data
			unsigned char data[] = {0xef, 0xbb, 0xbf, 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd}; //' ni hao
			wchar_t str[] = {0x4f60, 0x597d};

			{// Create the file
				std::ofstream fout(script_utf, std::ios::binary);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}

			FileSrc file(script_utf);
			PR_CHECK(*file, str[0]); ++file;
			PR_CHECK(*file, str[1]); ++file;
		}
		{// UTF 16 little endian File source

			// UTF-16le data (if host system is little-endian)
			unsigned short data[] = {0xfeff, 0x4f60, 0x597d}; //' ni hao
			wchar_t str[] = {0x4f60, 0x597d};

			{// Create the file
				std::ofstream fout(script_utf, std::ios::binary);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}

			FileSrc file(script_utf);
			PR_CHECK(*file, str[0]); ++file;
			PR_CHECK(*file, str[1]); ++file;
		}
		{// UTF 16 big endian File source

			// UTF-16be data (if host system is little-endian)
			unsigned short data[] = {0xfffe, 0x604f, 0x7d59}; //' ni hao
			wchar_t str[] = {0x4f60, 0x597d};

			{// Create the file
				std::ofstream fout(script_utf, std::ios::binary);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}

			FileSrc file(script_utf);
			PR_CHECK(*file, str[0]); ++file;
			PR_CHECK(*file, str[1]); ++file;
		}
		{ // Eat functions
			{
				StringSrc src(" \t\n,Text");
				EatDelimiters(src, "\n\t ,");
				PR_CHECK(*src, 'T');
			}
			{
				StringSrc src("991239Text");
				Eat(src, 2, 1, [](auto& s) { return *s < '5'; });
				PR_CHECK(*src, 'T');
			}
			{
				StringSrc src0("01 \t \t \r\n");
				EatLineSpace(src0, 2, 0);
				PR_CHECK(*src0, '\n');

				StringSrc src1("01 \t \t \r");
				EatLineSpace(src1, 2, 1);
				PR_CHECK(*src1, '\0');
			}
			{
				StringSrc src("01 \t \t \rA");
				EatWhiteSpace(src, 2, 0);
				PR_CHECK(*src, 'A');
			}
			{
				StringSrc src0("0123456\r\nABC");
				EatLine(src0, 0, 2);
				PR_CHECK(*src0, 'A');

				StringSrc src1("0123456");
				EatLine(src1, 0, 0);
				PR_CHECK(*src1, '\0');
			}
			{
				StringSrc src("{{ block }}#");
				EatBlock(src, L"{{", L"}}");
				PR_CHECK(*src, '#');
			}
			{
				StringSrc src0("\"A \\\"string\\\" within a string\"#");
				EatLiteral(src0);
				PR_CHECK(*src0, '#');

				StringSrc src1("\"A \\\\\"#  \"@ ");
				EatLiteral(src1);
				PR_CHECK(*src1, '#');
			}
			{
				StringSrc src(";comment \r\n#");
				EatLineComment(src, L";");
				PR_CHECK(*src, '\r');
			}
			{
				StringSrc src("<!-- comment \r\n -->#");
				EatBlockComment(src, L"<!--", L"-->");
				PR_CHECK(*src, '#');
			}
		}
		{// Buffer functions
			{
				auto len = 0;
				StringSrc src("_123abc#");
				PR_CHECK(BufferIdentifier(src, 0, &len), true);
				PR_CHECK(len, 7);
				PR_CHECK(src.ReadN(len), L"_123abc");
			}
			{
				auto len = 5;
				StringSrc src("123abc#");
				PR_CHECK(BufferIdentifier(src, 0, &len), false);
				PR_CHECK(len, 0);
			}
			{
				auto len = 0;
				StringSrc src("  \"Lit\\\"er\\\"al\" ");
				PR_CHECK(BufferLiteral(src, 2, &len), true);
				PR_CHECK(len, 15);
				src += 2; len -= 2;
				PR_CHECK(src.ReadN(len), L"\"Lit\\\"er\\\"al\"");
			}
			{
				auto len = 0;
				StringSrc src("\"\\\\\"   \"");
				PR_CHECK(BufferLiteral(src, 0, &len), true);
				PR_CHECK(len, 4);
				PR_CHECK(src.ReadN(len), L"\"\\\\\"");
			}
			{
				auto len = 0;
				StringSrc src("abc\ndef");
				PR_CHECK(BufferLine(src, true, 0, &len), true);
				PR_CHECK(len, 4);
				PR_CHECK(src.ReadN(len), L"abc\n");
			}
			{
				auto len = 0;
				StringSrc src("  abc\ndef");
				PR_CHECK(BufferLine(src, false, 2, &len), true);
				PR_CHECK(len, 5);
				src += 2; len -= 2;
				PR_CHECK(src.ReadN(len), L"abc");
			}
			{
				auto len = 0;
				StringSrc src("a b\tc\nd,end;f");
				PR_CHECK(BufferTo(src, L"end", true, 0, &len), true);
				PR_CHECK(len, 11);
				PR_CHECK(src.ReadN(len), L"a b\tc\nd,end");
			}
			{
				auto len = 0;
				StringSrc src("a b\tc\nd,");
				PR_CHECK(BufferTo(src, L"end", false, 0, &len), false);
				PR_CHECK(len, 8);
				PR_CHECK(src.ReadN(len), L"a b\tc\nd,");
			}
			{
				auto len = 0;
				StringSrc src("a b\tc\nd,");
				PR_CHECK(BufferWhile(src, [](Src& s, int i) { return !s.Match(L"\tc\n", i); }, 0, &len), true);
				PR_CHECK(len, 3);
				PR_CHECK(src.ReadN(len), L"a b");
			}
			{
				auto len = 0;
				StringSrc src("abcde");
				PR_CHECK(BufferWhile(src, [](Src& s, int i) { return s[i] != 'f'; }, 0, &len), false);
				PR_CHECK(len, 5);
				PR_CHECK(src.ReadN(len), L"abcde");
			}
			{
				auto len = 0;
				StringSrc src("a_b_c_d");
				PR_CHECK(BufferWhile(src, [](Src& s, int i) { return s[i] != '_' ? 2 : 0; }, 0, &len), false);
				PR_CHECK(len, 7);
				PR_CHECK(src.ReadN(len), L"a_b_c_d");
			}
		}
	}
}
#endif
