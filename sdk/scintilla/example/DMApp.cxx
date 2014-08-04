// A simple demonstration application using Scintilla
#include <stdio.h>

#include <windows.h>
#include <richedit.h>
#include <commdlg.h>

#include "Scintilla.h"
#include "SciLexer.h"
#include "resource.h"

#pragma comment(lib, "imm32.lib")

const char appName[] = "DMApp";
const char className[] = "DMAppWindow";
const int blockSize = 128 * 1024;

const COLORREF black = RGB(0,0,0);
const COLORREF white = RGB(0xff,0xff,0xff);

struct DMApp {
	HINSTANCE hInstance;
	HWND currentDialog;
	HWND wMain;
	HWND wEditor;
	bool isDirty;
	char fullPath[MAX_PATH];

	DMApp();

	LRESULT SendEditor(UINT Msg, WPARAM wParam=0, LPARAM lParam=0) {
		return ::SendMessage(wEditor, Msg, wParam, lParam);
	}

	void GetRange(int start, int end, char *text);

	void SetTitle();
	void New();
	void Open();
	void OpenFile(const char *fileName);
	void Save();
	void SaveAs();
	void SaveFile(const char *fileName);
	int SaveIfUnsure();

	void Command(int id);
	void EnableAMenuItem(int id, bool enable);
	void CheckMenus();
	void Notify(SCNotification *notification);

	void SetAStyle(int style, COLORREF fore, COLORREF back=white, int size=-1, const char *face=0);
	void InitialiseEditor();
};

static DMApp app;

DMApp::DMApp() {
	hInstance = 0;
	currentDialog = 0;
	wMain = 0;
	wEditor = 0;
	isDirty = false;
	fullPath[0] = '\0';
}

void DMApp::GetRange(int start, int end, char *text) {
	TEXTRANGE tr;
	tr.chrg.cpMin = start;
	tr.chrg.cpMax = end;
	tr.lpstrText = text;
	SendMessage(wEditor, EM_GETTEXTRANGE, 0, reinterpret_cast<LPARAM>(&tr));
}

void DMApp::SetTitle() {
	char title[MAX_PATH + 100];
	strcpy(title, appName);
	strcat(title, " - ");
	strcat(title, fullPath);
	::SetWindowText(wMain, title);
}

void DMApp::New() {
	SendEditor(SCI_CLEARALL);
	SendEditor(EM_EMPTYUNDOBUFFER);
	fullPath[0] = '\0';
	SetTitle();
	isDirty = false;
	SendEditor(SCI_SETSAVEPOINT);
}

void DMApp::OpenFile(const char *fileName) {
	New();
	SendEditor(SCI_CANCEL);
	SendEditor(SCI_SETUNDOCOLLECTION, 0);

	strcpy(fullPath, fileName);
	FILE *fp = fopen(fullPath, "rb");
	if (fp) {
		SetTitle();
		char data[blockSize];
		int lenFile = fread(data, 1, sizeof(data), fp);
		while (lenFile > 0) {
			SendEditor(SCI_ADDTEXT, lenFile,
					   reinterpret_cast<LPARAM>(static_cast<char *>(data)));
			lenFile = fread(data, 1, sizeof(data), fp);
		}
		fclose(fp);
	} else {
		char msg[MAX_PATH + 100];
		strcpy(msg, "Could not open file \"");
		strcat(msg, fullPath);
		strcat(msg, "\".");
		::MessageBox(wMain, msg, appName, MB_OK);
	}
	SendEditor(SCI_SETUNDOCOLLECTION, 1);
	::SetFocus(wEditor);
	SendEditor(EM_EMPTYUNDOBUFFER);
	SendEditor(SCI_SETSAVEPOINT);
	SendEditor(SCI_GOTOPOS, 0);
}

