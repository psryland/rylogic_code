//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/script_core.h"
#include "pr/script/fail_policy.h"
#include "pr/script/filter.h"
#include "pr/script/macros.h"
#include "pr/script/includes.h"
#include "pr/script/embedded.h"

namespace pr
{
	namespace script
	{
		// Takes a character stream and performs preprocessing on it.
		// This is a super-set of a C/C++ preprocessor.
		template<typename FailPolicy = ThrowOnFailure>
		struct Preprocessor :Src
		{
		private:
			// A source wrapper that strips line continuations and comments
			struct PPSource :Src
			{
				using LnCnt   = StripLineContinuations<Src>;
				using CmtStrp = StripComments<FailPolicy>;
				using BufType = pr::deque<wchar_t>;
				using OutSrc  = Buffer<BufType>;
				using Emit    = EmitCount;

				Src*    m_in;
				bool    m_del;
				LnCnt   m_slc;
				CmtStrp m_sc;
				OutSrc  m_out;
				Emit    m_emit; // The input or read position in 'm_out'

				~PPSource()
				{
					if (m_del)
						delete m_in;
				}
				PPSource(Src* src, bool del)
					:Src(src->Type(), Location())
					,m_in (src)
					,m_del(del)
					,m_slc(*m_in)
					,m_sc (m_slc)
					,m_out(m_sc)
					,m_emit()
				{}
				PPSource(PPSource&& rhs)
					:Src   (std::move(rhs))
					,m_in  (std::move(rhs.m_in ))
					,m_del (std::move(rhs.m_del))
					,m_slc (std::move(rhs.m_slc))
					,m_sc  (std::move(rhs.m_sc ))
					,m_out (std::move(rhs.m_out))
					,m_emit(std::move(rhs.m_emit))
				{
					m_slc.m_src = m_in;
					m_sc.m_src  = &m_slc;
					m_out.m_src = &m_sc;
					rhs.m_in    = nullptr;
					rhs.m_del   = false;
				}
				PPSource(PPSource const&) = delete;
				PPSource& operator =(PPSource&& rhs)
				{
					if (this != &rhs)
					{
						std::swap(m_in  , rhs.m_in);
						std::swap(m_del , rhs.m_del);
						std::swap(m_slc , rhs.m_slc);
						std::swap(m_sc  , rhs.m_sc);
						std::swap(m_out , rhs.m_out);
						std::swap(m_emit, rhs.m_emit);
						m_slc.m_src = m_in;
						m_sc.m_src  = &m_slc;
						m_out.m_src = &m_sc;
					}
					return *this;
				}
				PPSource& operator =(PPSource const&) = delete;

				// Debugging helper interface
				Location const& Loc() const override
				{
					return m_out.Loc();
				}
				SrcConstPtr DbgPtr() const override
				{
					return m_out.DbgPtr();
				}
				BufType const* DbgBuf() const
				{
					return m_out.DbgBuf();
				}

				// Pointer-like interface
				wchar_t operator * () const override { return *m_out; }
				PPSource& operator ++() override     { ++m_out; --m_emit; return *this; }

				// Array access to the buffered data. Buffer size grows to accommodate 'i'
				wchar_t operator [](size_t i) const { return m_out[i]; }
				wchar_t& operator [](size_t i)      { return m_out[i]; }

				// Push a character back onto the stream
				void push_front(wchar_t ch)
				{
					m_out.push_front(ch);
					++m_emit;
				}

				// Pop 'count' characters from the front of the stream
				void pop(size_t count = ~size_t())
				{
					if (count == ~size_t()) count = m_emit;
					m_out.pop_front(count);
					m_emit -= int(count);
				}

				// Returning [ofs, ofs+count) as a string
				string str(size_t ofs = 0, size_t count = ~size_t())
				{
					if (count == ~size_t()) count = m_emit;
					return m_out.str(ofs, count);
				}

