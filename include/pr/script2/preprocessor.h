//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/script_core.h"
#include "pr/script2/fail_policy.h"
#include "pr/script2/filter.h"
#include "pr/script2/macros.h"
#include "pr/script2/includes.h"

namespace pr
{
	namespace script2
	{
		// Takes a character stream and performs preprocessing on it.
		// This is a super-set of a C/C++ preprocessor.
		template
		<
			typename TMacroDB = MacroDB<>,
			typename TIncludeHandler = IncludeHandler<>,
			typename FailPolicy = ThrowOnFailure
		>
		struct Preprocessor :Src
		{
		private:

			// A source wrapper that strips line continuations and comments
			struct PPSource 
			{
				using OutSrc = Buffer<>;

				Src* m_in;
				StripLineContinuations<> m_slc;
				StripComments<FailPolicy> m_sc;
				Buffer<> m_buf;
				bool m_del;

				~PPSource()
				{
					if (m_del)
						delete m_in;
				}
				PPSource(Src* src, bool del)
					:m_in (src)
					,m_slc(*m_in)
					,m_sc (m_slc)
					,m_buf(m_sc)
					,m_del(del)
				{}
				PPSource(PPSource&& rhs)
					:m_in (std::move(rhs.m_in ))
					,m_slc(std::move(rhs.m_slc))
					,m_sc (std::move(rhs.m_sc ))
					,m_buf(std::move(rhs.m_buf))
					,m_del(std::move(rhs.m_del))
				{
					m_slc.m_reg.m_src = m_in;
					m_sc.m_reg.m_src  = &m_slc;
					m_buf.m_src       = &m_sc;
					rhs.m_in          = nullptr;
					rhs.m_del         = false;
				}
				PPSource(PPSource const&) = delete;
				PPSource& operator =(PPSource&&) = delete;
				PPSource& operator =(PPSource const&) = delete;
			};

			// The stack of input streams. Streams are pushed/popped from
			// the stack as files are opened, or macros are evaluated.
			std::vector<PPSource> m_stack;

			// The output buffer of the top source on the stack
			typename PPSource::OutSrc* m_src;

			// The database of macro definitions
			using MacroDB = TMacroDB;
			using Macro = typename MacroDB::Macro;
			MacroDB m_macros;

			// A stack recording the 'inclusion' state of nested #if/#endif blocks
			using BitStack = pr::BitStack<>;
			BitStack m_if_stack;

			// Include handler
			using IncludeHandler = TIncludeHandler;
			TIncludeHandler m_includes;

			// Debugging helpers for the watch window
			SrcConstPtr m_dbg_src;
			typename PPSource::OutSrc::buffer_type const* m_dbg_buf;

		public:
			Preprocessor()
				:Src(ESrcType::Preprocessor)
				,m_stack()
				,m_src()
				,m_macros()
				,m_if_stack()
				,m_includes()
				,m_dbg_buf()
				,m_dbg_src()
			{}
			Preprocessor(Preprocessor&& rhs)
				:Src(std::move(rhs))
				,m_stack(std::move(rhs.m_stack))
				,m_src(std::move(rhs.m_src))
				,m_macros(std::move(rhs.m_macros))
				,m_if_stack(std::move(rhs.m_if_stack))
				,m_includes(std::move(rhs.m_includes))
				,m_dbg_buf(rhs.m_dbg_buf)
				,m_dbg_src(rhs.m_dbg_src)
			{}
			Preprocessor(Src* src, bool delete_on_pop)
				:Preprocessor()
			{
				Push(src, delete_on_pop);
			}
			template <typename Char> Preprocessor(Char const* src)
				:Preprocessor()
			{
				Push(src);
			}

			// Behaviour switches
			struct Opts
			{
				// Allows missing include file errors to be suppressed
				bool MissingIncludeErrors;

				Opts()
					:MissingIncludeErrors(true)
				{}
			} Options;

			// Push a source onto the input stack
			void Push(Src* src, bool delete_on_pop)
			{
				m_stack.emplace_back(src, delete_on_pop);
				m_src = &m_stack.back().m_buf;

				// Assign helper pointer for the watch windows
				m_dbg_src = m_src->DbgPtr();
				m_dbg_buf = m_src->DbgBuf();

				seek(0);
			}

			// Push a source owned externally onto the input stack
			void Push(Src& src)
			{
				Push(&src, false);
			}

