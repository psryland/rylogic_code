// Scintilla source code edit control
/** @file LexerSimple.cxx
 ** A simple lexer with no state.
 **/
// Copyright 1998-2010 by Neil Hodgson <neilh@scintilla.org>
// The License.txt file describes the conditions under which this software may be distributed.

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <ctype.h>
#include <stdarg.h>
#include <assert.h>
#include <string>

#include "scintilla/include/scintilla/ILexer.h"
#include "scintilla/Scintilla.h"
#include "scintilla/SciLexer.h"
#include "scintilla/src/lexlib/PropSetSimple.h"
#include "scintilla/src/lexlib/WordList.h"
#include "scintilla/src/lexlib/LexAccessor.h"
#include "scintilla/src/lexlib/Accessor.h"
#include "scintilla/src/lexlib/LexerModule.h"
#include "scintilla/src/lexlib/LexerBase.h"
#include "scintilla/src/lexlib/LexerSimple.h"

#ifdef SCI_NAMESPACE
using namespace Scintilla;
#endif

LexerSimple::LexerSimple(const LexerModule *module_) : module(module_) {
	for (int wl = 0; wl < module->GetNumWordLists(); wl++) {
		if (!wordLists.empty())
			wordLists += "\n";
		wordLists += module->GetWordListDescription(wl);
	}
}

const char * SCI_METHOD LexerSimple::DescribeWordListSets() {
	return wordLists.c_str();
}

void SCI_METHOD LexerSimple::Lex(unsigned int startPos, int lengthDoc, int initStyle, IDocument *pAccess) {
	Accessor astyler(pAccess, &props);
	module->Lex(startPos, lengthDoc, initStyle, keyWordLists, astyler);
	astyler.Flush();
}

void SCI_METHOD LexerSimple::Fold(unsigned int startPos, int lengthDoc, int initStyle, IDocument *pAccess) {
	if (props.GetInt("fold")) {
		Accessor astyler(pAccess, &props);
		module->Fold(startPos, lengthDoc, initStyle, keyWordLists, astyler);
		astyler.Flush();
	}
}
