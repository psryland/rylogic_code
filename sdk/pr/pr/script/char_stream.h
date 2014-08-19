//**********************************
// Script charactor source
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#pragma once

#include <fstream>
#include "pr/script/script_core.h"

namespace pr
{
	namespace script
	{
		// Source types
		enum class ESrcType
		{
			Unknown,
			Pointer,
			Range,
			Buffered,
			File,
			Macro,
		};

		// An interface/base class for a source of characters
		struct Src
		{
		private:
			ESrcType     m_type; // The type of source this is
			mutable char m_peek; // The last char 'peek'ed from this source
			
			Src(Src const&); // no copying
			Src& operator=(Src const&);

		public:
			virtual ~Src() {}

			// Pointer-like interface
			char operator * () const    { char ch = peek(); if (ch != m_peek) { const_cast<Src*>(this)->seek(); m_peek = peek(); } return m_peek; }
			Src& operator ++()          { if (**this) { next(); seek(); m_peek = peek(); } return *this; }
			Src& operator +=(int count) { while (count--) ++(*this); return *this; }

			// Debugging info about the source char stream
			virtual ESrcType type() const { return m_type; }
			virtual Loc  loc() const      { return Loc(); } // Returns the location within the source
			virtual void loc(Loc&)        {}                // Allow the location within the source to be set

		protected:
			// Basic stream interface
			// 'next()' moves the internal position to the next character to be returned from 'peek()'
			// 'peek()' returns the character at the current position.
			// note: 'peek()' is called far more often than 'next()'
			// 'seek()' tests the character at the current position, and if not a valid character to
			//  return from this source, advances the internal position to the next valid character.
			//  Conceptually, 'seek()' should be called before every 'peek()' call since sources that
			//  wrap other sources have no way of knowing if the wrapped source has changed. The
			//  operator *() overcomes this however, so that seek is only called when necessary.
			virtual char peek() const = 0;
			virtual void next() = 0;
			virtual void seek() {}

			Src(ESrcType type = ESrcType::Unknown)
				:m_type(type)
				,m_peek(0)
			{}
		};

		// A interface to a random access source of characters
		// Note: no 'end()' method to allow for null terminated strings
		struct SeekSrc
		{
			virtual size_t      pos()             = 0;
			virtual void        pos(size_t n)     = 0;
			virtual char const* ptr() const       = 0;
			virtual char const* begin() const     = 0;
			virtual char operator [](int i) const = 0;
		};

		// Buffering helper
		template <typename TBuf = string> struct Buffer :Src
		{
			mutable TBuf m_buf;
			Src& m_src;

			Buffer(Src& src)
				:m_buf()
				,m_src(src)
			{}

			ESrcType type() const override { return m_src.type(); }
			Loc  loc() const override      { return m_src.loc(); }
			void loc(Loc& l) override      { m_src.loc(l); }

			// Returns true if no data is buffered
			bool empty() const                { return m_buf.empty(); }
			size_t size() const               { return m_buf.size(); }
			char begin() const                { return std::begin(m_buf); }
			char end() const                  { return std::end(m_buf); }
			void clear()                      { m_buf.resize(0); }     // don't define resize() as its confusing which portion is kept (front or back?)
			void push_back(char ch)           { m_buf.push_back(ch); }
			void push_front(char ch)          { m_buf.insert(m_buf.begin(), ch); }
			void pop_back()                   { m_buf.pop_back(); }
			void pop_front()                  { m_buf.erase(m_buf.begin()); }
			void buffer(int n = 1) const      { for (; n-- > 0; m_buf.push_back(*m_src), ++m_src) {} }
			char& operator [](size_t i)       { buffer(static_cast<int>(1 + i - m_buf.size())); return m_buf[i]; }
			char  operator [](size_t i) const { buffer(static_cast<int>(1 + i - m_buf.size())); return m_buf[i]; }

			// Helpers
			pr::hash::HashValue Hash() const { return Hash::Buffer(m_buf); }
			void BufferLine(bool inc_cr)     { for (; *m_src && *m_src != '\n'; buffer()) {} if (*m_src && inc_cr) buffer(); }
			void BufferLiteralChar()         { for (bool esc = true; *m_src && (*m_src != '\'' || esc); esc = (*m_src=='\\'), buffer()) {} if (*m_src) buffer(); }
			void BufferLiteralString()       { for (bool esc = true; *m_src && (*m_src != '\"' || esc); esc = (*m_src=='\\'), buffer()) {} if (*m_src) buffer(); }
			void BufferBlockComment()        { for (char prev = 0; *m_src && !(prev == '*' && *m_src == '/'); prev = *m_src, buffer()) {} if (*m_src) buffer(); }
			void BufferIdentifier()          { for (buffer(); pr::str::IsIdentifier(*m_src, false); buffer()) {} }

			// String compare - note asymmetric, i.e. buf="abcd", str="ab", buf.match(str) == true
			template <typename Str>             bool match(Str const& str, size_t count) const { size_t i; for (i = 0; i != count && str[i] == (*this)[i]; ++i) {} return i == count; }
			template <typename Str, size_t Len> bool match(Str const (&str)[Len]) const        { return match(str, Len); }

		private:
			char peek() const override
			{
				return empty() ? *m_src : m_buf[0];
			}
			void next() override
			{
				if (empty()) ++m_src;
				else pop_front();
			}
		};

