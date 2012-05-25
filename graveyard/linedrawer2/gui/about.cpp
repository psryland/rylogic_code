//*******************************************************************************************
//
//	About box for LineDrawer
//
//*******************************************************************************************
#include "Stdafx.h"
#include "LineDrawer/GUI/About.h"

//*****
// Message map
BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
	//{{AFX_MSG_MAP(CAboutDlg)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

//*****
// Constructor
CAboutDlg::CAboutDlg()
:CDialog(CAboutDlg::IDD)
{
	//{{AFX_DATA_INIT(CAboutDlg)
	//}}AFX_DATA_INIT
}

//*****
// Data exchange
void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CAboutDlg)
	//}}AFX_DATA_MAP
}
