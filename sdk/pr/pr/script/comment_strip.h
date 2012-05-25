//**********************************
// Script charactor source
//  Copyright © Rylogic Ltd 2007
//**********************************
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

#endif
