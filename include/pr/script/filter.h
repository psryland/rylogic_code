//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/buf8.h"
#include "pr/script/fail_policy.h"
#include "pr/script/script_core.h"

namespace pr
{
	namespace script
	{
		// String/Character literal helper
		struct StringLit
		{
			wchar_t end;
			bool escaped;

			StringLit()
				:end()
				,escaped()
			{}

			// Boolean test for within a string/character literal
			operator bool() const
			{
				return end != 0;
			}

			// Processes the current character in 'src'.
			// Returns true if currently within a string/character literal
			bool inc(wchar_t ch)
			{
				// If we're currently within a literal string or character then
				// just return characters until the literal ends
				if (*this)
				{
					if (ch == end && !escaped) end = 0;
					else escaped = ch == L'\\';
				}
				else if (ch == L'\"' || ch == L'\'')
				{
					end = ch;
				}
				return *this;
			}
		};

		// Wraps BufWN<> and a reference to a character source and location.
		// This allows other code to use a Src as a pointer with automatic location and buffering support
		template <typename BufN, typename TSrc = Src> struct BufSrc :Src
		{
			TSrc* m_src; // The source character stream (used to feed TBuf)
			BufN  m_reg; // The character "shift register"

			BufSrc(TSrc& src)
				:Src(src.Type())
				,m_src(&src)
				,m_reg(src) // note, this will advance 'src'
			{}

			// Debugging helper interface
			SrcConstPtr DbgPtr() const override
			{
				return m_reg.m_ch;
			}
			Location const& Loc() const override
			{
				return m_src->Loc();
			}

			// Pointer interface
			wchar_t operator*() const override
			{
				return m_reg.front();
			}
			BufSrc& operator ++() override
			{
				m_reg.shift(**m_src);
				if (**m_src) ++*m_src;
				return *this;
			}

			// Array access into the buffer
			wchar_t operator [](size_t i) const
			{
				return m_reg[i];
			}
			wchar_t& operator [](size_t i)
			{
				return m_reg[i];
			}
		};

		// Base class for a 'Src' filter. Simple pass through filter.
		// Filters are different to actual sources because they only contain
		// a reference to the underlying source. This means they are copy constructable.
		template <typename BufWN = BufW2, typename TSrc = Src> struct Filter :Src
		{
			using BufN = BufSrc<BufWN, TSrc>;
			BufN m_reg; // N-character shift register for fast short string buffering

			Filter(TSrc& src)
				:Src(src.Type())
				,m_reg(src)
			{
				seek(0);
			}

			// Debugging helper interface
			SrcConstPtr DbgPtr() const override
			{
				return m_reg.DbgPtr();
			}
			Location const& Loc() const override
			{
				return m_reg.Loc();
			}

			// Pointer-like interface
			wchar_t operator *() const override
			{
				return *m_reg;
			}
			Filter& operator ++() override
			{
				seek(1);
				return *this;
			}

			// Array access to the buffered data. Buffer size grows to accomodate 'i'
			wchar_t operator[](size_t i) const
			{
				return m_reg[i];
			}
			wchar_t& operator [](size_t i)
			{
				return m_reg[i];
			}

			// Seek to the next valid character to output
			virtual void seek(int n)
			{
				auto& src = m_reg;
				src += n;
			}
		};

		// Removes line continuation sequences in a character stream
		template <typename TSrc = Src> struct StripLineContinuations :Filter<BufW4,TSrc>
		{
			using base = Filter<BufW4,TSrc>;

			StripLineContinuations(TSrc& src)
				:base(src)
			{
				seek(0);
			}

			// Seek to the next valid character to output
			void seek(int n) override
			{
				auto& src = m_reg;
				for (src += n;;)
				{
					if (src[0] == L'\\' && src[1] == L'\n')                    { src += 2; continue; }
					if (src[0] == L'\\' && src[1] == L'\r' && src[2] == L'\n') { src += 3; continue; }
					break;
				}
			}
		};

		// Removes C++ style comments from a character stream
		template <typename FailPolicy = ThrowOnFailure, typename TSrc = Src> struct StripComments :Filter<BufW2, TSrc>
		{
			using base = Filter<BufW2,TSrc>;
			StringLit m_literal;
			
			StripComments(TSrc& src)
				:base(src)
				,m_literal()
			{
				seek(0);
			}

			// Seek to the next valid character to output
			void seek(int n) override
			{
				auto& src = m_reg;
				for (src += n;;)
				{
					// If we're currently within a literal string or character then
					// just return characters until the literal ends
					if (m_literal.inc(*src))
						break;

					// Otherwise remove comments
					if (*src == L'/')
					{
						if (src[1] == L'/')
						{
							for (src += 2; *src != L'\n' && *src; ++src) {}
							continue; // Don't eat the new line
						}
						if (src[1] == L'*')
						{
							auto beg = src.Loc();
							for (src += 2; src[0] != L'*' && src[1] != L'/' && *src; ++src) {}
							if (*src) src += 2; else return FailPolicy::Fail(EResult::SyntaxError, src.Loc(), pr::Fmt("Unmatched block comment at:\n%s", Narrow(beg.ToString()).c_str()));
							continue;
						}
					}

					// If we get here, then the next char is valid
					break;
				}
			}
		};