void DMApp::Open() {
	char openName[MAX_PATH] = "\0";
	OPENFILENAME ofn = {sizeof(OPENFILENAME)};
	ofn.hwndOwner = wMain;
	ofn.hInstance = hInstance;
	ofn.lpstrFile = openName;
	ofn.nMaxFile = sizeof(openName);
	char *filter = 
		"Web (.html;.htm;.asp;.shtml;.css;.xml)\0*.html;*.htm;*.asp;*.shtml;*.css;*.xml\0"
		"All Files (*.*)\0*.*\0\0";

	ofn.lpstrFilter = filter;
	ofn.lpstrCustomFilter = 0;
	ofn.nMaxCustFilter = 0;
	ofn.nFilterIndex = 0;
	ofn.lpstrTitle = "Open File";
	ofn.Flags = OFN_HIDEREADONLY;

	if (::GetOpenFileName(&ofn)) {
		OpenFile(openName);
	}
}

void DMApp::Save() {
	SaveFile(fullPath);
}

void DMApp::SaveAs() {
	char openName[MAX_PATH] = "\0";
	strcpy(openName, fullPath);
	OPENFILENAME ofn = {sizeof(ofn)};
	ofn.hwndOwner = wMain;
	ofn.hInstance = hInstance;
	ofn.lpstrFile = openName;
	ofn.nMaxFile = sizeof(openName);
	ofn.lpstrTitle = "Save File";
	ofn.Flags = OFN_HIDEREADONLY;

	if (::GetSaveFileName(&ofn)) {
		strcpy(fullPath, openName);
		SetTitle();
		SaveFile(fullPath);
		::InvalidateRect(wEditor, 0, 0);
	}
}

void DMApp::SaveFile(const char *fileName) {
	FILE *fp = fopen(fullPath, "wb");
	if (fp) {
		char data[blockSize + 1];
		int lengthDoc = SendEditor(SCI_GETLENGTH);
		for (int i = 0; i < lengthDoc; i += blockSize) {
			int grabSize = lengthDoc - i;
			if (grabSize > blockSize)
				grabSize = blockSize;
			GetRange(i, i + grabSize, data);
			fwrite(data, grabSize, 1, fp);
		}
		fclose(fp);
		SendEditor(SCI_SETSAVEPOINT);
	} else {
		char msg[MAX_PATH + 100];
		strcpy(msg, "Could not save file \"");
		strcat(msg, fullPath);
		strcat(msg, "\".");
		MessageBox(wMain, msg, appName, MB_OK);
	}
}

int DMApp::SaveIfUnsure() {
	if (isDirty) {
		char msg[MAX_PATH + 100];
		strcpy(msg, "Save changes to \"");
		strcat(msg, fullPath);
		strcat(msg, "\"?");
		int decision = MessageBox(wMain, msg, appName, MB_YESNOCANCEL);
		if (decision == IDYES) {
			Save();
		}
		return decision;
	}
	return IDYES;
}


void DMApp::Command(int id) {
	switch (id) {
	case IDM_FILE_NEW:
		if (SaveIfUnsure() != IDCANCEL) {
			New();
		}
		break;
	case IDM_FILE_OPEN:
		if (SaveIfUnsure() != IDCANCEL) {
			Open();
		}
		break;
	case IDM_FILE_SAVE:
		Save();
		break;
	case IDM_FILE_SAVEAS:
		SaveAs();
		break;
	case IDM_FILE_EXIT:
		if (SaveIfUnsure() != IDCANCEL) {
			::PostQuitMessage(0);
		}
		break;

	case IDM_EDIT_UNDO:
		SendEditor(WM_UNDO);
		break;
	case IDM_EDIT_REDO:
		SendEditor(SCI_REDO);
		break;
	case IDM_EDIT_CUT:
		SendEditor(WM_CUT);
		break;
	case IDM_EDIT_COPY:
		SendEditor(WM_COPY);
		break;
	case IDM_EDIT_PASTE:
		SendEditor(WM_PASTE);
		break;
	case IDM_EDIT_DELETE:
		SendEditor(WM_CLEAR);
		break;
	case IDM_EDIT_SELECTALL:
		SendEditor(SCI_SELECTALL);
		break;
	};
}

void DMApp::EnableAMenuItem(int id, bool enable) {
	if (enable)
		::EnableMenuItem(::GetMenu(wMain), id, MF_ENABLED | MF_BYCOMMAND);
	else
		::EnableMenuItem(::GetMenu(wMain), id, MF_DISABLED | MF_GRAYED | MF_BYCOMMAND);
}