		// A char stream that records a history of the characters that pass through it
		template <size_t Len> struct History :Src
		{
		private:
			mutable pr::Queue<char, Len+1> m_hist;
			Src& m_src;

		public:
			History(Src& src)
				:Src()
				,m_hist()
				,m_src(src)
			{
				m_hist.push_back(0);
			}
			char const* history() const
			{
				m_hist.canonicalise();
				return &m_hist[0];
			}

			ESrcType type() const override { return m_src.type(); }
			Loc  loc() const override      { return m_src.loc(); }
			void loc(Loc& l) override      { m_src.loc(l); }

		protected:
			char peek() const override { return *m_src; }
			void next() override
			{
				m_hist.back() = *m_src;
				m_hist.push_back_overwrite(0);
				++m_src;
			}
		};

		// A char source formed from a pointer to a null terminated string
		struct PtrSrc :Src ,SeekSrc
		{
		private:
			char const* m_ptr;   // The pointer to the current position in the input string
			char const* m_begin; // The pointer to the input string
			Loc*        m_loc;   // The location within the string

		public:
			explicit PtrSrc(char const* str, Loc* loc = 0)
				:Src(ESrcType::Pointer)
				,m_ptr(str)
				,m_begin(str)
				,m_loc(loc)
			{}

			Loc  loc() const override  { return m_loc ? *m_loc : Loc(); }
			void loc(Loc& l) override  { if (m_loc) *m_loc = l; }

			size_t      pos() override             { return static_cast<size_t>(m_ptr - m_begin); }
			void        pos(size_t n) override     { m_ptr = m_begin + n; }
			char const* ptr() const override       { return m_ptr; }
			char const* begin() const override     { return m_begin; }
			char operator [](int i) const override { return m_begin[i]; }

		protected:
			char peek() const override { return *m_ptr; }
			void next() override
			{
				if (m_loc) m_loc->inc(*m_ptr);
				++m_ptr;
			}
			void seek() override
			{
				// Handle line continuations
				for (; peek(); )
				{
					if (*m_ptr == '\\' && *(m_ptr+1) == '\n')                       { next(); next(); continue; }
					if (*m_ptr == '\\' && *(m_ptr+1) == '\r' && *(m_ptr+2) == '\n') { next(); next(); next(); continue; }
					break;
				}
			}
		};

		// A range of chars not necessarily terminated by a null
		struct RangeSrc :Src ,SeekSrc
		{
		private:
			char const* m_ptr;   // The pointer to the current position in the input string
			char const* m_begin; // The pointer to the input string
			char const* m_end;   // The end of the range
			Loc*        m_loc;   // The location within the string

		public:
			explicit RangeSrc(char const* begin, char const* end, Loc* loc = 0)
				:Src(ESrcType::Range)
				,m_ptr(begin)
				,m_begin(begin)
				,m_end(end)
				,m_loc(loc)
			{}

			Loc  loc() const override { return m_loc ? *m_loc : Loc(); }
			void loc(Loc& l) override { if (m_loc) *m_loc = l; }