			// Push a simple character string as a source
			template <typename Char> void Push(Char const* src)
			{
				Push(new Ptr<Char const*>(src), true);
			}

			// Pop the top source off the input stack
			void Pop()
			{
				m_stack.pop_back();
				auto top = !m_stack.empty() ? &m_stack.back() : nullptr;
				m_src = top ? &top->m_buf : nullptr;

				// Assign helper pointer for the watch windows
				m_dbg_buf = m_src ? m_src->DbgBuf() : nullptr;
				m_dbg_src = m_src ? m_src->DbgPtr() : SrcConstPtr();

				seek(0);
			}

			// Source type is the type of the stream on the top of the stack
			ESrcType Type() const override
			{
				return m_src ? m_src->Type() : ESrcType::Unknown;
			}
			SrcConstPtr DbgPtr() const override
			{
				return m_src ? m_src->DbgPtr() : SrcConstPtr();
			}
			Location const& Loc() const override
			{
				static FileLoc s_null_loc;
				return m_src ? m_src->Loc() : s_null_loc;
			}

			// Pointer-like interface
			wchar_t operator *() const override
			{
				return m_src ? **m_src : wchar_t();
			}
			Preprocessor& operator ++() override
			{
				seek(1);
				return *this;
			}

		private:

			// Advance by 'n' characters, popping from the input stack as needed
			void next(int n = 1)
			{
				for (; m_src && n--;)
				{
					// Pop a character from the source
					++*m_src;

					// If the source has expired, pop from the stack until we find the next non-expired source
					for (; m_src && **m_src == 0;)
						 Pop();
				}
				assert(m_src == nullptr || **m_src != 0 && "Should never return an end of stream from here");
			}

