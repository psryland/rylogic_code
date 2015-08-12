//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <iostream>
#include <fstream>
#include <cassert>

#include "pr/common/min_max_fix.h"
#include "pr/gui/wingui.h"
#include "pr/gui/scintilla_ctrl.h"
#include "pr/linedrawer/ldr_script_editor_dlg.h"
#include "pr/win32/win32.h"

using namespace pr::gui;

namespace pr
{
	namespace ldr
	{
		struct ScriptEditorDlgImpl :Form ,IScriptEditorDlg
		{
			using base = Form;

			ScintillaCtrl m_edit;
			Button m_btn_render;
			Button m_btn_close;
			MenuStrip m_menu;
			RenderCB& Render;
			//WTL::CAccelerator m_accel;

			enum :DWORD { DefaultStyle   = DefaultFormStyle & ~WS_VISIBLE }; //DS_CENTER|WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX|WS_SYSMENU }; // Not WS_VISIBLE
			enum :DWORD { DefaultStyleEx = DefaultFormStyleEx };
			enum
			{
				IDC_TEXT = 1000, IDC_BTN_RENDER, IDC_BTN_CLOSE,
				ID_LOAD, ID_SAVE, ID_CLOSE,
				ID_UNDO, ID_REDO, ID_CUT, ID_COPY, ID_PASTE,
			};

			// Construct the dialog template for this dialog
			static DlgTemplate Templ()
			{
				// Ensure the scintilla control is registered
				pr::win32::LoadDll<struct Scintilla>(L"scintilla.dll", L".\\lib\\$(platform)");

				int const menu_height = 10;//::GetSystemMetrics(SM_CYMENU);
				DlgTemplate templ(L"Script Editor", CW_USEDEFAULT, CW_USEDEFAULT, 430, 380, DefaultStyle, DefaultStyleEx);
				templ.Add(IDC_TEXT, ScintillaCtrl::WndClassName(), L"", 5, 5 + menu_height, 418, 338, ScintillaCtrl::DefaultStyle, ScintillaCtrl::DefaultStyleEx);
				templ.Add(IDC_BTN_RENDER, Button::WndClassName(), L"&Render", 320, 348 + menu_height, 50, 14, Button::DefaultStyleDefBtn, Button::DefaultStyleEx);
				templ.Add(IDC_BTN_CLOSE, Button::WndClassName(), L"&Close", 375, 348 + menu_height, 50, 14, Button::DefaultStyle, Button::DefaultStyleEx);
				return std::move(templ);
			}

			ScriptEditorDlgImpl(RenderCB& render_cb)
				:base(Templ(), "Ldr Script Editor")
				,m_edit(IDC_TEXT, "m_edit", this, EAnchor::All)
				,m_btn_render(IDC_BTN_RENDER, "m_btn_render", this, EAnchor::BottomRight)
				,m_btn_close(IDC_BTN_CLOSE, "m_btn_close", this, EAnchor::BottomRight)
				//,m_accel()
				,m_menu(MenuStrip::Strip)
				,Render(render_cb)
			{}

			// Message handler
			bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_INITDIALOG:
					#pragma region
					{
						// Attach all the child controls
						base::ProcessWindowMessage(hwnd, message, wparam, lparam, result);

						// Create the menu
						auto menu_file = MenuStrip(MenuStrip::Popup);
						menu_file.Insert(L"&Load", ID_LOAD);
						menu_file.Insert(L"&Save", ID_SAVE);
						menu_file.Insert(MenuStrip::Separator);
						menu_file.Insert(L"&Close", ID_CLOSE);
						m_menu.Insert(menu_file, L"&File");
						Menu(m_menu);

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
						m_edit.InitLdrStyle();
						m_edit.SetSel(-1, 0);
						m_edit.Focus(true);

						// Hook up button handlers
						m_btn_render.Click += [&](Button&, EmptyArgs const&)
							{
								if (!Render) return;
								auto text = m_edit.Text();
								Render(pr::Widen(text));
							};
						m_btn_close.Click += [&](Button&, EmptyArgs const&)
							{
								Close(EDialogResult::Close);
							};

						return false;
					}
					#pragma endregion
				case WM_COMMAND:
					#pragma region
					{
						auto id = LoWord(wparam);
						switch (id)
						{
						case IDCANCEL:
							{
								base::Close(EDialogResult::Cancel);
								return true;
							}
						case ID_LOAD:
							{
								COMDLG_FILTERSPEC filters[] = {{L"Ldr Script (*.ldr)",L"*.ldr"},{L"All Files",L"*.*"}};
								auto filename = OpenFileUI(m_hwnd, FileUIOptions(L"ldr", _countof(filters), &filters[0]));
								if (!filename.empty())
								{
									std::ifstream infile(filename[0]);
									if (!infile) MessageBoxW(m_hwnd, L"Failed to open file", L"Load Failed", MB_OK|MB_ICONERROR);
									else m_edit.Load(infile);
								}
								return true;
							}
						case ID_SAVE:
							{
								COMDLG_FILTERSPEC filters[] = {{L"Ldr Script (*.ldr)",L"*.ldr"},{L"All Files",L"*.*"}};
								auto filename = SaveFileUI(m_hwnd, FileUIOptions(L"ldr", _countof(filters), &filters[0]));
								if (!filename.empty())
								{
									std::ofstream outfile(filename);
									if (!outfile) MessageBoxW(m_hwnd, L"Failed to open file for writing", L"Save Failed", MB_OK|MB_ICONERROR);
									else m_edit.Save(outfile);
								}
								return true;
							}
						}
						break;
					}
					#pragma endregion
				case WM_PAINT:
					#pragma region
					{
						m_btn_render.Visible(Render != nullptr);
						break;
					}
					#pragma endregion
				}
				return base::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
			}

			// Create the non-modal window
			HWND Create(HWND parent = nullptr) override
			{
				auto p = WndRef::Lookup(parent);
				base::Create(p);
				return m_hwnd;
			}

			// Hide the window instead of closing
			bool HideOnClose() const override
			{
				return base::HideOnClose();
			}
			void HideOnClose(bool enable) override
			{
				base::HideOnClose(enable);
			}

			// Show the window as a non-modal window
			void Show(HWND parent) override
			{
				base::Show(SW_SHOW, parent);
			}

			// Show the window as a modal dialog
			INT_PTR ShowDialog(HWND parent) override
			{
				return INT_PTR(base::ShowDialog(parent));
			}

			// Implicit HWND conversion
			operator HWND() override { return base::operator HWND(); }

			// Get/Set the visibility of the window
			bool Visible() const override    { return base::Visible(); }
			void Visible(bool show) override { base::Visible(show); }

			// Get/Set the text in the dialog
			std::wstring Text() const override      { return pr::Widen(m_edit.Text()); }
			void Text(wchar_t const* text) override { m_edit.Text(pr::Narrow(text).c_str()); }
		};

		ScriptEditorDlg::ScriptEditorDlg()
			:m_dlg(new ScriptEditorDlgImpl(Render))
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