		// Removes newlines from a character stream
		template <typename FailPolicy = ThrowOnFailure, typename TSrc = Src> struct StripNewLines :Filter<BufW2, TSrc>
		{
			using base = Filter<BufW2,TSrc>;
			Buffer<> m_lines;
			size_t m_lines_max;
			size_t m_lines_min;
			StringLit m_literal;
			EmitCount m_emit;

			StripNewLines(TSrc& src, size_t lines_max = 1, size_t lines_min = 0)
				:base(src)
				,m_lines(m_reg)
				,m_lines_max(lines_max)
				,m_lines_min(std::min(lines_min, lines_max))
				,m_literal()
				,m_emit()
			{
				seek(0);
			}

			// Pointer-like interface
			wchar_t operator *() const override
			{
				return *m_lines;
			}
			
			// Seek to the next valid character to output
			void seek(int n) override
			{
				// Transforms consecutive blank lines.
				// Look for consecutive lines that contain only whitespace characters.
				// If the number of lines is less than 'm_lines_min' add lines up to 'm_lines_min'
				// If the number of lines is greater than 'm_lines_max' delete lines back to 'm_lines_max'
				// Blank lines are replaced with a single new line character
				auto& src = m_lines;
				for (src += n, m_emit -= n; m_emit == 0;)
				{
					// If we're currently within a literal string or character then
					// just return characters until the literal ends
					if (m_literal.inc(*src))
						break;

					// Transform new lines
					if (*src == L'\n')
					{
						// Buffer lines up to the max line count or until the next non-whitespace
						for (size_t i = m_emit; pr::str::IsWhiteSpace(src[i]); )
						{
							if (src[i] == L'\n')
							{
								// Fill the buffer with '\n' up to the max line count
								if (m_emit < m_lines_max)
									src[m_emit++] = L'\n';

								// Erase the remaining blank line
								src.erase(m_emit, (i+1) - m_emit);
								i = m_emit;
							}
							else
							{
								++i;
							}
						}

						// If below the minimum, insert lines
						for (; m_emit < m_lines_min; ++m_emit)
						{
							src.push_front(L'\n');
						}

						continue;
					}

					// If we get here, it's a valid character
					break;
				}
			}
		};
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script2_filter)
		{
			using namespace pr::str;
			using namespace pr::script;

			{// StripLineContinuations
				char const* str_in = "Li\
					on";
				char const* str_out = "Li					on";

				PtrA<> src(str_in);
				StripLineContinuations<> strip(src);
				for (;*strip; ++strip, ++str_out)
				{
					if (*strip == *str_out) continue;
					PR_CHECK(*strip, *str_out);
				}
				PR_CHECK(*str_out, 0);
			}
			{// StripComments
				char const* str_in = 
					"123// comment         \n"
					"456/* block */789     \n"
					"// many               \n"
					"// lines              \n"
					"// \"string\"         \n"
					"/* \"string\" */      \n"
					"\"string \\\" /*a*/ //b\"  \n"
					"/not a comment\n"
					"/*\n"
					"  more lines\n"
					"*/\n"
					"// multi\\\n"
					" line\\\n"
					" comment\n";
				char const* str_out = 
					"123\n"
					"456789     \n"
					"\n"
					"\n"
					"\n"
					"      \n"
					"\"string \\\" /*a*/ //b\"  \n"
					"/not a comment\n"
					"\n"
					"\n";

				PtrA<> src(str_in);
				StripLineContinuations<> cont(src);
				StripComments<> strip(cont);
				for (;*strip; ++strip, ++str_out)
				{
					if (*strip == *str_out) continue;
					PR_CHECK(*strip, *str_out);
				}
				PR_CHECK(*str_out, 0);
			}
			{// StripNewLines
				char const* str_in =
					"  \n"
					"      \n"
					"   \n"
					"\" multi-line \n"
					"\n"
					"\n"
					"string \"     \n"
					"         \n"
					"     \n"
					"abc  \n"
					"\n"
					"\n"
					"";
				{// min 0, max 0 lines
					char const* str_out =
						"  \" multi-line \n"
						"\n"
						"\n"
						"string \"     abc  ";

					PtrA<> src(str_in);
					StripNewLines<> strip(src,0,0);
					for (; *strip; ++strip, ++str_out)
					{
						if (*strip == *str_out) continue;
						PR_CHECK(*strip, *str_out);
					}
					PR_CHECK(*str_out, 0);
				}
				{
					char const* str_out =
						"  \n"
						"\" multi-line \n"
						"\n"
						"\n"
						"string \"     \n"
						"abc  \n"
						"";

					PtrA<> src(str_in);
					StripNewLines<> strip(src);
					for (; *strip; ++strip, ++str_out)
					{
						if (*strip == *str_out) continue;
						PR_CHECK(*strip, *str_out);
					}
					PR_CHECK(*str_out, 0);
				}
				{
					char const* str_out =
						"  \" multi-line \n"
						"\n"
						"\n"
						"string \"     abc  ";

					PtrA<> src(str_in);
					StripNewLines<> strip(src, 0);
					for (; *strip; ++strip, ++str_out)
					{
						if (*strip == *str_out) continue;
						PR_CHECK(*strip, *str_out);
					}
					PR_CHECK(*str_out, 0);

				}
				{
					char const* str_out =
						"  \n"
						"\n"
						"\" multi-line \n"
						"\n"
						"\n"
						"string \"     \n"
						"\n"
						"abc  \n"
						"\n"
						"";

					PtrA<> src(str_in);
					StripNewLines<> strip(src,2,2);
					for (; *strip; ++strip, ++str_out)
					{
						if (*strip == *str_out) continue;
						PR_CHECK(*strip, *str_out);
					}
					PR_CHECK(*str_out, 0);
				}
			}
		}
	}
}
#endif