			// Advance the source by at least 'n' preprocessed characters ensuring
			// the next character to be returned is a valid preprocessed character
			void seek(int n)
			{
				// Use in watch windows:
				//  m_dbg_buf  <- buffered characters
				//  m_dbg_src  <- input stream

				// Buffered characters are characters that have been confirmed as ok to use
				for (next(n); m_src && m_src->empty();)
				{
					// This reference can't be used after 'next' has been called.
					// However, none of this tokens should span file boundaries.
					auto& src = *m_src;

					// Cases should end with 'continue' to go round again or break to emit the current character
					switch (*src)
					{
					case L'\"':
					case L'\'':
						#pragma region Literal String/Char
						{
							// Buffer the literal string or char
							auto end = src[0];
							auto beg = src.Loc();
							for (int i = 1, esc = 0; src[i] && (src[i] != end || esc); esc = int(src[i] == L'\\'), ++i) {}
							if (src.back() != end) return FailPolicy::Fail(EResult::SyntaxError, Loc(), pr::Fmt("Unclosed literal string or character\n%s", Narrow(beg.ToString()).c_str()));
							break;
						}
						#pragma endregion
					case L'#':
						#pragma region PP Command
						{
							// Record the start of the PP command
							auto loc_beg = src.Loc();

							// Eat optional whitespace between the # and the keyword
							EatLineSpace(1, 0);

							// Match the preprocessor command
							switch (*src)
							{
							case L'd':
								#pragma region Define
								if (src.match(L"define", true))
								{
									EatLineSpace(0, 0);
									m_macros.Add(Macro(src, src.Loc()));
									continue;
								}
								#pragma endregion
								#pragma region DefIfNDef
								if (src.match(L"defifndef", true))
								{
									EatLineSpace(0, 0);
									Macro macro(src, src.Loc());
									if (!m_macros.Find(macro.m_hash))
										m_macros.Add(macro);
									continue;
								}
								#pragma endregion
								break;
							case L'e':
								#pragma region Else
								if (src.match(L"else", true))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #else");
									if (m_if_stack.top()) { SkipPreprocessorBlock(loc_beg); }
									else                  { m_if_stack.top() = true; EatLine(0, 1); }
									continue;
								}
								#pragma endregion
								#pragma region ElIf
								if (src.match(L"elif", true))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #elif");
									if (m_if_stack.top())  { SkipPreprocessorBlock(loc_beg); }
									else if (!PPDefined()) { SkipPreprocessorBlock(loc_beg); }
									else                   { m_if_stack.top() = true; EatLine(0, 1); }
									continue;
								}
								#pragma endregion
								#pragma region EndIf
								if (src.match(L"endif", true))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #endif");
									m_if_stack.pop();
									EatLine(0, 1);
									continue;
								}
								#pragma endregion
								#pragma region Eval
								if (src.match(L"eval", true))
								{
									EatLineSpace(0, 0);
									auto eval_src = std::make_unique<Buffer<pr::string<wchar_t>>>();
									auto& expr = eval_src->m_buf;

									// Extract text between '{' and '}'
									if (*src == '{') ++src; else return FailPolicy::Fail(EResult::ExpressionSyntaxError, loc_beg, "Expected the form: #eval{expression}");
									for (int nest = 1; *src; expr.push_back(*src), ++src)
									{
										nest += *src == '{';
										nest -= *src == '}';
										if (nest == 0) break;
									}
									if (*src == '}') ++src; else return FailPolicy::Fail(EResult::ExpressionSyntaxError, loc_beg, "No matching '}' found following #eval");

									// Expand any macros in the expression
									ExpandMacros(expr, typename Macro::Ancestor(nullptr, nullptr), loc_beg);

									// Replace any nested #eval{exp} with (exp)
									pr::str::Replace(expr, L"#eval", L"" );
									pr::str::Replace(expr, L"{"    , L"(");
									pr::str::Replace(expr, L"}"    , L")");

									// Evaluate the expression and push the result as a string onto the stack
									double result;
									if (!pr::Evaluate(expr.c_str(), result))
										return FailPolicy::Fail(EResult::ExpressionSyntaxError, loc_beg, "#eval expression cannot be evaluated");

									// Convert the result to a string
									expr = (static_cast<int64>(result) == result) ? pr::FmtS(L"%lld", static_cast<int64>(result)) : pr::FmtS(L"%f", result);

									// Push the eval source onto the input stack
									Push(eval_src.get(), true);
									eval_src.release();
									continue;
								}
								#pragma endregion
								#pragma region Embedded
								if (src.match(L"embedded", true))
								{
									continue;
								}
								#pragma endregion
								#pragma region Error
								if (src.match(L"error", true))
								{
									EatLineSpace(0,0);
									BufferLine();
									return FailPolicy::Fail(EResult::PreprocessError, loc_beg, Narrow(src.str()));
								}
								#pragma endregion
								break;
							case L'i':
								#pragma region IfNDef
								if (src.match(L"ifndef", true))
								{
									EatLineSpace(0, 0);
									BufferIdentifier();
									if (m_macros.Find(Macro::Hash(src)) == 0) { m_if_stack.push(true); EatLine(0, 1); }
									else                                      { m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region IfDef
								if (src.match(L"ifdef", true))
								{
									EatLineSpace(0, 0);
									BufferIdentifier();
									if (m_macros.Find(Macro::Hash(src)) != 0) { m_if_stack.push(true); EatLine(0, 1); }
									else                                      { m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region If
								if (src.match(L"if", true))
								{
									EatLineSpace(0, 0);
									if (PPDefined()) { m_if_stack.push(true);  EatLine(0, 1); }
									else             { m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region Include Path
								if (src.match(L"include_path", true))
								{
									EatLineSpace(1, 0);
									if (*src != L'<' && *src != L'\"')
										return FailPolicy::Fail(EResult::InvalidInclude, src.Loc(), "expected a string following #include_path");

									// Buffer up the include path
									auto end = *src == L'<' ? L'>' : L'\"';
									int i = 1; for (; src[i] && src[i] != L'\n' && src[i] != end; ++i) {}
									if (src[i] != end) return FailPolicy::Fail(EResult::InvalidInclude, src.Loc(), "include path string incomplete");
									auto inc_path = src.str(i, i - 1);
									src.clear();

									// Add the path to the include paths
									m_includes.AddSearchPath(inc_path);

									// Clear the buffered string and eat the rest of the line
									src.pop(i);
									EatLine(0, 0);
									continue;
								}
								#pragma endregion
								#pragma region Include
								if (src.match(L"include", true))
								{
									EatLineSpace(1, 0);
									loc_beg = src.Loc();

									if (*src != L'<' && *src != L'\"')
										return FailPolicy::Fail(EResult::InvalidInclude, loc_beg, "expected a string following #include");

									// Buffer up the include filepath
									auto end = *src == L'<' ? L'>' : L'\"';
									int i = 1; for (; src[i] && src[i] != L'\n' && src[i] != end; ++i) {}
									if (src[i] != end) return FailPolicy::Fail(EResult::InvalidInclude, loc_beg, "include string incomplete");
									auto filepath = src.str(i, i - 1);
									src.clear();

									// Open the include
									try
									{
										auto search_paths_only = end == L'>';
										auto inc = m_includes.Open(filepath, src.Loc(), search_paths_only);
										if (inc)
										{
											Push(inc.get(), true);
											inc.release();
										}
									}
									catch (std::exception const&)
									{
										// todo, this should be a setting on the IncludeHandler
										if (Options.MissingIncludeErrors)
											throw;
									}
									continue;
								}
								#pragma endregion
								break;
							case L'l':
								#pragma region Lit
								if (src.match(L"lit", true))
								{
									continue;
								}
								#pragma endregion
								#pragma region Line
								if (src.match(L"line", true))
								{
									EatLine(0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'p':
								#pragma region Pragma
								if (src.match(L"pragma", true))
								{
									EatLine(0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'u':
								#pragma region Undef
								if (src.match(L"undef", true))
								{
									EatLineSpace(0, 0);

									// Read the macro tag
									auto tag = Macro::ReadTag(src, src.Loc());
									m_macros.Remove(Macro::Hash(tag));

									EatLine(0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'w':
								#pragma region Warning
								if (src.match(L"warning", true))
								{
									EatLine(0, 1);
									continue;
								}
								#pragma endregion
								break;
							}

							// Unknown pp command
							auto msg = pr::Fmt(L"Unknown preprocessor command '%s'\n%s", src.str().c_str(), src.Loc().ToString().c_str());
							return FailPolicy::Fail(EResult::UnknownPreprocessorCommand, Loc(), pr::Narrow(msg));
						}
						#pragma endregion
					}

					// If we get here, then 'src[0]' is a valid output character
					break;
				}
			}

			// Recursively expand the expression in 'exp' with macro substitutions
			void ExpandMacros(pr::string<wchar_t>& exp, typename Macro::Ancestor const& parent, Location const& loc)
			{
				pr::string<wchar_t> ident;
				typename Macro::Params params;

				// Scan through 'src' looking for macro identifiers
				for (size_t i = 0, iend = exp.size(); i != iend; iend = exp.size())
				{
					if (!pr::str::IsIdentifier(exp[i], true)) { ++i; continue; }

					auto beg = i;

					// Found the start of an identifier, see if it's a macro identifier
					ident.assign(1, exp[i]);
					for (++i; i != iend && pr::str::IsIdentifier(exp[i], false); ++i)
						ident.push_back(exp[i]);

					// Find the macro?
					auto macro = m_macros.Find(Macro::Hash(ident));
					if (!macro) continue;

					// Check whether this macro is an ancestor
					if (parent.IsRecursive(macro))
						continue; // a recursive substitution.. ignore

					// Check the correct parameters have been given
					params.clear();
					if (i == iend || !macro->ReadParams<false>(&exp[i], params, loc))
						continue;

					// Recursively expand the macro into a temporary buffer
					ident.resize(0); // reuse
					macro->Expand(ident, params);
					ExpandMacros(ident, typename Macro::Ancestor(macro, &parent), loc);

					// Substitute the expanded macro into 'src'
					auto len = i - beg;
					exp.erase (beg, len);
					exp.insert(beg, ident);
					i = beg + ident.size();
				}
			}

			// Parse the line following an #if or #elif statement, returning true if the expression evaluates to true
			bool PPDefined()
			{
				auto& src = *m_src;
				pr::string<wchar_t> expr, exp;

				// Read the whole line into a string generating a numeric expression
				for (EatLineSpace(0,0); !pr::str::IsNewLine(*src); EatLineSpace(0,0))
				{
					// Append operators to the expression
					if (!pr::str::IsIdentifier(*src, true))
					{
						expr.push_back(*src);
						++src;
						continue;
					}

					// Read the macro or keyword identifier
					BufferIdentifier();

					// If the identifier is the keyword 'defined' then it should be followed
					// by an identifier optionally enclosed within '(' and ')'
					if (src.match(L"defined", true))
					{
						EatLineSpace(0,0);

						// Check for optional '()'
						auto wrapped = *src == '(';
						if (wrapped) ++src;

						// Read the identifier within the defined() expression
						BufferIdentifier(EResult::InvalidPreprocessorDirective);
						auto hash = Macro::Hash(src);
						src.clear();

						// Check the optional brackets are matched
						if (wrapped && *src == ')') ++src; else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "unmatched ')'"), false;

						// If the macro is defined, add a 1 to the expression
						expr.push_back(m_macros.Find(hash) != nullptr ? '1' : '0');
					}
					
					// Otherwise substitute the macro
					else
					{
						exp.assign(std::begin(src), std::end(src));
						auto hash = Macro::Hash(exp);
						auto macro = m_macros.Find(hash);
						if (!macro)
							return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), pr::FmtS("Identifier '%s' is not defined", pr::Narrow(exp).c_str())), false;

						// Read macro parameters if it has them
						typename Macro::Params params;
						if (!macro->ReadParams<false>(src, params, src.Loc()))
							return FailPolicy::Fail(EResult::ParameterCountMismatch, src.loc(), pr::FmtS("Missing parameters for macro %s. Expected %d", pr::Narrow(exp).c_str(), macro->m_params.size())), false;

						// Expand the macro with the given parameters
						exp.resize(0);
						macro->Expand(exp, params);

						// Recursively expand macros within 'exp'.
						ExpandMacros(exp, Macro::Ancestor(macro, nullptr), src.Loc());

						// Add the fully expanded macro to the expression
						expr.append(exp);
					}
				}

				// Evaluate the expression
				int res;
				if (!pr::EvaluateI(expr.c_str(), res)) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "Failed to evaluate conditional expression"), false;
				return res != 0;
			}

			// Eat characters from the stream up to an #elif, #else, or #endif for a previous #ifdef, #ifndef, or #elif
			void SkipPreprocessorBlock(Location const& beg)
			{
				auto& src = *m_src;
				for (int nest = 1; *src;)
				{
					if (*src == L'\"') { EatLiteralString(); continue; }
					if (*src != L'#')  { ++src; continue; }

					++src;
					EatLineSpace();
					nest += int(src.match(L"ifndef") || src.match(L"ifdef") || src.match(L"if"));
					nest -= int(src.match(L"endif") || (nest == 1 && (src.match(L"elif") || src.match(L"else"))));
					if (nest == 0)
					{
						src.push_front('#');
						break;
					}

					src.clear();
					++src;
				}
				if (*src == 0)
					return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, beg, "Unmatched #if, #ifdef, #ifndef, #else, or #elid");
			}

			// Buffer an identifier from the source stream into 'src'.
			void BufferIdentifier(EResult fail_result = EResult::InvalidIdentifier)
			{
				auto& src = *m_src;
				if  (  pr::str::IsIdentifier(*src.stream(), true )) src.buffer() else return FailPolicy::Fail(fail_result, src.Loc(), "An identifier was expected");
				for (; pr::str::IsIdentifier(*src.stream(), false); src.buffer()) {}
			}

			// Buffer up to the next '\n' from the source stream into 'src'
			void BufferLine()
			{
				auto& src = *m_src;
				for (; !pr::str::IsNewLine(*src.stream()); src.buffer()) {}
			}

			// Call 'next()' until 'pred' returns false
			template <typename Pred> void Eat(int eat_initial, int eat_final, Pred pred)
			{
				auto& src = *m_src;
				for (next(eat_initial); pred(*src); next()) {}
				next(eat_final);
			}
			void EatLineSpace(int eat_initial, int eat_final)
			{
				Eat(eat_initial, eat_final, pr::str::IsLineSpace<wchar_t>);
			}
			void EatLine(int eat_initial, int eat_final)
			{
				Eat(eat_initial, eat_final, [](wchar_t ch){ return !pr::str::IsNewLine(ch); });
			}
			void EatLiteralString()
			{
				auto& src = *m_src;
				if (*src != L'\"' || *src != L'\'')
					return FailPolicy::Fail(EResult::InvalidString, src.Loc(), "Literal string expected");

				auto end = *src;
				auto escape = false;
				Eat(1, 1, [&](wchar_t ch)
				{
					auto r = ch == end && !escape;
					escape = ch == L'\\';
					return r;
				});
			}
		};
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script2_input_stack)
		{
			using namespace pr::str;
			using namespace pr::script2;
			
			char const* src1 = "abcd";
			wchar_t const* src2 = L"123";
			pr::string<wchar_t> str1;

			Preprocessor<> pp(src1);
			str1.push_back(*pp); ++pp;
			str1.push_back(*pp); ++pp;
			pp.Push(src2);
			str1.push_back(*pp); ++pp;
			str1.push_back(*pp); ++pp;
			str1.push_back(*pp); ++pp;
			str1.push_back(*pp); ++pp;
			str1.push_back(*pp); ++pp;
			PR_CHECK(Equal(str1, L"ab123cd"), true);
			PR_CHECK(*pp, 0);
		}
		PRUnitTest(pr_script2_preprocessor)
		{
			using namespace pr::script2;

			{// ignored stuff
				char const* str_in =
					"\"#if ignore #define this stuff\"\n"
					;
				char const* str_out =
					"\"#if ignore #define this stuff\"\n"
					;

				Preprocessor<> pp(str_in);
				for (;*pp; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out, 0);
			}
			{// simple macros
				char const* str_in =
					"#  define ONE 1 // ignore me \n"
					"#  define NOT_ONE (!ONE) /*and me*/ \n"
					"#define TWO\\\n"
					"   2\n"
					"ONE\n"
					"NOT_ONE\n"
					"TWO\n"
					;
				char const* str_out =
					"1\n"
					"(!1)\n"
					"2\n"
					;

				Preprocessor<> pp(str_in);
				for (;*str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*pp, 0);
			}
		}
#pragma region
#if 0
		PRUnitTest(pr_script_preprocessor)
		{
			using namespace pr;
			using namespace pr::script;

			

			{// Multi-line proprocessor
				char const* str_in =
					"#define ml\\\n"
					"  MULTI\\\n"
					"LINE\n"
					"ml";
				char const* str_out =
					"MULTILINE";

				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// simple macro functions
				char const* str_in =
					"#\tdefine PLUS(x,y) \\\n"
					" (x)+(y) xx 0x _0x  \n"
					"PLUS  (1,(2,3))\n"
					;
				char const* str_out =
					"(1)+((2,3)) xx 01 _0x\n"
					;
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// recursive macros
				char const* str_in =
					"#define C(x) A(x) B(x) C(x)\n"
					"#define B(x) C(x)\n"
					"#define A(x) B(x)\n"
					"A(1)\n"
					;
				char const* str_out =
					"A(1) B(1) C(1)\n"
					;
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// #eval
				char const* str_in =
					"#eval{1+#eval{1+1}}\n"
					;
				char const* str_out =
					"3\n"
					;
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// recursive macros/evals
				char const* str_in =
					"#define X 3.0\n"
					"#define Y 4.0\n"
					"#define Len2 #eval{len2(X,Y)}\n"
					"#eval{X + Len2}\n";
				char const* str_out =
					"8\n";
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// includes
				char const* str_in =
					"#  define ONE 1 // ignore me \n"
					"#include \"inc\"\n"
					;
				char const* str_out =
					"included 1\n"
					;
				PtrSrc src(str_in);
				PPMacroDB macros;
				StrIncludes includes; includes.m_strings["inc"] = "included ONE";
				Preprocessor pp(src, &macros, &includes, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// #if/#else/#etc
				char const* str_in =
					"#  define ONE 1 // ignore me \n"
					"#  define NOT_ONE (!ONE) /*and me*/ \n"
					"#\tdefine PLUS(x,y) (x)+(y) xx 0x _0x  \n"
					"#ifdef ZERO\n"
					"#if NESTED\n"
					"  not output \"ignore #else\" \n"
					"#endif\n"
					"#elif (!NOT_ONE) && defined(PLUS)\n"
					"  output\n"
					"#else\n"
					"  not output\n"
					"#endif\n"
					"#ifndef ZERO\n"
					"#if defined(ZERO) || defined(PLUS)\n"
					"  output this\n"
					"#else\n"
					"  but not this\n"
					"#endif\n"
					"#endif\n"
					"#undef ONE\n"
					"#ifdef ONE\n"
					"  don't output\n"
					"#endif\n"
					"#define TWO\n"
					"#ifdef TWO\n"
					"  two defined\n"
					"#endif\n"
					"#defifndef ONE 1\n"
					"#defifndef ONE 2\n"
					"ONE\n"
					;
				char const* str_out =
					"  output\n"
					"  output this\n"
					"  two defined\n"
					"1\n"
					;
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// miscellaneous
				char const* str_in =
					"\"#error this would throw an error\"\n"
					"#pragma ignore this\n"
					"#line ignore this\n"
					"#warning ignore this\n"
					"#include_path \"some_path\"\n"
					"lastword"
					"#define ONE 1\n"
					"#eval{ONE+2-4+len2(3,4)}\n"
					"#define EVAL(x) #eval{x+1}\n"
					"EVAL(1)\n"
					"#lit Any old ch*rac#ers #if I {feel} #include --cheese like #en#end\n"
					"// #if 1 comments \n"
					"/*should pass thru #else*/\n"
					"#embedded(lua) --lua code\n return \"hello world\" #end\n"
					;
				char const* str_out =
					"\"#error this would throw an error\"\n"
					"lastword"
					"4\n"
					"2\n"
					"Any old ch*rac#ers #if I {feel} #include --cheese like #en\n"
					"// #if 1 comments \n"
					"/*should pass thru #else*/\n"
					"hello world\n"
					;
				PtrSrc src(str_in);
				PPMacroDB macros;
				StrIncludes includes; includes.m_strings["inc"] = "included ONE";
				EmbeddedLua lua_handler;
				Preprocessor pp(src, &macros, &includes, &lua_handler);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
				PR_CHECK(includes.m_paths[0], "some_path");
			}
			{// Preprocessor with no macro or include handler
				char const* str_in =
					"\t      \n"
					"\"#if ignore #define this stuff\"\n"
					"#  define ONE 1     \n"
					"#  define NOT_ONE (!ONE)  \n"
					"#\tdefine PLUS(x,y) \\\n"
					" (x)+(y) xx 0x _0x  \n"
					"ONE\n"
					"PLUS  (1,(2,3))\n"
					"#define C(x) A(x) B(x) C(x)\n"
					"#define B(x) C(x)\n"
					"#define A(x) B(x)\n"
					"A(1)\n"
					"#include \"inc\"\n"
					"#ifdef ZERO\n"
					"#if 0\n"
					"  not output \"ignore #else\" \n"
					"#endif\n"
					"#elif (!0) && defined(PLUS)\n"
					"  output\n"
					"#else\n"
					"  not output\n"
					"#endif\n"
					"#ifndef ZERO\n"
					"#if defined(ZERO) || defined(PLUS)\n"
					"  output this\n"
					"#else\n"
					"  but not this\n"
					"#endif\n"
					"#endif\n"
					"#undef ONE\n"
					"#ifdef ONE\n"
					"  don't output\n"
					"#endif\n"
					"\"#error this would throw an error\"\n"
					"#pragma ignore this\n"
					"#line ignore this\n"
					"#warning ignore this\n"
					"lastword"
					"#define ONE 1\n"
					"#defifndef TWO 2\n"
					"#defifndef ONE 3\n"
					"#eval{ONE+2-4+len2(3,4)}\n"
					"#lit Any old ch*rac#ers #if I {feel} #include --cheese like #en#end\n"
					"// #if 1 comments \n"
					"/*should pass thru #else*/\n"
					//"#lua --lua code\n return \"hello world\" #end\n"
					;
				char const* str_out =
					"\t      \n"
					"\"#if ignore #define this stuff\"\n"
					"ONE\n"
					"PLUS  (1,(2,3))\n"
					"A(1)\n"
					"\n"
					"  not output\n"
					"\"#error this would throw an error\"\n"
					"lastword0\n"
					"Any old ch*rac#ers #if I {feel} #include --cheese like #en\n"
					"// #if 1 comments \n"
					"/*should pass thru #else*/\n"
					//"#lua --lua code\n return \"hello world\" #end\n"
				;
				PtrSrc src(str_in);
				Preprocessor pp(src, 0, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// Line continuation tests line endings
				char const* str_in =
					"#define BLAH(x)\\\r\n"
					"   \\\r\n"
					"	(x + 1)\r\n"
					"BLAH(5)\r\n"
					"#define BOB\\\r\n"
					"	bob\r\n"
					"BLAH(bob)\r\n";
				char const* str_out =
					"(5 + 1)\r\n"
					"(bob + 1)\r\n"
				;
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
		}
#endif
#pragma endregion
	}
}
#endif
