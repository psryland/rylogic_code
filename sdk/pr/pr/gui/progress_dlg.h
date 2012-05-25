//***************************************************************************************************
// Progress Dialog
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#pragma once
#ifndef PR_GUI_PROGRESS_DLG_H
#define PR_GUI_PROGRESS_DLG_H

#if _WIN32_WINNT < 0x0501
#error "Requires _WIN32_WINNT >= 0x0501"
#endif

#include <atlbase.h>
#include <atlapp.h>
#include <atlctrls.h>
#include <atlframe.h>
#include <atlmisc.h>
#include <atlcrack.h>
#include <atldlgs.h>
#include "pr/threads/critical_section.h"
#include "pr/threads/background_task.h"

namespace pr
{
	namespace gui
	{
		// Self contained progress dialog and background worker
		// Not resource files, etc, needed
		// Use:
		//  struct Job :pr::threads::BackgroundTask
		//  {
		//     void DoWork() { ReportProgress(); }
		//  } job;
		//  pr::gui::ProgressDlg prog;
		//  int res = prog.DoModal("Counting stuff", job, ::GetConsoleWindow());
		//  printf("job %s\n", res == IDOK ? "completed" : res == IDCANCEL ? "cancelled" : "unknown");
		class ProgressDlg
			:public CIndirectDialogImpl<ProgressDlg>
			,pr::threads::BackgroundTask::IEvent
		{
			typedef CIndirectDialogImpl<ProgressDlg> DlgBase;
			char const*                  m_title; // The title bar text
			pr::threads::BackgroundTask* m_task;  // The task to run
			WTL::CStatic                 m_desc;  // The progress description
			WTL::CProgressBarCtrl        m_bar;   // The progress bar
			WTL::CButton                 m_btn;   // The cancel button
			struct { int m_count, m_total; char const* m_desc; } m_state; // The progress state
			pr::threads::CritSection     m_cs;
			
			// BackgroundTask callbacks
			void BGT_ReportProgress(pr::threads::BackgroundTask*, int count, int total, char const* desc)
			{
				pr::threads::CSLock lock(m_cs);
				m_state.m_count = count;
				m_state.m_total = total;
				m_state.m_desc  = desc;
				PostMessage(WM_PROGRESS_UPDATE);
			}
			void BGT_TaskComplete(pr::threads::BackgroundTask* task)
			{
				PostMessage(WM_COMMAND, task->Cancelled() ? IDABORT : IDOK, 0);
			}
			
			// Hide the standard 'DoModal()' from the interface
			INT_PTR DoModal(HWND, LPARAM) {}
			
		public:
			// Optional event handler
			struct IEvents
			{
				// Called when 'Cancel' is pressed. Return true to confirm the cancel
				virtual bool ProgressDlg_OnCancelled() = 0;
				virtual ~IEvents() {}
			} *OnEvent;
			
			// Example Task
			struct ExampleTask :pr::threads::BackgroundTask
			{
				void DoWork()
				{
					for (int i = 0; i != 100 && !Cancelled(); ++i,Sleep(100))
						ReportProgress(i,100,"example");
				}
			};
			
			ProgressDlg(IEvents* handler = 0)
			:m_title()
			,m_task()
			,m_desc()
			,m_bar()
			,m_btn()
			,m_cs()
			,OnEvent(handler)
			{}
			
			// Execute a work function in a different thread while displaying the modal dialog
			// Returns IDOK if the dialog completed, IDCANCEL if the operation was cancelled
			INT_PTR DoModal(char const* title, pr::threads::BackgroundTask& task, HWND hWndParent = ::GetActiveWindow())
			{
				m_title = title;
				m_task = &task;
				return DlgBase::DoModal(hWndParent);
			}
			
			// Update the progress.
			// If 'count' is within [0,total] then the progress bar displays as normal
			// If not, then the progress bar is set to marquee mode
			void Update(int count, int total, char const* text)
			{
				// The window is initially created "hidden" (actually zero sized)
				// On the first call to update, make the window the correct size and style
				CRect rect; GetWindowRect(&rect);
				if (rect.Width()*rect.Height() == 0)
				{
					SetWindowLongPtr(GWL_STYLE, WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_SYSMENU|WS_VISIBLE);
					MoveWindow(rect.left, rect.top, DefW, DefH);
					CenterWindow(GetParent());
				}
				if (text) m_desc.SetWindowTextA(text);
				if (total == 0) return; // can use 'total' to set the initial progress text
				LONG_PTR bar_style = m_bar.GetWindowLongPtr(GWL_STYLE);
				if (count < 0 || count > total)
				{
					if ((bar_style & PBS_MARQUEE) == 0)
					{
						m_bar.SetWindowLongPtr(GWL_STYLE, bar_style|PBS_MARQUEE);
						m_bar.SetMarquee(TRUE, 30);
					}
				}
				else
				{
					if ((bar_style & PBS_MARQUEE) != 0)
					{
						m_bar.SetWindowLongPtr(GWL_STYLE, bar_style&~PBS_MARQUEE);
						m_bar.SetMarquee(FALSE);
					}
					m_bar.SetRange32(0, total);
					m_bar.SetPos(count);
				}
			}
			
			enum { IDC_TEXT_DESC=1000, IDC_PROGRESS_BAR, WM_PROGRESS_UPDATE=WM_USER+1, DefW=480, DefH=180};
			BEGIN_DIALOG_EX(0,0,0,0,0)
				DIALOG_STYLE(WS_POPUP)
				DIALOG_FONT(8, TEXT("MS Shell Dlg"))
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_LTEXT("Processing...", IDC_TEXT_DESC, 0, 0, 0, 0, SS_LEFT, WS_EX_STATICEDGE)
				CONTROL_CONTROL("", IDC_PROGRESS_BAR, PROGRESS_CLASS, 0, 0, 0, 0, 0, 0)
				CONTROL_DEFPUSHBUTTON("Cancel", IDCANCEL, 0, 0, 0, 0, 0, 0)
			END_CONTROLS_MAP()
			BEGIN_MSG_MAP(ProgressDlg)
				MSG_WM_INITDIALOG(OnInitDialog)
				MSG_WM_DESTROY(OnDestroy)
				MSG_WM_SIZE(OnSize)
				MESSAGE_HANDLER_EX(WM_PROGRESS_UPDATE, OnProgressUpdate)
				COMMAND_ID_HANDLER_EX(IDCANCEL ,OnCancel)
				COMMAND_ID_HANDLER_EX(IDABORT  ,OnDone)
				COMMAND_ID_HANDLER_EX(IDOK     ,OnDone)
			END_MSG_MAP()
		
		private:
			// Handlers
			BOOL OnInitDialog(CWindow, LPARAM)
			{
				SetWindowText(m_title);
				m_desc.Attach(GetDlgItem(IDC_TEXT_DESC));
				m_bar .Attach(GetDlgItem(IDC_PROGRESS_BAR));
				m_btn .Attach(GetDlgItem(IDCANCEL));
				m_task->OnEvent += this;
				m_task->Run(true);
				return TRUE;
			}
			void OnDestroy()
			{
				m_task->Join(); // Don't close the window until the task has exited
				m_task->OnEvent -= this;
			}
			void OnSize(UINT nType, CSize)
			{
				const int btn_w = 80, btn_h = 24, prog_h = 18, bp = (btn_h-prog_h)/2, sp = 2, bdr = 5;
				struct L {
					static CRect& Clamp(CRect& rect)
					{
						if (rect.right  < rect.left) rect.right  = rect.left;
						if (rect.bottom < rect.top ) rect.bottom = rect.top;
						return rect;
					}};
				if (nType == SIZE_MINIMIZED) return;
				CRect r, client; GetClientRect(&client); client.DeflateRect(bdr,bdr,bdr,bdr);
				r = client; r.bottom -= (btn_h + sp);
				m_desc.MoveWindow(L::Clamp(r));
				r = client; r.bottom -= bp; r.top = r.bottom - prog_h; r.right -= btn_w + sp;
				m_bar.MoveWindow(L::Clamp(r));
				r = client; r.top = r.bottom - btn_h; r.left = r.right - btn_w;
				m_btn.MoveWindow(L::Clamp(r));
			}
			LRESULT OnProgressUpdate(UINT uMsg, WPARAM, LPARAM)
			{
				if (uMsg != WM_PROGRESS_UPDATE) { SetMsgHandled(FALSE); return S_OK; }
				pr::threads::CSLock lock(m_cs);
				Update(m_state.m_count, m_state.m_total, m_state.m_desc);
				return S_OK;
			}
			void OnCancel(UINT uNotifyCode, int, CWindow wndCtl)
			{
				if (OnEvent && !OnEvent->ProgressDlg_OnCancelled()) return;
				m_task->Cancel();
				OnDone(uNotifyCode, IDABORT, wndCtl);
			}
			void OnDone(UINT, int nID, CWindow)
			{
				EndDialog(nID == IDOK ? IDOK : IDCANCEL);
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

#endif
