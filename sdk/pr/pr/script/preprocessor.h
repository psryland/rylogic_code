//**********************************
// Script preprocessor
//  Copyright © Rylogic Ltd 2011
//**********************************
#ifndef PR_SCRIPT_PREPROCESSOR_H
#define PR_SCRIPT_PREPROCESSOR_H

#include "pr/script/script_core.h"
#include "pr/script/stream_stack.h"
#include "pr/script/keywords.h"
#include "pr/script/pp_macro.h"
#include "pr/script/pp_macro_db.h"
#include "pr/script/pp_includes.h"
#include "pr/script/embedded_code.h"

namespace pr
{
	namespace script
	{
		// The preprocessor operates on a character stream, not a token stream.
		// All macros, etc, are text substitutions
		struct Preprocessor :Src
		{
		private:
			SrcStack       m_src_stack; // A stack of char streams, macro expansions and includes are pushed onto here
			Buffer<>       m_buf;       // A local buffer of the input stream on the top of the stack
			pr::BitStack<> m_if_stack;  // A stack recording the 'inclusion' state of nested #if/#endif blocks

		public:
			// Macro/Include/Embedded Code handler interfaces
			IPPMacroDB*    m_macros;    // Macro definitions
			IIncludes*     m_includes;  // Include handler
			IEmbeddedCode* m_embedded;  // Handler for embedded code

			Preprocessor(IPPMacroDB* macros, IIncludes* includes, IEmbeddedCode* embedded)
			:Src(SrcType::Unknown)
			,m_src_stack()
			,m_buf(m_src_stack)
			,m_macros(macros)
			,m_includes(includes)
			,m_embedded(embedded)
			,m_if_stack()
			{}

			Preprocessor(Src& src, IPPMacroDB* macros, IIncludes* includes, IEmbeddedCode* embedded)
			:Src(SrcType::Unknown)
			,m_src_stack(src)
			,m_buf(m_src_stack)
			,m_macros(macros)
			,m_includes(includes)
			,m_embedded(embedded)
			,m_if_stack()
			{}

			// Add a char stream as an input source
			void push(Src* src, bool delete_on_pop)
			{
				m_src_stack.push(src, delete_on_pop);
			}

			// Allow users to test if the input is buffered. This provides a way
			// of reading through literal strings and #lit/#endlit sections
			bool input_buffered() const
			{
				return !m_buf.empty();
			}

			// debugging methods
			SrcType::Type type() const { return m_src_stack.type(); }
			Loc           loc()  const { return m_src_stack.loc();  }
			void          loc(Loc& l)  { m_src_stack.loc(l); }

		private:
			char peek() const { return *m_buf; }
			void next()       { ++m_buf; }
			void seek()
			{
				for (;;)
				{
					if (input_buffered()) return;

					// Look for the start of a preprocessor directive
					if (*m_buf == '#')
					{
						do { PPCommand(); } while (*m_buf == '#');
						continue;
					}

					// Read through literal strings
					if (*m_buf== '\"')
					{
						m_buf.BufferLiteralString();
						continue;
					}

					// Read through literal chars
					if (*m_buf== '\'')
					{
						m_buf.BufferLiteralChar();
						continue;
					}

					// Read through comments. Although typically a comment stripper would remove comments,
					// we can't assume comments have been removed. The preprocessor needs to correctly
					// ignore preprocessing directives within comments
					if (*m_buf == '/')
					{
						m_buf.buffer(2);
						if (m_buf[1] == '/') { m_buf.BufferLine(true); continue; }
						if (m_buf[1] == '*') { m_buf.BufferBlockComment(); continue; }
					}

					// Look for possible macro identifiers in the source (not within expanded macros)
					if (m_macros && m_buf.type() != SrcType::Macro && pr::str::IsIdentifier(*m_buf, true))
					{
						// Buffer the identifier
						m_buf.BufferIdentifier();

						// See if the identifier matches any macro definitions
						pr::hash::HashValue hash = m_buf.Hash();
						PPMacro const* macro = m_macros->Find(hash);
						if (macro)
						{
							// If the macro requires parameters see if we can read them from the source
							PPMacro::Params params;
							if (macro->ReadParams<false>(m_buf, params, m_buf.loc()))
							{
								// Create a buffered string source for the expanded macro
								// and get the macro to generate it's expanded version.
								std::auto_ptr<BufferedSrc> exp(new BufferedSrc(0, SrcType::Macro));
								macro->GetSubstString(exp->m_str, params);
								ExpandMacros(*exp.get(), PPMacroAncestor(macro, 0), m_buf.loc());
								m_src_stack.push(exp.release(), true);
								continue;
							}
						}
					}
					return;
				}
			}

