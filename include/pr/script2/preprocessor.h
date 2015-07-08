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
#include "pr/script2/embedded.h"

namespace pr
{
	namespace script2
	{
		// Takes a character stream and performs preprocessing on it.
		// This is a super-set of a C/C++ preprocessor.
		template
		<
			typename FailPolicy = ThrowOnFailure,
			typename TIncludeHandler = FileIncludes<FailPolicy>,
			typename TMacroDB = MacroDB<Macro<>, FailPolicy>,
			typename TEmbeddedCodeHandler = NoEmbeddedCode<FailPolicy>
		>
		struct Preprocessor :Src
		{
		private:

			// A source wrapper that strips line continuations and comments
			struct PPSource 
			{
				using LnCnt   = StripLineContinuations<Src>;
				using CmtStrp = StripComments<FailPolicy, LnCnt>;
				using OutSrc  = Buffer<pr::deque<wchar_t>, CmtStrp>;

				Src*    m_in;
				LnCnt   m_slc;
				CmtStrp m_sc;
				OutSrc  m_buf;
				bool    m_del;

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
			using MacroParams = typename Macro::Params;
			using MacroAncester = typename Macro::Ancestor;
			MacroDB m_macros;

			// A stack recording the 'inclusion' state of nested #if/#endif blocks
			using BitStack = pr::BitStack<>;
			BitStack m_if_stack;

			// Include handler
			using IncludeHandler = TIncludeHandler;
			IncludeHandler m_includes;

			// Embedded code handler
			using EmbeddedCodeHandler = TEmbeddedCodeHandler;
			EmbeddedCodeHandler m_embedded;

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
				,m_embedded()
				,m_dbg_buf()
				,m_dbg_src()
				,Macros(m_macros)
				,Includes(m_includes)
				,EmbeddedCode(m_embedded)
			{}
			Preprocessor(Preprocessor&& rhs)
				:Src(std::move(rhs))
				,m_stack(std::move(rhs.m_stack))
				,m_src(std::move(rhs.m_src))
				,m_macros(std::move(rhs.m_macros))
				,m_if_stack(std::move(rhs.m_if_stack))
				,m_includes(std::move(rhs.m_includes))
				,m_embedded(std::move(rhs.m_embedded))
				,m_dbg_buf(rhs.m_dbg_buf)
				,m_dbg_src(rhs.m_dbg_src)
				,Macros(m_macros)
				,Includes(m_includes)
				,EmbeddedCode(m_embedded)
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

			// Access the macro handler
			MacroDB& Macros;

			// Access the include handler
			IncludeHandler& Includes;

			// Access the embedded code handler
			EmbeddedCodeHandler& EmbeddedCode;

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

			// Returns true if src data is held in the internal buffer
			bool src_buffered() const
			{
				return !m_src->empty();
			}

		private:

			// Advance by 'n' characters, popping from the input stack as needed
			void next(int n = 1)
			{
				for (;m_src;)
				{
					// If the source has expired, pop from the stack until we find the next non-expired source
					if (**m_src == 0)
					{
						Pop();
						continue;
					}

					// Pop a character from the source
					if (n--)
					{
						++*m_src;
						continue;
					}
					break;
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
				next(n);

				// Buffered characters that get returned from this function are characters
				// that have been confirmed as ok to use. Buffering within the for-loop is
				// used however so don't put the empty() check in the for loop.
				if (m_src && src_buffered())
					return;

				for (; m_src;)
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
							EatLineSpace(src, 1, 0);

							// Match the preprocessor command
							switch (*src)
							{
							case L'd':
								#pragma region Define
								if (src.match(L"define", true))
								{
									EatLineSpace(src, 0, 0);
									m_macros.Add(Macro(src, src.Loc()));
									continue;
								}
								#pragma endregion
								#pragma region DefIfNDef
								if (src.match(L"defifndef", true))
								{
									EatLineSpace(src, 0, 0);
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
									else                  { m_if_stack.top() = true; EatLine(src, 0, 1); }
									continue;
								}
								#pragma endregion
								#pragma region ElIf
								if (src.match(L"elif", true))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #elif");
									if (m_if_stack.top())  { SkipPreprocessorBlock(loc_beg); }
									else if (!PPDefined()) { SkipPreprocessorBlock(loc_beg); }
									else                   { m_if_stack.top() = true; EatLine(src, 0, 1); }
									continue;
								}
								#pragma endregion
								#pragma region EndIf
								if (src.match(L"endif", true))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #endif");
									m_if_stack.pop();
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								#pragma region Eval
								if (src.match(L"eval", true))
								{
									EatLineSpace(src, 0, 0);
									pr::string<wchar_t> expr;

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
									RecursiveExpandMacros(expr, MacroAncester(nullptr, nullptr), loc_beg);

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
									auto eval_src = std::make_unique<Buffer<>>(ESrcType::Eval, std::begin(expr), std::end(expr));
									Push(eval_src.get(), true);
									eval_src.release();
									continue;
								}
								#pragma endregion
								#pragma region Embedded
								if (src.match(L"embedded", true))
								{
									// Read the embedded language used
									pr::string<wchar_t> lang;
									if (*src == L'(') ++src; else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang) ... #end");
									if (BufferIdentifier(src)) lang = src.str(), src.clear(); else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang) ... #end");
									if (*src == L')') ++src; else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang) ... #end");

