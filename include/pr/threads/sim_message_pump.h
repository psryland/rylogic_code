//*****************************************************************************************
// Simulation message pump
//  Copyright (c) Rylogic Limited 2009
//*****************************************************************************************
// Usage:
// The app's main gui class should inherit 'pr::threads::SimMessagePump'
// rather than 'WTL::CMessageLoop' and then 'RunSim()' should be called
// rather than 'Run()'. Any process that requires periodic calling should
// inherit 'pr::threads::Loop'
//
#ifndef PR_THREADS_SIM_MESSAGE_PUMP_H
#define PR_THREADS_SIM_MESSAGE_PUMP_H
#pragma once

#include <algorithm>
#include <atlapp.h>
#include <atlcrack.h>

namespace pr
{
	namespace threads
	{
		// A loop represents a process that should be run at a given frame rate
		struct Loop
		{
			DWORD m_rate_ms;
			long  m_tick_accum;

			Loop()
			:m_rate_ms(1000/60)
			,m_tick_accum(0)
			{}

			// The step function for the loop
			virtual void Step(DWORD elasped_ms) = 0;
		};

		// Predicate function for sorting loops by the next to be serviced
		inline bool NextToStep(Loop const* lhs, Loop const* rhs)
		{
			return (lhs->m_rate_ms - lhs->m_tick_accum) < (rhs->m_rate_ms - rhs->m_tick_accum);
		}

		// A message loop designed for simulation applications
		// This loop sleeps the thread until the next frame is due or until messages arrive.
		// To handle modal dialogs or TrackPopupMenu(), trap the WM_ENTERIDLE message (see below).
		class SimMessagePump :public WTL::CMessageLoop
		{
			typedef std::vector<Loop*> LoopCont;
			enum { TimerId = 5283 };

			LoopCont m_loop;             // A priority queue of loops. The loop at position 0 is the next to be stepped
			DWORD    m_last;             // The last recorded tick count
			int      m_max_loop_steps;   // The maximum number of loops to step before checking for messages
			HWND     m_hwnd;             // A hidden window that we can post messages to
			bool     m_pumping;          // True while the message pump is pumping

			// Call 'Step' on all loops that are pending
			// Returns the time in milliseconds until the next loop needs to be stepped
			DWORD StepLoops()
			{
				if (m_loop.empty())
					return INFINITE;

				// Add the elapsed time to the accumulators
				DWORD now = GetTickCount(), dt = now - m_last; m_last = now;
				for (LoopCont::iterator i = m_loop.begin(), iend = m_loop.end(); i != iend; ++i)
					(*i)->m_tick_accum += dt;

				// Step all loops that are pending
				for (int i = 0; i != m_max_loop_steps; ++i)
				{
					// Sort by step order
					std::sort(m_loop.begin(), m_loop.end(), NextToStep);

					Loop& loop = *m_loop[0];
					if (loop.m_tick_accum < (long)loop.m_rate_ms)
						return loop.m_rate_ms - loop.m_tick_accum; // Time till 'loop' needs to be stepped

					loop.m_tick_accum -= loop.m_rate_ms;
					loop.Step(loop.m_rate_ms);
				}

				// If we get here, the loops are taking too long. Return a timeout of 0 to indicate
				// loops still need stepping. This allows the message queue still to be processed though.
				OutputDebugStringA("SimMessagePump: WARNING - loops are staving the message queue\n");
				return 0;
			}

		public:
			SimMessagePump(int max_loop_steps = 10)
			:m_loop()
			,m_last(GetTickCount())
			,m_max_loop_steps(max_loop_steps)
			,m_hwnd(0)
			,m_pumping(false)
			{
				// WndProc for the hidden window that handles WM_TIMER messages
				struct CB {
				static LRESULT CALLBACK ModalIdleWndProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
				{
					SimMessagePump* This = reinterpret_cast<SimMessagePump*>(wparam);
					if (msg == WM_TIMER)
					{
						DWORD timeout = This->StepLoops();
						if (!This->m_pumping) SetTimer(This->m_hwnd, TimerId, timeout, 0);
					}
					return DefWindowProc(hwnd, msg, wparam, lparam);
				}};

				// Create a hidden window to post idle timer timeout messages to
				WNDCLASSW wc = {sizeof(WNDCLASSW)};
				wc.hInstance = ::GetModuleHandleW(0);
				wc.lpfnWndProc = CB::ModalIdleWndProc;
				wc.lpszClassName = L"SimMessageLoop_hwnd";
				::RegisterClassW(&wc);
				m_hwnd = ::CreateWindowW(L"SimMessageLoop_hwnd", L"SimMessageLoop_hwnd", 0, 0, 0, 0, 0, 0, 0, ::GetModuleHandleW(0), 0);
			}

			// Add a loop to be stepped by this simulation message pump
			void AddLoop(Loop* loop)
			{
				m_loop.push_back(loop);
			}

			// Run the thread message pump while maintaining the desired loop rates
			int RunSim()
			{
				for (;; m_pumping = true)
				{
					// Step any pending loops and get the time till the next loop to be stepped.
					DWORD timeout = StepLoops();

					// Check for messages and pump any received
					::MsgWaitForMultipleObjects(0, 0, TRUE, timeout, QS_ALLPOSTMESSAGE|QS_ALLINPUT|QS_ALLEVENTS);
					for (;::PeekMessage(&m_msg, 0, 0, 0, PM_REMOVE);)
					{
						if (m_msg.message == WM_QUIT) return int(m_msg.wParam);
						if (!PreTranslateMessage(&m_msg))
						{
							::TranslateMessage(&m_msg);
							::DispatchMessage(&m_msg);
						}
					}
				}
			}

			// When the standard DialogBox and TrackPopupMenu modal message loops go idle, they send
			// their parent window a WM_ENTERIDLE message. The parent window should trap this message
			// and call 'OnModalLoopIdle' to step pending loops and start a timer for future steps.
			void OnModalLoopIdle(UINT, CWindow)
			{
				m_pumping = false;
				DWORD timeout = StepLoops();
				SetTimer(m_hwnd, TimerId, timeout, 0);
			}
		};
	}
}

#endif