			// Parse a preprocessor command
			void PPCommand()
			{
				// Remember to set m_buf to appropriate contents before exit
				PR_ASSERT(PR_DBG, *m_buf == '#', "m_buf should contain a PP command");
				++m_buf;

				// Allow whitespace between # and the keyword, e.g. #   define BOB()
				Eat::LineSpace(m_buf);

				// Read the pp command
				string id;
				if (!pr::str::ExtractIdentifier(id, m_buf))
					throw Exception(EResult::InvalidIdentifier, m_buf.loc(), "invalid preprocessor command");

				// Find the hash of the pp command
				EPPKeyword kw = EPPKeyword::From(Hash::String(id));
				switch (kw)
				{
				default:
				case EPPKeyword::Invalid:
					throw Exception(EResult::UnknownPreprocessorCommand, m_buf.loc(), fmt("%s is an unknown preprocessor command", id.c_str()));
				case EPPKeyword::Pragma:
				case EPPKeyword::Line:
				case EPPKeyword::Warning:
					{
						Eat::Line(m_buf, true);
					}break;
				case EPPKeyword::Include:
					{
						Eat::LineSpace(m_buf);
						if (*m_buf != '<' && *m_buf != '\"')
							throw Exception(EResult::InvalidInclude, m_buf.loc(), "expected a string following #include");

						// Extract the string describing what to include
						string include;
						bool search_paths_only = *m_buf == '<';
						char end = search_paths_only ? '>' : '\"';
						for (++m_buf; *m_buf && *m_buf != '\n' && *m_buf != end; include += *m_buf, ++m_buf) {}
						if (*m_buf == end) ++m_buf; else throw Exception(EResult::InvalidInclude, m_buf.loc(), "include string incomplete");

						// Open the include
						Src* inc = m_includes ? m_includes->Open(include, m_buf.loc(), search_paths_only) : 0;
						if (inc) m_src_stack.push(inc, true);
					}break;
				case EPPKeyword::Define:
					{
						if (!m_macros) Eat::Line(m_buf, true);
						else
						{
							Eat::LineSpace(m_buf);
							m_macros->Add(PPMacro(m_buf, m_buf.loc()));
						}
					}break;
				case EPPKeyword::Undef:
					{
						if (!m_macros) Eat::Line(m_buf, true);
						else
						{
							Eat::LineSpace(m_buf);
							string id; if (!pr::str::ExtractIdentifier(id, m_buf)) throw Exception(EResult::InvalidIdentifier, m_buf.loc(), "invalid identifier");
							m_macros->Remove(Hash::String(id));
							Eat::Line(m_buf, true);
						}
					}break;
				case EPPKeyword::If:
					{
						Eat::LineSpace(m_buf);
						if (PPDefined()) { m_if_stack.push(true);  Eat::Line(m_buf, true); }
						else             { m_if_stack.push(false); SkipPreprocessorBlock(); }
					}break;
				case EPPKeyword::Ifdef:
					{
						Eat::LineSpace(m_buf);
						if (!pr::str::ExtractIdentifier(id, m_buf)) throw Exception(EResult::InvalidIdentifier, m_buf.loc(), "invalid identifier");
						if (m_macros && m_macros->Find(Hash::String(id)) != 0) { m_if_stack.push(true); Eat::Line(m_buf, true); }
						else                                                   { m_if_stack.push(false); SkipPreprocessorBlock(); }
					}break;
				case EPPKeyword::Ifndef:
					{
						Eat::LineSpace(m_buf);
						if (!pr::str::ExtractIdentifier(id, m_buf)) throw Exception(EResult::InvalidIdentifier, m_buf.loc(), "invalid identifier");
						if (m_macros && m_macros->Find(Hash::String(id)) == 0) { m_if_stack.push(true); Eat::Line(m_buf, true); }
						else                                                   { m_if_stack.push(false); SkipPreprocessorBlock(); }
					}break;
				case EPPKeyword::Elif:
					{
						if (m_if_stack.empty()) throw Exception(EResult::UnmatchedPreprocessorDirective, m_buf.loc(), "unmatched #elif");
						if (m_if_stack.top())  { SkipPreprocessorBlock(); }
						else if (!PPDefined()) { SkipPreprocessorBlock(); }
						else                   { m_if_stack.top() = true; Eat::Line(m_buf, true); }
					}break;
				case EPPKeyword::Else:
					{
						if (m_if_stack.empty()) throw Exception(EResult::UnmatchedPreprocessorDirective, m_buf.loc(), "unmatched #else");
						if (m_if_stack.top()) { SkipPreprocessorBlock(); }
						else                  { m_if_stack.top() = true; Eat::Line(m_buf, true); }
					}break;
				case EPPKeyword::Endif:
					{
						if (m_if_stack.empty()) throw Exception(EResult::UnmatchedPreprocessorDirective, m_buf.loc(), "unmatched #endif");
						m_if_stack.pop();
						Eat::Line(m_buf, true);
					}break;
				case EPPKeyword::Error:
					{
						string msg;
						Eat::LineSpace(m_buf);
						if (!pr::str::ExtractString(msg, m_buf)) throw Exception(EResult::InvalidString, m_buf.loc(), "string expected");
						throw Exception(EResult::PreprocessError, m_buf.loc(), msg);
					}break;
				case EPPKeyword::Eval:
					{
						Eat::LineSpace(m_buf);
						std::auto_ptr<BufferedSrc> expr(new BufferedSrc(0, SrcType::Macro));

						// Extract text between '{' and '}'
						Loc loc = m_buf.loc();
						if (*m_buf == '{') ++m_buf; else throw Exception(EResult::ExpressionSyntaxError, loc, "expected the form: #eval{expression}");
						for (int nest = 1; *m_buf; expr->m_str.push_back(*m_buf), ++m_buf)
						{
							nest += *m_buf == '{';
							nest -= *m_buf == '}';
							if (nest == 0) break;
						}
						if (*m_buf == '}') ++m_buf; else throw Exception(EResult::ExpressionSyntaxError, loc, "no matching '}' found following #eval");

						// Expand any macros in the expression
						ExpandMacros(*expr.get(), PPMacroAncestor(), loc);

						// Replace any nested #eval{exp} with (exp)
						pr::str::Replace(expr->m_str, "#eval", "");
						pr::str::Replace(expr->m_str, "{", "(");
						pr::str::Replace(expr->m_str, "}", ")");

						// Evaluate the expression and push the result as a string onto the stack
						double result;
						if (!pr::Evaluate(expr->m_str.c_str(), result)) throw Exception(EResult::ExpressionSyntaxError, loc, "#eval expression cannot be evaluated");
						expr->m_str = (static_cast<int64>(result)==result) ? fmt("%lld",static_cast<int64>(result)) : fmt("%f",result);
						m_src_stack.push(expr.release(), true);
					}break;
				case EPPKeyword::Lit:
					{
						++m_buf;

						// Buffer a literal section up to (but not including) the #end
						Buf8 end("#end");
						Buf8 src(m_buf.m_src, 4);
						for (; *m_buf.m_src && src != end; m_buf.push_back(src.front()), src.shift(*m_buf.m_src), ++m_buf.m_src) {}
					}break;
				case EPPKeyword::Embedded:
					{
						char lang[16];
						if (*m_buf == '(') ++m_buf; else throw Exception(EResult::InvalidPreprocessorDirective, m_buf.loc(), "expected the form: #embedded(lang) ... #end");
						if (!pr::str::ExtractIdentifier(lang, m_buf)) throw Exception(EResult::InvalidPreprocessorDirective, m_buf.loc(), "expected the form: #embedded(lang) ... #end");
						if (*m_buf == ')') ++m_buf; else throw Exception(EResult::InvalidPreprocessorDirective, m_buf.loc(), "expected the form: #embedded(lang) ... #end");
						Loc loc = m_buf.loc();

						// Buffer the code section up to (but not including) the #end
						Buf8 end("#end");
						Buf8 src(m_buf.m_src, 4);

						// Buffer the code and pass it to the embedded code handler
						if (m_embedded)
						{
							// Eventually, 'code' will contain the result which we want to treat like an expanded macro
							std::auto_ptr<BufferedSrc> code(new BufferedSrc(0, SrcType::Macro));
							for (; *m_buf && src != end; src.shift(*m_buf), ++m_buf) { code->m_str.push_back(src.front()); }

							// Expand any macros in the buffered text
							ExpandMacros(*code.get(), PPMacroAncestor(), loc);

							// Get the code handler to transform the code into a result
							string result;
							if (m_embedded->IEmbeddedCode_Execute(lang, code->m_str, loc, result))
							{
								code->m_str = result;
								m_src_stack.push(code.release(), true);
							}
						}

						// If no embedded code handler is provided, throw
						else
						{
							throw Exception(EResult::EmbeddedCodeNotSupported, m_buf.loc(), "embedded code found but no code handler provided");
							// If you want to silently ignore embedded code, use the IgnoreEmbeddedCode type
						}
						break;
					}
				}
			}