			size_t      pos() override             { return static_cast<size_t>(m_ptr - m_begin); }
			void        pos(size_t n) override     { m_ptr = m_begin + n; }
			char const* ptr() const override       { return m_ptr; }
			char const* begin() const override     { return m_begin; }
			char const* end() const                { return m_end; }
			char operator [](int i) const override { return m_begin[i]; }

		protected:
			char peek() const override { return m_ptr == m_end ? 0 : *m_ptr; }
			void next() override
			{
				if (m_loc) m_loc->inc(*m_ptr);
				++m_ptr;
			}
			void seek() override
			{
				for (;peek();)
				{
					if (m_end - m_ptr >= 2 && *m_ptr == '\\' && *(m_ptr+1) == '\n')                       { next(); next(); continue; }
					if (m_end - m_ptr >= 3 && *m_ptr == '\\' && *(m_ptr+1) == '\r' && *(m_ptr+2) == '\n') { next(); next(); next(); continue; }
					break;
				}
			}
		};

		// A null terminated string char source that contains it's own buffer
		struct BufferedSrc :Src ,SeekSrc
		{
			string m_str;  // The buffer
			size_t m_idx;  // The index position in the buffer
			Loc*   m_loc;  // The location within the buffer

			explicit BufferedSrc(Loc* loc = 0)
				:Src(ESrcType::Buffered)
				,m_str()
				,m_idx(0)
				,m_loc(loc)
			{}

			Loc  loc() const override { return m_loc ? *m_loc : Loc(); }
			void loc(Loc& l) override { if (m_loc) *m_loc = l; }

			size_t      pos() override             { return m_idx; }
			void        pos(size_t n) override     { m_idx = n; }
			char const* ptr() const override       { return begin() + m_idx; }
			char const* begin() const override     { return m_str.c_str(); }
			char const* end() const                { return begin() + m_str.size(); }
			char operator [](int i) const override { return m_str[i]; }

		protected:
			char peek() const override { return *(m_str.c_str() + m_idx); }
			void next() override
			{
				if (m_loc) m_loc->inc(m_str[m_idx]);
				++m_idx;
			}
			void seek() override
			{
				for (;peek();)
				{
					if (m_str.size() - m_idx >= 2 && m_str[m_idx] == '\\' && m_str[m_idx+1] == '\n')                           { next(); next(); continue; }
					if (m_str.size() - m_idx >= 3 && m_str[m_idx] == '\\' && m_str[m_idx+1] == '\r' && m_str[m_idx+2] == '\n') { next(); next(); next(); continue; }
					break;
				}
			}
		};

		// A file char source
		struct FileSrc0 :Src
		{
		private:
			Buf8 m_buf;                   // A short buffer read from the file
			mutable std::ifstream m_file; // The file stream source
			Loc* m_loc;                   // The location within the file

		public:
			explicit FileSrc0(char const* filepath, Loc* loc = 0)
				:Src(ESrcType::File)
				,m_buf()
				,m_file(filepath)
				,m_loc(loc)
			{
				for (int i = 0; i != 8; ++i)
					m_buf.push_back(fget());
			}
			bool is_open() const
			{
				return m_file.is_open();
			}

			Loc  loc() const override { return m_loc ? *m_loc : Loc(); }
			void loc(Loc& l) override { if (m_loc) *m_loc = l; }

		protected:
			char fget()
			{
				int ch = m_file.get();
				return m_file.good() ? (char)ch : 0;
			}

			char peek() const override { return m_buf[0]; }
			void next() override
			{
				if (m_loc) m_loc->inc(m_buf[0]);
				m_buf.shift(fget());
			}
			void seek() override
			{
				for (;peek();)
				{
					if (m_buf[0] == '\\' && m_buf[1] == '\n')                     { next(); next(); continue; }
					if (m_buf[0] == '\\' && m_buf[1] == '\r' && m_buf[2] == '\n') { next(); next(); next(); continue; }
					break;
				}
			}
		};

		// A file source that includes a 'Loc'
		struct FileSrc :FileSrc0
		{
			Loc m_file_loc;
			explicit FileSrc(char const* filepath)
				:FileSrc0(filepath, &m_file_loc)
				,m_file_loc(filepath, 0, 0)
			{}
		};

		// A char stream that transforms chars via a callback function
		struct TxfmSrc :Src
		{
			typedef int (*Func)(int ch); // Transform function (e.g. ::tolower, ::toupper, etc)

