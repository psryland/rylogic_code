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

#include "scintilla/include/scintilla/ILexer.h"
#include "scintilla/Scintilla.h"
#include "scintilla/SciLexer.h"

#include "scintilla/src/lexlib/WordList.h"
#include "scintilla/src/lexlib/LexAccessor.h"
#include "scintilla/src/lexlib/Accessor.h"
#include "scintilla/src/lexlib/StyleContext.h"
#include "scintilla/src/lexlib/CharacterSet.h"
#include "scintilla/src/lexlib/LexerModule.h"

#include "pr/linedrawer/ldr_object.h"
#include "pr/script/forward.h"

#ifdef SCI_NAMESPACE
using namespace Scintilla;
#endif

typedef unsigned int uint;
static enum class EWordList
{
	Keywords,
	Preprocessor,
	StringLiterals,
	Numbers,
};
static const char * const LdrWordListDesc[] = {
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
static CharacterSet identifier_start(CharacterSet::setAlpha, "_", 0x80, true);
static CharacterSet identifier(CharacterSet::setAlphaNum, "_", 0x80, true);
static CharacterSet number(CharacterSet::setDigits, ".-+abcdefABCDEF");
static CharacterSet hexnumber(CharacterSet::setDigits, "abcdefABCDEF");
//CharacterSet exponents(CharacterSet::setNone, "eEpP");
//CharacterSet operators(CharacterSet::setNone, "*/-+()={}~[];<>,.^%:#");
//CharacterSet escapechars(CharacterSet::setNone, "\"'\\");


static void StyleNameAndColour(StyleContext& sc)
{
	bool name = false, col = false;
	for (; sc.More() && sc.ch != '{' && (!name || !col); sc.Forward())
	{
		switch (sc.state)
		{
		case SCE_LDR_DEFAULT:
			if (!name && identifier_start.Contains(sc.ch)) { sc.SetState(SCE_LDR_NAME); break; }
			if (!col  && hexnumber.Contains(sc.ch)) { sc.SetState(SCE_LDR_COLOUR); break; }
			break;
		case SCE_LDR_NAME:
			if (!identifier.Contains(sc.ch))
			{
				name = true;
				char s[100] = {};
				sc.GetCurrent(s,sizeof(s));
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
		case SCE_LDR_COLOUR:
			if (!hexnumber.Contains(sc.ch))
			{
				name = true;
				col = true;
				sc.SetState(SCE_LDR_DEFAULT);
			}
			break;
		}
	}
	sc.SetState(SCE_LDR_DEFAULT);
}

// Colourise an ldr script
static void LexLdrDoc(unsigned int startPos, int length, int initStyle, WordList* /*keywordlists*/[], Accessor &styler)
{
	enum class ETok { None, Keyword, LineComment, BlockComment, StrLiteral, CharLiteral };

	ETok tok = ETok::None;
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
				if (sc.chNext == '*') { tok = ETok::BlockComment; sc.SetState(SCE_LDR_COMMENT); sc.Forward(); }
				if (sc.chNext == '/') { tok = ETok::LineComment;  sc.SetState(SCE_LDR_COMMENT); sc.Forward(); }
				break;
			case '"':
				tok = ETok::StrLiteral;
				sc.SetState(SCE_LDR_STRING);
				break;
			case '\'':
				tok = ETok::CharLiteral;
				sc.SetState(SCE_LDR_STRING);
				break;
			}
			break;
		case SCE_LDR_COMMENT:
			if (tok == ETok::LineComment && sc.atLineEnd && sc.chPrev != '\\')
				sc.SetState(SCE_LDR_DEFAULT);
			if (tok == ETok::BlockComment && sc.Match('*','/'))
				sc.Forward(),sc.ForwardSetState(SCE_LDR_DEFAULT);
			break;
		case SCE_LDR_STRING:
			if (tok == ETok::StrLiteral && sc.ch == '"' && sc.chPrev != '\\')
				sc.ForwardSetState(SCE_LDR_DEFAULT);
			if (tok == ETok::CharLiteral && sc.ch == '\'' && sc.chPrev != '\\')
				sc.ForwardSetState(SCE_LDR_DEFAULT);
			break;
		case SCE_LDR_NUMBER:
			if (!number.Contains(sc.ch))
				sc.SetState(SCE_LDR_DEFAULT);
			break;
		case SCE_LDR_KEYWORD:
			if (!identifier.Contains(sc.ch))
			{
				char s[100] = {}, *p = &s[1];
				sc.GetCurrentLowered(s,sizeof(s));
				
				pr::ldr::ELdrObject obj;
				if (pr::ldr::ELdrObject::TryParse(obj, p, false))
				{
					sc.ChangeState(SCE_LDR_OBJECT);
					sc.SetState(SCE_LDR_DEFAULT);
					StyleNameAndColour(sc);
					break;
				}
				pr::ldr::EKeyword kw;
				if (pr::ldr::EKeyword::TryParse(kw, p, false))
				{
					sc.ChangeState(SCE_LDR_KEYWORD);
					sc.SetState(SCE_LDR_DEFAULT);
					break;
				}
				sc.ChangeState(SCE_LDR_DEFAULT);
			}
			break;
		case SCE_LDR_PREPROC:
			if (!identifier.Contains(sc.ch))
			{
				char s[100] = {}, *p = s;
				sc.GetCurrentLowered(s,sizeof(s));
				for (++p; *p && IsASpaceOrTab(*p); ++p) {}
				
				pr::script::EPPKeyword kw;
				if (!pr::script::EPPKeyword::TryParse(kw, p, true))
					sc.ChangeState(SCE_LDR_DEFAULT);

				sc.SetState(SCE_LDR_DEFAULT);
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
static void FoldLdrDoc(unsigned int startPos, int length, int /* initStyle */, WordList *[], Accessor &styler)
{
	auto line_idx = styler.GetLine(startPos);
	auto fold_level = styler.LevelAt(line_idx) & SC_FOLDLEVELNUMBERMASK;
	auto fold_flags = SC_FOLDLEVELWHITEFLAG;

	for (auto i = startPos, iend = startPos + length; i != iend; ++i)
	{
		auto ch_curr = styler.SafeGetCharAt(i);
		auto ch_next = styler.SafeGetCharAt(i+1);
		auto eol = ch_curr == '\n' || (ch_curr == '\r' && ch_next == '\n');

		// Non-whitespace chars on the line?
		if ((fold_flags & SC_FOLDLEVELWHITEFLAG) != 0 && !isspacechar(ch_curr))
			fold_flags &= ~SC_FOLDLEVELWHITEFLAG;

		switch (styler.StyleAt(i))
		{
		case SCE_LDR_COMMENT:
			{
				if (ch_curr == '/' && ch_next == '/')
				{
					if (styler.SafeGetCharAt(i+2) == '{' && styler.SafeGetCharAt(i+3) == '{')
					{
						++fold_level;
						fold_flags |= SC_FOLDLEVELHEADERFLAG;
					}
					if (styler.SafeGetCharAt(i+2) == '}' && styler.SafeGetCharAt(i+3) == '}')
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
/*	int line_idx = styler.GetLine(startPos);
	int levelPrev = styler.LevelAt(line_idx) & SC_FOLDLEVELNUMBERMASK;
	int levelCurrent = levelPrev;
	int visibleChars = 0;
	bool foldCompact = styler.GetPropertyInt("fold.compact", 1) != 0;

	char ch_next = styler.SafeGetCharAt(startPos);//[startPos];
	int style_next = styler.StyleAt(startPos);
	for (auto i = startPos, iend = startPos + length; i != iend; ++i)
	{
		// Get the current and next char
		char ch = ch_next;
		ch_next = styler.SafeGetCharAt(i + 1);

		// Get the current and next style
		int style = style_next;
		style_next = styler.StyleAt(i + 1);

		bool atEOL = (ch == '\r' && ch_next != '\n') || (ch == '\n');
		if (style == SCE_LUA_WORD)
		{
			if (ch == 'i' || ch == 'd' || ch == 'f' || ch == 'e' || ch == 'r' || ch == 'u') {
				char s[10] = "";
				for (unsigned int j = 0; j < 8; j++) {
					if (!iswordchar(styler[i + j])) {
						break;
					}
					s[j] = styler[i + j];
					s[j + 1] = '\0';
				}

				if ((strcmp(s, "if") == 0) || (strcmp(s, "do") == 0) || (strcmp(s, "function") == 0) || (strcmp(s, "repeat") == 0)) {
					levelCurrent++;
				}
				if ((strcmp(s, "end") == 0) || (strcmp(s, "elseif") == 0) || (strcmp(s, "until") == 0)) {
					levelCurrent--;
				}
			}
		} else if (style == SCE_LUA_OPERATOR) {
			if (ch == '{' || ch == '(') {
				levelCurrent++;
			} else if (ch == '}' || ch == ')') {
				levelCurrent--;
			}
		} else if (style == SCE_LUA_LITERALSTRING || style == SCE_LUA_COMMENT) {
			if (ch == '[') {
				levelCurrent++;
			} else if (ch == ']') {
				levelCurrent--;
			}
		}

		if (atEOL) {
			int lev = levelPrev;
			if (visibleChars == 0 && foldCompact) {
				lev |= SC_FOLDLEVELWHITEFLAG;
			}
			if ((levelCurrent > levelPrev) && (visibleChars > 0)) {
				lev |= SC_FOLDLEVELHEADERFLAG;
			}
			if (lev != styler.LevelAt(line_idx)) {
				styler.SetLevel(line_idx, lev);
			}
			line_idx++;
			levelPrev = levelCurrent;
			visibleChars = 0;
		}
		if (!isspacechar(ch)) {
			visibleChars++;
		}
	}
	// Fill in the real level of the next line, keeping the current flags as they will be filled in later

	int flagsNext = styler.LevelAt(line_idx) & ~SC_FOLDLEVELNUMBERMASK;
	styler.SetLevel(line_idx, levelPrev | flagsNext);
	*/
}

LexerModule lmLdr(SCLEX_LDR, LexLdrDoc, "ldr", FoldLdrDoc, LdrWordListDesc);




	/*
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
	*/