			// Recursively expand the expression in 'src' with macro substitutions
			void ExpandMacros(BufferedSrc& src, PPMacroAncestor const& parent, Loc const& loc)
			{
				string id;
				PPMacro::Params params;

				// Scan through 'src' looking for macro identifiers
				for (Buffer<> buf(src); *buf;)
				{
					if (!pr::str::IsIdentifier(*buf, true)) { ++buf; continue; }

					// See if this is a macro identifier
					size_t begin = src.m_idx;
					pr::str::ExtractIdentifier(id, buf);
					PPMacro const* macro = m_macros ? m_macros->Find(Hash::String(id)) : 0;
					if (!macro) continue;
					
					// Check the correct parameters have been given
					params.clear();
					if (!macro->ReadParams<false>(buf, params, loc)) continue;

					// Check whether this macro is an ancestor
					PPMacroAncestor const* p = &parent;
					for (; p && p->m_macro != macro; p = p->m_parent) {}
					if (p) continue; // a recursive substitution.. ignore

					// Recursively expand the macro into a temporary buffer
					BufferedSrc ex(0, SrcType::Macro);
					macro->GetSubstString(ex.m_str, params);
					ExpandMacros(ex, PPMacroAncestor(macro, &parent), loc);

					// Substitute the expanded macro into 'src'
					size_t len = src.m_idx - begin;
					src.m_str.erase (begin, len);
					src.m_str.insert(begin, ex.m_str);
					src.m_idx = begin + ex.m_str.size();
				}
				src.m_idx = 0;
			}

