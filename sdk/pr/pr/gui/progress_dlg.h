//***************************************************************************************************
// Progress Dialog
//  Copyright © Rylogic Ltd 2014
//***************************************************************************************************
// Self contained progress dialog and background thread
// No resource files, etc, needed. See unittests for usage.

#pragma once
#ifndef PR_GUI_PROGRESS_DLG_H
#define PR_GUI_PROGRESS_DLG_H

#if _WIN32_WINNT < 0x0501
#error "Requires _WIN32_WINNT >= 0x0501"
#endif

#include <string>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <chrono>
#include <cassert>
#include <algorithm>

using std::max;
using std::min;

#include <atlbase.h>
#include <atlapp.h>
#include <atlctrls.h>
#include <atlframe.h>
#include <atlmisc.h>
#include <atlcrack.h>
#include <atldlgs.h>

namespace pr
{
	namespace gui
	{
		class ProgressDlg :public CIndirectDialogImpl<ProgressDlg>
		{
			typedef CIndirectDialogImpl<ProgressDlg> DlgBase;
			typedef std::unique_lock<std::mutex> Lock;
			struct State
			{
				std::string  m_title;  // The title bar text
				std::string  m_desc;   // The description text
				float        m_pc;     // Percent complete
				bool         m_has_title;
				bool         m_has_desc;

				State(char const* title = nullptr, char const* desc = nullptr, float pc = 0.0f)
					:m_title(title ? title : "")
					,m_desc(desc ? desc : "")
					,m_pc(pc)
					,m_has_title(title != nullptr)
					,m_has_desc(desc != nullptr)
				{}
				State& operator = (State const& rhs)
				{
					if (rhs.m_has_title) m_title = rhs.m_title;
					if (rhs.m_has_desc ) m_desc  = rhs.m_desc;
					m_pc = rhs.m_pc;
					return *this;
				}
			};
			enum { IDC_TEXT_DESC=1000, IDC_PROGRESS_BAR, WM_PROGRESS_UPDATE=WM_USER+1, DefW=480, DefH=180};

			State                    m_state;     // Progress dlg UI state
			bool                     m_done;      // Task complete flag
			bool                     m_cancel;    // Cancel signalled flag
			int                      m_result;    // Dialog result to return
			std::exception           m_exception; // An exception throw by the task
			bool                     m_shown;     // True when the dlg is visible
			WTL::CStatic             m_lbl_desc;  // The progress description
			WTL::CProgressBarCtrl    m_bar;       // The progress bar
			WTL::CButton             m_btn;       // The cancel button
			std::mutex               m_mutex;     // Sync
			std::condition_variable  m_cv;        // Sync for flags
			std::thread              m_worker;    // The worker thread

			// Hide the standard 'DoModal()' from the interface
			INT_PTR DoModal(HWND, LPARAM) {}

			// Handlers
			BOOL OnInitDialog(CWindow, LPARAM)
			{
				SetWindowTextA(m_state.m_title.c_str());

				m_lbl_desc.Attach(GetDlgItem(IDC_TEXT_DESC));
				m_bar     .Attach(GetDlgItem(IDC_PROGRESS_BAR));
				m_btn     .Attach(GetDlgItem(IDCANCEL));

				{// Flag as shown
					Lock lock(m_mutex);
					m_shown = true;
					m_cv.notify_all();
				}

				// Start a timer to check for the thread being complete
				SetTimer(1, 100, nullptr);
				return TRUE;
			}

			// Window destroyed
			void OnDestroy()
			{
				m_worker.join(); // Don't close the window until the task has exited
			}