		private:
			Src& m_src;
			Func m_txfm;

		public:
			TxfmSrc(Src& src, Func txfm = 0)
				:m_src(src)
				,m_txfm(txfm ? txfm : nochange)
			{}
			void set_transform(Func txfm)
			{
				m_txfm = txfm ? txfm : nochange;
			}

			ESrcType type() const override { return m_src.type(); }
			Loc  loc() const override      { return m_src.loc(); }
			void loc(Loc& l) override      { m_src.loc(l); }

		protected:
			static int nochange(int ch) { return ch; }
			char peek() const override { return static_cast<char>(m_txfm(*m_src)); }
			void next() override       { ++m_src; }
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_char_stream)
		{
			using namespace pr;
			using namespace pr::script;

			{//StringSrc
				char const* str = "This is a stream of characters\n";
				PtrSrc src(str);
				for (;*str; ++str, ++src)
					PR_CHECK(*src, *str);
				PR_CHECK(*src, 0);
			}
			{//FileSrc
				char const* str = "This is a stream of characters\n";
				char const* filepath = "test_file_source.pr_script";
				{
					std::ofstream file(filepath);
					file << str;
				}
				{
					FileSrc src(filepath);
					for (; *str; ++str, ++src)
						PR_CHECK(*src, *str);
					PR_CHECK(*src, 0);
				}
				{
					pr::filesys::EraseFile<std::string>(filepath);
					PR_CHECK(!pr::filesys::FileExists(filepath), true);
				}
			}
			{//Buffer
				char const* str1 = "1234567890";
				PtrSrc src(str1);

				Buffer<> buf(src);                      PR_CHECK(buf.empty(), true);
				PR_CHECK(*buf  , '1');                  PR_CHECK(buf.empty(), true);
				PR_CHECK(buf[0], '1');                  PR_CHECK(buf.size(), 1U);
				PR_CHECK(buf[1], '2');                  PR_CHECK(buf.size(), 2U);
				PR_CHECK(buf.match(str1, 4), true);     PR_CHECK(buf.size(), 4U);

				++buf;                                  PR_CHECK(buf.size(), 3U);
				PR_CHECK(*buf  , '2');                  PR_CHECK(buf.size(), 3U);
				PR_CHECK(buf[0], '2');                  PR_CHECK(buf.size(), 3U);
				PR_CHECK(buf[1], '3');                  PR_CHECK(buf.size(), 3U);
				PR_CHECK(buf.match(&str1[1], 4), true); PR_CHECK(buf.size(), 4U);
				PR_CHECK(!buf.match("235"), true);      PR_CHECK(buf.size(), 4U);

				buf += 4;                               PR_CHECK(buf.empty(), true);
				PR_CHECK(!buf.match("6780"), true);     PR_CHECK(buf.size(), 4U);
			}
			{//History
				char const* str_in = "12345678";
				PtrSrc src(str_in);
				History<4> hist(src);   PR_CHECK(pr::str::Equal(hist.history(), "")    , true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "1")   , true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "12")  , true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "123") , true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "1234"), true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "2345"), true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "3456"), true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "4567"), true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "5678"), true);
				++hist;                 PR_CHECK(pr::str::Equal(hist.history(), "5678"), true); // ++hist doesn't call next(), so the history isn't changed
			}
			{//TxfmSrc
				char const* str_in = "CaMeLCasE";
				char const* str_lwr = "camelcase";
				char const* str_upr = "CAMELCASE";
				{ // no change
					PtrSrc src(str_in);
					TxfmSrc nch(src);
					for (char const* out = str_in; *out; ++nch, ++out)
						PR_CHECK(*nch, *out);
					PR_CHECK(*nch, 0);
				}
				{ // lower case
					PtrSrc src(str_in);
					TxfmSrc lwr(src, ::tolower);
					for (char const* out = str_lwr; *out; ++lwr, ++out)
						PR_CHECK(*lwr, *out);
					PR_CHECK(*lwr, 0);
				}
				{ // upper case
					PtrSrc src(str_in);
					TxfmSrc upr(src);
					upr.set_transform(::toupper);
					for (char const* out = str_upr; *out; ++upr, ++out)
						PR_CHECK(*upr, *out);
					PR_CHECK(*upr, 0);
				}
			}
		}
	}
}
#endif