			// Parse a line following an #if or #elif statement, returning true if the expression evaluates to true
			bool PPDefined()
			{
				// Read the whole line
				string expr;
				for (Eat::LineSpace(m_buf); !pr::str::IsNewLine(*m_buf); Eat::LineSpace(m_buf))
				{
					if (!pr::str::IsIdentifier(*m_buf, true))
					{
						expr.push_back(*m_buf);
						++m_buf;
					}
					else
					{
						// Read the identifier
						string id; pr::str::ExtractIdentifier(id, m_buf);
						pr::hash::HashValue hash = Hash::String(id);

						// If the identifier is the keyword 'defined' then it should be followed
						// by an identifier optionally enclosed within '(' and ')'
						if (hash == EPPKeyword::Defined)
						{
							Eat::LineSpace(m_buf);
							bool wrapped = *m_buf == '(';
							if (wrapped) ++m_buf;
							if (!pr::str::ExtractIdentifier(id, m_buf)) throw Exception(EResult::InvalidPreprocessorDirective, m_buf.loc(), "expected an identifier");
							if (wrapped && *m_buf == ')') ++m_buf; else throw Exception(EResult::InvalidPreprocessorDirective, m_buf.loc(), "unmatched ')'");
							expr.push_back((m_macros && m_macros->Find(Hash::String(id)) != 0) ? '1' : '0');
						}

						// Otherwise substitute the macro
						else
						{
							PPMacro::Params params;
							PPMacro const* macro = m_macros ? m_macros->Find(hash) : 0;
							if (!macro) expr.append("(0)");
							else
							{
								// Read macro parameters if it has them
								if (!macro->ReadParams<false>(m_buf, params, m_buf.loc()))
									throw Exception(EResult::ParameterCountMismatch, m_buf.loc(), fmt("missing parameters for macro %s", id.c_str()));

								// Create a buffered string source for the expanded macro
								// and get the macro to generate it's expanded version.
								BufferedSrc exp(0, SrcType::Macro);
								macro->GetSubstString(exp.m_str, params);
								ExpandMacros(exp, PPMacroAncestor(macro, 0), m_buf.loc());
								expr.append(exp.m_str);
							}
						}
					}
				}
				// Evaluate the expression
				int res;
				if (!pr::EvaluateI(expr.c_str(), res)) throw Exception(EResult::InvalidPreprocessorDirective, m_buf.loc(), "failed to evaluate conditional expression");
				return res != 0;
			}

