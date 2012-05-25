#pragma once
#include "afxwin.h"
#include <string>

// SettingsDlg dialog

class SettingsDlg : public CDialog
{
	DECLARE_DYNAMIC(SettingsDlg)

public:
	SettingsDlg(CWnd* pParent = NULL);   // standard constructor
	virtual ~SettingsDlg();

// Dialog Data
	enum { IDD = IDD_DIALOG1 };

	afx_msg void OnBnClickedButtonBrowse();
	afx_msg void OnBnClickedButtonBrowseSource();

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()

public:
	std::string m_viewer;
	std::string m_file_types;
	std::string m_source;
	bool		m_recursive;
};