void DMApp::CheckMenus() {
	EnableAMenuItem(IDM_FILE_SAVE, isDirty);
	EnableAMenuItem(IDM_EDIT_UNDO, SendEditor(EM_CANUNDO));
	EnableAMenuItem(IDM_EDIT_REDO, SendEditor(SCI_CANREDO));
	EnableAMenuItem(IDM_EDIT_PASTE, SendEditor(EM_CANPASTE));
}

void DMApp::Notify(SCNotification *notification) {
	switch (notification->nmhdr.code) {
	case SCN_SAVEPOINTREACHED:
		isDirty = false;
		CheckMenus();
		break;

	case SCN_SAVEPOINTLEFT:
		isDirty = true;
		CheckMenus();
		break;
	}
}

void DMApp::SetAStyle(int style, COLORREF fore, COLORREF back, int size, const char *face) {
	SendEditor(SCI_STYLESETFORE, style, fore);
	SendEditor(SCI_STYLESETBACK, style, back);
	if (size >= 1)
		SendEditor(SCI_STYLESETSIZE, style, size);
	if (face) 
		SendEditor(SCI_STYLESETFONT, style, reinterpret_cast<LPARAM>(face));
}

const char htmlKeyWords[] = 
	"a abbr acronym address applet area b base basefont "
	"bdo big blockquote body br button caption center "
	"cite code col colgroup dd del dfn dir div dl dt em "
	"fieldset font form frame frameset h1 h2 h3 h4 h5 h6 "
	"head hr html i iframe img input ins isindex kbd label "
	"legend li link map menu meta noframes noscript "
	"object ol optgroup option p param pre q s samp "
	"script select small span strike strong style sub sup "
	"table tbody td textarea tfoot th thead title tr tt u ul "
	"var xmlns "
	"abbr accept-charset accept accesskey action align alink "
	"alt archive axis background bgcolor border "
	"cellpadding cellspacing char charoff charset checked cite "
	"class classid clear codebase codetype color cols colspan "
	"compact content coords "
	"data datafld dataformatas datapagesize datasrc datetime "
	"declare defer dir disabled enctype "
	"face for frame frameborder "
	"headers height href hreflang hspace http-equiv "
	"id ismap label lang language link longdesc "
	"marginwidth marginheight maxlength media method multiple "
	"name nohref noresize noshade nowrap "
	"object onblur onchange onclick ondblclick onfocus "
	"onkeydown onkeypress onkeyup onload onmousedown "
	"onmousemove onmouseover onmouseout onmouseup "
	"onreset onselect onsubmit onunload "
	"profile prompt readonly rel rev rows rowspan rules "
	"scheme scope shape size span src standby start style "
	"summary tabindex target text title type usemap "
	"valign value valuetype version vlink vspace width "
	"text password checkbox radio submit reset "
	"file hidden image "
	"public !doctype xml";

const char jsKeyWords[] = 
	"break case catch continue default "
	"do else for function if return throw try var while";

const char vbsKeyWords[] = 
	"and as byref byval case call const "
	"continue dim do each else elseif end error exit false for function global "
	"goto if in loop me new next not nothing on optional or private public "
	"redim rem resume select set sub then to true type while with "
	"boolean byte currency date double integer long object single string type "
	"variant";

