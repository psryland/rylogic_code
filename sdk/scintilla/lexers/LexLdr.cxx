// Scintilla source code edit control
/** @file LexLdr.cxx
 ** Lexer for LineDrawer Script.
 **
 ** Written by Paul Ryland
 **/

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <stdarg.h>
#include <assert.h>
#include <ctype.h>

#include "ILexer.h"
#include "Scintilla.h"
#include "SciLexer.h"

#include "WordList.h"
#include "LexAccessor.h"
#include "Accessor.h"
#include "StyleContext.h"
#include "CharacterSet.h"
#include "LexerModule.h"

#include "pr/ldraw/ldr_object.h"
#include "pr/script/forward.h"

using namespace Scintilla;

using namespace pr::ldr;
using namespace pr::script;

namespace
{
	enum class EWordList
	{
		Keywords,
		Preprocessor,
		StringLiterals,
		Numbers,
	};
	static const char* const LdrWordListDesc[] =
	{
		"Keywords",
		"Preprocessor",
		"String literals",
		"Numbers",
		"user2",
		"user3",
		"user4",
		"user5",
		0
	};
	static CharacterSet s_cs_identifier_start(CharacterSet::setAlpha, "_", 0x80, true);
	static CharacterSet s_cs_identifier(CharacterSet::setAlphaNum, "_", 0x80, true);
	static CharacterSet s_cs_number(CharacterSet::setDigits, ".-+abcdefABCDEF");
	static CharacterSet s_cs_hex_number(CharacterSet::setDigits, "abcdefABCDEF");

	// Style the name and colour fields of an LdrObject description
	static void StyleNameAndColour(StyleContext& sc)
	{
		bool name = false, col = false;
		for (; sc.More() && sc.ch != '{' && (!name || !col); sc.Forward())
		{
			switch (sc.state)
			{
			case SCE_LDR_DEFAULT:
				{
					if (!name && s_cs_identifier_start.Contains(sc.ch)) { sc.SetState(SCE_LDR_NAME); break; }
					if (!col && s_cs_hex_number.Contains(sc.ch)) { sc.SetState(SCE_LDR_COLOUR); break; }
					break;
				}
			case SCE_LDR_NAME:
				{
					if (!s_cs_identifier.Contains(sc.ch))
					{
						name = true;
						char s[100] = {};
						sc.GetCurrent(s, sizeof(s));
						char* end; ::strtoul(s, &end, 16);
						bool is_colour = size_t(end - s) == strlen(s);
						if (is_colour)
						{
							sc.ChangeState(SCE_LDR_COLOUR);
							col = true;
						}
						sc.SetState(SCE_LDR_DEFAULT);
					}
					break;
				}
			case SCE_LDR_COLOUR:
				{
					if (!s_cs_hex_number.Contains(sc.ch))
					{
						name = true;
						col = true;
						sc.SetState(SCE_LDR_DEFAULT);
					}
					break;
				}
			}
		}
		sc.SetState(SCE_LDR_DEFAULT);
	}