				// Pop 'ofs + count' characters from the front of the stream returning [ofs, ofs+count) as a string
				string pop_str(size_t ofs = 0, size_t count = ~size_t())
				{
					if (count == ~size_t()) count = m_emit;
					auto s = str(ofs, count);
					pop(count);
					return s;
				}

				// Erase a range within the buffered characters
				void erase(size_t ofs = 0, size_t count = ~size_t())
				{
					if (count == ~size_t()) count = m_emit;
					m_out.erase(ofs, count);
					if (m_emit >= ofs) m_emit -= int(count);
				}

				// sub-string match
				template <typename Str> int match_emit(Str const& str, bool emit_if_match = true)
				{
					auto r = m_out.match(str);
					if (emit_if_match && int(m_emit) < r) m_emit = r;
					return r;
				}
				template <typename Char> int match_emit(Char const* str, bool emit_if_match = true)
				{
					auto r = m_out.match(str);
					if (emit_if_match && int(m_emit) < r) m_emit = r;
					return r;
				}
				template <typename Str> int match_adv(Str const& str, bool adv_if_match = true)
				{
					auto r = m_out.match(str, adv_if_match);
					if (adv_if_match) m_emit -= r;
					return r;
				}
			};

			// The stack of input streams. Streams are pushed/popped from
			// the stack as files are opened, or macros are evaluated.
			pr::vector<PPSource> m_stack;

			// A stack recording the 'inclusion' state of nested #if/#endif blocks
			using BitStack = pr::BitStack<>;
			BitStack m_if_stack;

			// Default handlers
			MacroDB<> m_def_macros;
			Includes<FailPolicy> m_def_includes;
			EmbeddedCode<FailPolicy> m_def_embedded;

			// Debugging helpers for the watch window
			SrcConstPtr m_dbg_src;
			typename PPSource::OutSrc::buffer_type const* m_dbg_buf;

		public:
			Preprocessor(IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, IEmbeddedCode* emb = nullptr)
				:Src(ESrcType::Preprocessor, Location())
				,m_stack()
				,m_if_stack()
				,m_def_macros()
				,m_def_includes()
				,m_def_embedded()
				,m_dbg_buf()
				,m_dbg_src()
				,Macros(mac ? mac : &m_def_macros)
				,Includes(inc ? inc : &m_def_includes)
				,EmbeddedCode(emb ? emb : &m_def_embedded)
			{}
			Preprocessor(Preprocessor&& rhs) = delete;
			Preprocessor(Preprocessor const&) = delete;
			Preprocessor& operator =(Preprocessor&& rhs) = delete;
			Preprocessor& operator =(Preprocessor const& rhs) = delete;

			Preprocessor(Src* src, bool delete_on_pop, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, IEmbeddedCode* emb = nullptr)
				:Preprocessor(inc, mac, emb)
			{
				Push(src, delete_on_pop);
			}
			Preprocessor(Src& src, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, IEmbeddedCode* emb = nullptr)
				:Preprocessor(inc, mac, emb)
			{
				Push(src);
			}
			template <typename Char, typename = pr::str::enable_if_char_t<Char>>
			Preprocessor(Char const* src, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, IEmbeddedCode* emb = nullptr)
				:Preprocessor(inc, mac, emb)
			{
				Push(src);
			}

			// Access the macro handler
			IMacroHandler* Macros;

			// Access the include handler
			IIncludeHandler* Includes;

			// Access the embedded code handler
			IEmbeddedCode* EmbeddedCode;

			// Push a source onto the input stack
			void Push(Src* src, bool delete_on_pop)
			{
				m_stack.emplace_back(src, delete_on_pop);

				// Assign helper pointer for the watch windows
				m_dbg_src = m_stack.back().m_out.DbgPtr();
				m_dbg_buf = m_stack.back().m_out.DbgBuf();

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

				// Assign helper pointer for the watch windows
				m_dbg_buf = !m_stack.empty() ? m_stack.back().m_out.DbgBuf() : nullptr;
				m_dbg_src = !m_stack.empty() ? m_stack.back().m_out.DbgPtr() : SrcConstPtr();

				seek(0);
			}

