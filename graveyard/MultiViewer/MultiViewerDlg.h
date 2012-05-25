// MultiViewerDlg.h : header file
//

#pragma once
#include "afxwin.h"
#include "Common/Process.h"
#include "Common/StdVector.h"

// CMultiViewerDlg dialog
class CMultiViewerDlg : public CDialog
{
// Construction
public:
	CMultiViewerDlg(CWnd* pParent = NULL);	// standard constructor

	// Dialog Data
	enum { IDD = IDD_MULTIVIEWER_DIALOG };

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnDestroy();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnBnClickedButtonSettings();
	afx_msg void OnBnClickedButtonLeft();
	afx_msg void OnBnClickedButtonRight();
	DECLARE_MESSAGE_MAP()

protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	void BuildListOfFilesToView(const char* path);
	void ViewFile();
	bool IsViewable(const char* extension);

// Implementation
protected:
	HICON	m_hIcon;
	CButton m_left_button;
	CBitmap m_left_bitmap;
	CButton m_right_button;
	CBitmap m_right_bitmap;
	CButton m_settings_button;
	CBitmap m_settings_bitmap;
	std::string		m_source;
	std::string		m_viewer;
	std::string		m_file_types;
	bool			m_recursive;

	std::vector<std::string>	m_files;
	int							m_current;
	PR::Process					m_process;
};
