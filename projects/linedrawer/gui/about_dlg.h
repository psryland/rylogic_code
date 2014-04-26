//***************************************************************************************************
// Lighting Dialog
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#pragma once
#ifndef PR_LDR_ABOUTDLG_H
#define PR_LDR_ABOUTDLG_H

#include "linedrawer/main/forward.h"
#include "linedrawer/resources/linedrawer.res.h"

class CAboutLineDrawer :public CDialogImpl<CAboutLineDrawer>
{
	CEdit m_info;
public:
	enum { IDD = IDD_DIALOG_ABOUTBOX };
	BEGIN_MSG_MAP(CAboutLineDrawer)
		MESSAGE_HANDLER(WM_INITDIALOG ,OnInitDialog)
		COMMAND_ID_HANDLER(IDOK       ,OnCloseDialog)
		COMMAND_ID_HANDLER(IDCANCEL   ,OnCloseDialog)
	END_MSG_MAP()

	// Handler methods
	LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
	{
		CenterWindow(GetParent());
		m_info.Attach(GetDlgItem(IDC_EDIT_ABOUT));
		m_info.SetWindowText(ldr::AppString());
		return S_OK;
	}
	LRESULT OnCloseDialog(WORD, WORD wID, HWND, BOOL&)
	{
		EndDialog(wID);
		return S_OK;
	}
};

#endif