	// Colourise an ldr script. Sig typedef: Scintilla::LexerFunction
	static void LexLdrDoc(Sci_PositionU startPos, Sci_Position length, int initStyle, WordList* /*keywordlists*/[], Accessor& styler)
	{
		StyleContext sc(startPos, length, initStyle, styler);
		for (; sc.More(); sc.Forward())
		{
			switch (sc.state)
			{
			case SCE_LDR_DEFAULT:
				switch (sc.ch)
				{
				case '*':
					sc.SetState(SCE_LDR_KEYWORD);
					break;
				case '#':
					sc.SetState(SCE_LDR_PREPROC);
					break;
				case '/':
					if (sc.chNext == '*') { sc.SetState(SCE_LDR_COMMENT_BLK); sc.Forward(); }
					if (sc.chNext == '/') { sc.SetState(SCE_LDR_COMMENT_LINE); sc.Forward(); }
					break;
				case '"':
					sc.SetState(SCE_LDR_STRING_LITERAL);
					break;
				case '\'':
					sc.SetState(SCE_LDR_CHAR_LITERAL);
					break;
				}
				break;
			case SCE_LDR_COMMENT_LINE:
				if (sc.atLineEnd && sc.chPrev != '\\')
					sc.SetState(SCE_LDR_DEFAULT);
				break;
			case SCE_LDR_COMMENT_BLK:
				if (sc.Match('*', '/'))
					sc.Forward(), sc.ForwardSetState(SCE_LDR_DEFAULT);
				break;
			case SCE_LDR_STRING_LITERAL:
				if (sc.ch == '"' && sc.chPrev != '\\')
					sc.ForwardSetState(SCE_LDR_DEFAULT);
				break;
			case SCE_LDR_CHAR_LITERAL:
				if (sc.ch == '\'' && sc.chPrev != '\\')
					sc.ForwardSetState(SCE_LDR_DEFAULT);
				break;
			case SCE_LDR_NUMBER:
				if (!s_cs_number.Contains(sc.ch))
					sc.SetState(SCE_LDR_DEFAULT);
				break;
			case SCE_LDR_KEYWORD:
				if (!s_cs_identifier.Contains(sc.ch))
				{
					char s[100] = {}, *p = &s[1]; // Skip the '*'
					sc.GetCurrentLowered(s, sizeof(s) - 1);

					// When the text matches the keyword, go back to the default state
					ELdrObject ldr;
					if (pr::Enum<ELdrObject>::TryParse(ldr, p, false))
					{
						sc.ChangeState(SCE_LDR_OBJECT);
						sc.SetState(SCE_LDR_DEFAULT);
						StyleNameAndColour(sc);
						break;
					}
					pr::ldr::EKeyword kw;
					if (pr::Enum<pr::ldr::EKeyword>::TryParse(kw, p, false))
					{
						sc.ChangeState(SCE_LDR_KEYWORD);
						sc.SetState(SCE_LDR_DEFAULT);
						break;
					}
					sc.ChangeState(SCE_LDR_DEFAULT);
				}
				break;
			case SCE_LDR_PREPROC:
				if (!s_cs_identifier.Contains(sc.ch))
				{
					char s[100] = {}, *p = &s[1]; // Skip the '#'
					sc.GetCurrentLowered(s, sizeof(s));
					for (; *p && IsASpaceOrTab(*p); ++p) {} // skip the space between the # and keyword

					// When the text matches the keyword, go back to the default state
					EPPKeyword kw;
					if (pr::Enum<EPPKeyword>::TryParse(kw, p))
					{
						sc.ChangeState(SCE_LDR_PREPROC);
						sc.SetState(SCE_LDR_DEFAULT);
						break;
					}

					sc.ChangeState(SCE_LDR_DEFAULT);
				}
				break;
			case SCE_LDR_NAME:
				break;
			case SCE_LDR_COLOUR:
				break;
			}
		}
		sc.Complete();
	}

	// Fold an ldr script
	static void FoldLdrDoc(Sci_PositionU startPos, Sci_Position length, int /* initStyle */, WordList* [], Accessor& styler)
	{
		auto line_idx = styler.GetLine(startPos);
		auto fold_level = styler.LevelAt(line_idx) & SC_FOLDLEVELNUMBERMASK;
		auto fold_flags = SC_FOLDLEVELWHITEFLAG;

		for (auto i = startPos, iend = startPos + length; i != iend; ++i)
		{
			auto ch_curr = styler.SafeGetCharAt(i);
			auto ch_next = styler.SafeGetCharAt(i + 1);
			auto eol = ch_curr == '\n' || (ch_curr == '\r' && ch_next == '\n');

			// Non-whitespace chars on the line?
			if ((fold_flags & SC_FOLDLEVELWHITEFLAG) != 0 && !isspacechar(ch_curr))
				fold_flags &= ~SC_FOLDLEVELWHITEFLAG;

			switch (styler.StyleAt(i))
			{
			case SCE_LDR_COMMENT_BLK:
			case SCE_LDR_COMMENT_LINE:
				{
					if (ch_curr == '/' && ch_next == '/')
					{
						if (styler.SafeGetCharAt(i + 2) == '{' && styler.SafeGetCharAt(i + 3) == '{')
						{
							++fold_level;
							fold_flags |= SC_FOLDLEVELHEADERFLAG;
						}
						if (styler.SafeGetCharAt(i + 2) == '}' && styler.SafeGetCharAt(i + 3) == '}')
						{
							--fold_level;
						}
					}
					break;
				}
			case SCE_LDR_DEFAULT:
				{
					if (ch_curr == '{')
					{
						++fold_level;
						fold_flags |= SC_FOLDLEVELHEADERFLAG;
					}
					if (ch_curr == '}')
					{
						--fold_level;
					}
					break;
				}
			}

			if (eol)
			{
				auto lev = fold_flags & fold_level;
				styler.SetLevel(line_idx, lev);
				fold_flags = SC_FOLDLEVELWHITEFLAG;
				line_idx++;
			}
		}
	}
}

