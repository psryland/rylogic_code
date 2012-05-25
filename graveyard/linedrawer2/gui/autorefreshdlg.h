#pragma once

// AutoRefreshDlg dialog
class AutoRefreshDlg : public CDialog
{
	DECLARE_DYNAMIC(AutoRefreshDlg)
public:
	enum { IDD = IDD_AUTOREFRESH };
	AutoRefreshDlg(CWnd* pParent);
	virtual ~AutoRefreshDlg();

	DWORD m_refresh_period;
	BOOL m_auto_recentre;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()
};
