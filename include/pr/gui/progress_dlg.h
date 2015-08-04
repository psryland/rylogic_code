//***************************************************************************************************
// Progress Dialog
//  Copyright (c) Rylogic Ltd 2014
//***************************************************************************************************
// Self contained progress dialog and background thread
// No resource files, etc, needed. See unittests for usage.

#pragma once

#include <string>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <chrono>
#include <cassert>
#include <algorithm>
#include "pr/gui/wingui.h"

namespace pr
{
	namespace gui
	{
		class ProgressDlg :Form<ProgressDlg>
		{
			typedef Form<ProgressDlg> base;
			typedef std::unique_lock<std::mutex> Lock;
			struct State
			{
				std::wstring  m_title;  // The title bar text
				std::wstring  m_desc;   // The description text
				float        m_pc;     // Percent complete
				bool         m_has_title;
				bool         m_has_desc;

				State(wchar_t const* title = nullptr, wchar_t const* desc = nullptr, float pc = 0.0f)
					:m_title(title ? title : L"")
					,m_desc(desc ? desc : L"")
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

			State                   m_state;     // Progress dlg UI state
			bool                    m_done;      // Task complete flag
			bool                    m_cancel;    // Cancel signalled flag
			EDialogResult           m_result;    // Dialog result to return
			std::exception_ptr      m_exception; // An exception thrown by the task
			bool                    m_shown;     // True when the dlg is visible
			Label                   m_lbl_desc;  // The progress description
			ProgressBar             m_bar;       // The progress bar
			Button                  m_btn;       // The cancel button
			std::mutex              m_mutex;     // Sync
			std::condition_variable m_cv;        // Sync for flags
			std::thread             m_worker;    // The worker thread

			// A dialog layout description used for this indirect dialog
			DlgTemplate Template(wchar_t const* title, wchar_t const* desc) const
			{
				DlgTemplate templ(title, 0, 0, 240, 100, DWORD(DefaultFormStyle), DWORD(DefaultFormStyleEx));
				templ.Add(IDC_TEXT_DESC   , Label::WndClassName()      , desc      , 0, 0, 0, 0);
				templ.Add(IDC_PROGRESS_BAR, ProgressBar::WndClassName(), nullptr   , 0, 0, 0, 0);
				templ.Add(IDCANCEL        , Button::WndClassName()     , L"Cancel" , 0, 0, 0, 0, Button::DefaultStyle|BS_DEFPUSHBUTTON);
				return templ;
			}

			// Message map function
			bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
			{
				switch (message)
				{
				case WM_INITDIALOG:
					#pragma region WM_INITDIALOG
					{
					//	{// Flag as shown
					//		Lock lock(m_mutex);
					//		m_shown = true;
					//		m_cv.notify_all();
					//	}

						// Start a timer to check for the thread being complete
						SetTimer(m_hwnd, 1, 100, nullptr);
						break;
					}
					#pragma endregion
				case WM_DESTROY:
					#pragma region WM_DESTROY
					{
						if (m_worker.joinable())
							m_worker.join(); // Don't close the window until the task has exited
						break;
					}
					#pragma endregion
				case WM_WINDOWPOSCHANGED:
					#pragma region WM_WINDOWPOSCHANGED
					{
						if (IsIconic(m_hwnd)) break;
						
						const int btn_w = 80, btn_h = 24, prog_h = 18, bp = (btn_h-prog_h)/2, sp = 2, bdr = 5;
						auto Clamp = [](Rect& rect)
							{
								if (rect.right  < rect.left) rect.right  = rect.left;
								if (rect.bottom < rect.top ) rect.bottom = rect.top;
								return rect;
							};

						auto client = ClientRect().Adjust(bdr,bdr,-bdr,-bdr);
						Rect r;
						r = client;
						r.top += bdr;
						r.left += bdr;
						r.right -= bdr;
						r.bottom -= (btn_h + sp);
						m_lbl_desc.ParentRect(Clamp(r));

						r = client;
						r.bottom -= bp;
						r.top = r.bottom - prog_h;
						r.right -= btn_w + sp;
						m_bar.ParentRect(Clamp(r));

						r = client;
						r.top = r.bottom - btn_h;
						r.left = r.right - btn_w;
						m_btn.ParentRect(Clamp(r));

						::UpdateWindow(m_hwnd);
						break;
					}
					#pragma endregion
				case WM_PROGRESS_UPDATE:
					#pragma region WM_PROGRESS_UPDATE
					{
						Lock lock(m_mutex);

						// The window is initially created "hidden" (actually zero sized)
						// On the first call to update, make the window the correct size and style
						auto rect = ScreenRect();
						if (rect.width() * rect.height() == 0)
						{
							::SetWindowLongPtr(m_hwnd, GWL_STYLE, WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_SYSMENU|WS_VISIBLE);
							::MoveWindow(m_hwnd, rect.left, rect.top, DefW, DefH, TRUE);
							CenterWindow(GetParent(m_hwnd));
						}

						// Update the title
						if (m_state.m_has_title)
							::SetWindowTextW(m_hwnd, m_state.m_title.c_str());

						// Update the description text
						if (m_state.m_has_desc)
							::SetWindowTextW(m_lbl_desc, m_state.m_desc.c_str());

						// Use marquee style if pc is out of range
						auto bar_style = ::GetWindowLongPtr(m_bar, GWL_STYLE);
						if (m_state.m_pc < 0 || m_state.m_pc > 1.0f)
						{
							if ((bar_style & PBS_MARQUEE) == 0)
							{
								::SetWindowLongPtr(m_bar, GWL_STYLE, bar_style|PBS_MARQUEE);
								m_bar.Marquee(true, 30);
							}
						}
						else
						{
							if ((bar_style & PBS_MARQUEE) != 0)
							{
								::SetWindowLongPtr(m_bar, GWL_STYLE, bar_style & ~PBS_MARQUEE);
								m_bar.Marquee(false);
							}
							m_bar.Range(0, 100);
							m_bar.Pos(static_cast<int>(m_state.m_pc * 100));
						}
						break;
					}
					#pragma endregion
				case WM_TIMER:
					#pragma region WM_TIMER
					{
						Lock lock(m_mutex);
						if (m_done)
							::PostMessage(m_hwnd, WM_CLOSE, 0, 0L);
						else
							::SetTimer(m_hwnd, 1, 100, nullptr);
						break;
					}
					#pragma endregion
				case WM_CLOSE:
					#pragma region WM_CLOSE
					{
						Close(IDCLOSE);
						break;
					}
					#pragma endregion
				case WM_COMMAND:
					#pragma region WM_COMMAND
					{
						auto id = int(LOWORD(wparam));
						if (id == IDCANCEL)
						{
							// Query to cancel the 'cancel' button click
							CancelEventArgs args;
							OnCancel(args);
							if (!args.m_cancel) // Cancelling was not cancelled, so cancel...
							{
								Lock lock(m_mutex);
								m_cancel = true;
								m_cv.notify_all();
							}
						}
						break;
					}
					#pragma endregion
				}
				return base::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
			}

