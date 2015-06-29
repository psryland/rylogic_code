//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/script_core.h"
#include "pr/script2/buf8.h"
#include "pr/script2/fail_policy.h"

namespace pr
{
	namespace script2
	{
		// Takes a character stream and performs preprocessing on it.
		// This is a super-set of a C/C++ preprocessor.
		template
		<
			typename FailPolicy = ThrowOnFailure
			//,typename IncludeHandler = FileIncludes
		>
		struct Preprocessor :Src
		{
		private:

			#pragma region Character Source
			struct alignas(16) Source :Buf8
			{
				Src*    m_src;      // This points to the next character to be added to the Buf8, i.e. 8 chars into the future
				bool    m_clean_up; // True of 'm_src' should be deleted on destruction
				FileLoc m_loc;      // The source file location

				Source()
					:Buf8()
					,m_src()
					,m_clean_up()
					,m_loc()
				{}
				Source(Src* src, bool clean_up)
					:Buf8()
					,m_src(src)
					,m_clean_up(clean_up)
					,m_loc()
				{
					next(8);
					seek();
				}
				Source(Source&& rhs)
					:Buf8(rhs)
					,m_src(rhs.m_src)
					,m_clean_up(rhs.m_clean_up)
					,m_loc(rhs.m_loc)
				{
					rhs.m_src = nullptr;
					rhs.m_clean_up = false;
					rhs.m_loc = FileLoc();
				}
				Source(Source const& rhs) = delete;
				Source& operator =(Source const& rhs) = delete;
				~Source()
				{
					if (m_clean_up)
						delete m_src;
				}

				// Pointer interface
				wchar_t operator*() const
				{
					return front();
				}
				Source& operator ++()
				{
					next();
					seek();
					return *this;
				}
				Source& operator +=(int n)
				{
					for (;n--;) ++*this;
					return *this;
				}

			private:

				// Advance the source by 'n' characters
				void next(int n = 1)
				{
					for (;n--;)
					{
						// Set the location based on the input source, so line numbers/columns
						// are still correct when line continuations are used.
						m_loc.inc(**m_src);

						// Shift the next character in
						shift(**m_src);
						++*m_src;
					}
				}

				// Test the current character and advance while not a valid character to return
				void seek()
				{
					static Buf8 const line_continuation1(L"\\\n");
					static Buf8 const line_continuation2(L"\\\r\n");

					// Assumes the current character pointed to by '*m_src' has not been
					// tested as a valid character to return. 
					for (auto& ptr = *this;;)
					{
						if (line_continuation1.match(ptr)) { next(2); continue; }
						if (line_continuation2.match(ptr)) { next(3); continue; }
						break;
					}
				}
			};
			#pragma endregion

			// Debugging helper for see the upcoming data
			wchar_t const* m_src;

			// The stack of input streams. Streams are pushed/popped from
			// the stack as files are opened, or macros are evaluated.
			std::vector<Source> m_stack;
			Source* m_current; // Points to the top stream on the stack.

			// AST states
			enum class EState
			{
				Default,
				LineComment,
				BlockComment,
			};
			struct ASTState
			{
			private:
				EState m_value;
			
			public:
				// RAII struct for state changes
				struct Scope
				{
					EState* m_value; EState old_state;
					Scope(Scope&& rhs) :m_value(rhs.m_value) ,old_state(rhs.old_state)        { m_value = nullptr; }
					Scope(EState& value, EState new_state) :m_value(&value) ,old_state(value) { *m_value = new_state; }
					~Scope()                                                                  { if (m_value) *m_value = old_state; }
				};

				ASTState() :m_value(EState::Default){}
				Scope scope(EState new_state)       { return Scope(m_value, new_state); }

				// Get/Set the state value
				operator EState() const
				{
					return m_value;
				}
				ASTState& operator = (EState state)
				{
					m_value = state;
					return *this;
				}
			};
			ASTState m_state;

