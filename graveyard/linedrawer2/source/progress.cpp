// Progress.cpp : implementation file
//

#include "stdafx.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/Progress.h"


// Progress dialog
IMPLEMENT_DYNAMIC(Progress, CDialog)
Progress::Progress(CWnd* pParent)
:CDialog(Progress::IDD, pParent)
,m_continue(true)
{}

Progress::~Progress()
{}

void Progress::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_PROGRESS_BAR, m_progress);
}

BEGIN_MESSAGE_MAP(Progress, CDialog)
	ON_WM_CLOSE()
END_MESSAGE_MAP()

//*****
// Set the value in the progress bar
bool Progress::SetProgress(uint number, uint maximum, const char* caption)
{
	if( GetSafeHwnd() == 0 ) return true;
	
	// Show the window if there is any partial progress
	bool show_progress = number < maximum;
	ShowWindow((show_progress) ? (SW_SHOW) : (SW_HIDE));
	
	// Set the caption
	CWnd* wnd = GetDlgItem(IDC_STATIC_PROGRESS_DESCRIPTION);
	if( wnd ) wnd->SetWindowText(caption);

	// Set the progress bar position
	m_progress.SetRange32(0, maximum);
	m_progress.SetPos(number);

	if( !m_continue )
	{
		m_continue = true;
		ShowWindow(SW_HIDE);
		return false;
	}
	return true;
}

//*****
// Close was pressed
void Progress::OnClose()
{
	m_continue = false;
}