		public:

			using Form<ProgressDlg>::WndBackground;
			
			// 'func' should have 'ProgressDlg*' as the first parameter
			template <typename Func, typename... Args>
			ProgressDlg(wchar_t const* title, wchar_t const* desc, Func&& func, Args&&... args)
				:base(Template(title, desc), "progress_dlg")
				,m_state(title, desc, 0.0f)
				,m_done(false)
				,m_cancel(false)
				,m_result(EDialogResult::Ok)
				,m_exception()
				,m_shown()
				,m_lbl_desc(IDC_TEXT_DESC, "desc", this, EAnchor::TopLeft)
				,m_bar(IDC_PROGRESS_BAR, "bar", this, EAnchor::Left|EAnchor::Top|EAnchor::Right)
				,m_btn(IDCANCEL, "cancel", this, EAnchor::Bottom)
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
						// Passed as a pointer so that users have the option of passing nullptr
						std::bind(std::forward<Func>(func), this, std::forward<Args>(args)...)();

						// Notify task complete
						Lock lock(m_mutex);
						m_done = true;
						m_result = m_cancel ? EDialogResult::Cancel : EDialogResult::Ok;
						m_cv.notify_all();
					}
					catch (...)
					{
						Lock lock(m_mutex);
						m_done = true;
						m_result = EDialogResult::Abort;
						m_exception = std::current_exception();
						m_cv.notify_all();
					}
					Progress(1.0f);
				});
			}

			// Get/Set whether the form uses dialog-like message handling
			using base::DialogBehaviour;

			// Execute a work function in a different thread while displaying the modal dialog
			// Returns IDOK if the dialog completed, IDCANCEL if the operation was cancelled
			EDialogResult ShowDialog(HWND parenthwnd = ::GetActiveWindow(), int delay_ms = 0)
			{
				// Wait for up to 'delay_ms' in case no dialog is needed
				auto done = false;
				{
					Lock lock(m_mutex);
					done = m_cv.wait_for(lock, std::chrono::milliseconds(delay_ms), [&]{ return m_done; });
				}

				// If not done yet, show the dialog
				if (!done)
					base::ShowDialog(parenthwnd);

				// Ensure the thread has ended
				if (m_worker.joinable())
					m_worker.join();

				// Return the result
				if (m_result == EDialogResult::Abort)
					std::rethrow_exception(m_exception);

				return m_result;
			}

			// Called by the worker thread to update the UI
			bool Progress(float pc = -1.0f, wchar_t const* desc = nullptr, wchar_t const* title = nullptr)
			{
				Lock lock(m_mutex);
				m_state = State(title, desc, pc);

				if (IsWindow(m_hwnd))
					PostMessageW(m_hwnd, WM_PROGRESS_UPDATE, 0, 0);

				return !m_cancel;
			}

			// An event raised when the cancel button is hit
			EventHandler<ProgressDlg&, CancelEventArgs&> Cancelled;

			// Handlers
			virtual void OnCancel(CancelEventArgs& args)
			{
				Cancelled(*this, args);
			}
		};

#if 0
			//// Callback function passed to task function to update progress
			//typedef bool (__cdecl *UpdateProgressCB)(float pc, char const* desc);

			// Optional event handler
			struct IEvents
			{
				// Called when 'Cancel' is pressed. Return true to confirm the cancel
				virtual bool ProgressDlg_OnCancelled() = 0;
				virtual ~IEvents() {}
			} *OnEvent;
		};
#endif
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