		public:
			Preprocessor()
				:Src(ESrcType::Preprocessor)
				,m_stack()
				,m_state()
				,m_src()
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
			ESrcType Type() const override
			{
				return m_current ? m_current->m_src->Type() : ESrcType::Unknown;
			}

			// Push a source onto the input stack
			void Push(Src* src, bool delete_on_pop)
			{
				m_stack.emplace_back(src, delete_on_pop);
				m_current = &m_stack.back();
				m_src = m_current->m_ch;
				seek();
			}

			// Push a source owned externally onto the input stack
			void Push(Src& src)
			{
				Push(&src, false);
			}

			// Push a simple character string as a source
			template <typename Char> void Push(Char const* src)
			{
				Push(new Ptr<Char const*, TextLoc>(src), true);
			}

			// Pop the top source off the input stack
			void Pop()
			{
				m_stack.pop_back();
				if (!m_stack.empty())
				{
					m_current = &m_stack.back();
					m_src = m_current->m_ch;
				}
				else
				{
					m_current = nullptr;
					m_src = nullptr;
					m_state = EState::Default;
				}
			}

			// Pointer-like interface
			wchar_t operator *() const override
			{
				return m_current ? **m_current : wchar_t();
			}
			Preprocessor& operator ++() override
			{
				next();
				seek();
				return *this;
			}

			// Get/Set the source file/line/column location
			FileLoc const& loc() const override
			{
				static FileLoc s_null_loc;
				return m_current ? m_current->m_loc : s_null_loc;
			}
			void loc(FileLoc& l)
			{
				assert(m_current && "trying to assign a location to no input");
				m_current->m_loc = l;
			}

		private:

			// True if the source stack is empty
			bool done() const
			{
				return m_current == nullptr;
			}

			// Advance the source by 'n' characters
			void next(int n = 1)
			{
				for (;n--;)
				{
					++*m_current;
					if (**m_current == 0)
						Pop();
				}
			}

			// Move to the next character to be output from the preprocessor.
			// Assumes the current character has not been tested
			void seek()
			{
				for (auto emit = false; !done() && !emit;)
				{
					auto& src = *m_current;
					emit = true;

					// Handle the next character based on the AST
					switch (m_state)
					{
					default: assert("Unknown parser state"); next(); break;
					case EState::Default:
						#pragma region Default State
						{
							switch (src[0])
							{
							case L'/':
								switch (src[1])
								{
								case L'/': m_state = EState::LineComment; emit = false; break;
								case L'*': m_state = EState::BlockComment; emit = false; break;
								}
								break;
							}
							break;
						}
						#pragma endregion
					case EState::LineComment:
						#pragma region Line comment
						{
							next(2);
							for (;!done() && src[0] != L'\n'; next()) {}
							if (!done()) next(1);
							m_state = EState::Default;
							emit = false;
							break;
						}
						#pragma endregion
					case EState::BlockComment:
						#pragma region Block comment
						{
							auto loc_begin = loc();
							next(2);
							for (;!done() && src[0] != L'*' && src[1] != L'/'; next()) {}
							if (done()) FailPolicy::Fail(EResult::TokenNotFound, loc(), pr::Fmt("Unmatched block comment at:\n%s", loc_begin.str().c_str()));
							emit = false;
							break;
						}
						#pragma endregion
					}
				}
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

			Preprocessor<> pp;
			pr::string<wchar_t> str1;
			pp.Push(src1);
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

			char const* src =
				"// Line comment\n"
				"";
			wchar_t const* out =
				L"";

			Preprocessor<> pp(src);
			for (; *pp; ++pp, out += *out != 0)
			{
				if (*pp == *out) continue;
				PR_CHECK(*pp, *out);
			}
			PR_CHECK(*out, 0);
			PR_CHECK(*pp, 0);
		}
#pragma region
#if 0
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
				PtrSrc src(str_in);
				PPMacroDB macros;
				Preprocessor pp(src, &macros, 0, 0);
				for (;*str_out; ++pp, ++str_out)
					PR_CHECK(*pp, *str_out);
				PR_CHECK(*pp, 0);
			}
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

