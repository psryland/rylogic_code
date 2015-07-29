// Scintilla source code edit control
/** @file LexerNoExceptions.cxx
 ** A simple lexer with no state which does not throw exceptions so can be used in an external lexer.
 **/
// Copyright 1998-2010 by Neil Hodgson <neilh@scintilla.org>
// The License.txt file describes the conditions under which this software may be distributed.

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <ctype.h>
#include <stdarg.h>
#include <assert.h>

#include "scintilla/include/ILexer.h"
#include "scintilla/include/scintilla.h"
#include "scintilla/include/SciLexer.h"
#include "scintilla/src/lexlib/PropSetSimple.h"
#include "scintilla/src/lexlib/WordList.h"
#include "scintilla/src/lexlib/LexAccessor.h"
#include "scintilla/src/lexlib/Accessor.h"
#include "scintilla/src/lexlib/LexerModule.h"
#include "scintilla/src/lexlib/LexerBase.h"
#include "scintilla/src/lexlib/LexerNoExceptions.h"

#ifdef SCI_NAMESPACE
using namespace Scintilla;
#endif

int SCI_METHOD LexerNoExceptions::PropertySet(const char *key, const char *val) {
	try {
		return LexerBase::PropertySet(key, val);
	} catch (...) {
		// Should not throw into caller as may be compiled with different compiler or options
	}
	return -1;
}

int SCI_METHOD LexerNoExceptions::WordListSet(int n, const char *wl) {
	try {
		return LexerBase::WordListSet(n, wl);
	} catch (...) {
		// Should not throw into caller as may be compiled with different compiler or options
	}
	return -1;
}

void SCI_METHOD LexerNoExceptions::Lex(unsigned int startPos, int length, int initStyle, IDocument *pAccess) {
	try {
		Accessor astyler(pAccess, &props);
		Lexer(startPos, length, initStyle, pAccess, astyler);
		astyler.Flush();
	} catch (...) {
		// Should not throw into caller as may be compiled with different compiler or options
		pAccess->SetErrorStatus(SC_STATUS_FAILURE);
	}
}
void SCI_METHOD LexerNoExceptions::Fold(unsigned int startPos, int length, int initStyle, IDocument *pAccess) {
	try {
		Accessor astyler(pAccess, &props);
		Folder(startPos, length, initStyle, pAccess, astyler);
		astyler.Flush();
	} catch (...) {
		// Should not throw into caller as may be compiled with different compiler or options
		pAccess->SetErrorStatus(SC_STATUS_FAILURE);
	}
}
