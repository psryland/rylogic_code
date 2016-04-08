//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include <string>
#include <iostream>
#include <fstream>
#include <cassert>

#include "pr/linedrawer/ldr_script_editor_dlg.h"
#include "pr/common/min_max_fix.h"
#include "pr/gui/wingui.h"
#include "pr/gui/scintilla_ctrl.h"
#include "pr/win32/win32.h"

using namespace pr::gui;

namespace pr
{
	namespace ldr
	{
		struct ScriptEditorDlgImpl :Form ,ScriptEditorDlg
		{
			enum :DWORD { DefaultStyle = DefaultFormStyle & ~WS_VISIBLE }; //DS_CENTER|WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX|WS_SYSMENU }; // Not WS_VISIBLE
			enum :DWORD { DefaultStyleEx = DefaultFormStyleEx };
			enum
			{
				IDC_TEXT = 1000, IDC_BTN_RENDER, IDC_BTN_CLOSE,
				ID_LOAD, ID_SAVE, ID_CLOSE,
				ID_UNDO, ID_REDO, ID_CUT, ID_COPY, ID_PASTE,
			};
			static Params Params(HWND parent)
			{
				return FormParams<>().wndclass(RegisterWndClass<ScriptEditorDlgImpl>())
					.name("ldr-script-editor").title(L"Script Editor").wh(430, 380)
					.menu({{L"&File", ID_UNUSED}})
					.icon_bg((HICON)::SendMessageW(parent, WM_GETICON, ICON_BIG, 0))
					.icon_sm((HICON)::SendMessageW(parent, WM_GETICON, ICON_SMALL, 0))
					.parent(parent).hide_on_close(true).pin_window(true);
			}

			ScintillaCtrl m_edit;
			Button m_btn_render;
			Button m_btn_close;
			RenderCB m_render;

			// This code expects the scintilla.dll to be loaded already
			ScriptEditorDlgImpl(HWND parent, RenderCB render_cb)
				:Form(Params(parent))
				,ScriptEditorDlg(Internal())
				,m_edit      (ScintillaCtrl::Params<>().parent(this_).id(IDC_TEXT      ).name("m_edit").wh(Fill,Fill).margin(8,8,8,46).anchor(EAnchor::All))
				,m_btn_render(Button       ::Params<>().parent(this_).id(IDC_BTN_RENDER).name("m_btn_render").xy(12, -12).text(L"&Render (F5)").anchor(EAnchor::BottomLeft))
				,m_btn_close (Button       ::Params<>().parent(this_).id(IDC_BTN_CLOSE ).name("m_btn_close" ).xy(-12, -12).text(L"&Close").anchor(EAnchor::BottomRight))
				,m_render(render_cb)
			{
				// Set up the menu
				auto menu_file = Menu(Menu::EKind::Popup, {{L"&Load", ID_LOAD}, {L"&Save", ID_SAVE}, {MenuItem::Separator}, {L"&Close", IDCANCEL}}, false);
				MenuStrip().Set(L"&File", menu_file);

				// Initialise the edit control
				// Note: don't grab input focus until the editor is actually visible
				m_edit.InitLdrStyle();
				m_edit.SetSel(-1, 0);
				m_edit.Key += [this](Control&, KeyEventArgs const& args)
					{
						if (!args.m_down) return;
						if (args.m_vk_key == VK_F5)
							m_btn_render.OnClick();
					};

				// Hook up button handlers
				m_btn_render.Click += [this](Button&, EmptyArgs const&)
					{
						if (!m_render) return;
						auto text = m_edit.Text();
						m_render(pr::Widen(text));
					};
				m_btn_close.Click += [this](Button&, EmptyArgs const&)
					{
						Close(EDialogResult::Close);
					};

				m_btn_render.Visible(m_render != nullptr);
			}

			// Message handler
			bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_COMMAND:
					#pragma region
					{
						auto id = LoWord(wparam);
						switch (id)
						{
						case IDCANCEL:
							{
								Form::Close(EDialogResult::Cancel);
								return true;
							}
						case ID_LOAD:
							{
								COMDLG_FILTERSPEC filters[] = {{L"Ldr Script (*.ldr)",L"*.ldr"}};//,{L"All Files",L"*.*"}};
								auto filename = OpenFileUI(nullptr, FileUIOptions(L"ldr", &filters[0], _countof(filters)));
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
								COMDLG_FILTERSPEC filters[] = {{L"Ldr Script (*.ldr)",L"*.ldr"}};//,{L"All Files",L"*.*"}};
								auto filename = SaveFileUI(m_hwnd, FileUIOptions(L"ldr", &filters[0], _countof(filters)));
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
				}
				return Form::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
			}

			// Implicit HWND conversion
			operator HWND() const override
			{
				return *this;
			}

			// Hide the window instead of closing
			bool HideOnClose() const override
			{
				return Form::HideOnClose();
			}
			void HideOnClose(bool enable) override
			{
				Form::HideOnClose(enable);
			}

			// Show the window as a non-modal window
			void Show(HWND parent) override
			{
				Parent(parent);
				Form::Show(SW_SHOW);
			}

			// Show the window as a modal dialog
			INT_PTR ShowDialog(HWND parent) override
			{
				return INT_PTR(Form::ShowDialog(parent));
			}

			// Position the window relative to the owner window
			void PositionWindow(int x, int y, int w, int h) override
			{
				AutoSizePosition(x, y, w, h, m_parent);
				PositionWindow(x, y, w, h);
			}

			// Get/Set the visibility of the window
			bool Visible() const override
			{
				return Form::Visible();
			}
			void Visible(bool show) override
			{
				Form::Visible(show);
			}

			// Get/Set the text in the dialog
			std::wstring Text() const override
			{
				return pr::Widen(m_edit.Text());
			}
			void Text(wchar_t const* text) override
			{
				m_edit.Text(pr::Narrow(text).c_str());
			}

			// Get/Set the script render callback function
			RenderCB Render() const override
			{
				return m_render;
			}
			void Render(RenderCB cb)
			{
				m_render = cb;
				m_btn_render.Visible(m_render != nullptr);
			}
		};

		ScriptEditorDlg::ScriptEditorDlg(HWND parent, RenderCB render_cb)
			:m_dlg(new ScriptEditorDlgImpl(parent, render_cb))
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