			// Eat characters from the stream up to an #elif, #else, or #endif for a previous #ifdef, #ifndef, or #elif
			void SkipPreprocessorBlock()
			{
				string id;
				for (int nest = 1; *m_buf;)
				{
					if (*m_buf == '\"') { Eat::LiteralString(m_buf); continue; }
					if (*m_buf != '#')  { ++m_buf; continue; }

					++m_buf;
					Eat::LineSpace(m_buf);
					if (!pr::str::IsIdentifier(*m_buf, true)) continue;

					m_buf.BufferIdentifier();
					EPPKeyword kw = EPPKeyword::From(m_buf.Hash());
					nest += int(kw == EPPKeyword::Ifdef || kw == EPPKeyword::Ifndef || kw == EPPKeyword::If);
					nest -= int(kw == EPPKeyword::Endif || (nest == 1 && (kw == EPPKeyword::Elif || kw == EPPKeyword::Else)));
					if (nest == 0) { m_buf.push_front('#'); break; }

					m_buf.clear();
					++m_buf;
				}
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
		PRUnitTest(pr_script_preprocessor)
		{
			using namespace pr;
			using namespace pr::script;

			{// ignored stuff
				char const* str_in =
					"\"#if ignore #define this stuff\"\n"
					;
				char const* str_out =
					"\"#if ignore #define this stuff\"\n"
					;
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
			{// simple macros
				char const* str_in =
					"#  define ONE 1 // ignore me \n"
					"#  define NOT_ONE (!ONE) /*and me*/ \n"
					"ONE\n"
					"NOT_ONE\n"
					;
				char const* str_out =
					"1\n"
					"(!1)\n"
					;
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
					;
				char const* str_out =
					"  output\n"
					"  output this\n"
					"  two defined\n"
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
		}
	}
}
#endif

#endif
