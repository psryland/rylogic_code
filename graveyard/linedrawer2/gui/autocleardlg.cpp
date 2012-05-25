//*****************************************************************************
//
// Auto clear dialog
//
//*****************************************************************************
#include "Stdafx.h"
#include "AutoClearDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

BEGIN_MESSAGE_MAP(AutoClearDlg, CDialog)
	//{{AFX_MSG_MAP(AutoClearDlg)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

//*****
// AutoClearDlg dialog constructor
AutoClearDlg::AutoClearDlg(CWnd* pParent)
:CDialog(AutoClearDlg::IDD, pParent)
{
	//{{AFX_DATA_INIT(AutoClearDlg)
	m_period = 0.0f;
	//}}AFX_DATA_INIT
}

void AutoClearDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(AutoClearDlg)
	DDX_Text(pDX, IDC_EDIT_AutoClear, m_period);
	DDV_MinMaxFloat(pDX, m_period, 1.e-003f, 1000.f);
	//}}AFX_DATA_MAP
}
