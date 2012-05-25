//*********************************************
// Line Drawer
//	(C)opyright Rylogic Limited 2007
//*********************************************
#include "stdafx.h"
//#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/CoordinatesDlg.h"

// CCoordinatesDlg dialog
IMPLEMENT_DYNAMIC(CCoordinatesDlg, CDialog)

CCoordinatesDlg::CCoordinatesDlg(CWnd* pParent)
:CDialog(CCoordinatesDlg::IDD, pParent)
{}

CCoordinatesDlg::~CCoordinatesDlg()
{}

void CCoordinatesDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDIT_MOUSE_COORDS, m_mouse);
	DDX_Control(pDX, IDC_EDIT_FOCUS_COORDS, m_focus);
}


BEGIN_MESSAGE_MAP(CCoordinatesDlg, CDialog)
END_MESSAGE_MAP()

// CCoordinatesDlg message handlers