			// Source type is the type of the stream on the top of the stack
			ESrcType Type() const override
			{
				return !m_stack.empty() ? m_stack.back().Type() : ESrcType::Unknown;
			}
			SrcConstPtr DbgPtr() const override
			{
				return !m_stack.empty() ? m_stack.back().DbgPtr() : SrcConstPtr();
			}
			Location const& Loc() const override
			{
				static Location s_null_loc;
				return !m_stack.empty() ? m_stack.back().Loc() : s_null_loc;
			}

			// Pointer-like interface
			wchar_t operator *() const override
			{
				return !m_stack.empty() ? *m_stack.back() : wchar_t();
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
				for (;!m_stack.empty();)
				{
					auto& src = m_stack.back();

					// If the source has expired, pop from the stack until we find the next non-expired source
					if (*src == 0)
					{
						Pop();
						continue;
					}

					// Pop a character from the source
					if (n--)
					{
						++src;
						continue;
					}
					break;
				}
				assert(m_stack.empty() || *m_stack.back() != 0 && "Should never return an end of stream from here");
			}

			// Advance the source by at least 'n' preprocessed characters ensuring
			// the next character to be returned is a valid preprocessed character
			void seek(int n)
			{
				// Use in watch windows:
				//  m_dbg_buf  <- buffered characters
				//  m_dbg_src  <- input stream
				next(n);

				// Emit characters while 'm_emit' is > 0.
				// 'm_emit' is a count of the number of characters that have been confirmed as ok to use.
				for (; !m_stack.empty() && m_stack.back().m_emit == 0; )
				{
					// This reference can't be used after 'next' has been called.
					// However, none of these tokens should span source boundaries.
					auto& src = m_stack.back();
					auto& emit = src.m_emit;

					// Cases should end with 'continue' to go round again or break to emit the current character
					switch (*src)
					{
					case L'\"':
					case L'\'':
						#pragma region Literal String/Char
						{
							// Buffer the literal string or char
							auto beg = src.Loc();
							BufferLiteral(src, emit, *src);
							if (src[emit] != 0) ++emit; else return FailPolicy::Fail(EResult::SyntaxError, Loc(), pr::Fmt("Unclosed literal string or character\n%s", Narrow(beg.ToString()).c_str()));
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
								if (src.match_adv(L"define"))
								{
									EatLineSpace(src, 0, 0);
									Macros->Add(Macro(src, src.Loc()));
									continue;
								}
								#pragma endregion
								#pragma region DefIfNDef
								if (src.match_adv(L"defifndef"))
								{
									EatLineSpace(src, 0, 0);
									Macro macro(src, src.Loc());
									if (!Macros->Find(macro.m_hash))
										Macros->Add(macro);
									continue;
								}
								#pragma endregion
								break;
							case L'e':
								#pragma region Else
								if (src.match_adv(L"else"))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #else");
									if (m_if_stack.top()) { SkipPreprocessorBlock(loc_beg); }
									else                  { m_if_stack.top() = true; EatLine(src, 0, 1); }
									continue;
								}
								#pragma endregion
								#pragma region ElIf
								if (src.match_adv(L"elif"))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #elif");
									if (m_if_stack.top())  { SkipPreprocessorBlock(loc_beg); }
									else if (!PPDefined()) { SkipPreprocessorBlock(loc_beg); }
									else                   { m_if_stack.top() = true; EatLine(src, 0, 1); }
									continue;
								}
								#pragma endregion
								#pragma region EndIf
								if (src.match_adv(L"endif"))
								{
									if (m_if_stack.empty()) return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #endif");
									m_if_stack.pop();
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								#pragma region Eval
								if (src.match_adv(L"eval"))
								{
									EatLineSpace(src, 0, 0);
									string expr;

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
									RecursiveExpandMacros(expr, Macro::Ancestor(nullptr, nullptr), loc_beg);

									// Replace any nested #eval{exp} with (exp)
									pr::str::Replace(expr, L"#eval", L"" );
									pr::str::Replace(expr, L"{"    , L"(");
									pr::str::Replace(expr, L"}"    , L")");

									// Evaluate the expression and push the result as a string onto the stack
									double result;
									if (!pr::Evaluate(expr.c_str(), result))
										return FailPolicy::Fail(EResult::ExpressionSyntaxError, loc_beg, "#eval expression cannot be evaluated");

									// Convert the result to a string
									expr = (static_cast<long long>(result) == result) ? pr::FmtS(L"%lld", static_cast<long long>(result)) : pr::FmtS(L"%f", result);

									// Push the eval source onto the input stack
									auto eval_src = std::make_unique<Buffer<>>(ESrcType::Eval, std::begin(expr), std::end(expr));
									Push(eval_src.get(), true);
									eval_src.release();
									continue;
								}
								#pragma endregion
								#pragma region Embedded
								if (src.match_adv(L"embedded"))
								{
									// Read the embedded language used
									string lang;
									if (*src == L'(') ++src; else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang) ... #end");
									if (BufferIdentifier(src, emit)) lang = src.pop_str(); else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang) ... #end");
									if (*src == L')') ++src; else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang) ... #end");

									// Do not include the whitespace or blank line that follows #embedded(lang)
									EatLineSpace(src, 0,0);
									if (pr::str::IsNewLine(*src)) ++src;

									// Record the source location for the start of the code
									auto code_beg = src.Loc();

									// Buffer the code section up to (but not including) the #end
									if (!BufferTo(src, emit, L"#end", false))
										return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "Embedded code section '#embedded(lang)' does not have a closing '#end' marker");

