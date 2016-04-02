//***************************************************************************************************
// Progress Dialog
//  Copyright (c) Rylogic Ltd 2014
//***************************************************************************************************
// Self contained progress dialog and background thread
// No resource files, etc, needed. See unit tests for usage.

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
		class ProgressDlg : public Form
		{
			using Lock = std::unique_lock<std::timed_mutex>;
			struct State
			{
				HWND         m_hwnd;   // The hwnd of this progress window
				std::wstring m_title;  // The title bar text
				std::wstring m_desc;   // The description text
				float        m_pc;     // Percent complete
				bool         m_has_title;
				bool         m_has_desc;

				State(HWND hwnd, wchar_t const* title = nullptr, wchar_t const* desc = nullptr, float pc = 0.0f)
					:m_hwnd(hwnd)
					,m_title(title ? title : L"")
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
			enum { IDC_TEXT_DESC=1000, IDC_PROGRESS_BAR, WM_PROGRESS_UPDATE=WM_USER+1, ID_POLL_WORKER_COMPLETE=1, DefW=480, DefH=180};

			State                       m_state;     // Progress dlg UI state
			bool                        m_done;      // Task complete flag, set by the worker
			bool                        m_cancel;    // Cancel signalled flag, set by this thread
			EDialogResult               m_result;    // Dialog result to return
			std::exception_ptr          m_exception; // An exception thrown by the task
			bool                        m_shown;     // True when the dlg is visible
			Label                       m_lbl_desc;  // The progress description
			ProgressBar                 m_bar;       // The progress bar
			Button                      m_btn;       // The cancel button
			std::timed_mutex            m_mutex;     // Sync
			std::condition_variable_any m_cv;        // Sync for flags
			std::thread                 m_worker;    // The worker thread

			// A dialog layout description used for this indirect dialog
			static DlgTemplate const& Templ()
			{
				static auto templ = DlgTemplate(DlgParams().xy(0,0).wh(240,100))
					.Add(CtrlParams().id(IDC_TEXT_DESC).wndclass(Label::WndClassName()))
					.Add(CtrlParams().id(IDC_PROGRESS_BAR).wndclass(ProgressBar::WndClassName()))
					.Add(CtrlParams().id(IDCANCEL).wndclass(Button::WndClassName()).text(L"Cancel").style(Button::DefaultStyleDefBtn));
				return templ;
			}

			// Message map function
			bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_INITDIALOG:
					#pragma region
					{
						auto r = Form::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
						{
							Lock lock(m_mutex);
							m_state.m_hwnd = m_hwnd;
							Text(m_state.m_title);
							m_lbl_desc.Text(m_state.m_desc);
						}

						// Ensure the timer is running that polls for the thread being complete
						::SetTimer(m_hwnd, ID_POLL_WORKER_COMPLETE, 100, nullptr);
						return r;
					}
					#pragma endregion
				case WM_DESTROY:
					#pragma region
					{
						// In case of abnormal shutdown, don't close the window until the task has exited
						Cancel(BlockTillCancelled);
						break;
					}
					#pragma endregion
				case WM_WINDOWPOSCHANGED:
					#pragma region
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

						Invalidate();
						break;
					}
					#pragma endregion
				case WM_PROGRESS_UPDATE:
					#pragma region
					{
						// Lock so we can use m_state.
						// Holding the lock doesn't block the background thread it just
						// doesn't update m_state while it's locked
						Lock lock(m_mutex);

						// The window is initially created "hidden" (actually zero sized)
						// On the first call to update, make the window the correct size and style
						auto rect = ScreenRect();
						if (rect.width() * rect.height() == 0)
						{
							::MoveWindow(m_hwnd, rect.left, rect.top, DefW, DefH, TRUE);
							CenterWindow(GetParent(m_hwnd));
							Visible(true);
						}

						// Update the title
						if (m_state.m_has_title)
							::SetWindowTextW(m_hwnd, m_state.m_title.c_str());

						// Update the description text
						if (m_state.m_has_desc)
							::SetWindowTextW(m_lbl_desc, m_state.m_desc.c_str());

						// Use marquee style if pc is out of range
						auto bar_style = ::GetWindowLongPtrW(m_bar, GWL_STYLE);
						if (m_state.m_pc < 0 || m_state.m_pc > 1.0f)
						{
							if ((bar_style & PBS_MARQUEE) == 0)
							{
								::SetWindowLongPtrW(m_bar, GWL_STYLE, bar_style|PBS_MARQUEE);
								m_bar.Marquee(true, 30);
							}
						}
						else
						{
							if ((bar_style & PBS_MARQUEE) != 0)
							{
								::SetWindowLongPtrW(m_bar, GWL_STYLE, bar_style & ~PBS_MARQUEE);
								m_bar.Marquee(false);
							}
							m_bar.Range(0, 100);
							m_bar.Pos(static_cast<int>(m_state.m_pc * 100));
						}
						Invalidate();
						break;
					}
					#pragma endregion
				case WM_TIMER:
					#pragma region
					{
						if (wparam == ID_POLL_WORKER_COMPLETE)
						{
							Lock lock(m_mutex);
							if (m_done)
							{
								::PostMessageW(m_hwnd, WM_CLOSE, 0, 0L);
								::KillTimer(m_hwnd, ID_POLL_WORKER_COMPLETE);
							}
							return true;
						}
						break;
					}
					#pragma endregion
				}
				return Form::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
			}

		public:
			enum ECancelFlags
			{
				NonBlocking        = 0,
				BlockTillCancelled = 1 << 0,
				OptionalCancel     = 1 << 1,
				_bitops_allowed
			};
			struct Params :DlgParams
			{
				Params() { name("progress_dlg").templ(Templ()); }
			};

			ProgressDlg(pr::gui::Params const& p = Params())
				:Form(p)
				,m_state(nullptr, L"", L"", 0.0f)
				,m_done(false)
				,m_cancel(false)
				,m_result(EDialogResult::Ok)
				,m_exception()
				,m_shown()
				,m_lbl_desc(Label::Params().name("desc").id(IDC_TEXT_DESC).parent(this).anchor(EAnchor::All))
				,m_bar(ProgressBar::Params().name("bar").id(IDC_PROGRESS_BAR).parent(this).anchor(EAnchor::LeftTopRight))
				,m_btn(Button::Params().name("cancel").id(IDCANCEL).parent(this).anchor(EAnchor::Bottom))
				,m_mutex()
				,m_cv()
				,m_worker()
			{
				m_btn.Click += [&](Button&,EmptyArgs const&)
				{
					if (Cancel(OptionalCancel|BlockTillCancelled))
						Close();
				};
			}

			// Construct the dialog starting the worker thread immediately.
			// 'func' should have 'ProgressDlg*' as the first parameter
			template <typename Func, typename... Args>
			ProgressDlg(wchar_t const* title, wchar_t const* desc, Func&& func, Args&&... args) :ProgressDlg()
			{
				StartWorker(title, desc, std::forward<Func>(func), std::forward<Args>(args)...);
			}
			~ProgressDlg()
			{
				Close();
			}

			// Execute a work function in a different thread while displaying a non-modal dialog.
			// 'func' should have 'ProgressDlg*' as the first parameter
			template <typename Func, typename... Args>
			void Show(wchar_t const* title, wchar_t const* desc, Func&& func, Args&&... args)
			{
				StartWorker(title, desc, std::forward<Func>(func), std::forward<Args>(args)...);
				Show(SW_SHOW); // Remember to call Create() first
			}

			// Execute a work function in a different thread while displaying the modal dialog
			// Returns IDOK if the dialog completed, IDCANCEL if the operation was cancelled
			EDialogResult ShowDialog(WndRef parent = nullptr, int delay_ms = 0)
			{
				// Wait for up to 'delay_ms' in case no dialog is needed
				auto done = false;
				{
					Lock lock(m_mutex);
					done = m_cv.wait_for(lock, std::chrono::milliseconds(delay_ms), [&]{ return m_done; });
				}

				// If not done yet, show the dialog
				if (!done)
					ShowDialog(parent, nullptr);

				// Ensure the thread has ended
				BlockTillWorkerDone();

				// Return the result
				if (m_result == EDialogResult::Abort)
					std::rethrow_exception(m_exception);

				return m_result;
			}

			// Called by the worker thread to update the UI or by callers to set the progress state
			bool Progress(float pc = -1.0f, wchar_t const* desc = nullptr, wchar_t const* title = nullptr)
			{
				// Try to lock to update the state, skip if can't lock
				Lock lock(m_mutex, std::chrono::milliseconds::zero());
				if (!lock.owns_lock())
					return true;

				// Update the state
				m_state = State(m_state.m_hwnd, title, desc, pc);

				// If the owner window is visible, send progress update
				if (m_state.m_hwnd)
					::PostMessageW(m_state.m_hwnd, WM_PROGRESS_UPDATE, 0, 0);

				return !m_cancel;
			}

			// Cancel the background thread, with optional cancel-the-cancel event
			bool Cancel(ECancelFlags flags = NonBlocking)
			{
				// Query to cancel the 'cancel'
				if (flags & ECancelFlags::OptionalCancel)
				{
					CancelEventArgs args;
					OnCancelling(args);
					if (args.m_cancel)
						return false;
				}

				// Cancelling was not cancelled, so cancel...
				{
					Lock lock(m_mutex);
					m_cancel = true;
					m_cv.notify_all();
				}

				// Wait till the thread exits
				if (flags & ECancelFlags::BlockTillCancelled)
					BlockTillWorkerDone();

				return true;
			}

			// Close the form, cancelling the worker thread if necessary
			bool Close()
			{
				// Don't close the window until the task has exited
				Cancel(BlockTillCancelled);
				if (m_hwnd) ::KillTimer(m_hwnd, ID_POLL_WORKER_COMPLETE);
				return Close(int(m_result));
			}

			// An event raised when the cancel button is hit
			// Handlers can opt to not cancel.
			EventHandler<ProgressDlg&, CancelEventArgs&> Cancelling;

		protected:

			// Handlers
			virtual void OnCancelling(CancelEventArgs& args)
			{
				Cancelling(*this, args);
			}

			// Start the worker thread running
			template <typename Func, typename... Args>
			void StartWorker(wchar_t const* title, wchar_t const* desc, Func&& func, Args&&... args)
			{
				// Stop first ... if needed
				Cancel(BlockTillCancelled);

				// Reset flags
				m_done = false;
				m_cancel = false;
				m_state = State(m_hwnd, title, desc);

				// Start a timer to poll for the thread being complete
				if (m_hwnd)
					::SetTimer(m_hwnd, ID_POLL_WORKER_COMPLETE, 100, nullptr);

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

			// Blocks until the worker thread exits
			void BlockTillWorkerDone()
			{
				if (m_worker.joinable())
					m_worker.join();
			}

			// Hide the inherited Show()/ShowDialog()
			void Show(int show) override
			{
				return Form::Show(show);
			}
			EDialogResult ShowDialog(WndRef parent, void* init_param) override
			{
				return Form::ShowDialog(parent, init_param);
			}
			bool Close(int exit_code) override
			{
				return Form::Close(exit_code);
			}
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

