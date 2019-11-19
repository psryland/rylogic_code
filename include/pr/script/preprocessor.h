//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/script_core.h"
#include "pr/script/fail_policy.h"
#include "pr/script/filter.h"
#include "pr/script/src_stack.h"
#include "pr/script/macros.h"
#include "pr/script/includes.h"
#include "pr/script/embedded.h"

namespace pr::script
{
	// Takes a character stream and performs preprocessing on it.
	struct Preprocessor :Src
	{
		// Notes:
		//  - This is a super-set of a C/C++ preprocessor.
		//  - Line continuations have the highest precedence. They are applied to
		//    the input stream before considering literals and preprocessor directives.
		//  - 

	private:

		// A source wrapper that strips line continuations and comments
		struct Input :Src
		{
			// The number of characters to output before retesting 'IsOutputChar'
			int m_emit;

			// True if 'm_src' should be deleted on destruction
			bool m_owns_src;

			// True if this source is an expanded macro
			bool m_is_macro;

			// Source wrappers
			StripLineContinuations m_slc;
			StripComments m_out;

			Input(Src& src, bool owns_src, bool is_macro)
				:Src(src, EEncoding::already_decoded)
				,m_emit()
				,m_owns_src(owns_src)
				,m_is_macro(is_macro)
				,m_slc(m_src)
				,m_out(m_slc)
			{}
			~Input()
			{
				// Some wrapped sources are allocated with 'new'
				// and ownership is passed to this object.
				if (m_owns_src)
					delete &m_src;
			}

			// No copy or move
			Input(Input&& rhs) = delete;
			Input(Input const&) = delete;

		private:

			// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
			int Read() override
			{
				auto const ch = *m_out;
				if (ch != '\0') ++m_out;
				return ch;
			}
		};

		// The stack of input streams. Streams are pushed/popped from the stack as files are opened, or macros are evaluated.
		// Not using 'SrcStack' because 'emplace_back' is needed. 'Input' does not allow copy due to potentially needing to
		// delete the wrapped source in the destructor. Using 'list' because pushing a source needs to not invalidate sources
		// lower on the stack.
		using SourceStack = std::list<Input>;
		SourceStack m_stack;

		// A stack recording the 'inclusion' state of nested #if/#endif blocks
		using BitStack = pr::BitStack<>;
		BitStack m_if_stack;

		// A map of embedded code handlers
		using EmbeddedCodeHandlerCont = pr::vector<std::unique_ptr<IEmbeddedCode>>;
		EmbeddedCodeFactory m_emb_factory;
		EmbeddedCodeHandlerCont m_emb_handlers;

		// Preprocessor macro handler
		MacroDB m_macros;

		// Default handlers
		Includes m_def_includes;
		
		// Ignore missing includes or embedded code without handlers
		bool m_ignore_missing;

	public:

		Preprocessor(IIncludeHandler* inc = nullptr, EmbeddedCodeFactory emb = nullptr)
			:Src(EEncoding::already_decoded, Loc())
			,m_stack()
			,m_if_stack()
			,m_emb_factory(emb)
			,m_emb_handlers()
			, m_macros()
			,m_def_includes()
			,m_ignore_missing()
			,Includes(inc ? inc : &m_def_includes)
		{}
		Preprocessor(Src* src, bool delete_on_pop, IIncludeHandler* inc = nullptr, EmbeddedCodeFactory emb = nullptr)
			:Preprocessor(inc, emb)
		{
			Push(src, delete_on_pop, false);
		}
		Preprocessor(Src& src, IIncludeHandler* inc = nullptr, EmbeddedCodeFactory emb = nullptr)
			:Preprocessor(inc, emb)
		{
			Push(src);
		}
		Preprocessor(std::string_view src, IIncludeHandler* inc = nullptr, EmbeddedCodeFactory emb = nullptr)
			:Preprocessor(inc, emb)
		{
			Push(src);
		}
		Preprocessor(std::wstring_view src, IIncludeHandler* inc = nullptr, EmbeddedCodeFactory emb = nullptr)
			:Preprocessor(inc, emb)
		{
			Push(src);
		}