// Declare a lexer module instance for 'ldr' script
LexerModule lmLdr(SCLEX_LDR, LexLdrDoc, "ldr", FoldLdrDoc, LdrWordListDesc);




#if 0 // lua example
	WordList &keywords = *keywordlists[0];
	WordList &keywords2 = *keywordlists[1];
	WordList &keywords3 = *keywordlists[2];
	WordList &keywords4 = *keywordlists[3];
	WordList &keywords5 = *keywordlists[4];
	WordList &keywords6 = *keywordlists[5];
	WordList &keywords7 = *keywordlists[6];
	WordList &keywords8 = *keywordlists[7];

	int currentLine = styler.GetLine(startPos);
	// Initialize long string [[ ... ]] or block comment --[[ ... ]] nesting level,
	// if we are inside such a string. Block comment was introduced in Lua 5.0,
	// blocks with separators [=[ ... ]=] in Lua 5.1.
	// Continuation of a string (\z whitespace escaping) is controlled by stringWs.
	int nestLevel = 0;
	int sepCount = 0;
	int stringWs = 0;
	if (initStyle == SCE_LUA_LITERALSTRING || initStyle == SCE_LUA_COMMENT ||
		initStyle == SCE_LUA_STRING || initStyle == SCE_LUA_CHARACTER) {
		int lineState = styler.GetLineState(currentLine - 1);
		nestLevel = lineState >> 9;
		sepCount = lineState & 0xFF;
		stringWs = lineState & 0x100;
	}

	// Do not leak onto next line
	if (initStyle == SCE_LUA_STRINGEOL || initStyle == SCE_LUA_COMMENTLINE || initStyle == SCE_LUA_PREPROCESSOR) {
		initStyle = SCE_LUA_DEFAULT;
	}

	StyleContext sc(startPos, length, initStyle, styler);
	if (startPos == 0 && sc.ch == '#') {
		// shbang line: # is a comment only if first char of the script
		sc.SetState(SCE_LUA_COMMENTLINE);
	}
	for (; sc.More(); sc.Forward()) {
		if (sc.atLineEnd) {
			// Update the line state, so it can be seen by next line
			currentLine = styler.GetLine(sc.currentPos);
			switch (sc.state) {
			case SCE_LUA_LITERALSTRING:
			case SCE_LUA_COMMENT:
			case SCE_LUA_STRING:
			case SCE_LUA_CHARACTER:
				// Inside a literal string, block comment or string, we set the line state
				styler.SetLineState(currentLine, (nestLevel << 9) | stringWs | sepCount);
				break;
			default:
				// Reset the line state
				styler.SetLineState(currentLine, 0);
				break;
			}
		}
		if (sc.atLineStart && (sc.state == SCE_LUA_STRING)) {
			// Prevent SCE_LUA_STRINGEOL from leaking back to previous line
			sc.SetState(SCE_LUA_STRING);
		}

		// Handle string line continuation
		if ((sc.state == SCE_LUA_STRING || sc.state == SCE_LUA_CHARACTER) &&
				sc.ch == '\\') {
			if (sc.chNext == '\n' || sc.chNext == '\r') {
				sc.Forward();
				if (sc.ch == '\r' && sc.chNext == '\n') {
					sc.Forward();
				}
				continue;
			}
		}

		// Determine if the current state should terminate.
		if (sc.state == SCE_LUA_OPERATOR) {
			if (sc.ch == ':' && sc.chPrev == ':') {	// :: <label> :: forward scan
				sc.Forward();
				int ln = 0;
				while (IsASpaceOrTab(sc.GetRelative(ln)))	// skip over spaces/tabs
					ln++;
				int ws1 = ln;
				if (setWordStart.Contains(sc.GetRelative(ln))) {
					int c, i = 0;
					char s[100];
					while (setWord.Contains(c = sc.GetRelative(ln))) {	// get potential label
						if (i < 90)
							s[i++] = static_cast<char>(c);
						ln++;
					}
					s[i] = '\0'; int lbl = ln;
					if (!keywords.InList(s)) {
						while (IsASpaceOrTab(sc.GetRelative(ln)))	// skip over spaces/tabs
							ln++;
						int ws2 = ln - lbl;
						if (sc.GetRelative(ln) == ':' && sc.GetRelative(ln + 1) == ':') {
							// final :: found, complete valid label construct
							sc.ChangeState(SCE_LUA_LABEL);
							if (ws1) {
								sc.SetState(SCE_LUA_DEFAULT);
								sc.ForwardBytes(ws1);
							}
							sc.SetState(SCE_LUA_LABEL);
							sc.ForwardBytes(lbl - ws1);
							if (ws2) {
								sc.SetState(SCE_LUA_DEFAULT);
								sc.ForwardBytes(ws2);
							}
							sc.SetState(SCE_LUA_LABEL);
							sc.ForwardBytes(2);
						}
					}
				}
			}
			sc.SetState(SCE_LUA_DEFAULT);
		} else if (sc.state == SCE_LUA_NUMBER) {
			// We stop the number definition on non-numerical non-dot non-eEpP non-sign non-hexdigit char
			if (!setNumber.Contains(sc.ch)) {
				sc.SetState(SCE_LUA_DEFAULT);
			} else if (sc.ch == '-' || sc.ch == '+') {
				if (!setExponent.Contains(sc.chPrev))
					sc.SetState(SCE_LUA_DEFAULT);
			}
		} else if (sc.state == SCE_LUA_IDENTIFIER) {
			if (!(setWord.Contains(sc.ch) || sc.ch == '.') || sc.Match('.', '.')) {
				char s[100];
				sc.GetCurrent(s, sizeof(s));
				if (keywords.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD);
					if (strcmp(s, "goto") == 0) {	// goto <label> forward scan
						sc.SetState(SCE_LUA_DEFAULT);
						while (IsASpaceOrTab(sc.ch) && !sc.atLineEnd)
							sc.Forward();
						if (setWordStart.Contains(sc.ch)) {
							sc.SetState(SCE_LUA_LABEL);
							sc.Forward();
							while (setWord.Contains(sc.ch))
								sc.Forward();
							sc.GetCurrent(s, sizeof(s));
							if (keywords.InList(s))
								sc.ChangeState(SCE_LUA_WORD);
						}
						sc.SetState(SCE_LUA_DEFAULT);
					}
				} else if (keywords2.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD2);
				} else if (keywords3.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD3);
				} else if (keywords4.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD4);
				} else if (keywords5.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD5);
				} else if (keywords6.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD6);
				} else if (keywords7.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD7);
				} else if (keywords8.InList(s)) {
					sc.ChangeState(SCE_LUA_WORD8);
				}
				sc.SetState(SCE_LUA_DEFAULT);
			}
		} else if (sc.state == SCE_LUA_COMMENTLINE || sc.state == SCE_LUA_PREPROCESSOR) {
			if (sc.atLineEnd) {
				sc.ForwardSetState(SCE_LUA_DEFAULT);
			}
		} else if (sc.state == SCE_LUA_STRING) {
			if (stringWs) {
				if (!IsASpace(sc.ch))
					stringWs = 0;
			}
			if (sc.ch == '\\') {
				if (setEscapeSkip.Contains(sc.chNext)) {
					sc.Forward();
				} else if (sc.chNext == 'z') {
					sc.Forward();
					stringWs = 0x100;
				}
			} else if (sc.ch == '\"') {
				sc.ForwardSetState(SCE_LUA_DEFAULT);
			} else if (stringWs == 0 && sc.atLineEnd) {
				sc.ChangeState(SCE_LUA_STRINGEOL);
				sc.ForwardSetState(SCE_LUA_DEFAULT);
			}
		} else if (sc.state == SCE_LUA_CHARACTER) {
			if (stringWs) {
				if (!IsASpace(sc.ch))
					stringWs = 0;
			}
			if (sc.ch == '\\') {
				if (setEscapeSkip.Contains(sc.chNext)) {
					sc.Forward();
				} else if (sc.chNext == 'z') {
					sc.Forward();
					stringWs = 0x100;
				}
			} else if (sc.ch == '\'') {
				sc.ForwardSetState(SCE_LUA_DEFAULT);
			} else if (stringWs == 0 && sc.atLineEnd) {
				sc.ChangeState(SCE_LUA_STRINGEOL);
				sc.ForwardSetState(SCE_LUA_DEFAULT);
			}
		} else if (sc.state == SCE_LUA_LITERALSTRING || sc.state == SCE_LUA_COMMENT) {
			if (sc.ch == '[') {
				int sep = LongDelimCheck(sc);
				if (sep == 1 && sepCount == 1) {    // [[-only allowed to nest
					nestLevel++;
					sc.Forward();
				}
			} else if (sc.ch == ']') {
				int sep = LongDelimCheck(sc);
				if (sep == 1 && sepCount == 1) {    // un-nest with ]]-only
					nestLevel--;
					sc.Forward();
					if (nestLevel == 0) {
						sc.ForwardSetState(SCE_LUA_DEFAULT);
					}
				} else if (sep > 1 && sep == sepCount) {   // ]=]-style delim
					sc.Forward(sep);
					sc.ForwardSetState(SCE_LUA_DEFAULT);
				}
			}
		}

		// Determine if a new state should be entered.
		if (sc.state == SCE_LUA_DEFAULT) {
			if (IsADigit(sc.ch) || (sc.ch == '.' && IsADigit(sc.chNext))) {
				sc.SetState(SCE_LUA_NUMBER);
				if (sc.ch == '0' && toupper(sc.chNext) == 'X') {
					sc.Forward();
				}
			} else if (setWordStart.Contains(sc.ch)) {
				sc.SetState(SCE_LUA_IDENTIFIER);
			} else if (sc.ch == '\"') {
				sc.SetState(SCE_LUA_STRING);
				stringWs = 0;
			} else if (sc.ch == '\'') {
				sc.SetState(SCE_LUA_CHARACTER);
				stringWs = 0;
			} else if (sc.ch == '[') {
				sepCount = LongDelimCheck(sc);
				if (sepCount == 0) {
					sc.SetState(SCE_LUA_OPERATOR);
				} else {
					nestLevel = 1;
					sc.SetState(SCE_LUA_LITERALSTRING);
					sc.Forward(sepCount);
				}
			} else if (sc.Match('-', '-')) {
				sc.SetState(SCE_LUA_COMMENTLINE);
				if (sc.Match("--[")) {
					sc.Forward(2);
					sepCount = LongDelimCheck(sc);
					if (sepCount > 0) {
						nestLevel = 1;
						sc.ChangeState(SCE_LUA_COMMENT);
						sc.Forward(sepCount);
					}
				} else {
					sc.Forward();
				}
			} else if (sc.atLineStart && sc.Match('$')) {
				sc.SetState(SCE_LUA_PREPROCESSOR);	// Obsolete since Lua 4.0, but still in old code
			} else if (setLuaOperator.Contains(sc.ch)) {
				sc.SetState(SCE_LUA_OPERATOR);
			}
		}
	}

	if (setWord.Contains(sc.chPrev) || sc.chPrev == '.') {
		char s[100];
		sc.GetCurrent(s, sizeof(s));
		if (keywords.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD);
		} else if (keywords2.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD2);
		} else if (keywords3.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD3);
		} else if (keywords4.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD4);
		} else if (keywords5.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD5);
		} else if (keywords6.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD6);
		} else if (keywords7.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD7);
		} else if (keywords8.InList(s)) {
			sc.ChangeState(SCE_LUA_WORD8);
		}
	}
#endif