									// 'code' will contain the result of executing the code, which we want to treat like an expanded macro
									auto code = src.pop_str();
									string result;

									// Expand any macros in the buffered text
									RecursiveExpandMacros(code, Macro::Ancestor(nullptr, nullptr), loc_beg);

									// Get the code handler to transform the code into a result
									// Note, the code handler is expected to work or throw
									if (!EmbeddedCode->Execute(lang, code, code_beg, result))
										return FailPolicy::Fail(EResult::EmbeddedCodeNotSupported, loc_beg, "No support for embedded code available");

									// Push the code result as a source
									auto code_src = std::make_unique<Buffer<>>(ESrcType::EmbeddedCode, std::begin(result), std::end(result));
									Push(code_src.get(), true);
									code_src.release();
									continue;
								}
								#pragma endregion
								#pragma region Error
								if (src.match_adv(L"error"))
								{
									EatLineSpace(src, 0,0);
									BufferLine(src, emit);
									return FailPolicy::Fail(EResult::PreprocessError, loc_beg, Narrow(src.str()));
								}
								#pragma endregion
								break;
							case L'i':
								#pragma region IfNDef
								if (src.match_adv(L"ifndef"))
								{
									EatLineSpace(src, 0, 0);
									if (!BufferIdentifier(src, emit)) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected");
									if (Macros->Find(Hash(src.pop_str()))) { m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									else                                   { m_if_stack.push(true); EatLine(src, 0, 1); }
									continue;
								}
								#pragma endregion
								#pragma region IfDef
								if (src.match_adv(L"ifdef"))
								{
									EatLineSpace(src, 0, 0);
									if (!BufferIdentifier(src, emit)) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected");
									if (Macros->Find(Hash(src.pop_str()))) { m_if_stack.push(true); EatLine(src, 0, 1); }
									else                                   { m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region If
								if (src.match_adv(L"if"))
								{
									EatLineSpace(src, 0, 0);
									if (PPDefined()) { m_if_stack.push(true);  EatLine(src, 0, 1); }
									else             { m_if_stack.push(false); SkipPreprocessorBlock(loc_beg); }
									continue;
								}
								#pragma endregion
								#pragma region Include Path
								if (src.match_adv(L"include_path"))
								{
									EatLineSpace(src, 1, 0);
									if (*src != L'<' && *src != L'\"')
										return FailPolicy::Fail(EResult::InvalidInclude, src.Loc(), "expected a string following #include_path");

									// Buffer up the include path
									auto end = *src == L'<' ? L'>' : L'\"';
									BufferLiteral(src, emit, end);
									if (src[emit] == end) ++emit; else return FailPolicy::Fail(EResult::InvalidInclude, src.Loc(), "include path string incomplete");

									// Add the path to the include paths
									auto path = src.str(1, emit - 2); src.pop();
									Includes->AddSearchPath(path);

									// Eat the rest of the line
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								#pragma region Include
								if (src.match_adv(L"include"))
								{
									EatLineSpace(src, 1, 0);
									loc_beg = src.Loc();

									if (*src != L'<' && *src != L'\"')
										return FailPolicy::Fail(EResult::InvalidInclude, loc_beg, "expected a string following #include");

									// Buffer up the include filepath
									auto end = *src == L'<' ? L'>' : L'\"';
									BufferLiteral(src, emit, end);
									if (src[emit] == end) ++emit; else return FailPolicy::Fail(EResult::InvalidInclude, loc_beg, "include string incomplete");

									// Open the include
									auto path = src.str(1, emit - 2); src.pop();
									auto flags = end == L'\"' ? IIncludeHandler::EFlags::IncludeLocalDir : IIncludeHandler::EFlags::None;
									auto inc = Includes->Open(path, flags, src.Loc());
									if (inc)
										Push(inc.release(), true);

									continue;
								}
								#pragma endregion
								break;
							case L'l':
								#pragma region Lit
								if (src.match_adv(L"lit"))
								{
									// Do not include the single whitespace or blank line that follows #lit
									EatLineSpace(src, 0,0);
									if (pr::str::IsNewLine(*src)) ++src;

									// Buffer a literal section up to (but not including) the #end
									if (!BufferTo(src, emit, L"#end", false))
										return FailPolicy::Fail(EResult::UnmatchedPreprocessorDirective, loc_beg, "Literal section '#lit' does not have a closing '#end' marker");

									continue;
								}
								#pragma endregion
								#pragma region Line
								if (src.match_adv(L"line"))
								{
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'p':
								#pragma region Pragma
								if (src.match_adv(L"pragma"))
								{
									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'u':
								#pragma region Undef
								if (src.match_adv(L"undef"))
								{
									EatLineSpace(src, 0, 0);

									// Read the macro tag
									auto tag = Macro::ReadTag(src, src.Loc());
									Macros->Remove(Hash(tag));

									EatLine(src, 0, 1);
									continue;
								}
								#pragma endregion
								break;
							case L'w':
								#pragma region Warning
								if (src.match_adv(L"warning"))
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
							assert(emit == 0);

							// Buffer the identifier
							BufferIdentifier(src, emit);

							// See if the identifier matches any macro definitions
							auto macro = Macros->Find(Hash(src.str()));
							if (macro)
							{
								// This is a macro, so remove the macro identifier from the buffer
								src.pop();

								// If the macro requires parameters see if we can read them from the source
								Macro::Params params;
								if (macro->ReadParams<false>(src, params, src.Loc()))
								{
									// Create a buffered string source for the expanded macro
									// and get the macro to generate it's expanded version.
									string exp;
									macro->Expand(exp, params);
									RecursiveExpandMacros(exp, Macro::Ancestor(macro, nullptr), src.Loc());

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
			void RecursiveExpandMacros(string& exp, Macro::Ancestor const& parent, Location const& loc)
			{
				string subexp;
				Macro::Params params;

				// Scan through 'exp' looking for macro identifiers
				for (auto i = exp.c_str(); *i;)
				{
					if (!pr::str::IsIdentifier(*i, true)) { ++i; continue; }

					auto beg = i;

					// Found the start of an identifier, see if it's a macro identifier
					for (++i; *i && pr::str::IsIdentifier(*i, false); ++i) {}

					// Find the macro?
					auto macro = Macros->Find(Hash(beg, i));
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
					RecursiveExpandMacros(subexp, Macro::Ancestor(macro, &parent), loc);

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
				auto& src = m_stack.back();
				auto& emit = src.m_emit;
				string expr, exp;

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
					if (!BufferIdentifier(src, emit))
						return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected"), false;

					// If the identifier is the keyword 'defined' then it should be followed
					// by an identifier optionally enclosed within '(' and ')'
					if (src.match_adv(L"defined"))
					{
						EatLineSpace(src, 0, 0);

						// Check for optional '()'
						auto wrapped = *src == '(';
						if (wrapped) ++src;

						// Read the identifier within the defined() expression
						if (!BufferIdentifier(src, emit)) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "An identifier was expected"), false;
						auto hash = Hash(src.pop_str());

						// Check the optional brackets are matched
						if (wrapped) if (*src == ')') ++src; else return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), "unmatched ')'"), false;

						// If the macro is defined, add a 1 to the expression
						expr.push_back(Macros->Find(hash) != nullptr ? '1' : '0');
					}
					
					// Otherwise substitute the macro
					else
					{
						auto macro = Macros->Find(Hash(src.str()));
						if (!macro) return FailPolicy::Fail(EResult::InvalidPreprocessorDirective, src.Loc(), pr::FmtS("Identifier '%s' is not defined", pr::Narrow(exp).c_str())), false;
						src.pop();

						// Read macro parameters if it has them
						Macro::Params params;
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
				auto& src = m_stack.back();
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
					nest += int(src.match_emit(L"ifndef") || src.match_emit(L"ifdef") || src.match_emit(L"if"));
					nest -= int(src.match_emit(L"endif") || (nest == 1 && (src.match_emit(L"elif") || src.match_emit(L"else"))));
					if (nest == 0)
					{
						src.push_front(L'#');
						src.m_emit = 0; // reset emit so the seek() loop processes this next pp command
						break;
					}

					src.pop();
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
#include "pr/script/embedded_lua.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_input_stack)
		{
			using namespace pr::str;
			using namespace pr::script;
			
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
		PRUnitTest(pr_script_preprocessor)
		{
			using namespace pr::script;

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
					"# define    ONE  1\n" // same definition, allowed
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

				Includes<> inc; inc.AddString(L"inc", "included ONE");
				PtrA src(str_in);
				Preprocessor<> pp(&src, false, &inc);
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

				Includes<> inc;
				EmbeddedLua<> emb;
				Preprocessor<> pp(str_in, &inc, nullptr, &emb);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
			{// Using a preloaded buffer
				char const* str_in =
					"#define BOB(x) #x\n"
					"BOB(this is a string)\n"
					;
				char const* str_out =
					"\"this is a string\"\n"
					;

				Buffer<> buf(ESrcType::Buffered, str_in);
				Preprocessor<> pp(&buf, false);
				for (;*pp && *str_out; ++pp, ++str_out)
				{
					if (*pp == *str_out) continue;
					PR_CHECK(*pp, *str_out);
				}
				PR_CHECK(*str_out == 0 && *pp == 0, true);
			}
			{// X Macros
				char const* str_in =
					"#define LINE(x) x = #x\n"
					"#define DEFINE(values) values(LINE)\n"
					"#define Thing(x)\\\n"
					"	x(One)\\\n"
					"	x(Two)\\\n"
					"	x(Three)\n"
					"DEFINE(Thing)\n"
					"#undef Thing\n"
					;
				char const* str_out =
					"One = \"One\"	Two = \"Two\"	Three = \"Three\"\n"
					;
				Preprocessor<> pp(str_in);
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
