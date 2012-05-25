// AddObjectDlg.cpp : implementation file
//

#include "Stdafx.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/AddObjectDlg.h"

extern int ButtonWidth;
extern int ButtonHeight;
extern int TopAlign;
extern int LeftAlign;
extern int RightAlign;
extern int BottomAlign;
extern int ButtonSpace;
extern int ExpandButtonSize;
extern int SplitterWidth;

BEGIN_MESSAGE_MAP(AddObjectDlg, CDialog)
	ON_WM_GETMINMAXINFO()
	ON_WM_SIZE()
END_MESSAGE_MAP()

//*****
// AddObjectDlg dialog
IMPLEMENT_DYNAMIC(AddObjectDlg, CDialog)
AddObjectDlg::AddObjectDlg(CWnd* pParent, const char* window_title)
:CDialog(AddObjectDlg::IDD, pParent)
,m_object_string(_T(""))
,m_window_title(window_title)
{}

AddObjectDlg::~AddObjectDlg()
{}

void AddObjectDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX, IDC_EDIT_ADD_OBJECT, m_object_string);
}

BOOL AddObjectDlg::OnInitDialog()
{
	SetWindowText(m_window_title.GetString());
	UpdateData(FALSE);
	PostMessage(WM_SIZE);
	return TRUE;
}

void AddObjectDlg::OnGetMinMaxInfo(MINMAXINFO* lpMMI)
{
	lpMMI->ptMinTrackSize.x = LeftAlign + 2*(ButtonWidth  + ButtonSpace) + RightAlign;
	lpMMI->ptMinTrackSize.y = TopAlign  + BottomAlign + 3*ButtonHeight;
	CDialog::OnGetMinMaxInfo(lpMMI);
}

void AddObjectDlg::OnSize(UINT nType, int cx, int cy)
{
	CDialog::OnSize(nType, cx, cy);

	// Get the new window size
	CWnd* wnd;
	CRect rect, wndrect;
	GetClientRect(&rect);
	rect.left	+= LeftAlign;
	rect.right	-= RightAlign;
	rect.top	+= TopAlign;
	rect.bottom	-= BottomAlign;

	// Move the 'Ok' button
	wndrect.left	= rect.right  - ButtonWidth;
	wndrect.top		= rect.bottom - ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDOK);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Cancel' button
	wndrect.left	= rect.right  - (ButtonWidth + ButtonSpace) * 2;
	wndrect.top		= rect.bottom - ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDCANCEL);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Edit box'
	wndrect.left	= rect.left;
	wndrect.top		= rect.top;
	wndrect.right	= rect.right;
	wndrect.bottom	= rect.bottom - ButtonHeight - BottomAlign;
	wnd = GetDlgItem(IDC_EDIT_ADD_OBJECT);
	if( wnd ) wnd->MoveWindow(wndrect);

	Invalidate();
}