		// No copying or moving
		Preprocessor(Preprocessor&& rhs) = delete;
		Preprocessor(Preprocessor const&) = delete;
		Preprocessor& operator =(Preprocessor&& rhs) = delete;
		Preprocessor& operator =(Preprocessor const& rhs) = delete;

		// Access the include handler
		IIncludeHandler* const Includes;

		// The current position in the root underlying source
		Loc Location() const noexcept override
		{
			// The location within the source on the top of the stack
			return !m_stack.empty() ? m_stack.back().Location() : Loc();
		}

		// Push a source owned externally onto the input stack
		void Push(Src& src)
		{
			Push(&src, false, false);
		}

		// Push a simple character string as a source
		void Push(std::string_view src)
		{
			Push(new StringSrc(src), true, false);
		}
		void Push(std::wstring_view src)
		{
			Push(new StringSrc(src), true, false);
		}

		// Push a source onto the input stack
		void Push(Src* src, bool delete_on_pop, bool is_macro)
		{
			m_stack.emplace_back(*src, delete_on_pop, is_macro);
		}

		// Pop the top source off the input stack
		void Pop()
		{
			m_stack.pop_back();
		}

	private:

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() override
		{
			for (; !m_stack.empty();)
			{
				// If the source is exhausted, pop from the stack until we
				// find the next source with characters available.
				auto& src = m_stack.back();
				if (*src == '\0')
				{
					Pop();
					continue;
				}

				// Parse the next character
				if (src.m_emit == 0 && !IsOutputChar(src, src.m_emit))
					continue;

				// Return the next valid character
				assert(*src != '\0');
				auto ch = *src; ++src;
				src.m_emit -= int(src.m_emit != 0);
				return ch;
			}
			return '\0';
		}

