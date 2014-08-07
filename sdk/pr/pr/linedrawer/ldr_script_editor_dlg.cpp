//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <iostream>
#include <fstream>
#include <cassert>

#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atldlgs.h>
#include <atlframe.h>
#include <atlcrack.h>
//#include <atlctrls.h>
//#include <atlsplit.h>
//#include <atlmisc.h>

#include "pr/common/min_max_fix.h"
//#include "pr/common/assert.h"
//#include "pr/common/keystate.h"
//#include "pr/macros/enum.h"
//#include "pr/macros/count_of.h"
//#include "pr/str/prstring.h"
//#include "pr/linedrawer/ldr_objects_dlg.h"
//#include "pr/script/reader.h"
#include "pr/gui/scintilla.h"
#include "pr/linedrawer/ldr_script_editor_dlg.h"

namespace pr
{
	namespace ldr
	{
		class ScriptEditorDlgImpl
			:public CIndirectDialogImpl<ScriptEditorDlgImpl>
			,public CDialogResize<ScriptEditorDlgImpl>
			,public IScriptEditorDlg
		{
			typedef CIndirectDialogImpl<ScriptEditorDlgImpl> base;

			WTL::InitScintilla m_init_scintilla;
			WTL::ScintillaCtrl m_edit;
			WTL::CAccelerator m_accel;
			WTL::CMenu m_menu;
			RenderCB& Render;

			TCHAR const* LdrFileFilter() const
			{
				static TCHAR const filter[] = TEXT("Ldr Script (*.ldr)\0*.ldr\0All Files (*.*)\0*.*\0\0");
				return filter;
			}

			ScriptEditorDlgImpl(ScriptEditorDlgImpl const&);
			ScriptEditorDlgImpl& operator=(ScriptEditorDlgImpl const&);

		public:

			ScriptEditorDlgImpl(RenderCB& render_cb)
				:m_init_scintilla()
				,m_edit()
				,m_accel()
				,m_menu()
				,Render(render_cb)
			{}
			~ScriptEditorDlgImpl()
			{
				assert(!IsWindow() && "DestroyWindow() must be called before destruction");
			}

			// Create the non-modal dialog
			void Create(HWND parent = 0) override
			{
				if (base::Create(parent) == 0)
					throw std::exception("Failed to create script editor window");
			}

			// Close and destroy the dialog window
			void Close() override
			{
				if (!IsWindow()) return;
				DestroyWindow();
			}

			// Show the window as a non-modal window
			void Show(HWND parent = 0) override
			{
				if (!IsWindow()) Create(parent);
				Visible(true);
			}

			// Show the window as a modal dialog
			INT_PTR ShowDialog(HWND parent = 0) override
			{
				return DoModal(parent);
			}

			// Get/Set the visibility of the window
			bool Visible() const
			{
				return IsWindowVisible() != 0;
			}
			void Visible(bool show)
			{
				ShowWindow(show ? SW_SHOW : SW_HIDE);
				if (show) SetWindowPos(HWND_TOP,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE);
			}

			// Get/Set the text in the dialog
			std::string Text() const
			{
				return m_edit.Text();
			}
			void Text(char const* text)
			{
				m_edit.Text(text);
				m_edit.Invalidate();
			}

			// Handler methods
			BOOL OnInitDialog(CWindow, LPARAM)
			{
				// Create the menu
				CMenu menu_file; menu_file.CreatePopupMenu();
				menu_file.AppendMenuA(MF_STRING, ID_LOAD, "&Load");
				menu_file.AppendMenuA(MF_STRING, ID_SAVE, "&Save");
				menu_file.AppendMenuA(MF_SEPARATOR);
				menu_file.AppendMenuA(MF_STRING, ID_CLOSE, "&Close");

				m_menu.CreateMenu();
				m_menu.AppendMenuA(MF_POPUP, menu_file, "&File");
				SetMenu(m_menu);

				//// Create the keyboard accelerators
				//ACCEL table[] =
				//{
				//	{FCONTROL, 'Z', ID_UNDO},
				//	{FCONTROL, 'Y', ID_REDO},
				//	{FCONTROL, 'X', ID_CUT},
				//	{FCONTROL, 'C', ID_COPY},
				//	{FCONTROL, 'V', ID_PASTE},
				//};
				//m_accel.CreateAcceleratorTableA(table, PR_COUNTOF(table));

				// Initialise the edit control
				m_edit.Attach(GetDlgItem(IDC_TEXT));
				m_edit.InitLdrStyle();
				m_edit.CodePage(CP_UTF8);
				m_edit.Lexer(SCLEX_LDR);
				m_edit.LexerLanguage("ldr");
				m_edit.SetSel(-1, 0);
				m_edit.SetFocus();

				DlgResize_Init(true, false);
				return TRUE;
			}
			void OnCloseDialog(UINT, int, CWindow)
			{
				Visible(false);
			}
			void OnPaint(CDCHandle dc)
			{
				GetDlgItem(IDC_BTN_RENDER).ShowWindow(Render ? SW_SHOW : SW_HIDE);
				SetMsgHandled(FALSE);
			}
			void OnRender(UINT, int, CWindow)
			{
				if (Render)
					Render(Text());
			}
			void OnLoad(UINT, int, CWindow)
			{
				WTL::CFileDialog fd(TRUE,"ldr",0,6UL,LdrFileFilter(),m_hWnd);
				if (fd.DoModal() != IDOK) return;
			
				std::ifstream infile(fd.m_szFileName);
				if (!infile) MessageBox("Failed to open file", "Load Failed", MB_OK|MB_ICONERROR);
				else m_edit.Load(infile);
			}
			void OnSave(UINT, int, CWindow)
			{
				WTL::CFileDialog fd(FALSE,"ldr",0,6UL,LdrFileFilter(),m_hWnd);
				if (fd.DoModal() != IDOK) return;
			
				std::ofstream outfile(fd.m_szFileName);
				if (!outfile) MessageBox("Failed to open file for writing", "Save Failed", MB_OK|MB_ICONERROR);
				else m_edit.Save(outfile);
			}

			enum
			{
				IDC_TEXT = 1000, IDC_BTN_RENDER, IDC_BTN_CLOSE,
				ID_LOAD, ID_SAVE, ID_CLOSE,
				ID_UNDO, ID_REDO, ID_CUT, ID_COPY, ID_PASTE,
			};
		
			BEGIN_DIALOG_EX(0, 0, 430, 380, 0)
				DIALOG_STYLE(DS_CENTER|WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX|WS_SYSMENU) //|WS_VISIBLE
				DIALOG_CAPTION("Script Editor")
				DIALOG_FONT(8, TEXT("MS Shell Dlg"))
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_CONTROL(_T(""), IDC_TEXT, ScintillaCtrl::GetWndClassName(), WS_HSCROLL|WS_VSCROLL|ES_AUTOHSCROLL|ES_AUTOVSCROLL|ES_MULTILINE|ES_WANTRETURN, 5, 5, 418, 338, WS_EX_STATICEDGE)//NOT WS_BORDER|
				CONTROL_DEFPUSHBUTTON(_T("&Render"), IDC_BTN_RENDER, 320, 348, 50, 14, 0, WS_EX_LEFT)
				CONTROL_PUSHBUTTON(_T("&Close"), IDC_BTN_CLOSE, 375, 348, 50, 14, 0, WS_EX_LEFT)
			END_CONTROLS_MAP()
			BEGIN_DLGRESIZE_MAP(ScriptEditorDlgImpl)
				DLGRESIZE_CONTROL(IDC_TEXT, DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
				DLGRESIZE_CONTROL(IDC_BTN_RENDER, DLSZ_MOVE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
				DLGRESIZE_CONTROL(IDC_BTN_CLOSE, DLSZ_MOVE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
			END_DLGRESIZE_MAP()
			BEGIN_MSG_MAP(ScriptEditorDlgImpl)
				MSG_WM_INITDIALOG(OnInitDialog)
				MSG_WM_PAINT(OnPaint)
				COMMAND_ID_HANDLER_EX(IDC_BTN_RENDER , OnRender)
				COMMAND_ID_HANDLER_EX(IDC_BTN_CLOSE  , OnCloseDialog)
				COMMAND_ID_HANDLER_EX(ID_LOAD        , OnLoad)
				COMMAND_ID_HANDLER_EX(ID_SAVE        , OnSave)
				COMMAND_ID_HANDLER_EX(ID_CLOSE       , OnCloseDialog)
				COMMAND_ID_HANDLER_EX(IDCANCEL       , OnCloseDialog)
				CHAIN_MSG_MAP(CDialogResize<ScriptEditorDlgImpl>)
			END_MSG_MAP()
		};

		ScriptEditorDlg::ScriptEditorDlg()
			:m_dlg(std::make_unique<ScriptEditorDlgImpl>(Render))
		{}
	}
}

// Add a manifest dependency on common controls version 6
#if defined _M_IX86
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif