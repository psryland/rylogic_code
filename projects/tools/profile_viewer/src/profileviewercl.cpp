//**************************************************
// Profile Viewer
//  Copyright (c) Rylogic Ltd 2007
//**************************************************
// A console based profile manager

#include "Stdafx.h"
#include "ProfileViewerCL/ProfileDatabase.h"

struct ProfileViewer
{
	pr::Pipe<65535>		m_pipe;
	ProfileDatabase		m_db;
	std::vector<short>	m_y_stack;

	ProfileViewer()
	:m_pipe(_T("PRProfileStream"), ProfileViewer::OnRecv, this)
	{
		PushY(0);
		m_pipe.SpawnListenThread();
	}

	short GetOutputY() const	{ return m_y_stack.back(); }
	void PushY(short y)			{ m_y_stack.push_back(y); m_db.Output(GetOutputY()); }
	void PopY()					{ m_y_stack.pop_back();   m_db.Output(GetOutputY()); }

	// Main loop
	int DoModal()
	{
		for(;;)
		{
											PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
			cons().WaitKey();				PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
			if( !MainMenu() ) break;
		}
		return 0;
	}
	bool MainMenu()
	{
											PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		PushY(8);
		cons().Clear(0, 0, 0, 8);			PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		cons().Write(0, 0,
			"Main Menu:\n"
			"   (P)rofile options\n"
			"   (G)raph options\n"
			"   (T)able options\n"
			"   (F)requency\n"
			"   (Q)uit\n");					PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		cons().WaitKey();					PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		char ch = cons().GetChar();			PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		switch( ch )
		{
		case 'p': ProfileMenu();	break;
		case 'g': GraphMenu();		break;
		case 't': TableMenu();		break;
		case 'f': FrequencyMenu();	break;
		case 'q': PopY(); return false;
		}
		PopY();
		return true;
	}
	void ProfileMenu()
	{}
	void GraphMenu()
	{}
	void TableMenu()
	{
											PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		PushY(4);							PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		cons().Clear(0, 0, 0, 4);			PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		cons().Write(0, 0,
			"Table Menu:\n"
			"   (S)ort by\n"
			"   (U)nits\n"
			"   (Esc) back\n");				PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		cons().WaitKey();					PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		char ch = cons().GetChar();			PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		switch( ch )
		{
		case 's': SortByMenu(); break;
		case 'u': UnitsMenu(); break;
		}
		PopY();
	}
	void FrequencyMenu()
	{}
	void SortByMenu()
	{
		short base_y = GetOutputY();
		PushY(base_y + 3);
		cons().Clear(0, base_y, 0, 3);
		cons().Write(0, base_y,
			"Sort by: (0)Name, (1)Call Count, (2)Incl Time, (3)Excl Time\n"
			"(Esc) back\n"
			">");
		cons().WaitKey();						PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		char ch = cons().GetChar();				PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		if( ch >= '0' && ch <= '3' )
		{
			m_db.m_sort_by = static_cast<ESortBy>(ch - '0');
			m_db.m_sort_needed = true;
		}
		PopY();
	}
	void UnitsMenu()
	{
		short base_y = GetOutputY();
		PushY(base_y + 3);
		cons().Clear(0, base_y, 0, 3);
		cons().Write(0, base_y,
			"Units: (0)ms, (1)% of frame, (2)% of 60hz frame\n"
			"(Esc) back\n"
			">");
		cons().WaitKey();						PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		char ch = cons().GetChar();				PR_INFO(1, FmtS("Number of input events: %d\n", cons().GetInputEventCount()));
		if( ch >= '0' && ch <= '2' )
		{
			m_db.m_units = static_cast<EUnits>(ch - '0');
		}
		PopY();
	}
	// Incoming profile data from the pipe
	static void OnRecv(void const* data, std::size_t data_size, bool partial, void* user_data)
	{
		PR_ASSERT(PR_DBG, !partial, "");

		// Warning: this is happening in the pipe thread context
		ProfileViewer* This = static_cast<ProfileViewer*>(user_data);
		This->m_db.Update(data, data_size, partial);
		This->m_db.Output();
	}
};

int _tmain(int, _TCHAR*)
{
	ProfileViewer viewer;
	return viewer.DoModal();
}


