//***************************************************************************************************
// Background Task
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#ifndef PR_THREADS_BACKGROUND_TASK_H
#define PR_THREADS_BACKGROUND_TASK_H
#pragma once

#include "pr/threads/thread.h"
#include "pr/common/multi_cast.h"

namespace pr
{
	namespace threads
	{
		// A background worker thread
		// E.g:
		//  struct Job :pr::threads::BackgroundTask
		//  {
		//      int m_index;
		//      void DoWork()
		//      {
		//          for (m_index = 0; m_index != 200 && !Cancelled(); ++m_index, Sleep(50))
		//             ReportProgress(m_index, 100);
		//      }
		//  };
		// Use in conjunction with pr::gui::ProgressDlg
		class BackgroundTask :private pr::threads::Thread<BackgroundTask>
		{
			typedef pr::threads::Thread<BackgroundTask> ThreadBase;

			// Thread entry point
			void Main(void* ctx)
			{
				// Scoped object for notifying that the task has completed
				struct TaskCompleteNotify
				{
					BackgroundTask* me;
					TaskCompleteNotify(BackgroundTask* bgt) :me(bgt) {}
					~TaskCompleteNotify()
					{
						// Notify observers that the task is complete
						pr::MultiCast<IEvent*>::Lock lock(me->OnEvent);
						for (pr::MultiCast<IEvent*>::iter i = lock.begin(), iend = lock.end(); i != iend; ++i)
							(*i)->BGT_TaskComplete(me); // query the task for 'Cancelled()'
					}
				} notify_task_complete(this);

				DoWork(ctx);
			}

			// Derived types implement their task in here
			// Note: clients should catch any exceptions within this method.
			// Typically, the derived class would store a copy of any thrown
			// exception and rethrow it after calling WaitTillComplete().
			virtual void DoWork(void* ctx) = 0;

		protected:

			// Derived types call this to update their progress
			virtual void ReportProgress(int count, int total, char const* text = 0)
			{
				pr::MultiCast<IEvent*>::Lock lock(OnEvent);
				for (pr::MultiCast<IEvent*>::iter i = lock.begin(), iend = lock.end(); i != iend; ++i)
					(*i)->BGT_ReportProgress(this, count, total, text);
			}

		public:
			struct IEvent
			{
				// Note: these are called in the worker thread context
				virtual void BGT_ReportProgress(BackgroundTask* sender, int count, int total, char const* text) = 0;
				virtual void BGT_TaskComplete(BackgroundTask* sender) = 0;
				virtual ~IEvent() {}
			};
			pr::MultiCast<IEvent*> OnEvent;

			// Run the background task
			// If 'async' is true, this method returns immediately. Call 'Join()' to block until complete
			bool Run(bool async, void* ctx = 0)
			{
				if (!ThreadBase::Start(ctx)) return false;
				if (!async) Join();
				return true;
			}

			// Block until the background task is complete
			using ThreadBase::Join;

			// Allow the task to be cancelled. Note that it is still up to the
			// task to check the 'Cancelled()' method in its main loop
			using ThreadBase::IsCancelled;
			using ThreadBase::Cancel;
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