void DMApp::InitialiseEditor() {
	SendEditor(SCI_SETLEXER, SCLEX_HTML);
	SendEditor(SCI_SETSTYLEBITS, 7);

	SendEditor(SCI_SETKEYWORDS, 0, 
		reinterpret_cast<LPARAM>(htmlKeyWords));
	SendEditor(SCI_SETKEYWORDS, 1, 
		reinterpret_cast<LPARAM>(jsKeyWords));
	SendEditor(SCI_SETKEYWORDS, 2, 
		reinterpret_cast<LPARAM>(vbsKeyWords));

	// Set up the global default style. These attributes are used wherever no explicit choices are made.
	SetAStyle(STYLE_DEFAULT, black, white, 11, "Verdana");
	SendEditor(SCI_STYLECLEARALL);	// Copies global style to all others

	const COLORREF red = RGB(0xFF, 0, 0);
	const COLORREF offWhite = RGB(0xFF, 0xFB, 0xF0);
	const COLORREF darkGreen = RGB(0, 0x80, 0);
	const COLORREF darkBlue = RGB(0, 0, 0x80);

	// Hypertext default is used for all the document's text
	SetAStyle(SCE_H_DEFAULT, black, white, 11, "Times New Roman");
	
	// Unknown tags and attributes are highlighed in red. 
	// If a tag is actually OK, it should be added in lower case to the htmlKeyWords string.
	SetAStyle(SCE_H_TAG, darkBlue);
	SetAStyle(SCE_H_TAGUNKNOWN, red);
	SetAStyle(SCE_H_ATTRIBUTE, darkBlue);
	SetAStyle(SCE_H_ATTRIBUTEUNKNOWN, red);
	SetAStyle(SCE_H_NUMBER, RGB(0x80,0,0x80));
	SetAStyle(SCE_H_DOUBLESTRING, RGB(0,0x80,0));
	SetAStyle(SCE_H_SINGLESTRING, RGB(0,0x80,0));
	SetAStyle(SCE_H_OTHER, RGB(0x80,0,0x80));
	SetAStyle(SCE_H_COMMENT, RGB(0x80,0x80,0));
	SetAStyle(SCE_H_ENTITY, RGB(0x80,0,0x80));

	SetAStyle(SCE_H_TAGEND, darkBlue);
	SetAStyle(SCE_H_XMLSTART, darkBlue);	// <?
	SetAStyle(SCE_H_XMLEND, darkBlue);		// ?>
	SetAStyle(SCE_H_SCRIPT, darkBlue);		// <script
	SetAStyle(SCE_H_ASP, RGB(0x4F, 0x4F, 0), RGB(0xFF, 0xFF, 0));	// <% ... %>
	SetAStyle(SCE_H_ASPAT, RGB(0x4F, 0x4F, 0), RGB(0xFF, 0xFF, 0));	// <%@ ... %>

	SetAStyle(SCE_HB_DEFAULT, black);
	SetAStyle(SCE_HB_COMMENTLINE, darkGreen);
	SetAStyle(SCE_HB_NUMBER, RGB(0,0x80,0x80));
	SetAStyle(SCE_HB_WORD, darkBlue);
	SendEditor(SCI_STYLESETBOLD, SCE_HB_WORD, 1);
	SetAStyle(SCE_HB_STRING, RGB(0x80,0,0x80));
	SetAStyle(SCE_HB_IDENTIFIER, black);
	
	// This light blue is found in the windows system palette so is safe to use even in 256 colour modes.
	const COLORREF lightBlue = RGB(0xA6, 0xCA, 0xF0);
	// Show the whole section of VBScript with light blue background
	for (int bstyle=SCE_HB_DEFAULT; bstyle<=SCE_HB_STRINGEOL; bstyle++) {
		SendEditor(SCI_STYLESETFONT, bstyle, 
			reinterpret_cast<LPARAM>("Georgia"));
		SendEditor(SCI_STYLESETBACK, bstyle, lightBlue);
		// This call extends the backround colour of the last style on the line to the edge of the window
		SendEditor(SCI_STYLESETEOLFILLED, bstyle, 1);
	}
	SendEditor(SCI_STYLESETBACK, SCE_HB_STRINGEOL, RGB(0x7F,0x7F,0xFF));
	SendEditor(SCI_STYLESETFONT, SCE_HB_COMMENTLINE, 
		reinterpret_cast<LPARAM>("Comic Sans MS"));

	SetAStyle(SCE_HBA_DEFAULT, black);
	SetAStyle(SCE_HBA_COMMENTLINE, darkGreen);
	SetAStyle(SCE_HBA_NUMBER, RGB(0,0x80,0x80));
	SetAStyle(SCE_HBA_WORD, darkBlue);
	SendEditor(SCI_STYLESETBOLD, SCE_HBA_WORD, 1);
	SetAStyle(SCE_HBA_STRING, RGB(0x80,0,0x80));
	SetAStyle(SCE_HBA_IDENTIFIER, black);
	
	// Show the whole section of ASP VBScript with bright yellow background
	for (int bastyle=SCE_HBA_DEFAULT; bastyle<=SCE_HBA_STRINGEOL; bastyle++) {
		SendEditor(SCI_STYLESETFONT, bastyle, 
			reinterpret_cast<LPARAM>("Georgia"));
		SendEditor(SCI_STYLESETBACK, bastyle, RGB(0xFF, 0xFF, 0));
		// This call extends the backround colour of the last style on the line to the edge of the window
		SendEditor(SCI_STYLESETEOLFILLED, bastyle, 1);
	}
	SendEditor(SCI_STYLESETBACK, SCE_HBA_STRINGEOL, RGB(0xCF,0xCF,0x7F));
	SendEditor(SCI_STYLESETFONT, SCE_HBA_COMMENTLINE, 
		reinterpret_cast<LPARAM>("Comic Sans MS"));
		
	// If there is no need to support embedded Javascript, the following code can be dropped.
	// Javascript will still be correctly processed but will be displayed in just the default style.
	
	SetAStyle(SCE_HJ_START, RGB(0x80,0x80,0));
	SetAStyle(SCE_HJ_DEFAULT, black);
	SetAStyle(SCE_HJ_COMMENT, darkGreen);
	SetAStyle(SCE_HJ_COMMENTLINE, darkGreen);
	SetAStyle(SCE_HJ_COMMENTDOC, darkGreen);
	SetAStyle(SCE_HJ_NUMBER, RGB(0,0x80,0x80));
	SetAStyle(SCE_HJ_WORD, black);
	SetAStyle(SCE_HJ_KEYWORD, darkBlue);
	SetAStyle(SCE_HJ_DOUBLESTRING, RGB(0x80,0,0x80));
	SetAStyle(SCE_HJ_SINGLESTRING, RGB(0x80,0,0x80));
	SetAStyle(SCE_HJ_SYMBOLS, black);

	SetAStyle(SCE_HJA_START, RGB(0x80,0x80,0));
	SetAStyle(SCE_HJA_DEFAULT, black);
	SetAStyle(SCE_HJA_COMMENT, darkGreen);
	SetAStyle(SCE_HJA_COMMENTLINE, darkGreen);
	SetAStyle(SCE_HJA_COMMENTDOC, darkGreen);
	SetAStyle(SCE_HJA_NUMBER, RGB(0,0x80,0x80));
	SetAStyle(SCE_HJA_WORD, black);
	SetAStyle(SCE_HJA_KEYWORD, darkBlue);
	SetAStyle(SCE_HJA_DOUBLESTRING, RGB(0x80,0,0x80));
	SetAStyle(SCE_HJA_SINGLESTRING, RGB(0x80,0,0x80));
	SetAStyle(SCE_HJA_SYMBOLS, black);

	// Show the whole section of Javascript with off white background
	for (int jstyle=SCE_HJ_DEFAULT; jstyle<=SCE_HJ_SYMBOLS; jstyle++) {
		SendEditor(SCI_STYLESETFONT, jstyle, 
			reinterpret_cast<LPARAM>("Lucida Sans Unicode"));
		SendEditor(SCI_STYLESETBACK, jstyle, offWhite);
		SendEditor(SCI_STYLESETEOLFILLED, jstyle, 1);
	}
	SendEditor(SCI_STYLESETBACK, SCE_HJ_STRINGEOL, RGB(0xDF, 0xDF, 0x7F));
	SendEditor(SCI_STYLESETEOLFILLED, SCE_HJ_STRINGEOL, 1);

	// Show the whole section of Javascript with brown background
	for (int jastyle=SCE_HJA_DEFAULT; jastyle<=SCE_HJA_SYMBOLS; jastyle++) {
		SendEditor(SCI_STYLESETFONT, jastyle, 
			reinterpret_cast<LPARAM>("Lucida Sans Unicode"));
		SendEditor(SCI_STYLESETBACK, jastyle, RGB(0xDF, 0xDF, 0x7F));
		SendEditor(SCI_STYLESETEOLFILLED, jastyle, 1);
	}
	SendEditor(SCI_STYLESETBACK, SCE_HJA_STRINGEOL, RGB(0x0,0xAF,0x5F));
	SendEditor(SCI_STYLESETEOLFILLED, SCE_HJA_STRINGEOL, 1);
}