									// Do not include the whitespace or blank line that follows #embedded(lang)
									EatLineSpace(src, 0,0);
									if (pr::str::IsNewLine(*src)) ++src;

									// Record the source location for the start of the code
									auto code_beg = src.Loc();

									// Buffer the code section up to (but not including) the #end
									if (!BufferTo(src, L"#end", false))
										return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "Embedded code section '#embedded(lang)' does not have a closing '#end' marker");

									// 'code' will contain the result of executing the code, which we want to treat like an expanded macro
									auto code = src.str(); src.clear();
									pr::string<wchar_t> result;

									// Expand any macros in the buffered text
									RecursiveExpandMacros(code, MacroAncester(nullptr, nullptr), loc_beg);

									// Get the code handler to transform the code into a result
									// Note, the code handler is expected to work or throw
									m_embedded.Execute(lang, code, code_beg, result);

									// Push the code result as a source
									auto code_src = std::make_unique<Buffer<>>(ESrcType::EmbeddedCode, std::begin(result), std::end(result));
									Push(code_src.get(), true);
									code_src.release();
									continue;
								}
								#pragma endregion
								#pragma region Error
								if (src.match(L"error", true))
								{
									EatLineSpace(src, 0,0);
									BufferLine(src);
									return FailPolicy::Fail(EResult::PreprocessError, loc_beg, Narrow(src.str()));
								}
								#pragma endregion
								break;
							case L'i':
								#pragma region IfNDef
								if (src.match(L"ifndef", true))
								{
									EatLineSpace(src, 0, 0);
									if (!BufferIdentifier(src)) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected");
									if (m_macros.Find(Hash(src)) == nullptr) { src.clear(); m_if_stack.push(true); EatLine(src, 0, 1); }
									else                                     { src.clear(); m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region IfDef
								if (src.match(L"ifdef", true))
								{
									EatLineSpace(src, 0, 0);
									if (!BufferIdentifier(src)) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected");
									if (m_macros.Find(Hash(src)) != nullptr) { src.clear(); m_if_stack.push(true); EatLine(src, 0, 1); }
									else                                            { src.clear(); m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region If
								if (src.match(L"if", true))
								{
									EatLineSpace(src, 0, 0);
									if (PPDefined()) { m_if_stack.push(true);  EatLine(src, 0, 1); }
									else             { m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region Include Path
								if (src.match(L"include_path", true))
								{
									EatLineSpace(src, 1, 0);
									if (*src != L'<' && *src != L'\"')
										return FailPolicy::Fail(EResult::InvalidInclude, src.Loc(), "expected a string following #include_path");

									// Buffer up the include path
									auto end = *src == L'<' ? L'>' : L'\"';
									int i = 1; for (; src[i] && src[i] != L'\n' && src[i] != end; ++i) {}
									if (src[i] != end) return FailPolicy::Fail(EResult::InvalidInclude, src.Loc(), "include path string incomplete");

									// Add the path to the include paths
									m_includes.AddSearchPath(src.str(1, i - 1));

									// Clear the buffered string and eat the rest of the line
									src.clear();
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								#pragma region Include
								if (src.match(L"include", true))
								{
									EatLineSpace(src, 1, 0);
									loc_beg = src.Loc();

									if (*src != L'<' && *src != L'\"')
										return FailPolicy::Fail(EResult::InvalidInclude, loc_beg, "expected a string following #include");

									// Buffer up the include filepath
									auto end = *src == L'<' ? L'>' : L'\"';
									int i = 1; for (; src[i] && src[i] != L'\n' && src[i] != end; ++i) {}
									if (src[i] != end) return FailPolicy::Fail(EResult::InvalidInclude, loc_beg, "include string incomplete");
									auto filepath = src.str(1, i - 1);
									src.clear();

									// Open the include
									auto search_paths_only = end == L'>';
									auto inc = m_includes.Open(filepath, search_paths_only, src.Loc());
									if (inc)
									{
										Push(inc.get(), true);
										inc.release();
									}
									continue;
								}
								#pragma endregion
								break;
							case L'l':
								#pragma region Lit
								if (src.match(L"lit", true))
								{
									// Do not include the single whitespace or blank line that follows #lit
									EatLineSpace(src, 0,0);
									if (pr::str::IsNewLine(*src)) ++src;

									// Buffer a literal section up to (but not including) the #end
									if (!BufferTo(src, L"#end", false))
										return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "Literal section '#lit' does not have a closing '#end' marker");

									continue;
								}
								#pragma endregion
								#pragma region Line
								if (src.match(L"line", true))
								{
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'p':
								#pragma region Pragma
								if (src.match(L"pragma", true))
								{
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'u':
								#pragma region Undef
								if (src.match(L"undef", true))
								{
									EatLineSpace(src, 0, 0);

									// Read the macro tag
									auto tag = Macro::ReadTag(src, src.Loc());
									m_macros.Remove(Hash(tag));

									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'w':
								#pragma region Warning
								if (src.match(L"warning", true))
								{
									EatLine(src, 0, 1);
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
					default:
						#pragma region Expand Macros
						// Look for possible macro identifiers in the source (not within expanded macros)
						if (src.Type() != ESrcType::Macro && pr::str::IsIdentifier(*src, true))
						{
							// Buffer the identifier
							BufferIdentifier(src);

							// See if the identifier matches any macro definitions
							auto macro = m_macros.Find(Hash(src));
							if (macro)
							{
								// This is a macro, so remove the macro identifier from the buffer
								src.clear();

								// If the macro requires parameters see if we can read them from the source
								MacroParams params;
								if (macro->ReadParams<false>(src, params, src.Loc()))
								{
									// Create a buffered string source for the expanded macro
									// and get the macro to generate it's expanded version.
									pr::string<wchar_t> exp;
									macro->Expand(exp, params);
									RecursiveExpandMacros(exp, MacroAncester(macro, nullptr), src.Loc());

									// Push the expanded macro as a source
									auto macro_src = std::make_unique<Buffer<>>(ESrcType::Macro, std::begin(exp), std::end(exp));
									Push(macro_src.get(), true);
									macro_src.release();
								}
							}
						}
						break;
						#pragma endregion
					}

					// If we get here, then 'src[0]' is a valid output character
					break;
				}
			}

			// Recursively expand the expression in 'exp' with macro substitutions
			void RecursiveExpandMacros(pr::string<wchar_t>& exp, MacroAncester const& parent, Location const& loc)
			{
				pr::string<wchar_t> subexp;
				MacroParams params;

				// Scan through 'exp' looking for macro identifiers
				for (auto i = exp.c_str(); *i;)
				{
					if (!pr::str::IsIdentifier(*i, true)) { ++i; continue; }

					auto beg = i;

					// Found the start of an identifier, see if it's a macro identifier
					for (++i; *i && pr::str::IsIdentifier(*i, false); ++i) {}

					// Find the macro?
					auto macro = m_macros.Find(Hash(beg, i));
					if (!macro) continue;

					// Check whether this macro is an ancestor
					if (parent.IsRecursive(macro))
						continue; // a recursive substitution.. ignore

					// Check the correct parameters have been given
					params.clear();
					if (!macro->ReadParams<false>(i, params, loc))
						continue;

					// Recursively expand the macro into a temporary buffer
					subexp.resize(0); // reuse
					macro->Expand(subexp, params);
					RecursiveExpandMacros(subexp, MacroAncester(macro, &parent), loc);

					// Substitute the expanded macro into 'src'
					auto len = i - beg;
					auto ofs = beg - exp.c_str();
					exp.erase (ofs, len);
					exp.insert(ofs, subexp);
					i = exp.c_str() + ofs + subexp.size();
				}
			}

			// Parse the line following an #if or #elif statement, returning true if the expression evaluates to true
			bool PPDefined()
			{
				auto& src = *m_src;
				pr::string<wchar_t> expr, exp;

				// Read the whole line into a string generating a numeric expression
				for (EatLineSpace(src, 0,0); !pr::str::IsNewLine(*src); EatLineSpace(src, 0,0))
				{
					// Append operators to the expression
					if (!pr::str::IsIdentifier(*src, true))
					{
						expr.push_back(*src);
						++src;
						continue;
					}

					// Read the macro or keyword identifier
					if (!BufferIdentifier(src))
						return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected"), false;

					// If the identifier is the keyword 'defined' then it should be followed
					// by an identifier optionally enclosed within '(' and ')'
					if (src.match(L"defined", true))
					{
						EatLineSpace(src, 0, 0);

						// Check for optional '()'
						auto wrapped = *src == '(';
						if (wrapped) ++src;

						// Read the identifier within the defined() expression
						if (!BufferIdentifier(src)) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected"), false;
						auto hash = Hash(src);
						src.clear();

						// Check the optional brackets are matched
						if (wrapped) if (*src == ')') ++src; else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "unmatched ')'"), false;

						// If the macro is defined, add a 1 to the expression
						expr.push_back(m_macros.Find(hash) != nullptr ? '1' : '0');
					}
					
					// Otherwise substitute the macro
					else
					{
						auto macro = m_macros.Find(Hash(src));
						if (!macro) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), pr::FmtS("Identifier '%s' is not defined", pr::Narrow(exp).c_str())), false;
						src.clear();

						// Read macro parameters if it has them
						MacroParams params;
						if (!macro->ReadParams<false>(src, params, src.Loc()))
							return FailPolicy::Fail(EResult::ParameterCountMismatch, src.Loc(), pr::FmtS("Missing parameters for macro %s. Expected %d", pr::Narrow(exp).c_str(), macro->m_params.size())), false;

						// Expand the macro with the given parameters
						exp.resize(0);
						macro->Expand(exp, params);

						// Recursively expand macros within 'exp'.
						RecursiveExpandMacros(exp, Macro::Ancestor(macro, nullptr), src.Loc());

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
					if (*src == L'\"')
					{
						if (!EatLiteralString(src)) FailPolicy::Fail(EResult::InvalidString, src.Loc(), "Literal string expected");
						continue;
					}
					if (*src != L'#')  { ++src; continue; }

					++src;
					EatLineSpace(src, 0, 0);
					nest += int(src.match(L"ifndef") || src.match(L"ifdef") || src.match(L"if"));
					nest -= int(src.match(L"endif") || (nest == 1 && (src.match(L"elif") || src.match(L"else"))));
					if (nest == 0)
					{
						src.push_front(L'#');
						break;
					}

					src.clear();
					++src;
				}
				if (*src == 0)
					return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, beg, "Unmatched #if, #ifdef, #ifndef, #else, or #elid");
			}
		};
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/script2/embedded_lua.h"

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
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
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

				Preprocessor<> pp(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
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
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
			{// Multi-line preprocessor
				char const* str_in =
					"#define ml\\\n"
					"  MULTI\\\n"
					"LINE\n"
					"ml";
				char const* str_out =
					"MULTILINE";

				Preprocessor<> pp(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
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

				Preprocessor<> pp(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
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

				Preprocessor<> pp(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
			{// #eval
				char const* str_in =
					"#eval{1+#eval{1+1}}\n"
					;
				char const* str_out =
					"3\n"
					;

				Preprocessor<> pp(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
			{// recursive macros/evals
				char const* str_in =
					"#define X 3.0\n"
					"#define Y 4.0\n"
					"#define Len2 #eval{len2(X,Y)}\n"
					"#eval{X + Len2}\n";
				char const* str_out =
					"8\n";

				Preprocessor<> pp(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
			{// #if/#else/#etc
				char const* str_in =
					"#  define ONE 1 // ignore me \n"
					"#  define NOT_ONE (!ONE) /*and me*/ \n"
					"#\tdefine PLUS(x,y) (x)+(y) xx 0x _0x  \n"
					"#ifdef ZERO\n"
					"	#if NESTED\n"
					"		not output \"ignore #else\" \n"
					"	#endif\n"
					"#elif (!NOT_ONE) && defined(PLUS)\n"
					"	output\n"
					"#else\n"
					"	not output\n"
					"#endif\n"
					"#ifndef ZERO\n"
					"	#if defined(ZERO) || defined(PLUS)\n"
					"		output this\n"
					"	#else\n"
					"		but not this\n"
					"	#endif\n"
					"#endif\n"
					"#undef ONE\n"
					"#ifdef ONE\n"
					"	don't output\n"
					"#endif\n"
					"#define TWO\n"
					"#ifdef TWO\n"
					"	two defined\n"
					"#endif\n"
					"#defifndef ONE 1\n"
					"#defifndef ONE 2\n"
					"ONE\n"
					;
				char const* str_out =
					"	output\n"
					"	"//#if defined(ZERO) || ...
					"		output this\n"
					"	"//#else\n
					"	two defined\n"
					"1\n"
					;

				Preprocessor<> pp(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
			{// includes
				char const* str_in =
					"#  define ONE 1 // ignore me \n"
					"#include \"inc\"\n"
					;
				char const* str_out =
					"included 1\n"
					;

				Preprocessor<ThrowOnFailure, StrIncludes<>> pp;
				pp.Includes.m_strings[L"inc"] = L"included ONE";
				pp.Push(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
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
					"#embedded(lua) --lua code\n return \"hello world\" #end\n"
					;
				char const* str_out =
					"\"#error this would throw an error\"\n"
					"lastword"
					"4\n"
					"2\n"
					"Any old ch*rac#ers #if I {feel} #include --cheese like #en\n"
					"hello world\n"
					;

				Preprocessor<ThrowOnFailure, StrIncludes<>, MacroDB<>, EmbeddedLua<>> pp;
				pp.Includes.m_strings[L"inc"] = L"included ONE";
				pp.Push(str_in);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
		}
	}
}
#endif