		// Parse the character pointed to by 'src' as a possible preprocessor command.
		// Returns true if the current position of 'src' is a character that should be emitted.
		bool IsOutputChar(Src& src, int& emit)
		{
			assert(emit == 0);
			auto is_output = true;
			switch (*src)
			{
			case '\"':
			case '\'':
				#pragma region Literal String
				{
					// Note: consecutive strings are treated as a single string.
					// Loop here joining consecutive strings into one buffered string.
					auto quote = *src;
					int end = 0, beg = 0; // end of the last string, beg of the next string
					for (bool first = true; ; first = false)
					{
						// Buffer the literal string or char
						auto loc = src.Location();
						if (!BufferLiteral(src, beg, &end))
							throw ScriptException(EResult::InvalidString, loc, "Incomplete literal string or character");

						// If this is not the first consecutive string, delete the quotes between them
						if (!first)
						{
							src.Buffer().erase(size_t(beg) - 1, 2);
							end -= 2;
						}

						// Buffer to the next non-whitespace character
						BufferWhile(src, [](Src& s, int i) { return str::IsWhiteSpace(s[i]); }, end, &beg);
						if (src[beg] != quote)
							break;

						// Erase the whitespace between the strings
						src.Buffer().erase(end, size_t(beg) - end);
						beg = end;
					}
					emit = end;
					break;
				}
				#pragma endregion
			case '#':
				#pragma region PP Command
				{
					// Record the start of the PP command
					auto loc_beg = src.Location();

					// Eat optional whitespace between the # and the keyword
					EatLineSpace(src, 1, 0);

					// Match the preprocessor command
					switch (*src)
					{
					case 'd':
						#pragma region Define
						if (src.Match(L"define", true))
						{
							EatLineSpace(src, 0, 0);
							m_macros.Add(Macro(src, src.Location()));
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region DefIfNDef
						if (src.Match(L"defifndef", true))
						{
							EatLineSpace(src, 0, 0);
							Macro macro(src, src.Location());
							if (!m_macros.Find(macro.m_tag))
								m_macros.Add(macro);

							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region Depend
						if (src.Match(L"depend", true))
						{
							EatLineSpace(src, 0, 0);
							if (*src != '<' && *src != '\"')
								throw ScriptException(EResult::InvalidInclude, src.Location(), "expected a string following #depend");

							// Save the end marker
							auto end = *src == '<' ? '>' : '\"';
							auto flags = *src == '\"' ? EIncludeFlags::IncludeLocalDir : EIncludeFlags::None;
							++src; // skip the '<'

							// Buffer the filepath string
							auto len = 0;
							BufferWhile(src, [=](Src& s, int i) { return s[i] != end; }, 0, &len);
							if (src[len] != end) throw ScriptException(EResult::InvalidInclude, src.Location(), "#depend string incomplete");

							// Save the include filepath
							auto path = src.ReadN(len);
							++src; // skip the '>'. Don't eat the rest of the line

							// Open the dependent file but don't push it onto the source stack. The include handler
							// will see this as a referenced file but the content doesn't effect the script
							if (m_ignore_missing) flags |= EIncludeFlags::IgnoreMissing;
							auto inc = Includes->Open(path, flags, loc_beg);
							inc = nullptr; // release the include immediately

							is_output = false;
							break;
						}
						#pragma endregion
						break;
					case 'e':
						#pragma region Else
						if (src.Match(L"else", true))
						{
							if (m_if_stack.empty()) throw ScriptException(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #else");
							if (m_if_stack.top()) { SkipPreprocessorBlock(loc_beg); }
							else { m_if_stack.top() = true; EatLine(src, 0, 1); }
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region ElIf
						if (src.Match(L"elif", true))
						{
							if (m_if_stack.empty()) throw ScriptException(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #elif");
							if (m_if_stack.top()) { SkipPreprocessorBlock(loc_beg); }
							else if (!PPDefined()) { SkipPreprocessorBlock(loc_beg); }
							else { m_if_stack.top() = true; EatLine(src, 0, 1); }
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region EndIf
						if (src.Match(L"endif", true))
						{
							if (m_if_stack.empty()) throw ScriptException(EResult::UnmatchedPreprocessorDirective, loc_beg, "unmatched #endif");
							m_if_stack.pop();
							EatLine(src, 0, 1);
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region Eval
						if (src.Match(L"eval", true))
						{
							EatLineSpace(src, 0, 0);
							string_t expr;

							// Extract text between '{' and '}'
							if (*src == '{') ++src; else throw ScriptException(EResult::ExpressionSyntaxError, loc_beg, "Expected the form: #eval{expression}");
							for (int nest = 1; *src; expr.push_back(*src), ++src)
							{
								nest += *src == '{';
								nest -= *src == '}';
								if (nest == 0) break;
							}
							if (*src == '}') ++src; else throw ScriptException(EResult::ExpressionSyntaxError, loc_beg, "No matching '}' found following #eval");

							// Expand any macros in the expression
							RecursiveExpandMacros(expr, Macro::Ancestor(nullptr, nullptr), loc_beg);

							// Replace any nested '#eval{exp}' with (exp)
							str::Replace(expr, L"#eval", L"");
							str::Replace(expr, L"{", L"(");
							str::Replace(expr, L"}", L")");

							// Evaluate the expression and push the result as a string onto the stack
							double result;
							if (!Evaluate(expr.c_str(), result))
								throw ScriptException(EResult::ExpressionSyntaxError, loc_beg, "#eval expression cannot be evaluated");

							// Convert the result to a string
							expr = (static_cast<long long>(result) == result)
								? FmtS(L"%lld", static_cast<long long>(result))
								: FmtS(L"%f", result);

							// Push the 'eval' result onto the input stack
							auto eval_src = std::make_unique<StringSrc>(expr, StringSrc::EFlags::BufferLocally);
							Push(eval_src.get(), true, false);
							eval_src.release();
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region Embedded
						if (src.Match(L"embedded", true))
						{
							// Read the embedded language used: #embedded(lang[,support])
							string_t lang; int len;
							if (*src == '(') ++src; else throw ScriptException(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang[,support]) ... #end");
							if (BufferIdentifier(src, 0, &len)) lang = src.ReadN(len); else throw ScriptException(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang[,support]) ... #end");
							EatLineSpace(src, 0, 0);
							auto support = (*src == ',' && (++src).Match(L"support", true));
							if (*src == ')') ++src; else throw ScriptException(EResult::InvalidPreprocessorDirective, loc_beg, "Expected the form: #embedded(lang[,support]) ... #end");

							// Do not include the whitespace or blank line that follows #embedded(lang,support)
							EatLineSpace(src, 0, 0);
							if (str::IsNewLine(*src)) ++src;

							// Record the source location for the start of the code
							auto code_beg = src.Location();

							// Buffer the code section up to (but not including) the #end
							string_t code;
							if (BufferTo(src, L"#end", false, 0, &len)) code = src.ReadN(len); else throw ScriptException(EResult::UnmatchedPreprocessorDirective, loc_beg, "Embedded code section '#embedded' does not have a closing '#end' marker");

							// Expand any macros in the buffered text
							RecursiveExpandMacros(code, Macro::Ancestor(nullptr, nullptr), loc_beg);

							// Get the code handler to transform the code into a result. The code handler is expected to succeed or throw
							string_t result;
							try
							{
								auto emb = FindEmbeddedCodeHandler(lang);
								if (emb == nullptr && !m_ignore_missing)
									throw ScriptException(EResult::EmbeddedCodeNotSupported, loc_beg, pr::FmtS("No support for embedded '%S' code available", lang.c_str()));
								if (emb != nullptr && !emb->Execute(code.c_str(), support, result))
									throw ScriptException(EResult::EmbeddedCodeError, loc_beg, pr::FmtS("Embedded '%S' code could not be executed", lang.c_str()));
							}
							catch (ScriptException const&) { throw; }
							catch (std::exception const& ex) { throw ScriptException(EResult::EmbeddedCodeError, code_beg, ex.what()); }
							
							// Push the code result as a new source
							if (!result.empty())
							{
								// Push the result into the buffer of the 'StringSrc' since 'result' will go out of scope.
								auto code_src = std::make_unique<StringSrc>(result, StringSrc::EFlags::BufferLocally);
								Push(code_src.get(), true, false);
								code_src.release();
							}
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region End
						if (src.Match(L"end", true))
						{
							throw ScriptException(EResult::UnmatchedPreprocessorDirective, loc_beg, "#end directive is unmatched");
						}
						#pragma endregion
						#pragma region Error
						if (src.Match(L"error", true))
						{
							EatLineSpace(src, 0, 0);
							auto msg = src.ReadLine(false);
							throw ScriptException(EResult::PreprocessError, loc_beg, msg);
						}
						#pragma endregion
						break;
					case 'i':
						#pragma region IfNDef
						if (src.Match(L"ifndef", true))
						{
							auto len = 0;
							EatLineSpace(src, 0, 0);
							if (!BufferIdentifier(src, 0, &len)) throw ScriptException(EResult::InvalidPreprocessorDirective, src.Location(), "An identifier was expected");
							auto tag = src.ReadN(len);
							
							if (m_macros.Find(tag))
							{
								m_if_stack.push(false);
								SkipPreprocessorBlock(loc_beg);
							}
							else
							{
								m_if_stack.push(true);
								EatLine(src, 0, 1);
							}
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region IfDef
						if (src.Match(L"ifdef", true))
						{
							auto len = 0;
							EatLineSpace(src, 0, 0);
							if (!BufferIdentifier(src, 0, &len)) throw ScriptException(EResult::InvalidPreprocessorDirective, src.Location(), "An identifier was expected");
							auto tag = src.ReadN(len);

							if (m_macros.Find(tag))
							{
								m_if_stack.push(true);
								EatLine(src, 0, 1);
							}
							else
							{
								m_if_stack.push(false);
								SkipPreprocessorBlock(loc_beg);
							}
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region If
						if (src.Match(L"if", true))
						{
							EatLineSpace(src, 0, 0);
							if (PPDefined())
							{
								m_if_stack.push(true);
								EatLine(src, 0, 1);
							}
							else
							{
								m_if_stack.push(false);
								SkipPreprocessorBlock(loc_beg);
							}
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region Ignore Missing
						if (src.Match(L"ignore_missing", true))
						{
							EatLineSpace(src, 0, 0);
							if (*src == '\"') ++src; else throw ScriptException(EResult::InvalidInclude, src.Location(), "expected a string following #ignore_missing");

							// Buffer the state string
							auto len = 0;
							BufferWhile(src, [=](Src& s, int i) { return s[i] != '\"'; }, 0, & len);
							if (src[len] != '\"') throw ScriptException(EResult::InvalidInclude, src.Location(), "#ignore_missing string incomplete");

							// Save the state string
							auto state = src.ReadN(len);
							++src; // skip the quote. Don't eat the rest of the line

							// Set the 'ignore missing' state
							m_ignore_missing = str::EqualI(state, "on");

							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region Include Path
						if (src.Match(L"include_path", true))
						{
							EatLineSpace(src, 0, 0);
							if (*src != '<' && *src != '\"')
								throw ScriptException(EResult::InvalidInclude, src.Location(), "expected a string following #include_path");

							// Save the end marker
							auto end = *src == '<' ? '>' : '\"';
							++src; // skip the '<'

							// Buffer the include path string
							auto len = 0;
							BufferWhile(src, [=](Src& s, int i) { return s[i] != end; }, 0, &len);
							if (src[len] != end) throw ScriptException(EResult::InvalidInclude, src.Location(), "#include_path string incomplete");

							// Save the include path
							auto path = src.ReadN(len);
							++src; // skip the '>'. Don't eat the rest of the line

							// Add the path to the include paths
							Includes->AddSearchPath(path);

							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region Include
						if (src.Match(L"include", true))
						{
							EatLineSpace(src, 0, 0);
							if (*src != '<' && *src != '\"')
								throw ScriptException(EResult::InvalidInclude, src.Location(), "expected a string following #include");

							// Save the end marker
							auto end = *src == '<' ? '>' : '\"';
							auto flags = *src == '\"' ? EIncludeFlags::IncludeLocalDir : EIncludeFlags::None;
							++src; // skip the '<'

							// Buffer the include filepath
							auto len = 0;
							BufferWhile(src, [=](Src& s, int i) { return s[i] != end; }, 0, &len);
							if (src[len] != end) throw ScriptException(EResult::InvalidInclude, loc_beg, "#include string incomplete");

							// Save the include filepath
							auto path = src.ReadN(len);
							++src; // skip the '>'. Don't eat the rest of the line

							// Open the include
							if (m_ignore_missing) flags |= EIncludeFlags::IgnoreMissing;
							auto inc = Includes->Open(path, flags, loc_beg);
							if (inc) Push(inc.release(), true, false);

							is_output = false;
							break;
						}
						#pragma endregion
						break;
					case 'l':
						#pragma region Lit
						if (src.Match(L"lit", true))
						{
							// Do not include the whitespace or blank line that follows #lit
							EatLineSpace(src, 0, 0);
							if (str::IsNewLine(*src)) ++src;

							// Buffer a literal section up to (but not including) the #end
							auto len = 0;
							if (!BufferTo(src, L"#end", false, 0, &len))
								throw ScriptException(EResult::UnmatchedPreprocessorDirective, loc_beg, "Literal section '#lit' does not have a closing '#end' marker");
							
							emit = len;
							is_output = false;
							break;
						}
						#pragma endregion
						#pragma region Line
						if (src.Match(L"line", true))
						{
							// Ignore the #line directive
							EatLine(src, 0, 1);
							is_output = false;
							break;
						}
						#pragma endregion
						break;
					case 'p':
						#pragma region Pragma
						if (src.Match(L"pragma", true))
						{
							// Ignore the #pragma directive
							EatLine(src, 0, 1);
							is_output = false;
							break;
						}
						#pragma endregion
						break;
					case 'u':
						#pragma region Undef
						if (src.Match(L"undef", true))
						{
							EatLineSpace(src, 0, 0);

							// Read the macro tag
							auto len = 0;
							if (!BufferIdentifier(src, 0, &len)) throw ScriptException(EResult::InvalidPreprocessorDirective, src.Location(), "An identifier was expected");
							auto tag = src.ReadN(len);

							m_macros.Remove(tag);

							EatLine(src, 0, 1);
							is_output = false;
							break;
						}
						#pragma endregion
						break;
					case 'w':
						#pragma region Warning
						if (src.Match(L"warning", true))
						{
							// Ignore #warning directives
							EatLine(src, 0, 1);
							is_output = false;
							break;
						}
						#pragma endregion
						break;
					}

					// Unknown pp command
					if (is_output)
					{
						auto len = 0;
						BufferIdentifier(src, 0, &len);
						throw ScriptException(EResult::UnknownPreprocessorCommand, loc_beg, Fmt(L"Unknown preprocessor command '%s'", src.ReadN(len).c_str()));
					}
					break;
				}
				#pragma endregion
			default:
				#pragma region Expand Macros
				// Look for possible macro identifiers in the source (not within expanded macros)
				auto& input = static_cast<Input&>(src);
				if (!input.m_is_macro && str::IsIdentifier(*src, true))
				{
					auto len = 0;
					auto loc = src.Location();

					// Buffer the identifier
					BufferIdentifier(src, 0, &len);

					// See if the identifier matches any macro definitions
					auto macro = m_macros.Find(string_t(src.Buffer(0, len)));
					if (macro)
					{
						// This is a macro, so remove the macro identifier from the buffer
						src += len;

						// If the macro requires parameters see if we can read them from the source
						Macro::Params params;
						if (macro->ReadParams<false>(src, params, src.Location()))
						{
							// Create a buffered string source for the expanded macro
							// and get the macro to generate it's expanded version.
							string_t exp;
							macro->Expand(exp, params, loc);
							RecursiveExpandMacros(exp, Macro::Ancestor(macro, nullptr), src.Location());

							// Push the expanded macro as a source. Copy 'exp' into the buffer of
							// the StringSrc since 'exp' is about to go out of scope.
							auto macro_src = std::make_unique<StringSrc>(exp, StringSrc::EFlags::BufferLocally);
							Push(macro_src.get(), true, true);
							macro_src.release();
							is_output = false;
						}
					}
				}
				break;
				#pragma endregion
			}

			// 'is_output' is true if '*src' is a character that should be returned from the pre-processor stream.
			return is_output;
		}

		// Recursively expand the expression in 'exp' with macro substitutions
		void RecursiveExpandMacros(string_t& exp, Macro::Ancestor const& parent, Loc const& loc)
		{
			string_t tag;
			string_t subexp;
			Macro::Params params;

			// Scan through 'exp' looking for macro identifiers
			for (auto ptr = exp.c_str(); *ptr;)
			{
				if (!str::IsIdentifier(*ptr, true))
				{
					++ptr;
					continue;
				}

				auto beg = ptr;

				// Found the start of an identifier, see if it's a macro identifier
				str::ExtractIdentifier(tag, ptr);

				// Find the macro?
				auto macro = m_macros.Find(tag);
				if (!macro)
					continue;

				// Check whether this macro is an ancestor
				if (parent.IsRecursive(macro))
					continue; // a recursive substitution.. ignore

				// Check the correct parameters have been given
				params.clear();
				if (!macro->ReadParams<false>(ptr, params, loc))
					continue;

				// Recursively expand the macro into a temporary buffer
				subexp.resize(0); // reuse
				macro->Expand(subexp, params, loc);
				RecursiveExpandMacros(subexp, Macro::Ancestor(macro, &parent), loc);

				// Substitute the expanded macro into 'src'
				auto len = ptr - beg;
				auto ofs = beg - exp.data();
				exp.erase (ofs, len);
				exp.insert(ofs, subexp);
				ptr = exp.c_str() + ofs + subexp.size();
			}
		}

		// Parse the line following an #if or #elif statement, returning true if the expression evaluates to true
		bool PPDefined()
		{
			auto& src = (Src&)m_stack.back();
			//auto& emit = src.m_emit;
			string_t expr, exp;

			// Read the whole line into a string, generating an expression that should evaluate to an integer
			for (EatLineSpace(src, 0, 0); !str::IsNewLine(*src); EatLineSpace(src, 0, 0))
			{
				// Append operators to the expression
				if (!str::IsIdentifier(*src, true))
				{
					expr.push_back(*src);
					++src;
					continue;
				}

				// If the identifier is the keyword 'defined' then it should be followed
				// by an identifier optionally enclosed within '(' and ')'
				if (src.Match(L"defined", true))
				{
					EatLineSpace(src, 0, 0);

					// Check for optional '()'
					auto wrapped = *src == '(';
					if (wrapped) ++src;

					// Read the identifier within the defined() expression
					auto len = 0;
					if (!BufferIdentifier(src, 0, &len)) throw ScriptException(EResult::InvalidPreprocessorDirective, src.Location(), "An identifier was expected");
					auto tag = src.ReadN(len);

					// Check the optional brackets are matched
					if (wrapped) if (*src == ')') ++src; else throw ScriptException(EResult::InvalidPreprocessorDirective, src.Location(), "unmatched ')'");

					// If the macro is defined, add a 1 to the expression
					expr.push_back(m_macros.Find(tag) != nullptr ? '1' : '0');
				}

				// Otherwise substitute the macro
				else
				{
					auto loc = src.Location();

					// Read the macro or keyword identifier
					auto len = 0;
					BufferIdentifier(src, 0, &len);
					auto tag = src.ReadN(len);
					auto macro = m_macros.Find(tag);
					if (macro == nullptr) throw ScriptException(EResult::InvalidPreprocessorDirective, loc, Fmt(L"Identifier '%s' is not defined", tag.c_str()));

					// Read macro parameters if it has them
					Macro::Params params;
					if (!macro->ReadParams<false>(src, params, src.Location()))
						throw ScriptException(EResult::ParameterCountMismatch, loc, Fmt(L"Missing parameters for macro %s. Expected %d", exp.c_str(), macro->m_params.size()));

					// Expand the macro with the given parameters
					macro->Expand(exp, params, loc);

					// Recursively expand macros within 'exp'.
					RecursiveExpandMacros(exp, Macro::Ancestor(macro, nullptr), loc);

					// Add the fully expanded macro to the expression
					expr.append(exp);
				}
			}

			// Evaluate the expression
			int res;
			if (!Evaluate(expr, res)) throw ScriptException(EResult::InvalidPreprocessorDirective, src.Location(), "Failed to evaluate conditional expression");
			return res != 0;
		}

		// Eat characters from the stream up to an #elif, #else, or #endif for a previous #ifdef, #ifndef, or #elif
		void SkipPreprocessorBlock(Loc const& beg)
		{
			// The parser behaviour for inactive code blocks is tricky. Consider this code:
			//   #if 0
			//   "string \
			//   #endif/*
			//   #endif
			// Line continuations apply, so the first #endif is actually part of the second line.
			// The /* is part of the string so is ignored. No closing " is needed, the end of the
			// line automatically closes the string (similarly for '). If the third line was
			// '#endif"/*', then the opening block comment is visible and the second #endif is not
			// seen. The literal string handling in the comment stripper needs close strings when
			// a newline characters is seen.

			for (int nest = 1; !m_stack.empty();)
			{
				// If the source is exhausted, pop from the stack until we
				// find the next source with characters available.
				auto& src = m_stack.back();
				if (*src == '\0')
				{
					Pop();
					continue;
				}

				// Find the first non-whitespace character on the line.
				EatLineSpace(src, 0, 0);
				if (*src != '#')
				{
					// If it's not a preprocessor directive, consume the line and carry on.
					EatLine(src, 0, 1);
					continue;
				}

				// Skip the '#' and find the next non-whitespace character
				EatLineSpace(src, 1, 0); 

				// Handle nested directives
				nest += int(src.Match(L"ifndef") || src.Match(L"ifdef") || src.Match(L"if"));
				nest -= int(src.Match(L"endif") || (nest == 1 && (src.Match(L"elif") || src.Match(L"else"))));
				if (nest == 0)
				{
					// Add the '#' character back again
					src.Buffer().insert(0, 1, '#');
					return;
				}

				// Consume the rest of the line
				EatLine(src, 0, 1);
			}
			throw ScriptException(EResult::UnmatchedPreprocessorDirective, beg, "Unmatched #if, #ifdef, #ifndef, #else, or #elid");
		}

		// Get/Create the embedded code handler for 'lang
		IEmbeddedCode* FindEmbeddedCodeHandler(string_t const& lang)
		{
			// Look for the code handler for 'lang'
			for (auto& handler : m_emb_handlers)
			{
				if (!str::EqualI(handler->Lang(), lang.c_str())) continue;
				return handler.get();
			}

			// If not found, use the factory to create one
			auto handler = m_emb_factory ? m_emb_factory(lang.c_str()) : nullptr;
			if (handler == nullptr)
				return nullptr;

			// Store the code handler
			auto ptr = handler.get();
			m_emb_handlers.push_back(std::move(handler));
			return ptr;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/script/embedded_lua.h"

namespace pr::script
{
	PRUnitTest(InputStackTests)
	{
		using namespace pr::str;

		char const* src1 = "abcd";
		wchar_t const* src2 = L"123";
		pr::string<wchar_t> str1;

		Preprocessor pp(src1);
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
	PRUnitTest(PreprocessorTests)
	{
		{// Consecutive strings
			char const* str_in = 
				"\"consecutive \"  \t\"string\""
				;
			char const* str_out =
				"\"consecutive string\""
				;
			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// ignored stuff
			char const* str_in =
				"\"#if ignore #define this stuff\"\n"
				;
			char const* str_out =
				"\"#if ignore #define this stuff\"\n"
				;

			Preprocessor pp(str_in);
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

			Preprocessor pp(str_in);
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

			Preprocessor pp(str_in);
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

			Preprocessor pp(str_in);
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

			Preprocessor pp(str_in);
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

			Preprocessor pp(str_in);
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

			Preprocessor pp(str_in);
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

			Preprocessor pp(str_in);
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
				"#if 0\n"
				"\"string \\\n" // This unclosed string doesn't matter when skipping inactive blocks
				"#endif\n" // this is part of the previous line, so is ignored
				"#endif\n"
				;
			char const* str_out =
				"	output\n"
				"	"//#if defined(ZERO) || ...
				"		output this\n"
				"	"//#else\n
				"	two defined\n"
				"1\n"
				;

			Preprocessor pp(str_in);
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
				"#depend \"dep\"\n"
				;
			char const* str_out =
				"included 1\n\n"
				;

			Includes inc;
			inc.AddString(L"inc", "included ONE");
			inc.AddString(L"dep", "Anything");
			StringSrc src(str_in);
			Preprocessor pp(&src, false, &inc);
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
				"\n"
				"lastword"
				"4\n"
				"2\n"
				"Any old ch*rac#ers #if I {feel} #include --cheese like #en\n"
				"hello world\n"
				;

			Includes inc;
			Preprocessor pp(str_in, &inc, [](auto){ return std::make_unique<EmbeddedLua>(); });
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// Using a preloaded buffer
			std::string str_in =
				"#define BOB(x) #x\n"
				"BOB(this is a string)\n"
				;
			char const* str_out =
				"\"this is a string\"\n"
				;

			StringSrc src(str_in, StringSrc::EFlags::BufferLocally); str_in.clear();
			Preprocessor pp(src, false);
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
			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
	}
}
#endif
