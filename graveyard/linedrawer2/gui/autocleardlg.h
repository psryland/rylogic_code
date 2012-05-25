#ifndef AUTOCLEARDLG_H
#define AUTOCLEARDLG_H

#include "LineDrawer/Resource.h"

class AutoClearDlg : public CDialog
{
public:
	AutoClearDlg(CWnd* pParent);   // standard constructor

	// Dialog Data
	//{{AFX_DATA(AutoClearDlg)
	enum { IDD = IDD_AUTOCLEAR_DIALOG };
	float	m_period;
	//}}AFX_DATA

	// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(AutoClearDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

protected:
	// Generated message map functions
	//{{AFX_MSG(AutoClearDlg)
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

#endif//AUTOCLEARDLG_H
