// About.h : header file
//
#pragma once

// CAboutDlg dialog used for App About
class CAboutDlg : public CDialog
{
public:
	enum { IDD = IDD_ABOUTBOX };
	CAboutDlg() : CDialog(CAboutDlg::IDD) {}

protected:
	virtual void DoDataExchange(CDataExchange* pDX)	{ CDialog::DoDataExchange(pDX); }

protected:
	DECLARE_MESSAGE_MAP()
};
