//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#ifndef IMAGERN_TIMER_H
#define IMAGERN_TIMER_H
#pragma once

#include "imagern/main/forward.h"
#include "pr/threads/item_queue.h"

namespace ETimerCmd
{
	enum Type
	{
		// The video control has been displayed and we need to fade it out
		VideoCtrlCoolDown,
	};
}

// A message sent to the timer thread to set and stop various features
struct Event_TimerMsg
{
	ETimerCmd::Type m_cmd;
	Event_TimerMsg(ETimerCmd::Type cmd) m_cmd(cmd) {}
};

// A thread that drives things running on a timer in the main thread
class Timer
	:pr::threads::Thread<Timer>
	,pr::events::IRecv<Event_TimerMsg,true>
{
	pr::threads::ItemQueue<Event_TimerMsg> m_msgs;

public:
	Timer()
	:m_msgs()
	{
		Start();
	}
	~Timer()
	{
		Cancel();
		m_msgs.Signal();
		Stop();
	}
	
	// The timer thread that drives the main app
	void Main()
	{
		while (!Cancelled() && m_msgs.ItemsPending())
		{
			Event_TimerMsg msg;
			if (!m_msgs.Dequeue(msg)) { m_msgs.Wait(); continue; }
			
			// Respond to the message
			switch (msg.m_cmd)
			{
			default: break;
			case ETimerCmd::VideoCtrlCoolDown:
				break;
			}
		}
	}
	
	// A message from somewhere in the program to start/stop timer related behaviour
	void OnEvent(Event_TimerMsg const&)
	{
		// Careful, this can be called from any thread
	}
};

#endif