			// Resize the dialog
			void OnSize(UINT nType, CSize)
			{
				if (nType == SIZE_MINIMIZED) return;

				const int btn_w = 80, btn_h = 24, prog_h = 18, bp = (btn_h-prog_h)/2, sp = 2, bdr = 5;
				auto Clamp = [](CRect& rect)
					{
						if (rect.right  < rect.left) rect.right  = rect.left;
						if (rect.bottom < rect.top ) rect.bottom = rect.top;
						return rect;
					};

				CRect r, client;
				GetClientRect(&client);
				client.DeflateRect(bdr,bdr,bdr,bdr);

				r = client;
				r.top += bdr;
				r.left += bdr;
				r.right -= bdr;
				r.bottom -= (btn_h + sp);
				m_lbl_desc.MoveWindow(Clamp(r));

				r = client;
				r.bottom -= bp;
				r.top = r.bottom - prog_h;
				r.right -= btn_w + sp;
				m_bar.MoveWindow(Clamp(r));

				r = client;
				r.top = r.bottom - btn_h;
				r.left = r.right - btn_w;
				m_btn.MoveWindow(Clamp(r));
			}

			// Check for the worker thread being complete
			void OnTimer(UINT_PTR)
			{
				Lock lock(m_mutex);
				if (m_done)
					PostMessageA(WM_CLOSE);
				else
					SetTimer(1, 100, nullptr);
			}

			// Update the UI to the latest state
			LRESULT OnProgressUpdate(UINT uMsg, WPARAM, LPARAM)
			{
				if (uMsg != WM_PROGRESS_UPDATE) { SetMsgHandled(FALSE); return S_OK; }
				Lock lock(m_mutex);

				// The window is initially created "hidden" (actually zero sized)
				// On the first call to update, make the window the correct size and style
				CRect rect;
				GetWindowRect(&rect);
				if (rect.Width() * rect.Height() == 0)
				{
					SetWindowLongPtrA(GWL_STYLE, WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_SYSMENU|WS_VISIBLE);
					MoveWindow(rect.left, rect.top, DefW, DefH);
					CenterWindow(GetParent());
				}

				// Update the title
				if (m_state.m_has_title)
					SetWindowTextA(m_state.m_title.c_str());

				// Update the description text
				if (m_state.m_has_desc)
					m_lbl_desc.SetWindowTextA(m_state.m_desc.c_str());

				// Use marquee style if pc is out of range
				LONG_PTR bar_style = m_bar.GetWindowLongPtrA(GWL_STYLE);
				if (m_state.m_pc < 0 || m_state.m_pc > 1.0f)
				{
					if ((bar_style & PBS_MARQUEE) == 0)
					{
						m_bar.SetWindowLongPtrA(GWL_STYLE, bar_style|PBS_MARQUEE);
						m_bar.SetMarquee(TRUE, 30);
					}
				}
				else
				{
					if ((bar_style & PBS_MARQUEE) != 0)
					{
						m_bar.SetWindowLongPtrA(GWL_STYLE, bar_style & ~PBS_MARQUEE);
						m_bar.SetMarquee(FALSE);
					}
					m_bar.SetRange32(0, 100);
					m_bar.SetPos(static_cast<int>(m_state.m_pc * 100));
				}
				return S_OK;
			}

			// Handle cancel butten pressed
			void OnCancel(UINT, int, CWindow)
			{
				Lock lock(m_mutex);
				m_cancel = true;
				m_cv.notify_all();
			}

			// Handle closing the dialog
			void OnDone()
			{
				EndDialog(IDCLOSE);
			}

		public:

			//// Callback function passed to task function to update progress
			//typedef bool (__cdecl *UpdateProgressCB)(float pc, char const* desc);

			// Optional event handler
			struct IEvents
			{
				// Called when 'Cancel' is pressed. Return true to confirm the cancel
				virtual bool ProgressDlg_OnCancelled() = 0;
				virtual ~IEvents() {}
			} *OnEvent;

