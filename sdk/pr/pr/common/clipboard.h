//*******************************************************
// Clipboard
//  Copyright (c) Rylogic Ltd 2007
//*******************************************************
#ifndef PR_COMMON_CLIPBOARD_H
#define PR_COMMON_CLIPBOARD_H

#include <windows.h>

namespace pr
{
	// Set some text on the clip board
	template <typename String> bool SetClipBoardText(HWND hwnd, String const& str)
	{
		if (!OpenClipboard(hwnd)) return false;
		EmptyClipboard();

		// Allocate a global memory object for the text.
		HGLOBAL hglbCopy = GlobalAlloc(GMEM_MOVEABLE, str.size() + 1);
		if (!hglbCopy) { CloseClipboard(); return false; }

		// Lock the handle and copy the text to the buffer.
		void* lpstrCopy = GlobalLock(hglbCopy);
		memcpy(lpstrCopy, str.c_str(), str.size() + 1);
		GlobalUnlock(hglbCopy);

		// Place the handle on the clipboard.
		SetClipboardData(CF_TEXT, hglbCopy);
		CloseClipboard();
		return true;
	}

	// Get some text from the clip board
	template <typename String> bool GetClipBoardText(HWND hwnd, String& str)
	{
		if (!IsClipboardFormatAvailable(CF_TEXT)) return false;
		if (!OpenClipboard(hwnd)) return false;

		HGLOBAL hglb = GetClipboardData(CF_TEXT);
		if (!hglb) { CloseClipboard(); return false; }

		LPTSTR lptstr = static_cast<LPTSTR>(GlobalLock(hglb));
		if (!lptstr) { CloseClipboard(); return false; }

		str = lptstr;
		GlobalUnlock(hglb);
		CloseClipboard();
		return true;
	}
}

#endif