LRESULT CALLBACK WndProc(HWND hWnd, UINT iMessage, WPARAM wParam, LPARAM lParam) {
	switch (iMessage) {
	case WM_CREATE:
		app.wEditor = ::CreateWindow(
		              "Scintilla",
		              "Source",
		              WS_CHILD | WS_VSCROLL | WS_HSCROLL | WS_CLIPCHILDREN,
		              0, 0,
		              100, 100,
		              hWnd,
		              0,
		              app.hInstance,
		              0);
		app.InitialiseEditor();
		::ShowWindow(app.wEditor, SW_SHOW);
		::SetFocus(app.wEditor);
		return 0;

	case WM_SIZE:
		if (wParam != 1) {
			RECT rc;
			::GetClientRect(hWnd, &rc);
			::SetWindowPos(app.wEditor, 0, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, 0);
		}
		return 0;

	case WM_COMMAND:
		app.Command(LOWORD(wParam));
		app.CheckMenus();
		return 0;

	case WM_NOTIFY:
		app.Notify(reinterpret_cast<SCNotification *>(lParam));
		return 0;

	case WM_MENUSELECT:
		app.CheckMenus();
		return 0;

	case WM_CLOSE:
		if (app.SaveIfUnsure() != IDCANCEL) {
			::DestroyWindow(app.wEditor);
			::PostQuitMessage(0);
		}
		return 0;

	default:
		return DefWindowProc(hWnd, iMessage, wParam, lParam);
	}
}

