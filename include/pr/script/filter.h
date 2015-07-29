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

		// Base class for a 'Src' filter. Simple pass through filter.
		// Filters are different to actual sources because they only contain
		// a reference to the underlying source. This means they are copy constructable.
		template <typename BufWN = BufW2> struct Filter :Src
		{
			Src* m_src; // The source character stream (used to feed TBuf)
			BufWN m_reg; // The character "shift register"

			Filter()
				:Src(ESrcType::Unknown)
				,m_src()
				,m_reg()
			{}
			Filter(Src& src)
				:Filter()
			{
				Source(src);
			}

			// Set the input source
			void Source(Src& src)
			{
				m_type = src.Type();
				m_src = &src;
				m_reg.Load(*m_src);
				seek(0);
			}

			// Debugging helper interface
			SrcConstPtr DbgPtr() const override
			{
				return m_reg.m_ch;
			}
			Location const& Loc() const override
			{
				return m_src->Loc();
			}

			// Pointer-like interface
			wchar_t operator *() const override
			{
				return m_reg.front();
			}
			Filter& operator ++() override
			{
				seek(1);
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

			// Advance the buffer
			virtual void next(int n)
			{
				for (;n--;)
				{
					m_reg.shift(**m_src);
					if (**m_src) ++*m_src;
				}
			}

			// Seek to the next valid character to output
			virtual void seek(int n)
			{
				next(n);
			}
		};

		// Removes line continuation sequences in a character stream
		template <typename FailPolicy = ThrowOnFailure> struct StripLineContinuations :Filter<BufW4>
		{
			using base = Filter<BufW4>;

			StripLineContinuations(Src& src)
				:base(src)
			{
				seek(0);
			}

			// Seek to the next valid character to output
			void seek(int n) override
			{
				auto& src = m_reg;
				for (next(n);;)
				{
					if (src[0] == L'\\' && src[1] == L'\n')                    { next(2); continue; }
					if (src[0] == L'\\' && src[1] == L'\r' && src[2] == L'\n') { next(3); continue; }
					break;
				}
			}
		};

		// Removes C++ style comments from a character stream
		template <typename FailPolicy = ThrowOnFailure> struct StripComments :Filter<BufW2>
		{
			using base = Filter<BufW2>;
			StringLit m_literal;
			
			StripComments(Src& src)
				:base(src)
				,m_literal()
			{
				seek(0);
			}

			// Seek to the next valid character to output
			void seek(int n) override
			{
				auto& src = m_reg;
				for (next(n);;)
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
							for (next(2); *src != L'\n' && *src; next(1)) {}
							continue; // Don't eat the new line
						}
						if (src[1] == L'*')
						{
							auto loc_beg = m_src->Loc();
							for (next(2); *src && !(src[0] == L'*' && src[1] == L'/'); next(1)) {}
							if (*src) next(2); else return FailPolicy::Fail(EResult::SyntaxError, loc_beg, "Unmatched block comment");
							continue;
						}
					}

					// If we get here, then the next char is valid
					break;
				}
			}
		};

		// Removes newlines from a character stream
		template <typename FailPolicy = ThrowOnFailure> struct StripNewLines :Filter<BufW2>
		{
			using base = Filter<BufW2>;

			Buffer<> m_lines;
			size_t m_lines_max;
			size_t m_lines_min;
			StringLit m_literal;
			EmitCount m_emit;

			StripNewLines()
				:base()
				,m_lines()
				,m_lines_max()
				,m_lines_min()
				,m_literal()
				,m_emit()
			{}
			StripNewLines(Src& src, size_t lines_min = 0, size_t lines_max = 1)
				:StripNewLines()
			{
				SetLimits(lines_min, lines_max);
				Source(src);
			}

			// Set the input source
			void Source(Src& src)
			{
				m_type = src.Type();
				m_src = &src; 
				m_lines.Source(*m_src); // not using m_reg
				seek(0);
			}

			// Set the min/max line count
			void SetLimits(size_t lines_min = 0, size_t lines_max = 1)
			{
				m_lines_max = lines_max;
				m_lines_min = std::min(lines_min, lines_max);
			}

			// Pointer-like interface
			wchar_t operator *() const override
			{
				return *m_lines;
			}

			// Advance the buffer
			void next(int n) override
			{
				m_lines += n;
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
		PRUnitTest(pr_script_filter)
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
					"456/* blo/ck */789\n"
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
					"456789\n"
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
					StripNewLines<> strip(src, 0, 0);
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
