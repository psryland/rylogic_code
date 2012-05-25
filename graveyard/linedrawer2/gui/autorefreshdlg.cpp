// AutoRefreshDlg.cpp : implementation file
//

#include "Stdafx.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/AutoRefreshDlg.h"


// AutoRefreshDlg dialog
IMPLEMENT_DYNAMIC(AutoRefreshDlg, CDialog)
AutoRefreshDlg::AutoRefreshDlg(CWnd* pParent)
:CDialog(AutoRefreshDlg::IDD, pParent)
,m_refresh_period(0)
,m_auto_recentre(FALSE)
{}

AutoRefreshDlg::~AutoRefreshDlg()
{}

void AutoRefreshDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX, IDC_EDIT_REFRESH_FREQUENCY, m_refresh_period);
	DDV_MinMaxDWord(pDX, m_refresh_period, 0, 1000000);
	DDX_Check(pDX, IDC_CHECK_AUTORECENTRE, m_auto_recentre);
}


BEGIN_MESSAGE_MAP(AutoRefreshDlg, CDialog)
END_MESSAGE_MAP()
