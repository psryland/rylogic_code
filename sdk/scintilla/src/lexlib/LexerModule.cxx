// Scintilla source code edit control
/** @file LexerModule.cxx
 ** Colourise for particular languages.
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

#include "scintilla/include/ILexer.h"
#include "scintilla/include/scintilla.h"
#include "scintilla/include/SciLexer.h"
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

LexerModule::LexerModule(int language_,
	LexerFunction fnLexer_,
	const char *languageName_,
	LexerFunction fnFolder_,
        const char *const wordListDescriptions_[],
	int styleBits_) :
	language(language_),
	fnLexer(fnLexer_),
	fnFolder(fnFolder_),
	fnFactory(0),
	wordListDescriptions(wordListDescriptions_),
	styleBits(styleBits_),
	languageName(languageName_) {
}

LexerModule::LexerModule(int language_,
	LexerFactoryFunction fnFactory_,
	const char *languageName_,
	const char * const wordListDescriptions_[],
	int styleBits_) :
	language(language_),
	fnLexer(0),
	fnFolder(0),
	fnFactory(fnFactory_),
	wordListDescriptions(wordListDescriptions_),
	styleBits(styleBits_),
	languageName(languageName_) {
}

int LexerModule::GetNumWordLists() const {
	if (wordListDescriptions == NULL) {
		return -1;
	} else {
		int numWordLists = 0;

		while (wordListDescriptions[numWordLists]) {
			++numWordLists;
		}

		return numWordLists;
	}
}

const char *LexerModule::GetWordListDescription(int index) const {
	assert(index < GetNumWordLists());
	if (!wordListDescriptions || (index >= GetNumWordLists())) {
		return "";
	} else {
		return wordListDescriptions[index];
	}
}

int LexerModule::GetStyleBitsNeeded() const {
	return styleBits;
}

ILexer *LexerModule::Create() const {
	if (fnFactory)
		return fnFactory();
	else
		return new LexerSimple(this);
}

void LexerModule::Lex(unsigned int startPos, int lengthDoc, int initStyle,
	  WordList *keywordlists[], Accessor &styler) const {
	if (fnLexer)
		fnLexer(startPos, lengthDoc, initStyle, keywordlists, styler);
}

void LexerModule::Fold(unsigned int startPos, int lengthDoc, int initStyle,
	  WordList *keywordlists[], Accessor &styler) const {
	if (fnFolder) {
		int lineCurrent = styler.GetLine(startPos);
		// Move back one line in case deletion wrecked current line fold state
		if (lineCurrent > 0) {
			lineCurrent--;
			int newStartPos = styler.LineStart(lineCurrent);
			lengthDoc += startPos - newStartPos;
			startPos = newStartPos;
			initStyle = 0;
			if (startPos > 0) {
				initStyle = styler.StyleAt(startPos - 1);
			}
		}
		fnFolder(startPos, lengthDoc, initStyle, keywordlists, styler);
	}
}