static void RegisterWindowClass() {
	const char resourceName[] = "DMApp";

	WNDCLASS wndclass;
	wndclass.style = CS_HREDRAW | CS_VREDRAW;
	wndclass.lpfnWndProc = WndProc;
	wndclass.cbClsExtra = 0;
	wndclass.cbWndExtra = 0;
	wndclass.hInstance = app.hInstance;
	wndclass.hIcon = 0;
	wndclass.hCursor = NULL;
	wndclass.hbrBackground = NULL;
	wndclass.lpszMenuName = resourceName;
	wndclass.lpszClassName = className;

	if (!::RegisterClass(&wndclass))
		::exit(FALSE);
}

int PASCAL WinMain(HINSTANCE hInstance, HINSTANCE, LPSTR lpszCmdLine, int nCmdShow) {

	app.hInstance = hInstance;

	HACCEL hAccTable = LoadAccelerators(hInstance, "DMApp");

	//::LoadLibrary("Scintilla.DLL");
	//::LoadLibrary("SciLexer.DLL");
	Scintilla_RegisterClasses(hInstance);

	RegisterWindowClass();

	app.wMain = ::CreateWindowEx(
	             WS_EX_CLIENTEDGE,
	             className,
	             "Demonstration",
	             WS_CAPTION | WS_SYSMENU | WS_THICKFRAME |
	             WS_MINIMIZEBOX | WS_MAXIMIZEBOX |
	             WS_MAXIMIZE | WS_CLIPCHILDREN,
				 CW_USEDEFAULT, CW_USEDEFAULT, 
				 CW_USEDEFAULT, CW_USEDEFAULT, 
	             NULL,
	             NULL,
	             app.hInstance,
	             0);

	app.SetTitle();
	::ShowWindow(app.wMain, nCmdShow);

	bool going = true;
	MSG msg;
	msg.wParam = 0;
	while (going) {
		going = GetMessage(&msg, NULL, 0, 0);
		if (app.currentDialog && going) {
			if (!IsDialogMessage(app.currentDialog, &msg)) {
				if (TranslateAccelerator(msg.hwnd, hAccTable, &msg) == 0) {
					TranslateMessage(&msg);
					DispatchMessage(&msg);
				}
			}
		} else if (going) {
			if (TranslateAccelerator(app.wMain, hAccTable, &msg) == 0) {
				TranslateMessage(&msg);
				DispatchMessage(&msg);
			}
		}
	}

	return msg.wParam;
}
