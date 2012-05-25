// aboutdlg.cpp : implementation of the CAboutDlg class
//
/////////////////////////////////////////////////////////////////////////////

#include "clicket/stdafx.h"
#include "clicket/resource.h"
#include "clicket/aboutdlg.h"

LRESULT CAboutDlg::OnInitDialog(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& /*bHandled*/)
{
	CenterWindow(GetParent());
	return TRUE;
}
LRESULT CAboutDlg::OnCloseCmd(WORD /*wNotifyCode*/, WORD wID, HWND /*hWndCtl*/, BOOL& /*bHandled*/)
{
	EndDialog(wID);
	return 0;
}

LRESULT CInfoDlg::OnInitDialog(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& /*bHandled*/)
{
	CenterWindow(GetParent());
	CRect rect; GetClientRect(&rect);
	GetDlgItem(IDC_EDIT_WINDOW_INFO).MoveWindow(&rect);
	DoDataExchange(FALSE);
	return TRUE;
}
LRESULT CInfoDlg::OnCloseCmd(WORD /*wNotifyCode*/, WORD /*wID*/, HWND /*hWndCtl*/, BOOL& /*bHandled*/)
{
	ShowWindow(SW_HIDE);
	return 0;
}
LRESULT CInfoDlg::OnSize(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& /*bHandled*/)
{
	CRect rect; GetClientRect(&rect);
	GetDlgItem(IDC_EDIT_WINDOW_INFO).MoveWindow(&rect);
	return 0;
}
void CInfoDlg::Show(bool show, int X, int Y)
{
	if (show)
	{
		DoDataExchange(FALSE);
		ShowWindow(SW_SHOW);
		SetWindowPos(GetParent(), X, Y, 0, 0, SWP_NOSIZE);
	}
	else
	{
		ShowWindow(SW_HIDE);
	}
}
