//*********************************************
// Line Drawer
//	(C)opyright Rylogic Limited 2007
//*********************************************
#pragma once
#include "afxwin.h"
#include "LineDrawer/Resource.h"

// CCoordinatesDlg dialog
class CCoordinatesDlg : public CDialog
{
	DECLARE_DYNAMIC(CCoordinatesDlg)
public:
	CCoordinatesDlg(CWnd* pParent = NULL);   // standard constructor
	virtual ~CCoordinatesDlg();

	enum { IDD = IDD_COORDINATES };
	CEdit m_mouse;
	CEdit m_focus;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()
};
