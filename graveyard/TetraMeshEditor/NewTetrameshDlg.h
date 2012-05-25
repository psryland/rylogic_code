#pragma once

// NewTetrameshDlg dialog
class NewTetrameshDlg : public CDialog
{
	DECLARE_DYNAMIC(NewTetrameshDlg)
public:
	enum { IDD = IDD_DIALOG_NEW };
	NewTetrameshDlg(CWnd* pParent = NULL);
	virtual ~NewTetrameshDlg();

	BOOL	m_single;
	int		m_dimX;
	int		m_dimY;
	int		m_dimZ;
	float	m_sizeX;
	float	m_sizeY;
	float	m_sizeZ;
	
protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()
	virtual BOOL OnInitDialog();
	afx_msg void OnRadioSingle();
	afx_msg void OnRadioGrid();
};
