//**********************************
// Script character source
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#pragma once
#ifndef PR_SCRIPT_COMMENT_STRIP_H
#define PR_SCRIPT_COMMENT_STRIP_H

#include "pr/script/script_core.h"

namespace pr
{
	namespace script
	{
		// A char stream that removes comments
		struct CommentStrip :Src
		{
			Buffer<> m_buf;
			
			CommentStrip(Src& src) :Src(SrcType::Unknown) ,m_buf(src) {}
			SrcType::Type type() const { return m_buf.type(); }
			Loc           loc()  const { return m_buf.loc(); }
			void          loc(Loc& l)  { m_buf.loc(l); }
			
		protected:
			char peek() const { return *m_buf; }
			void next()       { ++m_buf; }
			void seek()
			{
				for (;;)
				{
					if (!m_buf.empty()) break;
					
					// Read through literal strings
					if (*m_buf == '\"')
					{
						m_buf.BufferLiteralString();
						continue;
					}
					
					// Read through literal chars
					if (*m_buf == '\'')
					{
						m_buf.BufferLiteralChar();
						continue;
					}
					
					// Remove comments
					if (*m_buf == '/')
					{
						m_buf.buffer(2);
						if (m_buf[1] == '/') { m_buf.clear(); Eat::LineComment (m_buf); continue; }
						if (m_buf[1] == '*') { m_buf.clear(); Eat::BlockComment(m_buf); continue; }
					}
					break;
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
		PRUnitTest(pr_script_comment_strip)
		{
			using namespace pr;
			using namespace pr::script;

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
				"*/\n";
			char const* str_out = 
				"123\n"
				"456789     \n"
				"\n"
				"\n"
				"\n"
				"      \n"
				"\"string \\\" /*a*/ //b\"  \n"
				"/not a comment\n"
				"\n";
			PtrSrc src(str_in);
			CommentStrip strip(src);
			for (;*str_out; ++strip, ++str_out)
				PR_CHECK(*strip, *str_out);
			PR_CHECK(*strip, 0);
		}
	}
}
#endif

#endif
