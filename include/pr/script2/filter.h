//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/forward.h"
#include "pr/script2/buf8.h"
#include "pr/script2/fail_policy.h"
#include "pr/script2/script_core.h"

namespace pr
{
	namespace script2
	{
		// Wraps BufWN<> and a reference to a character source and location.
		// This allows other code to use a Src as a pointer with automatic location and buffering support
		template <typename BufN, typename TLoc> struct BufSrc :Src
		{
			Src* m_src; // The source character stream (used to feed TBuf)
			BufN m_reg; // The character "shift register"
			TLoc m_loc; // The file location corresponding to m_reg[0]

			BufSrc(Src& src, TLoc loc = TLoc())
				:Src(src.Type())
				,m_src(&src)
				,m_reg(src) // note, this will advance 'src'
				,m_loc(loc)
			{}

			// Debugging helper interface
			SrcConstPtr DbgPtr() const override
			{
				return m_reg.m_ch;
			}
			TLoc const& Loc() const override
			{
				return m_loc;
			}

			// Pointer interface
			wchar_t operator*() const override
			{
				return m_reg.front();
			}
			BufSrc& operator ++() override
			{
				m_loc.inc(**this);
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
		template <typename BufWN = BufW2, typename TLoc = FileLoc> struct Filter :Src
		{
			using BufN = BufSrc<BufWN, TLoc>;
			BufN m_reg; // N-character shift register for fast short string buffering

			Filter(Src& src)
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
			TLoc const& Loc() const override
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
		template <typename TLoc = FileLoc> struct StripLineContinuations :Filter<BufW2,TLoc>
		{
			using base = Filter<BufW2,TLoc>;
			StripLineContinuations(Src& src)
				:base(src)
			{
				seek(0);
			}

			// Seek to the next valid character to output
			void seek(int n) override
			{
				auto& src = m_reg;
				for (src += n; src[0] == L'\\' && src[1] == L'\n'; src += 2) {}
			}
		};

		// Removes C++ style comments from a character stream
		template <typename FailPolicy = ThrowOnFailure, typename TLoc = FileLoc> struct StripComments :Filter<BufW2, TLoc>
		{
			using base = Filter<BufW2,TLoc>;
			wchar_t m_literal;
			bool m_escaped;
			
			StripComments(Src& src)
				:base(src)
				,m_literal()
				,m_escaped()
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
					if (m_literal != 0)
					{
						if (*src == m_literal && !m_escaped) m_literal = 0;
						else m_escaped = *src == L'\\';
					}
					else if (*src == L'\"' || *src == L'\'')
					{
						m_literal = *src;
					}
					if (m_literal)
					{
						break;
					}

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
		template <typename FailPolicy = ThrowOnFailure, typename TLoc = FileLoc> struct StripNewLines :Filter<BufW2, TLoc>
		{
			using base = Filter<BufW2,TLoc>;
			Buffer<> m_lines;
			size_t m_lines_max;
			size_t m_lines_min;
			wchar_t m_literal;
			bool m_escaped;

			StripNewLines(Src& src, size_t lines_max = 1, size_t lines_min = 0)
				:base(src)
				,m_lines(m_reg)
				,m_lines_max(lines_max)
				,m_lines_min(std::min(lines_min, lines_max))
				,m_literal()
				,m_escaped()
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
				for (src += n; ;)
				{
					// Buffered data is data to be output
					if (!src.empty())
						break;

					// If we're currently within a literal string or character then
					// just return characters until the literal ends
					if (m_literal != 0)
					{
						if (*src == m_literal && !m_escaped) m_literal = 0;
						else m_escaped = *src == L'\\';
					}
					else if (*src == L'\"' || *src == L'\'')
					{
						m_literal = *src;
					}
					if (m_literal)
					{
						break;
					}

					// Transform new lines
					if (*src == L'\n')
					{
						// Buffer lines up to the max line count or until the next non-whitespace
						size_t line_count = 0;
						for (; pr::str::IsWhiteSpace(*src.stream()); )
						{
							if (*src.stream() == L'\n')
							{
								// Fill the buffer with '\n' up to the max line count
								if (line_count < m_lines_max)
									src[line_count++] = L'\n';

								// Erase the remaining blank line
								if (line_count < src.size())
									src.erase(line_count);

								// Advance the underlying stream
								++src.stream();
							}
							else
							{
								src.buffer();
							}
						}

						// If below the minimum, insert lines
						for (; line_count < m_lines_min; ++line_count)
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
			using namespace pr::script2;

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