			template <typename Func, typename... Args>
			ProgressDlg(char const* title, char const* desc, Func&& func, Args&&... args)
				:m_state(title, desc, 0.0f)
				,m_done(false)
				,m_cancel(false)
				,m_result(IDOK)
				,m_exception()
				,m_lbl_desc()
				,m_bar()
				,m_btn()
				,m_mutex()
				,m_cv()
				,m_worker()
			//	,OnEvent(handler)
			{
				// Start the worker
				m_worker = std::thread([&]
				{
					try
					{
						// Run the task
						// Pass the dialog to the task function so that it can update progress.
						// Pased as a pointer so that users have the option of passing nullptr
						std::bind(std::forward<Func>(func), this, std::forward<Args>(args)...)();

						// Notify task complete
						Lock lock(m_mutex);
						m_done = true;
						m_result = m_cancel ? IDCANCEL : IDOK;
						m_cv.notify_all();
					}
					catch (std::exception const& ex)
					{
						Lock lock(m_mutex);
						m_done = true;
						m_result = IDABORT;
						m_exception = ex;
						m_cv.notify_all();
					}
					catch (...)
					{
						assert(false && "Unhandled exception in task");
					}
					Progress(1.0f);
				});
			}

			// Execute a work function in a different thread while displaying the modal dialog
			// Returns IDOK if the dialog completed, IDCANCEL if the operation was cancelled
			INT_PTR DoModal(int delay_ms = 0, HWND hWndParent = ::GetActiveWindow())
			{
				// Wait for up to 'delay_ms' in case no dialog is needed
				auto done = false;
				{
					Lock lock(m_mutex);
					done = m_cv.wait_for(lock, std::chrono::milliseconds(delay_ms), [&]{ return m_done; });
				}

				// If not done yet, show the dialog
				if (!done)
					DlgBase::DoModal(hWndParent);

				// Ensure the thread has ended
				if (m_worker.joinable())
					m_worker.join();

				// Return the result
				if (m_result == IDABORT)
					throw m_exception;

				return m_result;
			}

			// Called by the worker thread to update the UI
			bool Progress(float pc = -1.0f, char const* desc = nullptr, char const* title = nullptr)
			{
				Lock lock(m_mutex);
				m_state = State(title, desc, pc);

				if (IsWindow())
					PostMessageA(WM_PROGRESS_UPDATE);

				return !m_cancel;
			}

			BEGIN_DIALOG_EX(0,0,0,0,0)
				DIALOG_STYLE(WS_POPUP)
				DIALOG_FONT(8, TEXT("MS Shell Dlg"))
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_LTEXT("Processing...", IDC_TEXT_DESC, 0, 0, 0, 0, SS_LEFT, 0)
				CONTROL_CONTROL("", IDC_PROGRESS_BAR, PROGRESS_CLASS, 0, 0, 0, 0, 0, 0)
				CONTROL_DEFPUSHBUTTON("Cancel", IDCANCEL, 0, 0, 0, 0, 0, 0)
			END_CONTROLS_MAP()
			BEGIN_MSG_MAP(ProgressDlg)
				MSG_WM_INITDIALOG(OnInitDialog)
				MSG_WM_DESTROY(OnDestroy)
				MSG_WM_SIZE(OnSize)
				MSG_WM_CLOSE(OnDone)
				MSG_WM_TIMER(OnTimer)
				MESSAGE_HANDLER_EX(WM_PROGRESS_UPDATE, OnProgressUpdate)
				COMMAND_ID_HANDLER_EX(IDCANCEL ,OnCancel)
			END_MSG_MAP()
		};
	}
}

// Add a manifest dependency on common controls version 6
#if defined _M_IX86
  #pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
  #pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
  #pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
  #pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/fmt.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_gui_progress_dlg)
		{
			using namespace pr::gui;
			int arg = 42;

			ProgressDlg dlg("Progressing...", "This is a progress dialog",
				[](ProgressDlg* dlg, int a)
				{
					for (int i = 0; i != a; ++i)
					{
						if (!dlg->Progress((i + 1.0f) / (float)a, pr::FmtS("Processing index %d", i), nullptr))
							return;
						Sleep(50);
					}
				}, arg);

			auto r = dlg.DoModal(0);
			PR_CHECK(r == IDOK || r == IDCANCEL, true);
		}
	}
}
#endif

#endif
